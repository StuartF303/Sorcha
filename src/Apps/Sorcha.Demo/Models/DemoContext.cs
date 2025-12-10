// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Blueprint.Models;

namespace Sorcha.Demo.Models;

/// <summary>
/// Manages the state and configuration for the demo application
/// </summary>
public class DemoContext
{
    /// <summary>
    /// Settings for demo behavior
    /// </summary>
    public DemoSettings Settings { get; } = new();

    /// <summary>
    /// Register ID where blueprint transactions will be stored
    /// </summary>
    public string RegisterId { get; set; } = "demo-register";

    /// <summary>
    /// Maps participant names to their wallet information
    /// </summary>
    public Dictionary<string, ParticipantContext> Participants { get; } = new();

    /// <summary>
    /// Current blueprint being executed
    /// </summary>
    public Sorcha.Blueprint.Models.Blueprint? CurrentBlueprint { get; set; }

    /// <summary>
    /// Current blueprint instance ID (from Blueprint Service)
    /// </summary>
    public string? CurrentInstanceId { get; set; }

    /// <summary>
    /// Execution history for the current workflow
    /// </summary>
    public List<ActionExecutionResult> ExecutionHistory { get; } = new();

    /// <summary>
    /// Workflow state accumulated across actions
    /// </summary>
    public Dictionary<string, object> WorkflowState { get; } = new();

    /// <summary>
    /// Whether the workflow is complete
    /// </summary>
    public bool IsWorkflowComplete { get; set; }

    /// <summary>
    /// Resets the context for a new demo run (keeps wallets and settings)
    /// </summary>
    public void Reset()
    {
        CurrentBlueprint = null;
        CurrentInstanceId = null;
        ExecutionHistory.Clear();
        WorkflowState.Clear();
        IsWorkflowComplete = false;
    }

    /// <summary>
    /// Clears all wallet data
    /// </summary>
    public void ClearWallets()
    {
        Participants.Clear();
    }

    /// <summary>
    /// Gets wallet address for a participant
    /// </summary>
    public string? GetWalletAddress(string participantId)
    {
        return Participants.TryGetValue(participantId, out var participant)
            ? participant.WalletAddress
            : null;
    }

    /// <summary>
    /// Gets all participant IDs
    /// </summary>
    public IEnumerable<string> GetParticipantIds() => Participants.Keys;
}

/// <summary>
/// Settings for demo behavior
/// </summary>
public class DemoSettings
{
    /// <summary>
    /// When true, pauses after each action for user review
    /// </summary>
    public bool StepByStepMode { get; set; } = true;

    /// <summary>
    /// When true, shows detailed API calls and responses
    /// </summary>
    public bool VerboseMode { get; set; } = false;

    /// <summary>
    /// When true, shows JSON Schema validation details
    /// </summary>
    public bool ShowValidation { get; set; } = true;

    /// <summary>
    /// When true, shows calculation details
    /// </summary>
    public bool ShowCalculations { get; set; } = true;

    /// <summary>
    /// When true, shows routing decision logic
    /// </summary>
    public bool ShowRouting { get; set; } = true;

    /// <summary>
    /// When true, shows selective disclosure per participant
    /// </summary>
    public bool ShowDisclosure { get; set; } = true;
}
