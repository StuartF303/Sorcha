using Sorcha.TransactionHandler.Interfaces;
using System.Collections.Generic;

namespace Sorcha.Cli.Demo;

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
    /// Maps participant names to their wallet addresses
    /// </summary>
    public Dictionary<string, string> ParticipantWallets { get; } = new();

    /// <summary>
    /// Maps participant names to their mnemonics (INSECURE - demo only!)
    /// </summary>
    public Dictionary<string, string> ParticipantMnemonics { get; } = new();

    /// <summary>
    /// Transaction chain built during blueprint execution
    /// </summary>
    public List<ITransaction> TransactionChain { get; } = new();

    /// <summary>
    /// Current blueprint being executed
    /// </summary>
    public Sorcha.Blueprint.Models.Blueprint? CurrentBlueprint { get; set; }

    /// <summary>
    /// Execution data for each action (action index -> execution result data)
    /// </summary>
    public Dictionary<int, ActionExecutionData> ExecutionData { get; } = new();

    /// <summary>
    /// Resets the context for a new demo run
    /// </summary>
    public void Reset()
    {
        TransactionChain.Clear();
        ExecutionData.Clear();
        CurrentBlueprint = null;

        // Keep wallets and settings between runs
    }

    /// <summary>
    /// Clears all wallet data (used when "clear wallets" setting is triggered)
    /// </summary>
    public void ClearWallets()
    {
        ParticipantWallets.Clear();
        ParticipantMnemonics.Clear();
    }
}

/// <summary>
/// Settings for demo behavior
/// </summary>
public class DemoSettings
{
    /// <summary>
    /// When true, shows detailed API calls and responses
    /// </summary>
    public bool VerboseMode { get; set; } = false;

    /// <summary>
    /// When true, shows step-by-step execution with pauses
    /// </summary>
    public bool StepByStepMode { get; set; } = true;

    /// <summary>
    /// When true, shows JSON Schema validation details
    /// </summary>
    public bool ShowValidation { get; set; } = true;

    /// <summary>
    /// When true, shows calculation details (before/after values)
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

/// <summary>
/// Data collected during action execution
/// </summary>
public class ActionExecutionData
{
    public int ActionIndex { get; set; }
    public string ActionTitle { get; set; } = string.Empty;
    public string Participant { get; set; } = string.Empty;
    public object? InputData { get; set; }
    public object? OutputData { get; set; }
    public Dictionary<string, object>? CalculationsBefore { get; set; }
    public Dictionary<string, object>? CalculationsAfter { get; set; }
    public string? RoutingDecision { get; set; }
    public Dictionary<string, object>? DisclosureData { get; set; }
    public ITransaction? Transaction { get; set; }
}
