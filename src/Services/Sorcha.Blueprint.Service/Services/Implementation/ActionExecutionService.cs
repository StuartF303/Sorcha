// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Diagnostics;
using System.Text.Json;
using Sorcha.ServiceClients.Wallet;
using Sorcha.ServiceClients.Register;
using Sorcha.Blueprint.Engine.Interfaces;
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
    private readonly IExecutionEngine _executionEngine;
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
        IExecutionEngine executionEngine,
        ILogger<ActionExecutionService> logger)
    {
        _actionResolver = actionResolver ?? throw new ArgumentNullException(nameof(actionResolver));
        _stateReconstruction = stateReconstruction ?? throw new ArgumentNullException(nameof(stateReconstruction));
        _transactionBuilder = transactionBuilder ?? throw new ArgumentNullException(nameof(transactionBuilder));
        _registerClient = registerClient ?? throw new ArgumentNullException(nameof(registerClient));
        _walletClient = walletClient ?? throw new ArgumentNullException(nameof(walletClient));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _instanceStore = instanceStore ?? throw new ArgumentNullException(nameof(instanceStore));
        _executionEngine = executionEngine ?? throw new ArgumentNullException(nameof(executionEngine));
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

        // 7. Merge accumulated state with current data for routing and calculations
        var mergedData = accumulatedState.GetFlattenedData()
            .Where(kvp => kvp.Value != null)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value!);
        foreach (var kvp in request.PayloadData)
        {
            mergedData[kvp.Key] = kvp.Value;
        }

        // 8. Evaluate routing conditions to determine next action(s)
        var routingResult = await EvaluateRoutingAsync(blueprint, actionDef, mergedData, cancellationToken);

        // 9. Apply calculations
        var calculations = await EvaluateCalculationsAsync(actionDef, mergedData, cancellationToken);

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
        var signResult = await _walletClient.SignTransactionAsync(
            request.SenderWallet,
            transaction.TransactionData,
            derivationPath: null, // Use wallet's default signing key
            isPreHashed: false,
            cancellationToken);

        // Use raw signature bytes from wallet sign result
        transaction.Signature = signResult.Signature;

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
        // Delegate to the Blueprint Engine for full JSON Schema validation
        if (action.DataSchemas?.Any() == true)
        {
            var engineResult = await _executionEngine.ValidateAsync(data, action, cancellationToken);
            return new ValidationResult
            {
                IsValid = engineResult.IsValid,
                Errors = engineResult.Errors.Select(e => e.Message).ToList(),
                Warnings = []
            };
        }

        // Fallback: field-presence check when no schemas are defined
        var errors = new List<string>();
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

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = []
        };
    }

    private async Task<RoutingResult> EvaluateRoutingAsync(
        BlueprintModel blueprint,
        ActionModel action,
        Dictionary<string, object> mergedData,
        CancellationToken cancellationToken)
    {
        // Delegate to the Blueprint Engine for JSON Logic routing
        var engineResult = await _executionEngine.DetermineRoutingAsync(
            blueprint, action, mergedData, cancellationToken);

        // Map engine RoutedActions to service NextActions
        var nextActions = new List<NextAction>();

        foreach (var routedAction in engineResult.NextActions)
        {
            // Resolve action title from blueprint
            var targetActionDef = blueprint.Actions?.FirstOrDefault(
                a => a.Id.ToString() == routedAction.ActionId);

            nextActions.Add(new NextAction
            {
                ActionId = int.TryParse(routedAction.ActionId, out var id) ? id : 0,
                ActionTitle = targetActionDef?.Title ?? "",
                ParticipantId = routedAction.ParticipantId ?? targetActionDef?.Sender ?? "",
                BranchId = routedAction.BranchId
            });
        }

        return new RoutingResult
        {
            NextActions = nextActions,
            IsParallel = engineResult.IsParallel
        };
    }

    private async Task<Dictionary<string, object>?> EvaluateCalculationsAsync(
        ActionModel action,
        Dictionary<string, object> mergedData,
        CancellationToken cancellationToken)
    {
        if (action.Calculations == null || action.Calculations.Count == 0)
        {
            return null;
        }

        try
        {
            // Delegate to the Blueprint Engine for JSON Logic calculation evaluation
            var result = await _executionEngine.ApplyCalculationsAsync(mergedData, action, cancellationToken);

            // Return only the calculated fields (those defined in action.Calculations)
            var calculations = new Dictionary<string, object>();
            foreach (var fieldName in action.Calculations.Keys)
            {
                if (result.TryGetValue(fieldName, out var value))
                {
                    calculations[fieldName] = value;
                }
            }

            return calculations.Count > 0 ? calculations : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to evaluate calculations for action {ActionId}", action.Id);
            return null;
        }
    }

    private Dictionary<string, Dictionary<string, object>> ApplyDisclosures(
        ActionModel action,
        Dictionary<string, object> data,
        BlueprintModel blueprint,
        Dictionary<string, string> participantWallets)
    {
        // Delegate to the Blueprint Engine for JSON Pointer disclosure filtering
        var engineResults = _executionEngine.ApplyDisclosures(data, action);

        var disclosedPayloads = new Dictionary<string, Dictionary<string, object>>();

        foreach (var result in engineResults)
        {
            // Resolve participant ID to wallet address
            var recipientAddress = result.ParticipantId;
            if (participantWallets.TryGetValue(recipientAddress, out var walletAddress))
            {
                recipientAddress = walletAddress;
            }

            if (string.IsNullOrEmpty(recipientAddress))
            {
                _logger.LogWarning("No wallet address for disclosure recipient {ParticipantId}", result.ParticipantId);
                continue;
            }

            disclosedPayloads[recipientAddress] = result.DisclosedData;
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
