// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Grpc.Core;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Discovery;
using Sorcha.Peer.Service.Protos;

namespace Sorcha.Peer.Service.Services;

/// <summary>
/// gRPC service implementation for central node connection management
/// </summary>
/// <remarks>
/// Implements the CentralNodeConnection service defined in CentralNodeConnection.proto.
/// Handles peer node connections to this central node, assigns session IDs, and returns
/// connection metadata including current system register version.
///
/// RPCs implemented:
/// - ConnectToCentralNode: Establishes peer connection and assigns session
/// - DisconnectFromCentralNode: Gracefully disconnects peer and cleanup resources
/// - GetCentralNodeStatus: Returns central node health and system register status
/// </remarks>
public class CentralNodeConnectionService : CentralNodeConnection.CentralNodeConnectionBase
{
    private readonly ILogger<CentralNodeConnectionService> _logger;
    private readonly PeerListManager _peerListManager;
    private readonly CentralNodeDiscoveryService _centralNodeDiscoveryService;
    private readonly Dictionary<string, ConnectedPeerSession> _activeSessions;
    private readonly object _sessionsLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CentralNodeConnectionService"/> class
    /// </summary>
    public CentralNodeConnectionService(
        ILogger<CentralNodeConnectionService> logger,
        PeerListManager peerListManager,
        CentralNodeDiscoveryService centralNodeDiscoveryService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _peerListManager = peerListManager ?? throw new ArgumentNullException(nameof(peerListManager));
        _centralNodeDiscoveryService = centralNodeDiscoveryService ?? throw new ArgumentNullException(nameof(centralNodeDiscoveryService));
        _activeSessions = new Dictionary<string, ConnectedPeerSession>();
    }

    /// <summary>
    /// Handles peer connection requests to this central node
    /// </summary>
    public override async Task<ConnectionResponse> ConnectToCentralNode(ConnectRequest request, ServerCallContext context)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.PeerId))
            {
                _logger.LogWarning("Connection request rejected: missing peer ID");
                return new ConnectionResponse
                {
                    Success = false,
                    Message = "Peer ID is required"
                };
            }

            _logger.LogInformation("Peer {PeerId} requesting connection to central node", request.PeerId);

            // Validate this is actually a central node
            if (!_centralNodeDiscoveryService.IsCentralNode())
            {
                _logger.LogError("Connection request rejected: this node is not configured as a central node");
                return new ConnectionResponse
                {
                    Success = false,
                    Message = "This node is not a central node"
                };
            }

            // Generate session ID
            var sessionId = GenerateSessionId(request.PeerId);

            // Record peer in active sessions
            lock (_sessionsLock)
            {
                _activeSessions[request.PeerId] = new ConnectedPeerSession
                {
                    PeerId = request.PeerId,
                    SessionId = sessionId,
                    ConnectedAt = DateTime.UtcNow,
                    LastHeartbeat = DateTime.UtcNow,
                    LastSyncVersion = request.LastKnownVersion,
                    Status = Protos.PeerConnectionStatus.Connected
                };
            }

            // Add peer to PeerListManager if not already present
            // (This allows central node to track connected peers)
            var peerNode = new PeerNode
            {
                PeerId = request.PeerId,
                Address = request.PeerInfo?.Address ?? "unknown",
                Port = request.PeerInfo?.Port ?? 0,
                LastSeen = DateTimeOffset.UtcNow,
                FirstSeen = DateTimeOffset.UtcNow
            };
            await _peerListManager.AddOrUpdatePeerAsync(peerNode);

            _logger.LogInformation("Peer {PeerId} connected successfully with session {SessionId}",
                request.PeerId, sessionId);

            // Return connection response
            return new ConnectionResponse
            {
                Success = true,
                Message = "Connection established successfully",
                SessionId = sessionId,
                CentralNodeId = _centralNodeDiscoveryService.GetHostname(),
                CurrentSystemRegisterVersion = 0, // TODO: Get from SystemRegisterRepository
                ConnectedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                HeartbeatIntervalSeconds = 30,
                Config = new ConnectionConfig
                {
                    HeartbeatTimeoutSeconds = 30,
                    PeriodicSyncIntervalMinutes = 5,
                    PushNotificationsEnabled = true,
                    MaxConcurrentSyncs = 3
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling connection request from peer {PeerId}", request.PeerId);
            return new ConnectionResponse
            {
                Success = false,
                Message = $"Connection failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Handles peer disconnection requests
    /// </summary>
    public override async Task<DisconnectionResponse> DisconnectFromCentralNode(DisconnectRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Peer {PeerId} requesting disconnect (reason: {Reason}, session: {SessionId})",
                request.PeerId, request.Reason, request.SessionId);

            // Remove from active sessions
            lock (_sessionsLock)
            {
                if (_activeSessions.Remove(request.PeerId))
                {
                    _logger.LogInformation("Removed peer {PeerId} from active sessions", request.PeerId);
                }
            }

            // Note: We don't remove from PeerListManager to maintain peer history

            return new DisconnectionResponse
            {
                Success = true,
                Message = "Disconnection successful",
                DisconnectedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling disconnection from peer {PeerId}", request.PeerId);
            return new DisconnectionResponse
            {
                Success = false,
                Message = $"Disconnection failed: {ex.Message}",
                DisconnectedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }
    }

    /// <summary>
    /// Returns central node status and health information
    /// </summary>
    public override async Task<CentralNodeStatus> GetCentralNodeStatus(StatusRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogDebug("Status request from peer {PeerId}", request.PeerId);

            // Get connected peer count
            int activePeerCount;
            List<ConnectedPeerInfo> connectedPeers = new();

            lock (_sessionsLock)
            {
                activePeerCount = _activeSessions.Count;

                if (request.IncludePeerList)
                {
                    connectedPeers = _activeSessions.Values.Select(session => new ConnectedPeerInfo
                    {
                        PeerId = session.PeerId,
                        SessionId = session.SessionId,
                        ConnectedAt = new DateTimeOffset(session.ConnectedAt).ToUnixTimeMilliseconds(),
                        LastHeartbeatAt = new DateTimeOffset(session.LastHeartbeat).ToUnixTimeMilliseconds(),
                        LastSyncVersion = session.LastSyncVersion,
                        Status = session.Status
                    }).ToList();
                }
            }

            return new CentralNodeStatus
            {
                NodeId = _centralNodeDiscoveryService.GetHostname(),
                Health = NodeHealth.Healthy,
                CurrentSystemRegisterVersion = 0, // TODO: Get from SystemRegisterRepository
                TotalBlueprints = 0, // TODO: Get from SystemRegisterRepository
                ActivePeerCount = activePeerCount,
                LastBlueprintPublishedAt = 0, // TODO: Get from SystemRegisterRepository
                UptimeSeconds = 0, // TODO: Track uptime
                ConnectedPeers = { connectedPeers }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting central node status");
            return new CentralNodeStatus
            {
                NodeId = _centralNodeDiscoveryService.GetHostname(),
                Health = NodeHealth.Unhealthy,
                ActivePeerCount = 0
            };
        }
    }

    /// <summary>
    /// Generates a unique session ID for a peer connection
    /// </summary>
    private static string GenerateSessionId(string peerId)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = Guid.NewGuid().ToString("N")[..8];
        return $"{peerId}_{timestamp}_{random}";
    }

    /// <summary>
    /// Gets the count of active peer sessions
    /// </summary>
    public int GetActivePeerCount()
    {
        lock (_sessionsLock)
        {
            return _activeSessions.Count;
        }
    }

    /// <summary>
    /// Records heartbeat for a peer
    /// </summary>
    public void RecordHeartbeat(string peerId, long lastSyncVersion)
    {
        lock (_sessionsLock)
        {
            if (_activeSessions.TryGetValue(peerId, out var session))
            {
                session.LastHeartbeat = DateTime.UtcNow;
                session.LastSyncVersion = lastSyncVersion;
                _logger.LogDebug("Recorded heartbeat for peer {PeerId}", peerId);
            }
        }
    }
}

/// <summary>
/// Represents an active peer connection session
/// </summary>
internal class ConnectedPeerSession
{
    public string PeerId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public long LastSyncVersion { get; set; }
    public Protos.PeerConnectionStatus Status { get; set; }
}
