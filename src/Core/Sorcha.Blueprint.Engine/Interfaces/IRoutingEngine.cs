// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Blueprint.Engine.Interfaces;

/// <summary>
/// Routing engine that determines the next action and participant in a workflow.
/// </summary>
/// <remarks>
/// Uses conditional routing based on JSON Logic expressions to determine
/// which participant should perform the next action.
/// 
/// Routing flow:
/// 1. Evaluate the current action's routing conditions in order
/// 2. First condition that evaluates to true determines the next participant
/// 3. Find the next action for that participant in the blueprint
/// 4. Return routing decision
/// 
/// Special cases:
/// - If no conditions match, the workflow is complete (terminal action)
/// - If a condition matches but no action exists for that participant, error
/// - If the next action is the same as current, it's a loop (usually an error)
/// 
/// Example routing conditions:
/// [
///   { "participantId": "manager", "condition": { "&gt;": [{"var": "amount"}, 10000] } },
///   { "participantId": "clerk", "condition": true }  // default/fallback
/// ]
/// </remarks>
public interface IRoutingEngine
{
    /// <summary>
    /// Determine the next action and participant based on routing conditions.
    /// </summary>
    /// <param name="blueprint">The blueprint definition containing all actions.</param>
    /// <param name="currentAction">The action that was just completed.</param>
    /// <param name="data">The action data used to evaluate conditions.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A routing result containing:
    /// - The next action to perform (or null if workflow is complete)
    /// - The participant who should perform it
    /// - The condition that matched (for audit purposes)
    /// </returns>
    /// <remarks>
    /// This method evaluates routing conditions using the IJsonLogicEvaluator.
    /// Conditions are evaluated in the order they appear in the action definition.
    /// 
    /// The routing result can indicate:
    /// - Success: Next action and participant determined
    /// - Complete: No conditions matched (workflow finished)
    /// - Error: Condition matched but next action not found
    /// </remarks>
    Task<RoutingResult> DetermineNextAsync(
        Blueprint blueprint,
        Models.Action currentAction,
        Dictionary<string, object> data,
        CancellationToken ct = default);
}
