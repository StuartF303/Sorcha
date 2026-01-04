// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

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
