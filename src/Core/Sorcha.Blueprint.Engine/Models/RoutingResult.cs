// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Blueprint.Engine.Models;

/// <summary>
/// Result of routing determination indicating the next step in the workflow.
/// </summary>
/// <remarks>
/// The routing result is produced by evaluating routing conditions
/// against the action data to determine which participant should
/// perform the next action.
/// 
/// Three possible outcomes:
/// 1. Normal routing: NextActionId and NextParticipantId are set
/// 2. Workflow complete: IsWorkflowComplete is true, no next action
/// 3. Rejection: RejectedToParticipantId is set (workflow returns to sender)
/// </remarks>
public class RoutingResult
{
    /// <summary>
    /// The ID of the next action to perform.
    /// </summary>
    /// <remarks>
    /// Null if the workflow is complete or rejected.
    /// This ID corresponds to an action in the blueprint.
    /// </remarks>
    public string? NextActionId { get; set; }

    /// <summary>
    /// The participant ID (DID) who should perform the next action.
    /// </summary>
    /// <remarks>
    /// Null if the workflow is complete.
    /// Determined by evaluating routing conditions.
    /// </remarks>
    public string? NextParticipantId { get; set; }

    /// <summary>
    /// Indicates whether the workflow has reached completion.
    /// </summary>
    /// <remarks>
    /// True when no routing conditions match, indicating
    /// this is a terminal action with no further steps.
    /// </remarks>
    public bool IsWorkflowComplete { get; set; }

    /// <summary>
    /// If set, indicates the action was rejected and should return to this participant.
    /// </summary>
    /// <remarks>
    /// Used in approval workflows where an action can be rejected
    /// and sent back to the original requester.
    /// 
    /// When set, this takes precedence over NextActionId/NextParticipantId.
    /// </remarks>
    public string? RejectedToParticipantId { get; set; }

    /// <summary>
    /// The routing condition that matched (for audit/debugging).
    /// </summary>
    /// <remarks>
    /// Contains the JSON Logic expression that evaluated to true.
    /// Useful for understanding why a particular route was chosen.
    /// </remarks>
    public string? MatchedCondition { get; set; }

    /// <summary>
    /// Creates a routing result for workflow completion.
    /// </summary>
    public static RoutingResult Complete() => new()
    {
        IsWorkflowComplete = true
    };

    /// <summary>
    /// Creates a routing result for the next action.
    /// </summary>
    public static RoutingResult Next(string actionId, string participantId, string? matchedCondition = null) => new()
    {
        NextActionId = actionId,
        NextParticipantId = participantId,
        MatchedCondition = matchedCondition
    };

    /// <summary>
    /// Creates a routing result for rejection.
    /// </summary>
    public static RoutingResult Reject(string participantId) => new()
    {
        RejectedToParticipantId = participantId
    };
}
