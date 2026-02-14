// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Validator.Service.Models;

namespace Sorcha.Validator.Service.Services.Interfaces;

/// <summary>
/// Core validation engine that validates transactions against blueprint rules,
/// cryptographic requirements, and chain integrity before they can be included in dockets.
/// </summary>
public interface IValidationEngine
{
    /// <summary>
    /// Validate a single transaction
    /// </summary>
    /// <param name="transaction">Transaction to validate</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result with detailed errors if invalid</returns>
    Task<ValidationEngineResult> ValidateTransactionAsync(
        Transaction transaction,
        CancellationToken ct = default);

    /// <summary>
    /// Validate a batch of transactions
    /// </summary>
    /// <param name="transactions">Transactions to validate</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation results for each transaction</returns>
    Task<IReadOnlyList<ValidationEngineResult>> ValidateBatchAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken ct = default);

    /// <summary>
    /// Validate a transaction's structure (IDs, timestamps, required fields)
    /// </summary>
    /// <param name="transaction">Transaction to validate</param>
    /// <returns>Validation result</returns>
    ValidationEngineResult ValidateStructure(Transaction transaction);

    /// <summary>
    /// Validate a transaction's payload against the blueprint schema
    /// </summary>
    /// <param name="transaction">Transaction to validate</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<ValidationEngineResult> ValidateSchemaAsync(
        Transaction transaction,
        CancellationToken ct = default);

    /// <summary>
    /// Verify all cryptographic signatures on a transaction
    /// </summary>
    /// <param name="transaction">Transaction to verify</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<ValidationEngineResult> VerifySignaturesAsync(
        Transaction transaction,
        CancellationToken ct = default);

    /// <summary>
    /// Validate transaction chain integrity (previousId references)
    /// </summary>
    /// <param name="transaction">Transaction to validate</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<ValidationEngineResult> ValidateChainAsync(
        Transaction transaction,
        CancellationToken ct = default);

    /// <summary>
    /// Validate a transaction's blueprint conformance (sender authorization, action sequencing, starting action)
    /// </summary>
    /// <param name="transaction">Transaction to validate</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<ValidationEngineResult> ValidateBlueprintConformanceAsync(
        Transaction transaction,
        CancellationToken ct = default);

    /// <summary>
    /// Get validation engine statistics
    /// </summary>
    /// <returns>Validation statistics</returns>
    ValidationEngineStats GetStats();
}

/// <summary>
/// Result of validation engine processing
/// </summary>
public record ValidationEngineResult
{
    /// <summary>Transaction ID</summary>
    public required string TransactionId { get; init; }

    /// <summary>Register ID</summary>
    public required string RegisterId { get; init; }

    /// <summary>Whether validation passed</summary>
    public required bool IsValid { get; init; }

    /// <summary>Validation errors if any</summary>
    public IReadOnlyList<ValidationEngineError> Errors { get; init; } = [];

    /// <summary>Time taken for validation</summary>
    public TimeSpan ValidationDuration { get; init; }

    /// <summary>When validation was performed</summary>
    public DateTimeOffset ValidatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Creates a successful validation result</summary>
    public static ValidationEngineResult Success(
        string transactionId,
        string registerId,
        TimeSpan duration) => new()
    {
        TransactionId = transactionId,
        RegisterId = registerId,
        IsValid = true,
        ValidationDuration = duration
    };

    /// <summary>Creates a failed validation result</summary>
    public static ValidationEngineResult Failure(
        string transactionId,
        string registerId,
        TimeSpan duration,
        params ValidationEngineError[] errors) => new()
    {
        TransactionId = transactionId,
        RegisterId = registerId,
        IsValid = false,
        Errors = errors,
        ValidationDuration = duration
    };
}

/// <summary>
/// A validation error for a transaction
/// </summary>
public record ValidationEngineError
{
    /// <summary>Error code for programmatic handling</summary>
    public required string Code { get; init; }

    /// <summary>Human-readable error message</summary>
    public required string Message { get; init; }

    /// <summary>Error category</summary>
    public required ValidationErrorCategory Category { get; init; }

    /// <summary>Optional field name where error occurred</summary>
    public string? Field { get; init; }

    /// <summary>Whether this error is fatal (stops further validation)</summary>
    public bool IsFatal { get; init; }

    /// <summary>Additional error details</summary>
    public Dictionary<string, object>? Details { get; init; }
}

/// <summary>
/// Categories of validation errors
/// </summary>
public enum ValidationErrorCategory
{
    /// <summary>Transaction structure errors (missing/invalid fields)</summary>
    Structure,

    /// <summary>Schema validation errors (payload doesn't match blueprint schema)</summary>
    Schema,

    /// <summary>Cryptographic errors (invalid signatures, hash mismatch)</summary>
    Cryptographic,

    /// <summary>Chain validation errors (invalid previousId, broken chain)</summary>
    Chain,

    /// <summary>Blueprint errors (blueprint not found, action not found)</summary>
    Blueprint,

    /// <summary>Permission errors (signer not authorized)</summary>
    Permission,

    /// <summary>Timing errors (expired, future timestamp)</summary>
    Timing,

    /// <summary>Internal/system errors</summary>
    Internal
}

/// <summary>
/// Validation engine statistics
/// </summary>
public record ValidationEngineStats
{
    /// <summary>Total transactions validated</summary>
    public long TotalValidated { get; init; }

    /// <summary>Total successful validations</summary>
    public long TotalSuccessful { get; init; }

    /// <summary>Total failed validations</summary>
    public long TotalFailed { get; init; }

    /// <summary>Success rate (0-1)</summary>
    public double SuccessRate => TotalValidated > 0
        ? (double)TotalSuccessful / TotalValidated
        : 0;

    /// <summary>Average validation duration</summary>
    public TimeSpan AverageValidationDuration { get; init; }

    /// <summary>Errors by category</summary>
    public IReadOnlyDictionary<ValidationErrorCategory, long> ErrorsByCategory { get; init; }
        = new Dictionary<ValidationErrorCategory, long>();

    /// <summary>Validations currently in progress</summary>
    public int InProgress { get; init; }
}
