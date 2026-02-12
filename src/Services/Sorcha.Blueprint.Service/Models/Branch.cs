// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Blueprint.Service.Models;

/// <summary>
/// Tracks a parallel execution branch in a workflow.
/// Used when routing evaluates to multiple next actions.
/// </summary>
public class Branch
{
    /// <summary>
    /// Unique identifier for the branch within the instance
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Current action ID being executed in this branch
    /// </summary>
    public required int CurrentActionId { get; set; }

    /// <summary>
    /// Current state of the branch
    /// </summary>
    public BranchState State { get; set; } = BranchState.Active;

    /// <summary>
    /// The ID of the last completed transaction in this branch
    /// </summary>
    public string? LastTransactionId { get; set; }

    /// <summary>
    /// Optional deadline for branch completion.
    /// If set, the branch will time out if not completed by this time.
    /// </summary>
    public DateTimeOffset? Deadline { get; init; }

    /// <summary>
    /// Timestamp when the branch was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp when the branch was completed (if completed)
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// The route ID that created this branch
    /// </summary>
    public string? SourceRouteId { get; init; }
}
