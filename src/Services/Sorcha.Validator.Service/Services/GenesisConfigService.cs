// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using StackExchange.Redis;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Services.Interfaces;

namespace Sorcha.Validator.Service.Services;

/// <summary>
/// Redis-backed genesis configuration cache with optional local L1 cache.
/// Provides fast access to consensus, validator, and leader election configurations.
/// </summary>
public class GenesisConfigService : IGenesisConfigService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly GenesisConfigCacheConfiguration _config;
    private readonly ILogger<GenesisConfigService> _logger;
    private readonly ResiliencePipeline _pipeline;
    private readonly JsonSerializerOptions _jsonOptions;

    // L1 local cache
    private readonly ConcurrentDictionary<string, LocalCacheEntry> _localCache = new();

    // Version tracking for staleness checks
    private readonly ConcurrentDictionary<string, string> _versionCache = new();

    /// <inheritdoc/>
    public event EventHandler<ConfigChangedEventArgs>? ConfigChanged;

    public GenesisConfigService(
        IConnectionMultiplexer redis,
        IOptions<GenesisConfigCacheConfiguration> config,
        ILogger<GenesisConfigService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _database = _redis.GetDatabase();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _pipeline = BuildResiliencePipeline();
    }

    private ResiliencePipeline BuildResiliencePipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                MaxRetryAttempts = _config.MaxRetries,
                Delay = _config.RetryDelay,
                BackoffType = DelayBackoffType.Exponential
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 10,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(30)
            })
            .AddTimeout(TimeSpan.FromSeconds(5))
            .Build();
    }

    #region Key Generation

    private string GetConfigKey(string registerId) =>
        $"{_config.KeyPrefix}{registerId}:config";

    private string GetVersionKey(string registerId) =>
        $"{_config.KeyPrefix}{registerId}:version";

    private string GetLastCheckKey(string registerId) =>
        $"{_config.KeyPrefix}{registerId}:lastcheck";

    #endregion

    /// <inheritdoc/>
    public async Task<ConsensusConfig> GetConsensusConfigAsync(
        string registerId,
        CancellationToken ct = default)
    {
        var fullConfig = await GetFullConfigAsync(registerId, ct);
        return fullConfig.Consensus;
    }

    /// <inheritdoc/>
    public async Task<ValidatorConfig> GetValidatorConfigAsync(
        string registerId,
        CancellationToken ct = default)
    {
        var fullConfig = await GetFullConfigAsync(registerId, ct);
        return fullConfig.Validators;
    }

    /// <inheritdoc/>
    public async Task<LeaderElectionConfig> GetLeaderElectionConfigAsync(
        string registerId,
        CancellationToken ct = default)
    {
        var fullConfig = await GetFullConfigAsync(registerId, ct);
        return fullConfig.LeaderElection;
    }

    /// <inheritdoc/>
    public async Task<GenesisConfiguration> GetFullConfigAsync(
        string registerId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registerId);

        try
        {
            // Check L1 local cache first
            if (_config.EnableLocalCache && TryGetFromLocalCache(registerId, out var localConfig))
            {
                return localConfig!;
            }

            // Check L2 Redis cache
            var config = await GetFromRedisAsync(registerId, ct);

            if (config != null)
            {
                // Populate L1 cache
                if (_config.EnableLocalCache)
                {
                    SetInLocalCache(registerId, config);
                }

                return config;
            }

            // Not in cache - need to fetch from source and cache
            config = await FetchAndCacheConfigAsync(registerId, ct);

            if (config == null)
            {
                throw new InvalidOperationException(
                    $"Failed to load genesis configuration for register {registerId}");
            }

            return config;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to get genesis config for register {RegisterId}", registerId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsConfigStaleAsync(
        string registerId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registerId);

        try
        {
            // Check when we last checked for staleness
            var lastCheckKey = GetLastCheckKey(registerId);
            var lastCheckValue = await _database.StringGetAsync(lastCheckKey);

            if (!lastCheckValue.IsNullOrEmpty)
            {
                var lastCheck = DateTimeOffset.Parse(lastCheckValue.ToString());
                if (DateTimeOffset.UtcNow - lastCheck < _config.StaleCheckInterval)
                {
                    // Recently checked, not stale
                    return false;
                }
            }

            // Check if version has changed
            // In production, this would query the Register Service for the latest
            // control blueprint version and compare with cached version
            var versionKey = GetVersionKey(registerId);
            var cachedVersion = await _database.StringGetAsync(versionKey);

            if (cachedVersion.IsNullOrEmpty)
            {
                // No version cached, needs refresh
                return true;
            }

            // Update last check timestamp
            await _database.StringSetAsync(
                lastCheckKey,
                DateTimeOffset.UtcNow.ToString("O"),
                _config.StaleCheckInterval);

            // For now, assume not stale unless explicitly invalidated
            // In production, would compare with actual source version
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check staleness for register {RegisterId}", registerId);
            // Assume stale on error to trigger refresh
            return true;
        }
    }

    /// <inheritdoc/>
    public async Task RefreshConfigAsync(
        string registerId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registerId);

        _logger.LogInformation("Refreshing genesis config for register {RegisterId}", registerId);

        // Get current version for change detection
        string? previousVersionId = null;
        if (_versionCache.TryGetValue(registerId, out var cached))
        {
            previousVersionId = cached;
        }

        // Remove from L1 cache
        _localCache.TryRemove(registerId, out _);

        // Remove from L2 cache
        var configKey = GetConfigKey(registerId);
        await _database.KeyDeleteAsync(configKey);

        // Fetch fresh config
        var newConfig = await FetchAndCacheConfigAsync(registerId, ct);

        // Raise event if version changed
        if (newConfig != null && previousVersionId != null &&
            previousVersionId != newConfig.ControlBlueprintVersionId)
        {
            RaiseConfigChanged(registerId, previousVersionId, newConfig);
        }
    }

    #region Private Methods

    private async Task<GenesisConfiguration?> GetFromRedisAsync(
        string registerId,
        CancellationToken ct)
    {
        try
        {
            return await _pipeline.ExecuteAsync(async token =>
            {
                var key = GetConfigKey(registerId);
                var json = await _database.StringGetAsync(key);

                if (json.IsNullOrEmpty)
                    return null;

                return JsonSerializer.Deserialize<GenesisConfiguration>(
                    json.ToString(), _jsonOptions);
            }, ct);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning(
                "Circuit breaker open, cannot fetch genesis config {RegisterId}",
                registerId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Redis error fetching genesis config {RegisterId}",
                registerId);
            return null;
        }
    }

    private async Task<GenesisConfiguration?> FetchAndCacheConfigAsync(
        string registerId,
        CancellationToken ct)
    {
        // In production, this would:
        // 1. Query Register Service for genesis transaction
        // 2. Extract control blueprint from genesis
        // 3. Parse configuration from blueprint data

        // For now, return default configuration
        // TODO: Implement actual genesis fetch from Register Service

        _logger.LogDebug(
            "Fetching genesis config from source for register {RegisterId}",
            registerId);

        var config = CreateDefaultConfiguration(registerId);

        // Cache the configuration
        await CacheConfigAsync(config, ct);

        return config;
    }

    private async Task CacheConfigAsync(
        GenesisConfiguration config,
        CancellationToken ct)
    {
        try
        {
            await _pipeline.ExecuteAsync(async token =>
            {
                var configKey = GetConfigKey(config.RegisterId);
                var versionKey = GetVersionKey(config.RegisterId);

                var json = JsonSerializer.Serialize(config, _jsonOptions);

                // Store config and version
                var batch = _database.CreateBatch();
                _ = batch.StringSetAsync(configKey, json, config.CacheTtl);
                _ = batch.StringSetAsync(versionKey, config.ControlBlueprintVersionId, config.CacheTtl);
                batch.Execute();

                await Task.CompletedTask;
            }, ct);

            // Update version tracking
            _versionCache[config.RegisterId] = config.ControlBlueprintVersionId;

            // Update L1 cache
            if (_config.EnableLocalCache)
            {
                SetInLocalCache(config.RegisterId, config);
            }

            _logger.LogDebug(
                "Cached genesis config for register {RegisterId}, version {Version}",
                config.RegisterId, config.ControlBlueprintVersionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to cache genesis config for register {RegisterId}",
                config.RegisterId);
        }
    }

    private static GenesisConfiguration CreateDefaultConfiguration(string registerId)
    {
        return new GenesisConfiguration
        {
            RegisterId = registerId,
            GenesisTransactionId = $"genesis-{registerId}",
            ControlBlueprintVersionId = $"control-v1-{registerId}",
            Consensus = new ConsensusConfig
            {
                SignatureThresholdMin = 2,
                SignatureThresholdMax = 10,
                DocketTimeout = TimeSpan.FromSeconds(30),
                MaxSignaturesPerDocket = 100,
                MaxTransactionsPerDocket = 1000,
                DocketBuildInterval = TimeSpan.FromMilliseconds(100)
            },
            Validators = new ValidatorConfig
            {
                RegistrationMode = "public",
                MinValidators = 1,
                MaxValidators = 100,
                RequireStake = false,
                StakeAmount = null
            },
            LeaderElection = new LeaderElectionConfig
            {
                Mechanism = "rotating",
                HeartbeatInterval = TimeSpan.FromSeconds(1),
                LeaderTimeout = TimeSpan.FromSeconds(5),
                TermDuration = TimeSpan.FromMinutes(1)
            },
            LoadedAt = DateTimeOffset.UtcNow,
            CacheTtl = TimeSpan.FromMinutes(30)
        };
    }

    private bool TryGetFromLocalCache(
        string registerId,
        out GenesisConfiguration? config)
    {
        config = null;

        if (!_localCache.TryGetValue(registerId, out var entry))
            return false;

        // Check if expired
        if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _localCache.TryRemove(registerId, out _);
            return false;
        }

        config = entry.Config;
        return true;
    }

    private void SetInLocalCache(string registerId, GenesisConfiguration config)
    {
        // Enforce max entries limit
        if (_localCache.Count >= _config.LocalCacheMaxEntries)
        {
            // Remove oldest entries
            var toRemove = _localCache
                .OrderBy(kvp => kvp.Value.ExpiresAt)
                .Take(_localCache.Count - _config.LocalCacheMaxEntries + 1)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in toRemove)
            {
                _localCache.TryRemove(key, out _);
            }
        }

        var entry = new LocalCacheEntry
        {
            Config = config,
            ExpiresAt = DateTimeOffset.UtcNow.Add(_config.LocalCacheTtl)
        };

        _localCache[registerId] = entry;
    }

    private void RaiseConfigChanged(
        string registerId,
        string previousVersionId,
        GenesisConfiguration newConfig)
    {
        var changedProperties = new List<string>();

        // In production, would compare old vs new config to determine what changed
        // For now, just note that version changed
        changedProperties.Add("ControlBlueprintVersion");

        var args = new ConfigChangedEventArgs
        {
            RegisterId = registerId,
            PreviousVersionId = previousVersionId,
            NewVersionId = newConfig.ControlBlueprintVersionId,
            ChangedProperties = changedProperties
        };

        _logger.LogInformation(
            "Genesis config changed for register {RegisterId}: {Previous} -> {New}",
            registerId, previousVersionId, newConfig.ControlBlueprintVersionId);

        ConfigChanged?.Invoke(this, args);
    }

    #endregion

    #region Inner Classes

    private class LocalCacheEntry
    {
        public required GenesisConfiguration Config { get; init; }
        public required DateTimeOffset ExpiresAt { get; init; }
    }

    #endregion
}
