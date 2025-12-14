// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Overall peer connection status
/// </summary>
public enum PeerConnectionStatus
{
    /// <summary>
    /// Peer is disconnected from all central nodes
    /// </summary>
    Disconnected = 0,

    /// <summary>
    /// Peer is attempting to connect to a central node
    /// </summary>
    Connecting = 1,

    /// <summary>
    /// Peer is connected and heartbeat active
    /// </summary>
    Connected = 2,

    /// <summary>
    /// Heartbeat timeout detected, attempting failover
    /// </summary>
    HeartbeatTimeout = 3,

    /// <summary>
    /// Operating without central node connection (using last known replica)
    /// </summary>
    Isolated = 4
}
