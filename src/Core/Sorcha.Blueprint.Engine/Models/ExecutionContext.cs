// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Blueprint.Models;

namespace Sorcha.Blueprint.Engine.Models;

/// <summary>
/// Execution context containing all information needed to execute a blueprint action.
/// </summary>
/// <remarks>
/// The execution context is passed to the execution engine and contains:
/// - Blueprint definition (the workflow template)
/// - Current action details (which step is being executed)
/// - Action data (submitted by the participant)
/// - Historical context (previous action data)
/// - Participant information (who is executing)
/// - Execution mode (validation-only or full execution)
/// 
/// This context is immutable once created and can be safely shared
/// across multiple concurrent executions.
/// </remarks>
public class ExecutionContext
{
    /// <summary>
    /// The blueprint definition containing all actions and workflow rules.
    /// </summary>
    public required Blueprint Blueprint { get; init; }

    /// <summary>
    /// The current action being executed within the blueprint.
    /// </summary>
    public required Sorcha.Blueprint.Models.Action Action { get; init; }

    /// <summary>
    /// The action data submitted by the participant.
    /// </summary>
    /// <remarks>
    /// This is the primary input data that will be:
    /// 1. Validated against the action's JSON Schema
    /// 2. Used as input for JSON Logic calculations
    /// 3. Evaluated against routing conditions
    /// 4. Filtered through disclosure rules
    /// </remarks>
    public required Dictionary<string, object> ActionData { get; init; }

    /// <summary>
    /// Aggregated data from previous actions in the workflow instance.
    /// </summary>
    /// <remarks>
    /// This allows actions to reference data from earlier steps.
    /// For example, an approval action might reference the amount
    /// from the initial request action.
    /// 
    /// Null for the first action in a workflow instance.
    /// </remarks>
    public Dictionary<string, object>? PreviousData { get; init; }

    /// <summary>
    /// Hash or ID of the previous transaction in the workflow.
    /// </summary>
    /// <remarks>
    /// Used to link actions together in the blockchain.
    /// Null for the first action (workflow start).
    /// </remarks>
    public string? PreviousTransactionHash { get; init; }

    /// <summary>
    /// Unique identifier for this workflow instance.
    /// </summary>
    /// <remarks>
    /// All actions in the same workflow instance share this ID.
    /// Used for tracking and querying workflow progress.
    /// </remarks>
    public string? InstanceId { get; init; }

    /// <summary>
    /// The participant ID (DID) of the user executing this action.
    /// </summary>
    public required string ParticipantId { get; init; }

    /// <summary>
    /// The wallet address of the participant executing this action.
    /// </summary>
    /// <remarks>
    /// This may be a stealth/derived address for privacy.
    /// Used for signing transactions and encryption.
    /// </remarks>
    public required string WalletAddress { get; init; }

    /// <summary>
    /// Execution mode determining whether to perform full execution or validation only.
    /// </summary>
    /// <remarks>
    /// - ValidationOnly: Used client-side for instant feedback
    /// - Full: Used server-side for complete workflow execution
    /// </remarks>
    public ExecutionMode Mode { get; init; } = ExecutionMode.Full;
}

/// <summary>
/// Execution mode for the blueprint engine.
/// </summary>
public enum ExecutionMode
{
    /// <summary>
    /// Validation-only mode: validates data but doesn't create transactions.
    /// Used client-side in Blazor WASM for instant feedback.
    /// </summary>
    ValidationOnly,

    /// <summary>
    /// Full execution mode: validates, calculates, routes, and prepares transactions.
    /// Used server-side in Blueprint Service.
    /// </summary>
    Full
}
