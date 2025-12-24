// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Local peer connection status information (in-memory only)
/// </summary>
public class ActivePeerInfo
{
    /// <summary>
    /// Unique identifier for this peer
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string PeerId { get; set; } = string.Empty;

    /// <summary>
    /// ID of connected hub node (null if disconnected)
    /// </summary>
    [MaxLength(64)]
    public string? ConnectedHubNodeId { get; set; }

    /// <summary>
    /// When connection was established (UTC)
    /// </summary>
    public DateTime ConnectionEstablished { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last heartbeat sent or received (UTC)
    /// </summary>
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last synchronized system register version
    /// </summary>
    public long LastSyncVersion { get; set; } = 0;

    /// <summary>
    /// Current connection status
    /// </summary>
    [Required]
    public PeerConnectionStatus Status { get; set; } = PeerConnectionStatus.Disconnected;

    /// <summary>
    /// Current heartbeat sequence number
    /// </summary>
    public long HeartbeatSequence { get; set; } = 0;

    /// <summary>
    /// Consecutive missed heartbeats (reset on success)
    /// </summary>
    public int MissedHeartbeats { get; set; } = 0;

    /// <summary>
    /// Updates heartbeat state after successful heartbeat
    /// </summary>
    public void RecordHeartbeat()
    {
        LastHeartbeat = DateTime.UtcNow;
        HeartbeatSequence++;
        MissedHeartbeats = 0;
    }

    /// <summary>
    /// Records a missed heartbeat and updates status if threshold reached
    /// </summary>
    public void RecordMissedHeartbeat()
    {
        MissedHeartbeats++;

        if (MissedHeartbeats >= PeerServiceConstants.MaxMissedHeartbeats)
        {
            Status = PeerConnectionStatus.HeartbeatTimeout;
        }
    }

    /// <summary>
    /// Checks if heartbeat is timed out (30 seconds since last heartbeat)
    /// </summary>
    /// <returns>True if heartbeat is timed out</returns>
    public bool IsHeartbeatTimedOut()
    {
        var elapsed = DateTime.UtcNow - LastHeartbeat;
        return elapsed.TotalSeconds > PeerServiceConstants.HeartbeatTimeoutSeconds;
    }
}
