// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Validator.Service.Managers;
using Sorcha.Register.Core.Storage;
using Sorcha.Register.Models;
using Sorcha.Register.Models.Enums;

namespace Sorcha.Validator.Service.Validators;

/// <summary>
/// Validates blockchain chain integrity for registers
/// NOTE: This component runs in the Validator.Service secured environment
/// with access to encryption keys and cryptographic operations.
/// </summary>
public class ChainValidator
{
    private readonly IRegisterRepository _repository;
    private readonly DocketManager _docketManager;

    public ChainValidator(
        IRegisterRepository repository,
        DocketManager docketManager)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _docketManager = docketManager ?? throw new ArgumentNullException(nameof(docketManager));
    }

    /// <summary>
    /// Validates the entire docket chain for a register
    /// </summary>
    public async Task<ChainValidationResult> ValidateDocketChainAsync(
        string registerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registerId);

        var result = new ChainValidationResult
        {
            RegisterId = registerId,
            IsValid = true
        };

        // Get all dockets
        var dockets = (await _repository.GetDocketsAsync(registerId, cancellationToken))
            .OrderBy(d => d.Id)
            .ToList();

        if (dockets.Count == 0)
        {
            result.AddInfo("No dockets found in register");
            return result;
        }

        // Validate first docket
        var firstDocket = dockets.First();
        if (firstDocket.Id != 1)
        {
            result.AddError($"First docket ID should be 1, but is {firstDocket.Id}");
        }

        if (!string.IsNullOrEmpty(firstDocket.PreviousHash))
        {
            result.AddWarning($"First docket should have empty PreviousHash, but has: {firstDocket.PreviousHash}");
        }

        // Validate docket hashes
        foreach (var docket in dockets)
        {
            if (!_docketManager.VerifyDocketHash(docket))
            {
                result.AddError($"Docket {docket.Id} has invalid hash");
            }
        }

        // Validate chain links
        for (int i = 1; i < dockets.Count; i++)
        {
            var currentDocket = dockets[i];
            var previousDocket = dockets[i - 1];

            // Check sequential IDs
            if (currentDocket.Id != previousDocket.Id + 1)
            {
                result.AddError($"Docket chain break: Docket {previousDocket.Id} followed by {currentDocket.Id}");
            }

            // Check previous hash link
            if (currentDocket.PreviousHash != previousDocket.Hash)
            {
                result.AddError($"Docket {currentDocket.Id} PreviousHash does not match Docket {previousDocket.Id} Hash");
            }

            // Check state
            if (currentDocket.State != DocketState.Sealed)
            {
                result.AddWarning($"Docket {currentDocket.Id} is not sealed (State: {currentDocket.State})");
            }
        }

        // Validate register height matches highest docket
        var register = await _repository.GetRegisterAsync(registerId, cancellationToken);
        if (register != null)
        {
            var highestDocket = dockets.Last();
            if (register.Height != highestDocket.Id)
            {
                result.AddError($"Register height ({register.Height}) does not match highest docket ID ({highestDocket.Id})");
            }
        }

        return result;
    }

    /// <summary>
    /// Validates transaction chain integrity
    /// </summary>
    public async Task<ChainValidationResult> ValidateTransactionChainAsync(
        string registerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registerId);

        var result = new ChainValidationResult
        {
            RegisterId = registerId,
            IsValid = true
        };

        var transactions = (await _repository.GetTransactionsAsync(registerId, cancellationToken))
            .OrderBy(t => t.TimeStamp)
            .ToList();

        if (transactions.Count == 0)
        {
            result.AddInfo("No transactions found in register");
            return result;
        }

        // Build transaction lookup
        var txLookup = transactions.ToDictionary(t => t.TxId);

        // Validate transaction chain links
        foreach (var tx in transactions)
        {
            // Skip genesis transactions
            if (string.IsNullOrEmpty(tx.PrevTxId))
            {
                continue;
            }

            // Check if previous transaction exists
            if (!txLookup.ContainsKey(tx.PrevTxId))
            {
                result.AddWarning($"Transaction {tx.TxId} references non-existent previous transaction {tx.PrevTxId}");
            }
        }

        // Validate orphaned transactions (not in any docket)
        var orphanedCount = transactions.Count(t => !t.DocketNumber.HasValue);
        if (orphanedCount > 0)
        {
            result.AddInfo($"{orphanedCount} orphaned transactions (not sealed in any docket)");
        }

        // Validate docket transaction references
        var dockets = await _repository.GetDocketsAsync(registerId, cancellationToken);
        foreach (var docket in dockets)
        {
            foreach (var txId in docket.TransactionIds)
            {
                if (!txLookup.ContainsKey(txId))
                {
                    result.AddError($"Docket {docket.Id} references non-existent transaction {txId}");
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Performs complete validation of a register
    /// </summary>
    public async Task<ChainValidationResult> ValidateCompleteChainAsync(
        string registerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registerId);

        var docketResult = await ValidateDocketChainAsync(registerId, cancellationToken);
        var transactionResult = await ValidateTransactionChainAsync(registerId, cancellationToken);

        var combinedResult = new ChainValidationResult
        {
            RegisterId = registerId,
            IsValid = docketResult.IsValid && transactionResult.IsValid
        };

        combinedResult.Errors.AddRange(docketResult.Errors);
        combinedResult.Errors.AddRange(transactionResult.Errors);
        combinedResult.Warnings.AddRange(docketResult.Warnings);
        combinedResult.Warnings.AddRange(transactionResult.Warnings);
        combinedResult.Info.AddRange(docketResult.Info);
        combinedResult.Info.AddRange(transactionResult.Info);

        return combinedResult;
    }
}

/// <summary>
/// Result of chain validation
/// </summary>
public class ChainValidationResult
{
    public required string RegisterId { get; set; }
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Info { get; set; } = new();

    public void AddError(string message)
    {
        Errors.Add(message);
        IsValid = false;
    }

    public void AddWarning(string message)
    {
        Warnings.Add(message);
    }

    public void AddInfo(string message)
    {
        Info.Add(message);
    }

    public override string ToString()
    {
        var lines = new List<string>
        {
            $"Chain Validation Result for Register: {RegisterId}",
            $"Status: {(IsValid ? "VALID" : "INVALID")}",
            ""
        };

        if (Errors.Count > 0)
        {
            lines.Add("Errors:");
            lines.AddRange(Errors.Select(e => $"  - {e}"));
            lines.Add("");
        }

        if (Warnings.Count > 0)
        {
            lines.Add("Warnings:");
            lines.AddRange(Warnings.Select(w => $"  - {w}"));
            lines.Add("");
        }

        if (Info.Count > 0)
        {
            lines.Add("Info:");
            lines.AddRange(Info.Select(i => $"  - {i}"));
        }

        return string.Join(Environment.NewLine, lines);
    }
}
