// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Discovery;
using System.Diagnostics;

namespace Sorcha.Peer.Service.Monitoring;

/// <summary>
/// Service for monitoring peer health and network status
/// </summary>
public class HealthMonitorService
{
    private readonly ILogger<HealthMonitorService> _logger;
    private readonly PeerServiceConfiguration _configuration;
    private readonly PeerListManager _peerListManager;
    private readonly PeerDiscoveryService _peerDiscoveryService;

    public HealthMonitorService(
        ILogger<HealthMonitorService> logger,
        IOptions<PeerServiceConfiguration> configuration,
        PeerListManager peerListManager,
        PeerDiscoveryService peerDiscoveryService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _peerListManager = peerListManager ?? throw new ArgumentNullException(nameof(peerListManager));
        _peerDiscoveryService = peerDiscoveryService ?? throw new ArgumentNullException(nameof(peerDiscoveryService));
    }

    /// <summary>
    /// Performs a health check on all known peers
    /// </summary>
    public async Task<HealthCheckResult> PerformHealthCheckAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Performing peer health check");

        var sw = Stopwatch.StartNew();
        var allPeers = _peerListManager.GetAllPeers();
        var checkedCount = 0;
        var aliveCount = 0;
        var failedCount = 0;

        // Check a subset of peers (to avoid overwhelming the network)
        var peersToCheck = allPeers
            .OrderBy(p => p.LastSeen)
            .Take(_configuration.PeerDiscovery.MaxConcurrentDiscoveries)
            .ToList();

        var checkTasks = peersToCheck.Select(async peer =>
        {
            try
            {
                var nodeAddress = $"{peer.Address}:{peer.Port}";
                var isAlive = await _peerDiscoveryService.PingPeerAsync(nodeAddress, cancellationToken);

                if (isAlive)
                {
                    await _peerListManager.UpdateLastSeenAsync(peer.PeerId, cancellationToken);
                    Interlocked.Increment(ref aliveCount);
                }
                else
                {
                    await _peerListManager.IncrementFailureCountAsync(peer.PeerId, cancellationToken);
                    Interlocked.Increment(ref failedCount);
                }

                Interlocked.Increment(ref checkedCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking health of peer {PeerId}", peer.PeerId);
                await _peerListManager.IncrementFailureCountAsync(peer.PeerId, cancellationToken);
                Interlocked.Increment(ref failedCount);
                Interlocked.Increment(ref checkedCount);
            }
        });

        await Task.WhenAll(checkTasks);

        sw.Stop();

        var result = new HealthCheckResult
        {
            TotalPeers = allPeers.Count,
            CheckedPeers = checkedCount,
            AlivePeers = aliveCount,
            FailedPeers = failedCount,
            HealthyPeers = _peerListManager.GetHealthyPeerCount(),
            CheckDurationMs = sw.ElapsedMilliseconds,
            Timestamp = DateTimeOffset.UtcNow
        };

        _logger.LogInformation(
            "Health check complete: {Alive}/{Checked} alive, {Healthy} healthy, took {Duration}ms",
            aliveCount, checkedCount, result.HealthyPeers, result.CheckDurationMs);

        return result;
    }

    /// <summary>
    /// Determines the overall service health status
    /// </summary>
    public PeerServiceStatus DetermineServiceStatus()
    {
        var healthyCount = _peerListManager.GetHealthyPeerCount();
        var minHealthy = _configuration.PeerDiscovery.MinHealthyPeers;

        if (healthyCount == 0)
        {
            _logger.LogWarning("No healthy peers available - service offline");
            return PeerServiceStatus.Offline;
        }
        else if (healthyCount < minHealthy)
        {
            _logger.LogWarning("Below minimum healthy peers ({Count}/{Min}) - service degraded",
                healthyCount, minHealthy);
            return PeerServiceStatus.Degraded;
        }
        else
        {
            _logger.LogDebug("Service healthy with {Count} peers", healthyCount);
            return PeerServiceStatus.Online;
        }
    }

    /// <summary>
    /// Gets current network statistics
    /// </summary>
    public NetworkStatistics GetNetworkStatistics()
    {
        var allPeers = _peerListManager.GetAllPeers();
        var healthyPeers = _peerListManager.GetHealthyPeers();

        var stats = new NetworkStatistics
        {
            TotalPeers = allPeers.Count,
            HealthyPeers = healthyPeers.Count,
            AverageLatencyMs = healthyPeers.Any()
                ? (int)healthyPeers.Average(p => p.AverageLatencyMs)
                : 0,
            SeedNodes = allPeers.Count(p => p.IsSeedNode),
            Timestamp = DateTimeOffset.UtcNow
        };

        return stats;
    }
}

/// <summary>
/// Result of a health check operation
/// </summary>
public class HealthCheckResult
{
    public int TotalPeers { get; set; }
    public int CheckedPeers { get; set; }
    public int AlivePeers { get; set; }
    public int FailedPeers { get; set; }
    public int HealthyPeers { get; set; }
    public long CheckDurationMs { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Network statistics snapshot
/// </summary>
public class NetworkStatistics
{
    public int TotalPeers { get; set; }
    public int HealthyPeers { get; set; }
    public int AverageLatencyMs { get; set; }
    public int SeedNodes { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
