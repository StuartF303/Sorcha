// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Demo.Models;

/// <summary>
/// Result from executing a single action in a blueprint workflow
/// </summary>
public class ActionExecutionResult
{
    /// <summary>
    /// Action index in the blueprint
    /// </summary>
    public int ActionIndex { get; set; }

    /// <summary>
    /// Action title
    /// </summary>
    public string ActionTitle { get; set; } = string.Empty;

    /// <summary>
    /// Participant who executed the action
    /// </summary>
    public string ParticipantId { get; set; } = string.Empty;

    /// <summary>
    /// Wallet address used
    /// </summary>
    public string WalletAddress { get; set; } = string.Empty;

    /// <summary>
    /// Whether execution was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Transaction hash (if submitted to Register)
    /// </summary>
    public string? TransactionHash { get; set; }

    /// <summary>
    /// Register ID where transaction was submitted
    /// </summary>
    public string? RegisterId { get; set; }

    /// <summary>
    /// Input data provided for the action
    /// </summary>
    public Dictionary<string, object> InputData { get; set; } = new();

    /// <summary>
    /// Output data from action execution
    /// </summary>
    public Dictionary<string, object> OutputData { get; set; } = new();

    /// <summary>
    /// Next action index determined by routing
    /// </summary>
    public int? NextActionIndex { get; set; }

    /// <summary>
    /// Next participant determined by routing
    /// </summary>
    public string? NextParticipant { get; set; }

    /// <summary>
    /// Whether this action completed the workflow
    /// </summary>
    public bool WorkflowComplete { get; set; }

    /// <summary>
    /// When the action was executed
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// Error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Time taken to execute (milliseconds)
    /// </summary>
    public long ExecutionTimeMs { get; set; }
}
