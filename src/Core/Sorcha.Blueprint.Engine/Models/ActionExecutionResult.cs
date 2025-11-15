// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Blueprint.Engine.Models;

/// <summary>
/// Result of executing a blueprint action.
/// </summary>
/// <remarks>
/// Contains the complete result of action execution including:
/// - Validation results
/// - Processed data (original + calculated fields)
/// - Routing decision (next participant)
/// - Disclosure results (filtered data for each participant)
/// - Any errors or warnings
/// 
/// This is the primary output from IExecutionEngine.ExecuteActionAsync()
/// </remarks>
public class ActionExecutionResult
{
    /// <summary>
    /// Overall success status of the action execution.
    /// </summary>
    /// <remarks>
    /// False if validation fails or any critical error occurs.
    /// True if the action was processed successfully.
    /// </remarks>
    public bool Success { get; set; }

    /// <summary>
    /// Validation result containing success/failure and detailed errors.
    /// </summary>
    public ValidationResult Validation { get; set; } = new();

    /// <summary>
    /// The processed action data (original data merged with calculated values).
    /// </summary>
    /// <remarks>
    /// This contains:
    /// - All original fields from ActionData
    /// - All calculated fields added by JSON Logic calculations
    /// 
    /// Only populated if validation succeeds.
    /// </remarks>
    public Dictionary<string, object> ProcessedData { get; set; } = new();

    /// <summary>
    /// Calculated values that were added to the data.
    /// </summary>
    /// <remarks>
    /// This is a subset of ProcessedData containing only the fields
    /// that were calculated (not submitted by the user).
    /// Useful for debugging and audit trails.
    /// </remarks>
    public Dictionary<string, object> CalculatedValues { get; set; } = new();

    /// <summary>
    /// Routing decision indicating the next action and participant.
    /// </summary>
    /// <remarks>
    /// Only populated if validation and calculations succeed.
    /// Determines where the workflow goes next.
    /// </remarks>
    public RoutingResult Routing { get; set; } = new();

    /// <summary>
    /// Disclosure results containing filtered data for each participant.
    /// </summary>
    /// <remarks>
    /// Each participant receives a filtered view of the data
    /// based on their disclosure rules. This enables privacy-preserving
    /// workflows where different participants see different data.
    /// 
    /// Only populated if validation and calculations succeed.
    /// </remarks>
    public List<DisclosureResult> Disclosures { get; set; } = new();

    /// <summary>
    /// Critical errors that prevented execution.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// - "Blueprint not found"
    /// - "Action not found in blueprint"
    /// - "Invalid participant"
    /// 
    /// If any errors exist, Success will be false.
    /// </remarks>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Non-critical warnings about the execution.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// - "Calculation produced null value"
    /// - "No routing conditions matched (workflow complete)"
    /// 
    /// Warnings don't prevent execution but should be reviewed.
    /// </remarks>
    public List<string> Warnings { get; set; } = new();
}
