// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.UI.Core.Models.Admin;

/// <summary>
/// View model for validator mempool status display.
/// </summary>
public record ValidatorStatusViewModel
{
    public int TotalPendingTransactions { get; init; }
    public List<RegisterMempoolStat> RegisterMempoolStats { get; init; } = [];
    public TimeSpan? OldestPendingAge { get; init; }
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;
    public bool IsLoaded { get; init; }
}

/// <summary>
/// Per-register mempool statistics.
/// </summary>
public record RegisterMempoolStat
{
    public string RegisterId { get; init; } = string.Empty;
    public string RegisterName { get; init; } = string.Empty;
    public int PendingCount { get; init; }
    public TimeSpan? OldestEntryAge { get; init; }
}
