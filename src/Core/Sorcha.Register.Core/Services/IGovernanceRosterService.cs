// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Register.Models;

namespace Sorcha.Register.Core.Services;

/// <summary>
/// Result of a quorum calculation
/// </summary>
public class QuorumResult
{
    /// <summary>
    /// Whether quorum has been met
    /// </summary>
    public bool IsQuorumMet { get; init; }

    /// <summary>
    /// Number of votes required for quorum
    /// </summary>
    public int VotesRequired { get; init; }

    /// <summary>
    /// Number of approval votes received
    /// </summary>
    public int VotesReceived { get; init; }

    /// <summary>
    /// Size of the adjusted voting pool
    /// </summary>
    public int VotingPool { get; init; }

    /// <summary>
    /// Whether the Owner override was used (bypasses quorum)
    /// </summary>
    public bool IsOwnerOverride { get; init; }
}

/// <summary>
/// Represents a snapshot of a register's admin roster
/// </summary>
public class AdminRoster
{
    /// <summary>
    /// Register ID this roster belongs to
    /// </summary>
    public string RegisterId { get; init; } = string.Empty;

    /// <summary>
    /// Current roster members (from the latest Control transaction)
    /// </summary>
    public RegisterControlRecord ControlRecord { get; init; } = new();

    /// <summary>
    /// Number of Control transactions processed to derive this roster
    /// </summary>
    public int ControlTransactionCount { get; init; }

    /// <summary>
    /// Transaction ID of the last Control transaction
    /// </summary>
    public string? LastControlTxId { get; init; }
}

/// <summary>
/// Service for reconstructing and managing register governance rosters from Control transactions
/// </summary>
public interface IGovernanceRosterService
{
    /// <summary>
    /// Reconstructs the current admin roster from the Control transaction chain
    /// </summary>
    /// <param name="registerId">Register ID to reconstruct roster for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current admin roster, or null if register not found</returns>
    Task<AdminRoster?> GetCurrentRosterAsync(
        string registerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether a quorum has been met for a governance operation
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="operation">The governance operation being proposed</param>
    /// <param name="approvals">List of approval signatures received</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quorum calculation result</returns>
    Task<QuorumResult> ValidateQuorumAsync(
        string registerId,
        GovernanceOperation operation,
        List<ApprovalSignature> approvals,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a governance proposal against the current roster
    /// </summary>
    /// <param name="roster">Current admin roster</param>
    /// <param name="operation">Proposed governance operation</param>
    /// <returns>Validation result with errors if invalid</returns>
    GovernanceValidationResult ValidateProposal(
        AdminRoster roster,
        GovernanceOperation operation);

    /// <summary>
    /// Applies a governance operation to a roster, producing a new roster snapshot
    /// </summary>
    /// <param name="currentRoster">Current roster</param>
    /// <param name="operation">Operation to apply</param>
    /// <param name="newAttestation">Attestation for the new member (Add/Transfer only)</param>
    /// <returns>Updated roster control record</returns>
    RegisterControlRecord ApplyOperation(
        RegisterControlRecord currentRoster,
        GovernanceOperation operation,
        RegisterAttestation? newAttestation = null);
}

/// <summary>
/// Result of governance proposal validation
/// </summary>
public class GovernanceValidationResult
{
    /// <summary>
    /// Whether the proposal is valid
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Validation errors (empty if valid)
    /// </summary>
    public List<string> Errors { get; init; } = new();

    public static GovernanceValidationResult Success() => new() { IsValid = true };
    public static GovernanceValidationResult Failure(params string[] errors) =>
        new() { IsValid = false, Errors = errors.ToList() };
}
