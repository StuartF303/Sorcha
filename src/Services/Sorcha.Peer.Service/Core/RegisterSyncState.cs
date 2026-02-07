// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Synchronization state for a register subscription
/// </summary>
public enum RegisterSyncState
{
    /// <summary>
    /// Subscription created, waiting to start sync
    /// </summary>
    Subscribing = 0,

    /// <summary>
    /// Actively pulling docket chain and historical transactions (full replica only)
    /// </summary>
    Syncing = 1,

    /// <summary>
    /// Full docket chain and all transactions replicated — eligible for validation/docket building
    /// </summary>
    FullyReplicated = 2,

    /// <summary>
    /// Receiving new transactions from subscription point onward (forward-only mode)
    /// </summary>
    Active = 3,

    /// <summary>
    /// Sync encountered an unrecoverable error
    /// </summary>
    Error = 4
}
