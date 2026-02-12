// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.UI.Core.Models.Actions;

/// <summary>
/// Notification received when an action event occurs.
/// Matches the server-side ActionNotification record from Blueprint Service.
/// </summary>
public record ActionNotification
{
    /// <summary>
    /// Transaction hash associated with this action
    /// </summary>
    public string TransactionHash { get; init; } = string.Empty;

    /// <summary>
    /// Wallet address this action is for
    /// </summary>
    public string WalletAddress { get; init; } = string.Empty;

    /// <summary>
    /// Register address where the action is recorded
    /// </summary>
    public string RegisterAddress { get; init; } = string.Empty;

    /// <summary>
    /// Blueprint ID for this action (optional)
    /// </summary>
    public string? BlueprintId { get; init; }

    /// <summary>
    /// Action ID within the blueprint (optional)
    /// </summary>
    public string? ActionId { get; init; }

    /// <summary>
    /// Workflow instance ID (optional)
    /// </summary>
    public string? InstanceId { get; init; }

    /// <summary>
    /// Timestamp when notification was generated
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Human-readable message (optional)
    /// </summary>
    public string? Message { get; init; }
}

/// <summary>
/// Notification for action availability events (simpler format used by NotificationService).
/// </summary>
public record ActionAvailableNotification
{
    /// <summary>
    /// Workflow instance ID
    /// </summary>
    public string InstanceId { get; init; } = string.Empty;

    /// <summary>
    /// Action ID within the blueprint
    /// </summary>
    public int ActionId { get; init; }

    /// <summary>
    /// Human-readable action title
    /// </summary>
    public string ActionTitle { get; init; } = string.Empty;

    /// <summary>
    /// Participant ID this action is for
    /// </summary>
    public string ParticipantId { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when notification was generated
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Notification for action rejection events.
/// </summary>
public record ActionRejectedNotification
{
    /// <summary>
    /// Workflow instance ID
    /// </summary>
    public string InstanceId { get; init; } = string.Empty;

    /// <summary>
    /// Action ID that was rejected
    /// </summary>
    public int RejectedActionId { get; init; }

    /// <summary>
    /// Target action ID to route to
    /// </summary>
    public int TargetActionId { get; init; }

    /// <summary>
    /// Participant ID for the target action
    /// </summary>
    public string TargetParticipantId { get; init; } = string.Empty;

    /// <summary>
    /// Reason for rejection
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when notification was generated
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Notification for workflow completion events.
/// </summary>
public record WorkflowCompletedNotification
{
    /// <summary>
    /// Workflow instance ID that completed
    /// </summary>
    public string InstanceId { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when workflow completed
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
