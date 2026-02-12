// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Validator.Core.Models;

namespace Sorcha.Validator.Core.Validators;

/// <summary>
/// Validates transaction chain integrity including previousId references,
/// blueprint versioning, and instance tracking.
/// This validator is stateless and can run in secure enclaves.
/// </summary>
public interface IChainValidator
{
    /// <summary>
    /// Validates that a transaction's previousId correctly references its chain
    /// </summary>
    /// <param name="transaction">Transaction to validate</param>
    /// <param name="previousTransaction">The transaction referenced by previousId (null for genesis)</param>
    /// <returns>Validation result with errors if any</returns>
    ValidationResult ValidateChainLink(
        TransactionChainData transaction,
        TransactionChainData? previousTransaction);

    /// <summary>
    /// Validates a blueprint publication transaction's chain
    /// </summary>
    /// <param name="blueprintTx">Blueprint publication transaction</param>
    /// <param name="previousTx">Previous transaction (genesis for new, prior version for updates)</param>
    /// <returns>Validation result with errors if any</returns>
    ValidationResult ValidateBlueprintChain(
        TransactionChainData blueprintTx,
        TransactionChainData? previousTx);

    /// <summary>
    /// Validates an action transaction's chain within an instance
    /// </summary>
    /// <param name="actionTx">Action transaction</param>
    /// <param name="previousTx">Previous transaction in the instance chain</param>
    /// <returns>Validation result with errors if any</returns>
    ValidationResult ValidateActionChain(
        TransactionChainData actionTx,
        TransactionChainData previousTx);

    /// <summary>
    /// Validates that previousData matches the data from the previous transaction
    /// </summary>
    /// <param name="previousData">Data claimed to be from previous transaction</param>
    /// <param name="actualPreviousData">Actual data from previous transaction</param>
    /// <returns>Validation result with errors if any</returns>
    ValidationResult ValidatePreviousData(
        string? previousData,
        string? actualPreviousData);

    /// <summary>
    /// Determines the transaction type based on chain position
    /// </summary>
    /// <param name="transaction">Transaction to analyze</param>
    /// <param name="previousTransaction">Previous transaction in chain</param>
    /// <returns>The determined transaction type</returns>
    ChainTransactionType DetermineTransactionType(
        TransactionChainData transaction,
        TransactionChainData? previousTransaction);
}

/// <summary>
/// Transaction data required for chain validation (minimal, stateless)
/// </summary>
public record TransactionChainData
{
    /// <summary>Transaction ID (hash)</summary>
    public required string TransactionId { get; init; }

    /// <summary>Register this transaction belongs to</summary>
    public required string RegisterId { get; init; }

    /// <summary>Reference to previous transaction (null only for genesis)</summary>
    public string? PreviousId { get; init; }

    /// <summary>Blueprint ID this transaction relates to</summary>
    public string? BlueprintId { get; init; }

    /// <summary>Action ID within the blueprint (null for blueprint publications)</summary>
    public int? ActionId { get; init; }

    /// <summary>Type of transaction</summary>
    public ChainTransactionType TransactionType { get; init; }

    /// <summary>Hash of the transaction data (for previousData validation)</summary>
    public string? DataHash { get; init; }

    /// <summary>Timestamp of the transaction</summary>
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Types of transactions in the chain
/// </summary>
public enum ChainTransactionType
{
    /// <summary>Unknown/not yet determined transaction type</summary>
    Unknown = 0,

    /// <summary>Genesis block transaction (register initialization)</summary>
    Genesis = 1,

    /// <summary>Blueprint publication or version update</summary>
    BlueprintPublication = 2,

    /// <summary>First action in a workflow instance (previousId = blueprint tx)</summary>
    InstanceInitiation = 3,

    /// <summary>Subsequent action in a workflow instance</summary>
    InstanceAction = 4,

    /// <summary>Control action for register governance</summary>
    ControlAction = 5
}
