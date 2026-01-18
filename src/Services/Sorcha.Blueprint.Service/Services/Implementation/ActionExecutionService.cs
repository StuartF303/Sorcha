// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Diagnostics;
using System.Text.Json;
using Sorcha.ServiceClients.Wallet;
using Sorcha.ServiceClients.Register;
using Sorcha.Blueprint.Service.Models;
using Sorcha.Blueprint.Service.Models.Requests;
using Sorcha.Blueprint.Service.Models.Responses;
using Sorcha.Blueprint.Service.Services.Interfaces;
using Sorcha.Blueprint.Service.Storage;
using ActionModel = Sorcha.Blueprint.Models.Action;
using BlueprintModel = Sorcha.Blueprint.Models.Blueprint;

namespace Sorcha.Blueprint.Service.Services.Implementation;

/// <summary>
/// Orchestrates workflow action execution.
/// Coordinates state reconstruction, validation, routing, transaction building, and notifications.
/// </summary>
public class ActionExecutionService : IActionExecutionService
{
    private readonly IActionResolverService _actionResolver;
    private readonly IStateReconstructionService _stateReconstruction;
    private readonly ITransactionBuilderService _transactionBuilder;
    private readonly IRegisterServiceClient _registerClient;
    private readonly IWalletServiceClient _walletClient;
    private readonly INotificationService _notificationService;
    private readonly IInstanceStore _instanceStore;
    private readonly ILogger<ActionExecutionService> _logger;
    private static readonly ActivitySource ActivitySource = new("Sorcha.Blueprint.Service.ActionExecution");

    public ActionExecutionService(
        IActionResolverService actionResolver,
        IStateReconstructionService stateReconstruction,
        ITransactionBuilderService transactionBuilder,
        IRegisterServiceClient registerClient,
        IWalletServiceClient walletClient,
        INotificationService notificationService,
        IInstanceStore instanceStore,
        ILogger<ActionExecutionService> logger)
    {
        _actionResolver = actionResolver ?? throw new ArgumentNullException(nameof(actionResolver));
        _stateReconstruction = stateReconstruction ?? throw new ArgumentNullException(nameof(stateReconstruction));
        _transactionBuilder = transactionBuilder ?? throw new ArgumentNullException(nameof(transactionBuilder));
        _registerClient = registerClient ?? throw new ArgumentNullException(nameof(registerClient));
        _walletClient = walletClient ?? throw new ArgumentNullException(nameof(walletClient));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _instanceStore = instanceStore ?? throw new ArgumentNullException(nameof(instanceStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ActionSubmissionResponse> ExecuteAsync(
        string instanceId,
        int actionId,
        ActionSubmissionRequest request,
        string delegationToken,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("ExecuteAction");
        activity?.SetTag("instance.id", instanceId);
        activity?.SetTag("action.id", actionId);

        _logger.LogInformation("Executing action {ActionId} for instance {InstanceId}", actionId, instanceId);

        // 1. Get the instance
        var instance = await _instanceStore.GetAsync(instanceId, cancellationToken);
        if (instance == null)
        {
            throw new InvalidOperationException($"Instance {instanceId} not found");
        }

        // 2. Get the blueprint
        var blueprint = await _actionResolver.GetBlueprintAsync(instance.BlueprintId, cancellationToken);
        if (blueprint == null)
        {
            throw new InvalidOperationException($"Blueprint {instance.BlueprintId} not found");
        }

        // 3. Get the action definition
        var actionDef = _actionResolver.GetActionDefinition(blueprint, actionId.ToString());
        if (actionDef == null)
        {
            throw new InvalidOperationException($"Action {actionId} not found in blueprint {blueprint.Id}");
        }

        // 4. Validate the action can be executed (is it a current action?)
        if (!instance.CurrentActionIds.Contains(actionId) && !actionDef.IsStartingAction)
        {
            throw new InvalidOperationException($"Action {actionId} is not a current action for instance {instanceId}");
        }

        // 5. Reconstruct accumulated state from prior transactions
        var accumulatedState = await _stateReconstruction.ReconstructAsync(
            blueprint,
            instanceId,
            actionId,
            instance.RegisterId,
            delegationToken,
            instance.ParticipantWallets,
            cancellationToken);

        activity?.SetTag("state.action_count", accumulatedState.ActionCount);

        // 6. Validate input data against schema
        var validationResult = await ValidateActionDataAsync(actionDef, request.PayloadData, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 7. Evaluate routing conditions to determine next action(s)
        var routingResult = EvaluateRouting(actionDef, request.PayloadData, accumulatedState);

        // 8. Apply calculations
        var calculations = EvaluateCalculations(actionDef, request.PayloadData, accumulatedState);

        // 9. Apply disclosure rules for recipients
        var disclosedPayloads = ApplyDisclosures(actionDef, request.PayloadData, blueprint, instance.ParticipantWallets);

        // 10. Build transaction
        var transaction = await _transactionBuilder.BuildActionTransactionAsync(
            blueprint,
            instance,
            actionDef,
            request.PayloadData,
            disclosedPayloads,
            accumulatedState.PreviousTransactionId,
            cancellationToken);

        // 11. Sign transaction (using default signing key, no derivation path)
        var signatureBase64 = await _walletClient.SignTransactionAsync(
            request.SenderWallet,
            transaction.TransactionData,
            derivationPath: null, // Use wallet's default signing key
            cancellationToken);

        // Convert base64 signature to bytes for transaction model
        transaction.Signature = Convert.FromBase64String(signatureBase64);

        // 12. Submit to Register
        var submittedTx = await _registerClient.SubmitTransactionAsync(
            instance.RegisterId,
            transaction.ToTransactionModel(),
            cancellationToken);

        // 13. Update instance state
        instance = await UpdateInstanceAfterExecutionAsync(
            instance,
            actionId,
            submittedTx.TxId,
            routingResult,
            cancellationToken);

        // 14. Notify participants via SignalR
        await NotifyParticipantsAsync(instance, actionDef, routingResult, cancellationToken);

        // 15. Build response
        var response = new ActionSubmissionResponse
        {
            TransactionId = submittedTx.TxId,
            InstanceId = instanceId,
            NextActions = routingResult.NextActions.Select(na => new NextActionResponse
            {
                ActionId = na.ActionId,
                ActionTitle = na.ActionTitle,
                ParticipantId = na.ParticipantId,
                BranchId = na.BranchId
            }).ToList(),
            Calculations = calculations,
            IsComplete = routingResult.NextActions.Count == 0,
            Warnings = validationResult.Warnings
        };

        _logger.LogInformation(
            "Action {ActionId} executed successfully for instance {InstanceId}. Transaction: {TxId}, Complete: {IsComplete}",
            actionId, instanceId, submittedTx.TxId, response.IsComplete);

        return response;
    }

    /// <inheritdoc/>
    public async Task<ActionRejectionResponse> RejectAsync(
        string instanceId,
        int actionId,
        ActionRejectionRequest request,
        string delegationToken,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("RejectAction");
        activity?.SetTag("instance.id", instanceId);
        activity?.SetTag("action.id", actionId);

        _logger.LogInformation("Rejecting action {ActionId} for instance {InstanceId}", actionId, instanceId);

        // 1. Get the instance
        var instance = await _instanceStore.GetAsync(instanceId, cancellationToken);
        if (instance == null)
        {
            throw new InvalidOperationException($"Instance {instanceId} not found");
        }

        // 2. Get the blueprint
        var blueprint = await _actionResolver.GetBlueprintAsync(instance.BlueprintId, cancellationToken);
        if (blueprint == null)
        {
            throw new InvalidOperationException($"Blueprint {instance.BlueprintId} not found");
        }

        // 3. Get the action definition
        var actionDef = _actionResolver.GetActionDefinition(blueprint, actionId.ToString());
        if (actionDef == null)
        {
            throw new InvalidOperationException($"Action {actionId} not found in blueprint {blueprint.Id}");
        }

        // 4. Validate rejection is allowed for this action
        if (actionDef.RejectionConfig == null)
        {
            throw new InvalidOperationException($"Action {actionId} does not allow rejection");
        }

        // 5. Validate reason if required
        if (actionDef.RejectionConfig.RequireReason && string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ValidationException("Rejection reason is required for this action");
        }

        // 6. Get the target action
        var targetAction = _actionResolver.GetActionDefinition(blueprint, actionDef.RejectionConfig.TargetActionId.ToString());
        if (targetAction == null)
        {
            throw new InvalidOperationException($"Rejection target action {actionDef.RejectionConfig.TargetActionId} not found");
        }

        // 7. Build rejection transaction
        var rejectionData = new Dictionary<string, object>
        {
            ["rejectionReason"] = request.Reason,
            ["rejectedActionId"] = actionId,
            ["fieldErrors"] = request.FieldErrors ?? new Dictionary<string, string>()
        };

        var transaction = await _transactionBuilder.BuildRejectionTransactionAsync(
            blueprint,
            instance,
            actionDef,
            rejectionData,
            instance.LastTransactionId,
            cancellationToken);

        // 8. Submit to Register
        var submittedTx = await _registerClient.SubmitTransactionAsync(
            instance.RegisterId,
            transaction.ToTransactionModel(),
            cancellationToken);

        // 9. Update instance state
        if (actionDef.RejectionConfig.IsTerminal)
        {
            instance.State = InstanceState.Rejected;
            instance.CompletedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            // Route to target action
            instance.CurrentActionIds = [actionDef.RejectionConfig.TargetActionId];
        }
        instance.LastTransactionId = submittedTx.TxId;
        instance = await _instanceStore.UpdateAsync(instance, cancellationToken);

        // 10. Notify participants
        var targetParticipantId = actionDef.RejectionConfig.TargetParticipantId ?? targetAction.Sender;
        await _notificationService.NotifyActionRejectedAsync(
            instanceId,
            actionId,
            targetAction.Id,
            targetParticipantId,
            request.Reason,
            cancellationToken);

        return new ActionRejectionResponse
        {
            TransactionId = submittedTx.TxId,
            InstanceId = instanceId,
            TargetAction = new TargetActionResponse
            {
                ActionId = targetAction.Id,
                ActionTitle = targetAction.Title,
                ParticipantId = targetParticipantId
            }
        };
    }

    private async Task<ValidationResult> ValidateActionDataAsync(
        ActionModel action,
        Dictionary<string, object> data,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Validate against JSON schemas if present
        if (action.DataSchemas?.Any() == true)
        {
            foreach (var schema in action.DataSchemas)
            {
                // Schema validation would be done here using JsonSchema.Net
                // For now, we'll do basic validation
            }
        }

        // Check required action data
        if (action.RequiredActionData?.Any() == true)
        {
            foreach (var required in action.RequiredActionData)
            {
                if (!data.ContainsKey(required))
                {
                    errors.Add($"Required field '{required}' is missing");
                }
            }
        }

        await Task.CompletedTask; // For async signature

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    private RoutingResult EvaluateRouting(
        ActionModel action,
        Dictionary<string, object> data,
        AccumulatedState state)
    {
        var nextActions = new List<NextAction>();

        // Evaluate routes in order
        if (action.Routes != null)
        {
            var flattenedData = state.GetFlattenedData();
            // Merge current action data
            foreach (var kvp in data)
            {
                flattenedData[kvp.Key] = kvp.Value;
            }

            foreach (var route in action.Routes)
            {
                var conditionMatches = false;

                if (route.Condition == null || route.IsDefault)
                {
                    conditionMatches = route.IsDefault;
                }
                else
                {
                    // Evaluate JSON Logic condition
                    // Using JsonLogic.NET or similar library
                    conditionMatches = EvaluateJsonLogicCondition(route.Condition.ToString(), flattenedData!);
                }

                if (conditionMatches)
                {
                    foreach (var nextActionId in route.NextActionIds ?? [])
                    {
                        nextActions.Add(new NextAction
                        {
                            ActionId = nextActionId,
                            ActionTitle = "", // Would be filled from blueprint
                            ParticipantId = "", // Would be filled from blueprint
                            BranchId = route.NextActionIds?.Count() > 1 ? Guid.NewGuid().ToString() : null
                        });
                    }
                    break; // First matching route wins
                }
            }
        }
        else if (action.Condition != null)
        {
            // Legacy condition-based routing
            var conditionResult = EvaluateJsonLogicCondition(action.Condition.ToString(), data);
            if (conditionResult && int.TryParse(action.Condition.ToString(), out var nextId))
            {
                nextActions.Add(new NextAction
                {
                    ActionId = nextId,
                    ActionTitle = "",
                    ParticipantId = ""
                });
            }
        }

        return new RoutingResult
        {
            NextActions = nextActions,
            IsParallel = nextActions.Count > 1
        };
    }

    private bool EvaluateJsonLogicCondition(string condition, Dictionary<string, object> data)
    {
        // Simplified JSON Logic evaluation
        // In production, use JsonLogic.NET library
        try
        {
            // Basic equality check for simple conditions
            if (condition.Contains("\"==\""))
            {
                // Parse the condition
                // For now, return true as a placeholder
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private Dictionary<string, object>? EvaluateCalculations(
        ActionModel action,
        Dictionary<string, object> data,
        AccumulatedState state)
    {
        if (action.Calculations == null || action.Calculations.Count == 0)
        {
            return null;
        }

        var results = new Dictionary<string, object>();
        var flattenedData = state.GetFlattenedData();
        foreach (var kvp in data)
        {
            flattenedData[kvp.Key] = kvp.Value;
        }

        foreach (var (fieldName, expression) in action.Calculations)
        {
            try
            {
                // Evaluate JSON Logic expression
                // For now, store the expression as-is
                results[fieldName] = expression.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to evaluate calculation {FieldName}", fieldName);
            }
        }

        return results.Count > 0 ? results : null;
    }

    private Dictionary<string, Dictionary<string, object>> ApplyDisclosures(
        ActionModel action,
        Dictionary<string, object> data,
        BlueprintModel blueprint,
        Dictionary<string, string> participantWallets)
    {
        var disclosedPayloads = new Dictionary<string, Dictionary<string, object>>();

        foreach (var disclosure in action.Disclosures)
        {
            // ParticipantAddress can be either a direct wallet address or a participant ID
            var recipientAddress = disclosure.ParticipantAddress;

            // Try to resolve as participant ID first
            if (participantWallets.TryGetValue(recipientAddress, out var walletAddress))
            {
                recipientAddress = walletAddress;
            }

            if (string.IsNullOrEmpty(recipientAddress))
            {
                _logger.LogWarning("No wallet address for disclosure recipient {ParticipantAddress}", disclosure.ParticipantAddress);
                continue;
            }

            var disclosedData = new Dictionary<string, object>();

            foreach (var path in disclosure.DataPointers)
            {
                if (path == "/*" || path == "/")
                {
                    // Disclose all data
                    foreach (var kvp in data)
                    {
                        disclosedData[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    // Disclose specific path
                    var fieldName = path.TrimStart('/');
                    if (data.TryGetValue(fieldName, out var value))
                    {
                        disclosedData[fieldName] = value;
                    }
                }
            }

            disclosedPayloads[recipientAddress] = disclosedData;
        }

        return disclosedPayloads;
    }

    private async Task<Instance> UpdateInstanceAfterExecutionAsync(
        Instance instance,
        int completedActionId,
        string transactionId,
        RoutingResult routingResult,
        CancellationToken cancellationToken)
    {
        // Remove completed action from current actions
        instance.CurrentActionIds.Remove(completedActionId);

        // Add next actions
        foreach (var nextAction in routingResult.NextActions)
        {
            if (!instance.CurrentActionIds.Contains(nextAction.ActionId))
            {
                instance.CurrentActionIds.Add(nextAction.ActionId);
            }

            // Track parallel branches
            if (!string.IsNullOrEmpty(nextAction.BranchId))
            {
                instance.ActiveBranches.Add(new Branch
                {
                    Id = nextAction.BranchId,
                    CurrentActionId = nextAction.ActionId,
                    State = BranchState.Active
                });
            }
        }

        // Update transaction tracking
        instance.LastTransactionId = transactionId;
        instance.CompletedActionCount++;

        if (instance.FirstTransactionId == null)
        {
            instance.FirstTransactionId = transactionId;
        }

        // Check if workflow is complete
        if (instance.CurrentActionIds.Count == 0)
        {
            instance.State = InstanceState.Completed;
            instance.CompletedAt = DateTimeOffset.UtcNow;
        }

        return await _instanceStore.UpdateAsync(instance, cancellationToken);
    }

    private async Task NotifyParticipantsAsync(
        Instance instance,
        ActionModel completedAction,
        RoutingResult routingResult,
        CancellationToken cancellationToken)
    {
        foreach (var nextAction in routingResult.NextActions)
        {
            await _notificationService.NotifyActionAvailableAsync(
                instance.Id,
                nextAction.ActionId,
                nextAction.ActionTitle,
                nextAction.ParticipantId,
                cancellationToken);
        }

        if (routingResult.NextActions.Count == 0)
        {
            await _notificationService.NotifyWorkflowCompletedAsync(
                instance.Id,
                cancellationToken);
        }
    }
}

/// <summary>
/// Result of action data validation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
}

/// <summary>
/// Result of routing evaluation
/// </summary>
public class RoutingResult
{
    public List<NextAction> NextActions { get; init; } = [];
    public bool IsParallel { get; init; }
}

/// <summary>
/// Exception for validation errors
/// </summary>
public class ValidationException : Exception
{
    public List<string> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = [message];
    }

    public ValidationException(List<string> errors) : base(string.Join("; ", errors))
    {
        Errors = errors;
    }
}
