// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.UI.Core.Models.Workflows;

/// <summary>
/// View model for workflow instance display.
/// </summary>
public record WorkflowInstanceViewModel
{
    public string InstanceId { get; init; } = string.Empty;
    public string BlueprintId { get; init; } = string.Empty;
    public string BlueprintName { get; init; } = string.Empty;
    public string Status { get; init; } = "active";
    public string? CurrentActionName { get; init; }
    public int CurrentStepNumber { get; init; }
    public int TotalSteps { get; init; }
    public int ParticipantCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// View model for a pending action assigned to the current user.
/// </summary>
public record PendingActionViewModel
{
    public string ActionId { get; init; } = string.Empty;
    public string InstanceId { get; init; } = string.Empty;
    public string BlueprintName { get; init; } = string.Empty;
    public string ActionName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Priority { get; init; } = "normal";
    public DateTimeOffset AssignedAt { get; init; }
    public DateTimeOffset? DueAt { get; init; }
    public System.Text.Json.JsonElement? DataSchema { get; init; }
}

/// <summary>
/// View model for submitting action data.
/// </summary>
public record ActionSubmissionViewModel
{
    public string ActionId { get; init; } = string.Empty;
    public string InstanceId { get; init; } = string.Empty;
    public Dictionary<string, object> Data { get; init; } = new();
}
