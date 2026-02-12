// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Admin-configured replication mode for a register subscription
/// </summary>
public enum ReplicationMode
{
    /// <summary>
    /// Receive new transactions from point of subscription onward.
    /// Lightweight — no historical data pulled.
    /// </summary>
    ForwardOnly = 0,

    /// <summary>
    /// Pull the complete docket chain, retrieve all transactions listed in each docket,
    /// then receive new transactions going forward.
    /// Required for validation and docket building.
    /// </summary>
    FullReplica = 1
}
