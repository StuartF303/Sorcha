// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Connection status for a hub node
/// </summary>
public enum HubNodeConnectionStatus
{
    /// <summary>
    /// Not connected to this hub node
    /// </summary>
    Disconnected = 0,

    /// <summary>
    /// Connection attempt in progress
    /// </summary>
    Connecting = 1,

    /// <summary>
    /// Successfully connected and heartbeat active
    /// </summary>
    Connected = 2,

    /// <summary>
    /// Connection attempt failed
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Connected but heartbeat timeout occurred
    /// </summary>
    HeartbeatTimeout = 4
}
