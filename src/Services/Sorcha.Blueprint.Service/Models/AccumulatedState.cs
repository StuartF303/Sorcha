// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;

namespace Sorcha.Blueprint.Service.Models;

/// <summary>
/// Represents the accumulated state from prior workflow transactions.
/// This is a runtime object used for state reconstruction during action execution.
/// It is not persisted - state is always reconstructed from transactions in the register.
/// </summary>
public record AccumulatedState
{
    /// <summary>
    /// Decrypted data from each action, keyed by action ID.
    /// Contains the payload data from prior transactions in the workflow.
    /// </summary>
    public Dictionary<string, JsonElement> ActionData { get; init; } = new();

    /// <summary>
    /// The ID of the most recent transaction in the workflow chain.
    /// Used as the PreviousTxId when building the next transaction.
    /// </summary>
    public string? PreviousTransactionId { get; init; }

    /// <summary>
    /// Number of actions completed in this workflow instance.
    /// </summary>
    public int ActionCount { get; init; }

    /// <summary>
    /// Active branch states for parallel workflows.
    /// Key is branch ID, value is the current state of that branch.
    /// </summary>
    public Dictionary<string, BranchState> BranchStates { get; init; } = new();

    /// <summary>
    /// Flattened view of all accumulated data for JSON Logic evaluation.
    /// Merges all action data into a single dictionary for routing conditions.
    /// </summary>
    public Dictionary<string, object?> GetFlattenedData()
    {
        var result = new Dictionary<string, object?>();

        foreach (var (actionId, data) in ActionData)
        {
            // Add data with action prefix for scoped access
            result[$"action_{actionId}"] = data;

            // Also add properties at root level for simple access
            if (data.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in data.EnumerateObject())
                {
                    // Later actions override earlier ones at root level
                    result[property.Name] = property.Value;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the data for a specific action.
    /// </summary>
    /// <param name="actionId">The action ID</param>
    /// <returns>The action data, or null if not found</returns>
    public JsonElement? GetActionData(string actionId)
    {
        return ActionData.TryGetValue(actionId, out var data) ? data : null;
    }

    /// <summary>
    /// Gets the data for a specific action ID (integer overload).
    /// </summary>
    /// <param name="actionId">The action ID</param>
    /// <returns>The action data, or null if not found</returns>
    public JsonElement? GetActionData(int actionId)
    {
        return GetActionData(actionId.ToString());
    }
}

/// <summary>
/// State of a parallel execution branch
/// </summary>
public enum BranchState
{
    /// <summary>Branch is currently executing</summary>
    Active,

    /// <summary>Branch has completed successfully</summary>
    Completed,

    /// <summary>Branch timed out before completion</summary>
    TimedOut,

    /// <summary>Branch was rejected</summary>
    Rejected
}
