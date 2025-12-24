// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Grpc.Core;
using Sorcha.Peer.Service.Discovery;
using Sorcha.Peer.Service.Protos;

namespace Sorcha.Peer.Service.Services;

/// <summary>
/// gRPC service implementation for heartbeat monitoring between peers and hub nodes
/// </summary>
/// <remarks>
/// Implements the Heartbeat service defined in Heartbeat.proto.
/// Runs on hub nodes and provides RPCs for peer nodes to send heartbeat messages
/// and monitor connection health.
///
/// Heartbeat protocol:
/// - Interval: 30 seconds (configurable)
/// - Timeout: 30 seconds per heartbeat
/// - Missed heartbeat threshold: 2 consecutive (60s total) triggers failover
///
/// Heartbeat tracking:
/// - Records timestamp of each heartbeat
/// - Detects version lag by comparing peer's LastSyncVersion with current system register version
/// - Provides recommended actions (SYNC, FAILOVER, etc.)
/// </remarks>
public class HeartbeatService : Protos.Heartbeat.HeartbeatBase
{
    private readonly ILogger<HeartbeatService> _logger;
    private readonly HubNodeDiscoveryService _centralNodeDiscoveryService;
    private readonly PeerListManager _peerListManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeartbeatService"/> class
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="centralNodeDiscoveryService">Hub node discovery service</param>
    /// <param name="peerListManager">Peer list manager for tracking connected peers</param>
    public HeartbeatService(
        ILogger<HeartbeatService> logger,
        HubNodeDiscoveryService centralNodeDiscoveryService,
        PeerListManager peerListManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _centralNodeDiscoveryService = centralNodeDiscoveryService ?? throw new ArgumentNullException(nameof(centralNodeDiscoveryService));
        _peerListManager = peerListManager ?? throw new ArgumentNullException(nameof(peerListManager));
    }

    /// <summary>
    /// Handles unary heartbeat message from peer node
    /// </summary>
    /// <param name="request">Heartbeat message from peer</param>
    /// <param name="context">Server call context</param>
    /// <returns>Heartbeat acknowledgement</returns>
    public override Task<HeartbeatAcknowledgement> SendHeartbeat(HeartbeatMessage request, ServerCallContext context)
    {
        try
        {
            _logger.LogDebug("Received heartbeat from peer {PeerId} (sequence {Sequence}, sync version {Version})",
                request.PeerId, request.SequenceNumber, request.LastSyncVersion);

            // Record heartbeat timestamp
            var receiveTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Check for clock skew (allow ±60 seconds tolerance)
            var clockSkew = Math.Abs(receiveTime - request.Timestamp);
            if (clockSkew > 60000) // 60 seconds in milliseconds
            {
                _logger.LogWarning("Clock skew detected for peer {PeerId}: {Skew}ms",
                    request.PeerId, clockSkew);
            }

            // TODO: Get current system register version from repository
            // For now, use a placeholder
            long currentSystemRegisterVersion = request.LastSyncVersion; // Placeholder

            // Determine recommended action based on version lag
            var recommendedAction = RecommendedAction.None;
            var message = "OK";

            if (currentSystemRegisterVersion > request.LastSyncVersion)
            {
                var versionLag = currentSystemRegisterVersion - request.LastSyncVersion;
                _logger.LogInformation("Peer {PeerId} is behind system register by {Lag} versions",
                    request.PeerId, versionLag);

                recommendedAction = RecommendedAction.Sync;
                message = $"Sync needed - {versionLag} versions behind";
            }

            // Calculate server-side latency
            var serverLatencyMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - receiveTime;

            // Create acknowledgement
            var acknowledgement = new HeartbeatAcknowledgement
            {
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                HubNodeId = _centralNodeDiscoveryService.GetHostname(),
                CurrentSystemRegisterVersion = currentSystemRegisterVersion,
                Message = message,
                RecommendedAction = recommendedAction,
                ServerLatencyMs = serverLatencyMs
            };

            _logger.LogDebug("Sent heartbeat acknowledgement to peer {PeerId} (recommended action: {Action})",
                request.PeerId, recommendedAction);

            return Task.FromResult(acknowledgement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing heartbeat from peer {PeerId}", request.PeerId);

            // Return failure acknowledgement
            return Task.FromResult(new HeartbeatAcknowledgement
            {
                Success = false,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                HubNodeId = _centralNodeDiscoveryService.GetHostname(),
                Message = $"Heartbeat processing failed: {ex.Message}",
                RecommendedAction = RecommendedAction.None
            });
        }
    }

    /// <summary>
    /// Handles bidirectional streaming heartbeat monitoring
    /// </summary>
    /// <param name="requestStream">Stream of heartbeat messages from peer</param>
    /// <param name="responseStream">Stream of acknowledgements to peer</param>
    /// <param name="context">Server call context</param>
    /// <returns>Task representing the streaming operation</returns>
    public override async Task MonitorHeartbeat(
        IAsyncStreamReader<HeartbeatMessage> requestStream,
        IServerStreamWriter<HeartbeatAcknowledgement> responseStream,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Started bidirectional heartbeat monitoring stream");

            int heartbeatCount = 0;
            int missedHeartbeats = 0;
            string? peerId = null;

            // Read heartbeats from stream
            await foreach (var heartbeat in requestStream.ReadAllAsync(context.CancellationToken))
            {
                peerId = heartbeat.PeerId;
                heartbeatCount++;

                _logger.LogDebug("Received heartbeat {Count} from peer {PeerId} (sequence {Sequence})",
                    heartbeatCount, heartbeat.PeerId, heartbeat.SequenceNumber);

                // Reset missed heartbeats on successful receive
                missedHeartbeats = 0;

                // Process heartbeat (same logic as SendHeartbeat)
                var receiveTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // TODO: Get current system register version
                long currentSystemRegisterVersion = heartbeat.LastSyncVersion; // Placeholder

                var recommendedAction = RecommendedAction.None;
                var message = "OK";

                if (currentSystemRegisterVersion > heartbeat.LastSyncVersion)
                {
                    recommendedAction = RecommendedAction.Sync;
                    message = $"Sync needed";
                }

                // Check for excessive missed heartbeats
                if (missedHeartbeats >= 2)
                {
                    _logger.LogWarning("Peer {PeerId} has missed {Count} heartbeats - recommending failover",
                        heartbeat.PeerId, missedHeartbeats);
                    recommendedAction = RecommendedAction.Failover;
                    message = "Too many missed heartbeats";
                }

                // Send acknowledgement
                var acknowledgement = new HeartbeatAcknowledgement
                {
                    Success = true,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    HubNodeId = _centralNodeDiscoveryService.GetHostname(),
                    CurrentSystemRegisterVersion = currentSystemRegisterVersion,
                    Message = message,
                    RecommendedAction = recommendedAction,
                    ServerLatencyMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - receiveTime
                };

                await responseStream.WriteAsync(acknowledgement, context.CancellationToken);

                _logger.LogDebug("Sent heartbeat acknowledgement {Count} to peer {PeerId}",
                    heartbeatCount, heartbeat.PeerId);
            }

            _logger.LogInformation("Heartbeat monitoring stream completed for peer {PeerId}. Total heartbeats: {Count}",
                peerId, heartbeatCount);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Heartbeat monitoring stream cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in heartbeat monitoring stream");
            throw;
        }
    }

    /// <summary>
    /// Gets current heartbeat status for a peer
    /// </summary>
    /// <param name="request">Heartbeat status request</param>
    /// <param name="context">Server call context</param>
    /// <returns>Heartbeat status</returns>
    public override Task<HeartbeatStatus> GetHeartbeatStatus(HeartbeatStatusRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogDebug("Heartbeat status requested for peer {PeerId}", request.PeerId);

            // TODO: Implement heartbeat status tracking
            // For now, return a placeholder status

            var status = new HeartbeatStatus
            {
                PeerId = request.PeerId,
                SessionId = request.SessionId,
                LastHeartbeatTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                MissedHeartbeats = 0,
                IsHealthy = true,
                StatusMessage = "Healthy",
                TotalHeartbeatsReceived = 0,
                TotalHeartbeatsMissed = 0,
                AverageLatencyMs = 0,
                LastKnownSyncVersion = 0,
                VersionLag = 0,
                HealthStatus = HeartbeatHealthStatus.Healthy
            };

            return Task.FromResult(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting heartbeat status for peer {PeerId}", request.PeerId);
            throw new RpcException(new Status(StatusCode.Internal, $"Get heartbeat status failed: {ex.Message}"));
        }
    }
}
