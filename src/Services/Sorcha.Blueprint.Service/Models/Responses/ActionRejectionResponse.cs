// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Blueprint.Service.Models.Responses;

/// <summary>
/// Response from rejecting an action
/// </summary>
public record ActionRejectionResponse
{
    /// <summary>
    /// The transaction ID for the rejection
    /// </summary>
    public required string TransactionId { get; init; }

    /// <summary>
    /// The workflow instance ID
    /// </summary>
    public required string InstanceId { get; init; }

    /// <summary>
    /// The target action that the workflow routes to after rejection
    /// </summary>
    public required TargetActionResponse TargetAction { get; init; }

    /// <summary>
    /// Timestamp when the rejection was processed
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Information about the target action for rejection routing
/// </summary>
public record TargetActionResponse
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
    /// The participant ID who should handle the rejected action
    /// </summary>
    public required string ParticipantId { get; init; }
}
