// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Peer.Service.Connection;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Discovery;
using Sorcha.Peer.Service.Observability;
using System.Diagnostics;

namespace Sorcha.Peer.Service.Monitoring;

/// <summary>
/// Background service for sending periodic heartbeats to connected central node and monitoring connection health
/// </summary>
/// <remarks>
/// Heartbeat protocol:
/// - Interval: 30 seconds (configurable)
/// - Timeout: 30 seconds per heartbeat RPC
/// - Missed heartbeat threshold: 2 consecutive (60s total) triggers failover
/// - Heartbeat sequence number increments monotonically
/// - Reports last sync version to allow central node to detect peer lag
/// </remarks>
public class HeartbeatMonitorService : BackgroundService
{
    private readonly ILogger<HeartbeatMonitorService> _logger;
    private readonly CentralNodeConnectionManager _connectionManager;
    private readonly PeerListManager _peerListManager;
    private readonly CentralNodeDiscoveryService _discoveryService;
    private readonly PeerServiceMetrics _metrics;
    private readonly PeerServiceActivitySource _activitySource;
    private long _sequenceNumber = 0;
    private int _missedHeartbeats = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeartbeatMonitorService"/> class
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="connectionManager">Central node connection manager</param>
    /// <param name="peerListManager">Peer list manager</param>
    /// <param name="discoveryService">Central node discovery service</param>
    public HeartbeatMonitorService(
        ILogger<HeartbeatMonitorService> logger,
        CentralNodeConnectionManager connectionManager,
        PeerListManager peerListManager,
        CentralNodeDiscoveryService discoveryService,
        PeerServiceMetrics metrics,
        PeerServiceActivitySource activitySource)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _peerListManager = peerListManager ?? throw new ArgumentNullException(nameof(peerListManager));
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
    }

    /// <summary>
    /// Executes the heartbeat monitoring loop
    /// </summary>
    /// <param name="stoppingToken">Cancellation token</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Skip heartbeat monitoring if this is a central node
        if (_discoveryService.IsCentralNode())
        {
            _logger.LogInformation("This is a central node - heartbeat monitoring disabled");
            return;
        }

        _logger.LogInformation("Heartbeat monitor service started (interval: {Interval}s, timeout: {Timeout}s)",
            PeerServiceConstants.HeartbeatIntervalSeconds,
            PeerServiceConstants.HeartbeatTimeoutSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(PeerServiceConstants.HeartbeatIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);

                // Skip if no active connection
                var activeNode = _connectionManager.GetActiveCentralNode();
                if (activeNode == null)
                {
                    _logger.LogDebug("No active central node connection - skipping heartbeat");
                    continue;
                }

                // Send heartbeat
                await SendHeartbeatAsync(activeNode, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Heartbeat monitor service stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Heartbeat monitor service encountered error");
                // Continue running even on error
            }
        }

        _logger.LogInformation("Heartbeat monitor service stopped");
    }

    /// <summary>
    /// Sends a heartbeat message to the connected central node using gRPC
    /// </summary>
    /// <param name="centralNode">Central node information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task SendHeartbeatAsync(CentralNodeInfo centralNode, CancellationToken cancellationToken)
    {
        var localPeerInfo = _peerListManager.GetLocalPeerStatus();
        var sequenceNumber = Interlocked.Increment(ref _sequenceNumber);
        var startTime = DateTime.UtcNow;

        using var activity = _activitySource.StartHeartbeatActivity(centralNode.NodeId, sequenceNumber);

        var heartbeatRequest = new Protos.HeartbeatMessage
        {
            PeerId = localPeerInfo?.PeerId ?? "unknown",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            SequenceNumber = sequenceNumber,
            LastSyncVersion = localPeerInfo?.LastSyncVersion ?? 0,
            SessionId = string.Empty, // TODO: Get from connection manager
            NodeType = "Peer"
        };

        _logger.LogDebug(
            "Sending heartbeat {SequenceNumber} to central node {NodeId} (last sync version: {LastSyncVersion})",
            sequenceNumber, centralNode.NodeId, localPeerInfo?.LastSyncVersion ?? 0);

        try
        {
            // Create timeout for this heartbeat
            using var timeoutCts = new CancellationTokenSource(
                TimeSpan.FromSeconds(PeerServiceConstants.HeartbeatTimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

            // Get active gRPC channel
            var channel = _connectionManager.GetActiveChannel();
            if (channel == null)
            {
                _logger.LogWarning(
                    "No active gRPC channel for heartbeat {SequenceNumber} - skipping",
                    sequenceNumber);
                await HandleHeartbeatTimeoutAsync(centralNode);
                return;
            }

            // Create gRPC client and send heartbeat
            var client = new Protos.Heartbeat.HeartbeatClient(channel);
            var response = await client.SendHeartbeatAsync(heartbeatRequest, cancellationToken: linkedCts.Token);

            // Calculate latency
            var latency = (DateTime.UtcNow - startTime).TotalMilliseconds;

            // Heartbeat successful - reset missed count
            _missedHeartbeats = 0;

            // Update peer info with heartbeat timestamp
            if (localPeerInfo != null)
            {
                localPeerInfo.RecordHeartbeat();
            }

            // Update central node heartbeat tracking
            centralNode.LastHeartbeatSent = DateTime.UtcNow;
            centralNode.LastHeartbeatAcknowledged = DateTime.UtcNow;

            // Record metrics
            _metrics.RecordHeartbeatLatency(latency, centralNode.NodeId);
            _activitySource.RecordSuccess(activity, TimeSpan.FromMilliseconds(latency));

            _logger.LogDebug(
                "Heartbeat {SequenceNumber} acknowledged by central node {NodeId} (latency: {Latency}ms, action: {Action})",
                sequenceNumber, centralNode.NodeId, latency, response.RecommendedAction);

            // Handle recommended actions from central node
            await HandleRecommendedActionAsync(response.RecommendedAction, response.CurrentSystemRegisterVersion, localPeerInfo);
        }
        catch (OperationCanceledException)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogWarning(
                "Heartbeat {SequenceNumber} timed out to central node {NodeId} after {Duration}ms (missed count: {MissedCount})",
                sequenceNumber, centralNode.NodeId, duration.TotalMilliseconds, _missedHeartbeats + 1);
            await HandleHeartbeatTimeoutAsync(centralNode);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _activitySource.RecordFailure(activity, ex, duration);

            _logger.LogWarning(ex,
                "Heartbeat {SequenceNumber} failed to central node {NodeId} after {Duration}ms (missed count: {MissedCount})",
                sequenceNumber, centralNode.NodeId, duration.TotalMilliseconds, _missedHeartbeats + 1);

            await HandleHeartbeatTimeoutAsync(centralNode);
        }
    }

    /// <summary>
    /// Handles recommended actions from heartbeat acknowledgement
    /// </summary>
    /// <param name="action">Recommended action</param>
    /// <param name="currentVersion">Current system register version from central node</param>
    /// <param name="localPeerInfo">Local peer information</param>
    private async Task HandleRecommendedActionAsync(
        Protos.RecommendedAction action,
        long currentVersion,
        ActivePeerInfo? localPeerInfo)
    {
        switch (action)
        {
            case Protos.RecommendedAction.Sync:
                if (localPeerInfo != null && currentVersion > localPeerInfo.LastSyncVersion)
                {
                    _logger.LogInformation("Peer is behind system register - version {Current} vs {Expected}. Incremental sync recommended.",
                        localPeerInfo.LastSyncVersion, currentVersion);
                    // TODO: Trigger incremental sync via SystemRegisterReplicationService
                }
                break;

            case Protos.RecommendedAction.Failover:
                _logger.LogWarning("Central node recommends failover - triggering failover to next node");
                await _connectionManager.FailoverToNextNodeAsync();
                break;

            case Protos.RecommendedAction.Reconnect:
                _logger.LogWarning("Central node recommends reconnection - disconnecting and reconnecting");
                await _connectionManager.DisconnectAsync();
                await _connectionManager.ConnectToCentralNodeAsync();
                break;

            case Protos.RecommendedAction.ReduceFrequency:
                _logger.LogInformation("Central node requests reduced heartbeat frequency (currently not implemented)");
                break;

            case Protos.RecommendedAction.None:
            default:
                // No action needed
                break;
        }
    }

    /// <summary>
    /// Handles heartbeat timeout by incrementing missed count and triggering failover if threshold reached
    /// </summary>
    /// <param name="centralNode">Central node that timed out</param>
    private async Task HandleHeartbeatTimeoutAsync(CentralNodeInfo centralNode)
    {
        _missedHeartbeats++;

        _logger.LogWarning("Heartbeat timeout for central node {NodeId} - missed count: {MissedCount}/{Threshold}",
            centralNode.NodeId,
            _missedHeartbeats,
            PeerServiceConstants.MaxMissedHeartbeats);

        // Record missed heartbeat in peer info
        var localPeerInfo = _peerListManager.GetLocalPeerStatus();
        if (localPeerInfo != null)
        {
            localPeerInfo.RecordMissedHeartbeat();
        }

        // Update central node status
        centralNode.ConnectionStatus = CentralNodeConnectionStatus.HeartbeatTimeout;

        // Trigger failover if threshold reached
        if (_missedHeartbeats >= PeerServiceConstants.MaxMissedHeartbeats)
        {
            _logger.LogWarning("Heartbeat timeout threshold reached ({Threshold}) - triggering failover",
                PeerServiceConstants.MaxMissedHeartbeats);

            // Reset missed count before failover
            _missedHeartbeats = 0;

            // Attempt failover to next central node
            var failoverSuccess = await _connectionManager.FailoverToNextNodeAsync();

            if (!failoverSuccess)
            {
                _logger.LogError("Failover failed - no central nodes reachable (isolated mode)");
                _peerListManager.UpdateLocalPeerStatus(null, PeerConnectionStatus.Isolated);
            }
            else
            {
                _logger.LogInformation("Failover successful - connected to new central node");
            }
        }
    }

    /// <summary>
    /// Gets the current heartbeat sequence number
    /// </summary>
    /// <returns>Current sequence number</returns>
    public long GetCurrentSequenceNumber()
    {
        return Interlocked.Read(ref _sequenceNumber);
    }

    /// <summary>
    /// Gets the number of consecutive missed heartbeats
    /// </summary>
    /// <returns>Missed heartbeat count</returns>
    public int GetMissedHeartbeatCount()
    {
        return _missedHeartbeats;
    }

    /// <summary>
    /// Checks if heartbeat is currently timed out
    /// </summary>
    /// <returns>True if timeout threshold reached</returns>
    public bool IsHeartbeatTimedOut()
    {
        return _missedHeartbeats >= PeerServiceConstants.MaxMissedHeartbeats;
    }
}
