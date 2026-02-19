// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.UI.Core.Models.Workflows;

/// <summary>
/// Result view model from submitting an action execution.
/// Maps to ActionSubmissionResponse from the Blueprint Service.
/// </summary>
public record ActionSubmissionResultViewModel
{
    public string TransactionId { get; init; } = string.Empty;
    public string InstanceId { get; init; } = string.Empty;
    public bool IsComplete { get; init; }
    public List<NextActionInfo>? NextActions { get; init; }
    public List<string>? Warnings { get; init; }
}

/// <summary>
/// Information about the next action in the workflow after a submission.
/// Maps to NextActionResponse from the Blueprint Service.
/// </summary>
public record NextActionInfo
{
    public int ActionId { get; init; }
    public string ActionTitle { get; init; } = string.Empty;
    public string ParticipantId { get; init; } = string.Empty;
    public string? BranchId { get; init; }
}
