// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Validator.Service.Models;

namespace Sorcha.Validator.Service.Services.Interfaces;

/// <summary>
/// Collects signatures from confirming validators for a proposed docket.
/// Used by the leader to gather consensus before committing a docket.
/// </summary>
public interface ISignatureCollector
{
    /// <summary>
    /// Collect signatures from confirming validators
    /// </summary>
    /// <param name="docket">Docket to get signatures for</param>
    /// <param name="config">Consensus configuration</param>
    /// <param name="validators">List of validators to request signatures from</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Signature collection result</returns>
    Task<SignatureCollectionResult> CollectSignaturesAsync(
        Docket docket,
        ConsensusConfig config,
        IReadOnlyList<ValidatorInfo> validators,
        CancellationToken ct = default);

    /// <summary>
    /// Request a single signature from a validator
    /// </summary>
    /// <param name="validator">Validator to request from</param>
    /// <param name="docket">Docket to sign</param>
    /// <param name="term">Current election term</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Signature response or null on failure</returns>
    Task<ValidatorSignatureResponse?> RequestSignatureAsync(
        ValidatorInfo validator,
        Docket docket,
        long term,
        CancellationToken ct = default);
}

/// <summary>
/// Result of signature collection attempt
/// </summary>
public record SignatureCollectionResult
{
    /// <summary>All collected signatures (including initiator)</summary>
    public required IReadOnlyList<ValidatorSignature> Signatures { get; init; }

    /// <summary>Whether minimum threshold was met</summary>
    public required bool ThresholdMet { get; init; }

    /// <summary>Whether collection timed out</summary>
    public required bool TimedOut { get; init; }

    /// <summary>Total validators contacted</summary>
    public required int TotalValidators { get; init; }

    /// <summary>Number of responses received</summary>
    public required int ResponsesReceived { get; init; }

    /// <summary>Number of approvals</summary>
    public required int Approvals { get; init; }

    /// <summary>Number of rejections</summary>
    public required int Rejections { get; init; }

    /// <summary>Time taken for collection</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>Validators that didn't respond</summary>
    public IReadOnlyList<string>? NonResponders { get; init; }

    /// <summary>Rejection details by validator</summary>
    public IReadOnlyDictionary<string, string>? RejectionDetails { get; init; }

    /// <summary>Approval percentage</summary>
    public double ApprovalPercentage => TotalValidators > 0
        ? (double)Approvals / TotalValidators * 100
        : 0;
}

/// <summary>
/// A signature from a validator
/// </summary>
public record ValidatorSignature
{
    /// <summary>Validator who provided the signature</summary>
    public required string ValidatorId { get; init; }

    /// <summary>The signature</summary>
    public required Signature Signature { get; init; }

    /// <summary>When the signature was provided</summary>
    public required DateTimeOffset SignedAt { get; init; }

    /// <summary>Whether this is the initiator's signature</summary>
    public required bool IsInitiator { get; init; }
}

/// <summary>
/// Response from a validator when requesting signature
/// </summary>
public record ValidatorSignatureResponse
{
    /// <summary>Validator ID</summary>
    public required string ValidatorId { get; init; }

    /// <summary>Whether the validator approved</summary>
    public required bool Approved { get; init; }

    /// <summary>Signature if approved</summary>
    public Signature? Signature { get; init; }

    /// <summary>Rejection reason if not approved</summary>
    public DocketRejectionReason? RejectionReason { get; init; }

    /// <summary>Rejection details</summary>
    public string? RejectionDetails { get; init; }

    /// <summary>Response latency</summary>
    public TimeSpan Latency { get; init; }
}
