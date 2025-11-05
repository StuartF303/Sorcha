// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Represents the operational status of the peer service
/// </summary>
public enum PeerServiceStatus
{
    /// <summary>
    /// Service is offline and not connected to any peers
    /// </summary>
    Offline,

    /// <summary>
    /// Service is online and connected to at least MinHealthyPeers
    /// </summary>
    Online,

    /// <summary>
    /// Service has some peer connections but below healthy threshold
    /// </summary>
    Degraded
}
