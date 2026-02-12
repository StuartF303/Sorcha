// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using StackExchange.Redis;

namespace Sorcha.Validator.Service.Services;

/// <summary>
/// Redis-backed registry for tracking registers that should be monitored for docket building.
/// Persists across container restarts.
/// </summary>
public class RegisterMonitoringRegistry : IRegisterMonitoringRegistry
{
    private const string RedisKeyPrefix = "validator:monitoring:";
    private const string RedisSetKey = "validator:monitoring:registers";

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RegisterMonitoringRegistry> _logger;

    public RegisterMonitoringRegistry(
        IConnectionMultiplexer redis,
        ILogger<RegisterMonitoringRegistry> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void RegisterForMonitoring(string registerId)
    {
        if (string.IsNullOrWhiteSpace(registerId))
            throw new ArgumentException("Register ID cannot be null or whitespace", nameof(registerId));

        try
        {
            var db = _redis.GetDatabase();
            var timestampKey = $"{RedisKeyPrefix}{registerId}:timestamp";

            // Check if already registered
            if (db.SetContains(RedisSetKey, registerId))
            {
                _logger.LogDebug("Register {RegisterId} already registered for monitoring", registerId);
                return;
            }

            // Add to set and store timestamp
            db.SetAdd(RedisSetKey, registerId);
            db.StringSet(timestampKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            _logger.LogInformation("Registered {RegisterId} for docket build monitoring (persisted to Redis)", registerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register {RegisterId} for monitoring in Redis", registerId);
            throw;
        }
    }

    /// <inheritdoc />
    public void UnregisterFromMonitoring(string registerId)
    {
        try
        {
            var db = _redis.GetDatabase();
            var timestampKey = $"{RedisKeyPrefix}{registerId}:timestamp";

            if (db.SetRemove(RedisSetKey, registerId))
            {
                var timestamp = db.StringGet(timestampKey);
                db.KeyDelete(timestampKey);

                var registeredAt = timestamp.HasValue
                    ? DateTimeOffset.FromUnixTimeSeconds((long)timestamp)
                    : DateTimeOffset.MinValue;

                _logger.LogInformation(
                    "Unregistered {RegisterId} from docket build monitoring (was registered at {RegisteredAt})",
                    registerId, registeredAt);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister {RegisterId} from monitoring in Redis", registerId);
            throw;
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAll()
    {
        try
        {
            var db = _redis.GetDatabase();
            var members = db.SetMembers(RedisSetKey);
            return members.Select(m => m.ToString()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve monitored registers from Redis");
            return Enumerable.Empty<string>();
        }
    }

    /// <inheritdoc />
    public bool IsRegistered(string registerId)
    {
        try
        {
            var db = _redis.GetDatabase();
            return db.SetContains(RedisSetKey, registerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if {RegisterId} is registered in Redis", registerId);
            return false;
        }
    }
}
