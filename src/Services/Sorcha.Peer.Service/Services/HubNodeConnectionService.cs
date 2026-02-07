// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Diagnostics;
using Grpc.Core;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Discovery;
using Sorcha.Peer.Service.Protos;
using Sorcha.Peer.Service.Replication;

namespace Sorcha.Peer.Service.Services;

/// <summary>
/// gRPC service implementation for hub node connection management
/// </summary>
/// <remarks>
/// Implements the HubNodeConnection service defined in HubNodeConnection.proto.
/// Handles peer node connections to this hub node, assigns session IDs, and returns
/// connection metadata including current system register version.
///
/// RPCs implemented:
/// - ConnectToHubNode: Establishes peer connection and assigns session
/// - DisconnectFromHubNode: Gracefully disconnects peer and cleanup resources
/// - GetHubNodeStatus: Returns hub node health and system register status
/// </remarks>
public class HubNodeConnectionService : HubNodeConnection.HubNodeConnectionBase
{
    private readonly ILogger<HubNodeConnectionService> _logger;
    private readonly PeerListManager _peerListManager;
    private readonly HubNodeDiscoveryService _hubNodeDiscoveryService;
    private readonly SystemRegisterCache _systemRegisterCache;
    private readonly Dictionary<string, ConnectedPeerSession> _activeSessions;
    private readonly object _sessionsLock = new();
    private static readonly long _startTimeTicks = Stopwatch.GetTimestamp();

    /// <summary>
    /// Initializes a new instance of the <see cref="HubNodeConnectionService"/> class
    /// </summary>
    public HubNodeConnectionService(
        ILogger<HubNodeConnectionService> logger,
        PeerListManager peerListManager,
        HubNodeDiscoveryService hubNodeDiscoveryService,
        SystemRegisterCache systemRegisterCache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _peerListManager = peerListManager ?? throw new ArgumentNullException(nameof(peerListManager));
        _hubNodeDiscoveryService = hubNodeDiscoveryService ?? throw new ArgumentNullException(nameof(hubNodeDiscoveryService));
        _systemRegisterCache = systemRegisterCache ?? throw new ArgumentNullException(nameof(systemRegisterCache));
        _activeSessions = new Dictionary<string, ConnectedPeerSession>();
    }

    /// <summary>
    /// Handles peer connection requests to this hub node
    /// </summary>
    public override async Task<ConnectionResponse> ConnectToHubNode(ConnectRequest request, ServerCallContext context)
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

            _logger.LogInformation("Peer {PeerId} requesting connection to hub node", request.PeerId);

            // Validate this is actually a hub node
            if (!_hubNodeDiscoveryService.IsHubNode())
            {
                _logger.LogError("Connection request rejected: this node is not configured as a hub node");
                return new ConnectionResponse
                {
                    Success = false,
                    Message = "This node is not a hub node"
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
            // (This allows hub node to track connected peers)
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
                HubNodeId = _hubNodeDiscoveryService.GetHostname(),
                CurrentSystemRegisterVersion = _systemRegisterCache.GetCurrentVersion(),
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
    public override async Task<DisconnectionResponse> DisconnectFromHubNode(DisconnectRequest request, ServerCallContext context)
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
    /// Returns hub node status and health information
    /// </summary>
    public override async Task<HubNodeStatus> GetHubNodeStatus(StatusRequest request, ServerCallContext context)
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

            return new HubNodeStatus
            {
                NodeId = _hubNodeDiscoveryService.GetHostname(),
                Health = NodeHealth.Healthy,
                CurrentSystemRegisterVersion = _systemRegisterCache.GetCurrentVersion(),
                TotalBlueprints = _systemRegisterCache.GetBlueprintCount(),
                ActivePeerCount = activePeerCount,
                LastBlueprintPublishedAt = _systemRegisterCache.GetLastUpdateTime() != DateTime.MinValue
                    ? new DateTimeOffset(_systemRegisterCache.GetLastUpdateTime(), TimeSpan.Zero).ToUnixTimeMilliseconds()
                    : 0,
                UptimeSeconds = (long)Stopwatch.GetElapsedTime(_startTimeTicks).TotalSeconds,
                ConnectedPeers = { connectedPeers }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hub node status");
            return new HubNodeStatus
            {
                NodeId = _hubNodeDiscoveryService.GetHostname(),
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
