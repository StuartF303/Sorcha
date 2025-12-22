// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Validator.Service.Models;

/// <summary>
/// A signed approval or rejection of a proposed docket by a validator
/// </summary>
public class ConsensusVote
{
    /// <summary>
    /// Unique vote identifier
    /// </summary>
    public required string VoteId { get; init; }

    /// <summary>
    /// Target docket being voted on
    /// </summary>
    public required string DocketId { get; init; }

    /// <summary>
    /// Validator casting this vote
    /// </summary>
    public required string ValidatorId { get; init; }

    /// <summary>
    /// Approve or reject decision
    /// </summary>
    public required VoteDecision Decision { get; init; }

    /// <summary>
    /// Reason for rejection (required if Decision == Reject)
    /// </summary>
    public string? RejectionReason { get; init; }

    /// <summary>
    /// When the vote was cast
    /// </summary>
    public required DateTimeOffset VotedAt { get; init; }

    /// <summary>
    /// Validator's cryptographic signature on the vote
    /// </summary>
    public required Signature ValidatorSignature { get; init; }

    /// <summary>
    /// Hash of the docket being voted on (for verification)
    /// </summary>
    public required string DocketHash { get; init; }
}
