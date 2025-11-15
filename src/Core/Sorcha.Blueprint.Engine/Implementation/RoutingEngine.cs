// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Blueprint.Engine.Interfaces;
using Sorcha.Blueprint.Engine.Models;

namespace Sorcha.Blueprint.Engine.Implementation;

/// <summary>
/// Routing engine that determines the next action and participant in a workflow.
/// </summary>
/// <remarks>
/// Uses conditional routing based on JSON Logic expressions to determine
/// which participant should perform the next action.
/// 
/// Thread-safe and can be used concurrently.
/// </remarks>
public class RoutingEngine : IRoutingEngine
{
    private readonly IJsonLogicEvaluator _evaluator;

    public RoutingEngine(IJsonLogicEvaluator evaluator)
    {
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    /// <summary>
    /// Determine the next action and participant based on routing conditions.
    /// </summary>
    public async Task<RoutingResult> DetermineNextAsync(
        Blueprint blueprint,
        Models.Action currentAction,
        Dictionary<string, object> data,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(currentAction);
        ArgumentNullException.ThrowIfNull(data);

        // Use the action's Participants conditions for routing
        var conditions = currentAction.Participants?.ToList() ?? new List<Condition>();

        if (!conditions.Any())
        {
            // No routing conditions means workflow is complete
            return RoutingResult.Complete();
        }

        // Evaluate conditions to find the next participant
        var nextParticipantId = await _evaluator.EvaluateConditionsAsync(data, conditions, ct);

        if (nextParticipantId == null)
        {
            // No conditions matched - workflow is complete
            return RoutingResult.Complete();
        }

        // Find the next action for this participant in the blueprint
        var nextAction = FindNextActionForParticipant(blueprint, currentAction, nextParticipantId);

        if (nextAction == null)
        {
            // Participant matched but no action found
            // This could indicate end of workflow for this participant
            return RoutingResult.Complete();
        }

        // Create the matched condition string for audit
        var matchedCondition = conditions
            .FirstOrDefault(c => c.Principal == nextParticipantId)?.Criteria
            .FirstOrDefault();

        return RoutingResult.Next(
            nextAction.Id.ToString(),
            nextParticipantId,
            matchedCondition
        );
    }

    /// <summary>
    /// Finds the next action for a participant in the blueprint.
    /// </summary>
    /// <remarks>
    /// The next action is typically the action with an ID greater than
    /// the current action, but this is a simplified implementation.
    /// In a real system, you might have explicit routing references.
    /// </remarks>
    private static Models.Action? FindNextActionForParticipant(
        Blueprint blueprint,
        Models.Action currentAction,
        string participantId)
    {
        // Get all actions after the current one
        var currentIndex = blueprint.Actions.FindIndex(a => a.Id == currentAction.Id);
        
        if (currentIndex < 0)
        {
            // Current action not found in blueprint
            return null;
        }

        // Look for the next action that this participant can perform
        // This is a simple linear search - production systems might use
        // explicit action references or more complex routing logic
        for (int i = currentIndex + 1; i < blueprint.Actions.Count; i++)
        {
            var action = blueprint.Actions[i];
            
            // Check if this action can be performed by the participant
            // by looking at the action's sender or participants
            if (action.Sender == participantId)
            {
                return action;
            }

            // Also check if the participant is in the action's participants list
            if (action.Participants?.Any(p => p.Principal == participantId) == true)
            {
                return action;
            }
        }

        // No next action found for this participant
        return null;
    }
}
