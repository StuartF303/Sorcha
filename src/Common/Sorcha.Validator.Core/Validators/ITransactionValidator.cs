// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using Sorcha.Validator.Core.Models;

namespace Sorcha.Validator.Core.Validators;

/// <summary>
/// Validates transactions against blueprint rules and cryptographic requirements
/// </summary>
public interface ITransactionValidator
{
    /// <summary>
    /// Validates a transaction's structure, signatures, and basic integrity
    /// </summary>
    /// <param name="transactionId">Transaction ID</param>
    /// <param name="registerId">Target register ID</param>
    /// <param name="blueprintId">Blueprint ID</param>
    /// <param name="payload">Transaction payload</param>
    /// <param name="payloadHash">Expected payload hash</param>
    /// <param name="signatures">List of signatures</param>
    /// <param name="createdAt">Transaction timestamp</param>
    /// <returns>Validation result with errors if any</returns>
    ValidationResult ValidateTransactionStructure(
        string transactionId,
        string registerId,
        string blueprintId,
        JsonElement payload,
        string payloadHash,
        List<TransactionSignature> signatures,
        DateTimeOffset createdAt);

    /// <summary>
    /// Validates that the payload hash matches the actual payload
    /// </summary>
    ValidationResult ValidatePayloadHash(JsonElement payload, string expectedHash);

    /// <summary>
    /// Validates transaction signatures
    /// </summary>
    ValidationResult ValidateSignatures(List<TransactionSignature> signatures, string transactionId);
}

/// <summary>
/// Transaction signature information
/// </summary>
public record TransactionSignature(string PublicKey, string SignatureValue, string Algorithm);
