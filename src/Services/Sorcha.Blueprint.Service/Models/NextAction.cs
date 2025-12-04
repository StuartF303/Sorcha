// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Blueprint.Service.Models;

/// <summary>
/// Represents the next action to be executed in a workflow.
/// Returned as part of action submission response to inform participants.
/// </summary>
public class NextAction
{
    /// <summary>
    /// The action ID within the blueprint
    /// </summary>
    public required int ActionId { get; init; }

    /// <summary>
    /// Display title of the action
    /// </summary>
    public required string ActionTitle { get; init; }

    /// <summary>
    /// The participant who should execute this action
    /// </summary>
    public required string ParticipantId { get; init; }

    /// <summary>
    /// The wallet address of the participant (if bound)
    /// </summary>
    public string? ParticipantWallet { get; init; }

    /// <summary>
    /// Branch ID for parallel workflows.
    /// Null for sequential workflows or the main branch.
    /// </summary>
    public string? BranchId { get; init; }

    /// <summary>
    /// Deadline for completing this action (optional).
    /// Used for time-sensitive workflows or parallel branch management.
    /// </summary>
    public DateTimeOffset? Deadline { get; init; }
}
