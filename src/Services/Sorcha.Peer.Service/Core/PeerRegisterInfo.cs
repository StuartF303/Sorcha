// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.ComponentModel.DataAnnotations;

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Information about a register that a remote peer advertises.
/// Used to track which registers each peer holds for targeted sync and gossip.
/// </summary>
public class PeerRegisterInfo
{
    /// <summary>
    /// Identifier of the register
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string RegisterId { get; set; } = string.Empty;

    /// <summary>
    /// Sync state of this register on the remote peer
    /// </summary>
    [Required]
    public RegisterSyncState SyncState { get; set; } = RegisterSyncState.Subscribing;

    /// <summary>
    /// Latest transaction version the peer has for this register
    /// </summary>
    public long LatestVersion { get; set; } = 0;

    /// <summary>
    /// Latest docket version the peer has for this register
    /// </summary>
    public long LatestDocketVersion { get; set; } = 0;

    /// <summary>
    /// Whether this register is publicly advertised by the peer
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// When this information was last updated
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Whether this peer can serve as a full replica source for this register
    /// </summary>
    public bool CanServeFullReplica => SyncState == RegisterSyncState.FullyReplicated;
}
