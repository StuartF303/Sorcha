// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Validator.Core.Models;

namespace Sorcha.Validator.Core.Validators;

/// <summary>
/// Stateless chain validator that can run in secure enclaves.
/// Validates transaction chain integrity including previousId references,
/// blueprint versioning, and instance tracking.
/// </summary>
public class ChainValidatorCore : IChainValidator
{
    /// <inheritdoc/>
    public ValidationResult ValidateChainLink(
        TransactionChainData transaction,
        TransactionChainData? previousTransaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var errors = new List<ValidationError>();

        // Determine what type of transaction this is
        var txType = DetermineTransactionType(transaction, previousTransaction);

        switch (txType)
        {
            case ChainTransactionType.Genesis:
                // Genesis transactions should have no previousId
                if (!string.IsNullOrEmpty(transaction.PreviousId))
                {
                    errors.Add(ValidationErrorType.GenesisViolation.ToValidationError(
                        "Genesis transaction should not have a previousId",
                        "PreviousId"));
                }
                break;

            case ChainTransactionType.BlueprintPublication:
                // Blueprint publications can reference genesis or prior blueprint version
                if (previousTransaction == null && !string.IsNullOrEmpty(transaction.PreviousId))
                {
                    errors.Add(ValidationErrorType.InvalidPreviousId.ToValidationError(
                        $"Previous transaction '{transaction.PreviousId}' not found",
                        "PreviousId"));
                }
                else if (previousTransaction != null)
                {
                    // Validate blueprint chain
                    var blueprintResult = ValidateBlueprintChain(transaction, previousTransaction);
                    errors.AddRange(blueprintResult.Errors);
                }
                break;

            case ChainTransactionType.InstanceInitiation:
            case ChainTransactionType.InstanceAction:
                // Action transactions must have a valid previous transaction
                if (string.IsNullOrEmpty(transaction.PreviousId))
                {
                    errors.Add(ValidationErrorType.InvalidPreviousId.ToValidationError(
                        "Action transactions must have a previousId",
                        "PreviousId"));
                }
                else if (previousTransaction == null)
                {
                    errors.Add(ValidationErrorType.InvalidPreviousId.ToValidationError(
                        $"Previous transaction '{transaction.PreviousId}' not found",
                        "PreviousId"));
                }
                else
                {
                    var actionResult = ValidateActionChain(transaction, previousTransaction);
                    errors.AddRange(actionResult.Errors);
                }
                break;

            case ChainTransactionType.ControlAction:
                // Control actions follow similar rules to instance actions
                if (string.IsNullOrEmpty(transaction.PreviousId))
                {
                    errors.Add(ValidationErrorType.InvalidPreviousId.ToValidationError(
                        "Control actions must have a previousId",
                        "PreviousId"));
                }
                else if (previousTransaction == null)
                {
                    errors.Add(ValidationErrorType.InvalidPreviousId.ToValidationError(
                        $"Previous transaction '{transaction.PreviousId}' not found",
                        "PreviousId"));
                }
                break;
        }

        // Validate register consistency
        if (previousTransaction != null &&
            !string.Equals(transaction.RegisterId, previousTransaction.RegisterId, StringComparison.Ordinal))
        {
            errors.Add(ValidationErrorType.BrokenChain.ToValidationError(
                $"Transaction register '{transaction.RegisterId}' doesn't match previous transaction register '{previousTransaction.RegisterId}'",
                "RegisterId"));
        }

        // Validate temporal ordering
        if (previousTransaction != null && transaction.Timestamp < previousTransaction.Timestamp)
        {
            errors.Add(ValidationErrorType.InvalidTimestamp.ToValidationError(
                $"Transaction timestamp {transaction.Timestamp:O} is before previous transaction timestamp {previousTransaction.Timestamp:O}",
                "Timestamp"));
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors.ToArray());
    }

    /// <inheritdoc/>
    public ValidationResult ValidateBlueprintChain(
        TransactionChainData blueprintTx,
        TransactionChainData? previousTx)
    {
        ArgumentNullException.ThrowIfNull(blueprintTx);

        var errors = new List<ValidationError>();

        // Blueprint publications must have a BlueprintId
        if (string.IsNullOrEmpty(blueprintTx.BlueprintId))
        {
            errors.Add(ValidationErrorType.MissingBlueprintId.ToValidationError(
                "Blueprint publication must have a BlueprintId",
                "BlueprintId"));
        }

        // If there's a previous transaction, validate the chain
        if (previousTx != null)
        {
            // Previous transaction must be either:
            // 1. Genesis transaction (for first blueprint version)
            // 2. Previous version of the same blueprint

            if (previousTx.TransactionType == ChainTransactionType.Genesis)
            {
                // Valid: new blueprint referencing genesis
            }
            else if (previousTx.TransactionType == ChainTransactionType.BlueprintPublication)
            {
                // Must be the same blueprint (version update)
                if (!string.IsNullOrEmpty(previousTx.BlueprintId) &&
                    !string.Equals(blueprintTx.BlueprintId, previousTx.BlueprintId, StringComparison.Ordinal))
                {
                    errors.Add(ValidationErrorType.InvalidBlueprintVersion.ToValidationError(
                        $"Blueprint version update must reference previous version of same blueprint. Expected '{previousTx.BlueprintId}', got '{blueprintTx.BlueprintId}'",
                        "BlueprintId"));
                }
            }
            else
            {
                // Invalid: blueprint referencing an action or other transaction type
                errors.Add(ValidationErrorType.BrokenChain.ToValidationError(
                    $"Blueprint publication cannot reference transaction of type '{previousTx.TransactionType}'",
                    "PreviousId"));
            }
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors.ToArray());
    }

    /// <inheritdoc/>
    public ValidationResult ValidateActionChain(
        TransactionChainData actionTx,
        TransactionChainData previousTx)
    {
        ArgumentNullException.ThrowIfNull(actionTx);
        ArgumentNullException.ThrowIfNull(previousTx);

        var errors = new List<ValidationError>();

        // Determine if this is instance initiation (Action 0) or subsequent action
        var isInstanceInitiation = actionTx.ActionId == 0;

        if (isInstanceInitiation)
        {
            // Action 0 must reference a blueprint publication
            if (previousTx.TransactionType != ChainTransactionType.BlueprintPublication)
            {
                errors.Add(ValidationErrorType.InvalidActionSequence.ToValidationError(
                    $"Instance initiation (Action 0) must reference a blueprint publication, not '{previousTx.TransactionType}'",
                    "ActionId"));
            }

            // BlueprintId must match
            if (!string.IsNullOrEmpty(previousTx.BlueprintId) &&
                !string.Equals(actionTx.BlueprintId, previousTx.BlueprintId, StringComparison.Ordinal))
            {
                errors.Add(ValidationErrorType.BrokenChain.ToValidationError(
                    $"Action's blueprint '{actionTx.BlueprintId}' doesn't match referenced blueprint '{previousTx.BlueprintId}'",
                    "BlueprintId"));
            }
        }
        else
        {
            // Subsequent actions must reference prior actions in the same instance
            if (previousTx.TransactionType != ChainTransactionType.InstanceInitiation &&
                previousTx.TransactionType != ChainTransactionType.InstanceAction)
            {
                errors.Add(ValidationErrorType.InvalidActionSequence.ToValidationError(
                    $"Action {actionTx.ActionId} must reference a prior action, not '{previousTx.TransactionType}'",
                    "ActionId"));
            }

            // BlueprintId must match
            if (!string.IsNullOrEmpty(previousTx.BlueprintId) &&
                !string.Equals(actionTx.BlueprintId, previousTx.BlueprintId, StringComparison.Ordinal))
            {
                errors.Add(ValidationErrorType.BrokenChain.ToValidationError(
                    $"Action's blueprint '{actionTx.BlueprintId}' doesn't match prior action's blueprint '{previousTx.BlueprintId}'",
                    "BlueprintId"));
            }

            // Note: ActionId sequence validation is blueprint-specific
            // Some blueprints allow non-sequential action IDs (branching/routing)
            // The schema validator handles action sequence rules
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors.ToArray());
    }

    /// <inheritdoc/>
    public ValidationResult ValidatePreviousData(
        string? previousData,
        string? actualPreviousData)
    {
        // If no previousData claimed, that's valid
        if (string.IsNullOrEmpty(previousData))
        {
            return ValidationResult.Success();
        }

        // If previousData is claimed but actual is empty, that's an error
        if (string.IsNullOrEmpty(actualPreviousData))
        {
            return ValidationResult.Failure(
                ValidationErrorType.PreviousDataMismatch.ToValidationError(
                    "Claimed previousData but previous transaction has no data",
                    "PreviousData"));
        }

        // Compare data hashes
        if (!string.Equals(previousData, actualPreviousData, StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Failure(
                ValidationErrorType.PreviousDataMismatch.ToValidationError(
                    $"Previous data mismatch. Expected: '{actualPreviousData}', Got: '{previousData}'",
                    "PreviousData"));
        }

        return ValidationResult.Success();
    }

    /// <inheritdoc/>
    public ChainTransactionType DetermineTransactionType(
        TransactionChainData transaction,
        TransactionChainData? previousTransaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        // If explicitly set to a known type, use that
        if (transaction.TransactionType != ChainTransactionType.Unknown)
        {
            return transaction.TransactionType;
        }

        // No previousId = Genesis
        if (string.IsNullOrEmpty(transaction.PreviousId))
        {
            return ChainTransactionType.Genesis;
        }

        // Has BlueprintId but no ActionId = Blueprint publication
        if (!string.IsNullOrEmpty(transaction.BlueprintId) && !transaction.ActionId.HasValue)
        {
            return ChainTransactionType.BlueprintPublication;
        }

        // Has ActionId
        if (transaction.ActionId.HasValue)
        {
            // Action 0 = Instance initiation
            if (transaction.ActionId.Value == 0)
            {
                return ChainTransactionType.InstanceInitiation;
            }

            // Other actions = Instance action
            return ChainTransactionType.InstanceAction;
        }

        // If previous transaction is available, use it to determine type
        if (previousTransaction != null)
        {
            if (previousTransaction.TransactionType == ChainTransactionType.Genesis)
            {
                // Referencing genesis with a BlueprintId = Blueprint publication
                if (!string.IsNullOrEmpty(transaction.BlueprintId))
                {
                    return ChainTransactionType.BlueprintPublication;
                }
            }
        }

        // Default to control action for register governance
        return ChainTransactionType.ControlAction;
    }
}
