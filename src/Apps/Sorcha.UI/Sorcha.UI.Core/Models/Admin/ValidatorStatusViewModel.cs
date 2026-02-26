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

    /// <summary>
    /// Overall throughput in dockets (transactions) processed per minute.
    /// </summary>
    public double DocketsPerMinute { get; init; }

    /// <summary>
    /// Current queue depth across all registers.
    /// </summary>
    public int QueueDepth { get; init; }

    /// <summary>
    /// Timestamp of the last processed transaction.
    /// </summary>
    public DateTimeOffset? LastProcessedAt { get; init; }

    /// <summary>
    /// Overall health status: Healthy, Degraded, or Unhealthy.
    /// </summary>
    public string HealthStatus { get; init; } = "Unknown";
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

    /// <summary>
    /// Current chain height for this register.
    /// </summary>
    public long ChainHeight { get; init; }

    /// <summary>
    /// The last block number that was validated.
    /// </summary>
    public long LastValidatedBlock { get; init; }

    /// <summary>
    /// Processing status: Idle, Processing, Error.
    /// </summary>
    public string ProcessingStatus { get; init; } = "Idle";

    /// <summary>
    /// Timestamp of last activity on this register.
    /// </summary>
    public DateTimeOffset? LastActivity { get; init; }
}
