// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Overall peer connection status in the P2P network
/// </summary>
public enum PeerConnectionStatus
{
    /// <summary>
    /// Not connected to any peers
    /// </summary>
    Disconnected = 0,

    /// <summary>
    /// Attempting to connect to peers (bootstrap in progress)
    /// </summary>
    Connecting = 1,

    /// <summary>
    /// Connected to one or more peers with active heartbeats
    /// </summary>
    Connected = 2,

    /// <summary>
    /// Heartbeat timeout detected on peer connections
    /// </summary>
    HeartbeatTimeout = 3,

    /// <summary>
    /// Operating with no peer connections (using last known local data)
    /// </summary>
    Isolated = 4
}
