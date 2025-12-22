// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Sorcha.Validator.Service.Configuration;

namespace Sorcha.Validator.Service.Services;

/// <summary>
/// Background service that monitors memory pools and triggers docket building
/// based on hybrid conditions (time threshold OR size threshold)
/// </summary>
public class DocketBuildTriggerService : BackgroundService
{
    private readonly IDocketBuilder _docketBuilder;
    private readonly DocketBuildConfiguration _config;
    private readonly ILogger<DocketBuildTriggerService> _logger;

    // Track last build time per register
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastBuildTimes = new();

    // Active registers to monitor (in production, this would come from configuration/database)
    private readonly ConcurrentDictionary<string, bool> _activeRegisters = new();

    public DocketBuildTriggerService(
        IDocketBuilder docketBuilder,
        IOptions<DocketBuildConfiguration> config,
        ILogger<DocketBuildTriggerService> logger)
    {
        _docketBuilder = docketBuilder ?? throw new ArgumentNullException(nameof(docketBuilder));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Docket build trigger service starting. Time threshold: {TimeThreshold}, Size threshold: {SizeThreshold}",
            _config.TimeThreshold, _config.SizeThreshold);

        // Use time threshold as the check interval (or minimum of 1 second)
        var checkInterval = _config.TimeThreshold > TimeSpan.FromSeconds(1)
            ? _config.TimeThreshold
            : TimeSpan.FromSeconds(1);

        using var timer = new PeriodicTimer(checkInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                _logger.LogTrace("Checking docket build triggers for {RegisterCount} active registers",
                    _activeRegisters.Count);

                // Check each active register
                foreach (var registerId in _activeRegisters.Keys)
                {
                    try
                    {
                        await CheckAndBuildDocketAsync(registerId, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error checking/building docket for register {RegisterId}", registerId);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Docket build trigger service stopping");
        }
    }

    /// <summary>
    /// Checks if a register should build a docket and triggers build if needed
    /// </summary>
    private async Task CheckAndBuildDocketAsync(string registerId, CancellationToken cancellationToken)
    {
        // Get last build time (or use epoch if never built)
        var lastBuildTime = _lastBuildTimes.GetOrAdd(registerId, DateTimeOffset.UnixEpoch);

        // Check if we should build
        var shouldBuild = await _docketBuilder.ShouldBuildDocketAsync(registerId, lastBuildTime, cancellationToken);

        if (!shouldBuild)
        {
            _logger.LogTrace("Register {RegisterId} does not meet build thresholds yet", registerId);
            return;
        }

        _logger.LogInformation("Triggering docket build for register {RegisterId}", registerId);

        // Build docket
        var docket = await _docketBuilder.BuildDocketAsync(registerId, forceBuild: false, cancellationToken);

        if (docket != null)
        {
            _logger.LogInformation("Successfully built docket {DocketNumber} for register {RegisterId}",
                docket.DocketNumber, registerId);

            // Update last build time
            _lastBuildTimes[registerId] = DateTimeOffset.UtcNow;

            // TODO Phase 5: Trigger consensus process here
            // await _consensusEngine.StartConsensusAsync(docket, cancellationToken);
        }
        else
        {
            _logger.LogWarning("Failed to build docket for register {RegisterId}", registerId);
        }
    }

    /// <summary>
    /// Registers a register for docket building monitoring
    /// </summary>
    public void RegisterForMonitoring(string registerId)
    {
        if (_activeRegisters.TryAdd(registerId, true))
        {
            _logger.LogInformation("Registered {RegisterId} for docket build monitoring", registerId);
            _lastBuildTimes.TryAdd(registerId, DateTimeOffset.UtcNow);
        }
    }

    /// <summary>
    /// Unregisters a register from docket building monitoring
    /// </summary>
    public void UnregisterFromMonitoring(string registerId)
    {
        if (_activeRegisters.TryRemove(registerId, out _))
        {
            _logger.LogInformation("Unregistered {RegisterId} from docket build monitoring", registerId);
            _lastBuildTimes.TryRemove(registerId, out _);
        }
    }

    /// <summary>
    /// Manually triggers a docket build for a register
    /// </summary>
    public async Task<bool> TriggerManualBuildAsync(string registerId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Manual docket build triggered for register {RegisterId}", registerId);

        var docket = await _docketBuilder.BuildDocketAsync(registerId, forceBuild: true, cancellationToken);

        if (docket != null)
        {
            _lastBuildTimes[registerId] = DateTimeOffset.UtcNow;
            return true;
        }

        return false;
    }
}
