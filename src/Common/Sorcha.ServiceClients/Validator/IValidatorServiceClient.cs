// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;

namespace Sorcha.ServiceClients.Validator;

/// <summary>
/// Client interface for Validator Service operations
/// </summary>
public interface IValidatorServiceClient
{
    /// <summary>
    /// Submits a genesis transaction to the Validator Service mempool
    /// </summary>
    /// <param name="request">Genesis transaction details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if submitted successfully</returns>
    Task<bool> SubmitGenesisTransactionAsync(
        GenesisTransactionSubmission request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits an action transaction to the Validator Service for validation and mempool inclusion
    /// </summary>
    /// <param name="request">Action transaction submission details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure with details</returns>
    Task<TransactionSubmissionResult> SubmitTransactionAsync(
        ActionTransactionSubmission request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for submitting genesis transactions
/// </summary>
public record GenesisTransactionSubmission
{
    public required string TransactionId { get; init; }
    public required string RegisterId { get; init; }
    public required JsonElement ControlRecordPayload { get; init; }
    public required string PayloadHash { get; init; }
    public required List<GenesisSignature> Signatures { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public string? RegisterName { get; init; }
    public string? TenantId { get; init; }
}

/// <summary>
/// Signature for genesis transaction
/// </summary>
public record GenesisSignature
{
    public required string PublicKey { get; init; }
    public required string SignatureValue { get; init; }
    public required string Algorithm { get; init; }
}

/// <summary>
/// Request model for submitting action transactions to the Validator Service
/// </summary>
public record ActionTransactionSubmission
{
    public required string TransactionId { get; init; }
    public required string RegisterId { get; init; }
    public required string BlueprintId { get; init; }
    public required string ActionId { get; init; }
    public required JsonElement Payload { get; init; }
    public required string PayloadHash { get; init; }
    public required List<SignatureInfo> Signatures { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public string? PreviousTransactionId { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Signature information for transaction submission
/// </summary>
public record SignatureInfo
{
    public required string PublicKey { get; init; }
    public required string SignatureValue { get; init; }
    public required string Algorithm { get; init; }
}

/// <summary>
/// Result of submitting a transaction to the Validator Service
/// </summary>
public record TransactionSubmissionResult
{
    public bool Success { get; init; }
    public string TransactionId { get; init; } = string.Empty;
    public string RegisterId { get; init; } = string.Empty;
    public DateTimeOffset? AddedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
}
