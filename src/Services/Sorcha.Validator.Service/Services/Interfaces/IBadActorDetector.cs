// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Validator.Service.Services.Interfaces;

/// <summary>
/// Detects and logs potential bad actor behavior for future analysis.
/// Currently logging-only; future versions may implement throttling/removal.
/// </summary>
public interface IBadActorDetector
{
    /// <summary>
    /// Log a docket rejection (as confirmer)
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="initiatorId">Validator who proposed the invalid docket</param>
    /// <param name="docketId">Docket ID</param>
    /// <param name="reason">Rejection reason</param>
    /// <param name="details">Additional details</param>
    void LogDocketRejection(
        string registerId,
        string initiatorId,
        string docketId,
        DocketRejectionReason reason,
        string? details = null);

    /// <summary>
    /// Log a transaction validation failure
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="senderId">Transaction sender</param>
    /// <param name="transactionId">Transaction ID</param>
    /// <param name="errorType">Type of validation error</param>
    /// <param name="details">Additional details</param>
    void LogTransactionValidationFailure(
        string registerId,
        string senderId,
        string transactionId,
        string errorType,
        string? details = null);

    /// <summary>
    /// Log a double-vote attempt
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="validatorId">Validator who attempted double-vote</param>
    /// <param name="docketId">Docket ID</param>
    /// <param name="term">Election term</param>
    void LogDoubleVote(
        string registerId,
        string validatorId,
        string docketId,
        long term);

    /// <summary>
    /// Log a leader impersonation attempt
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="fakeLeaderId">Validator claiming to be leader</param>
    /// <param name="actualLeaderId">Actual leader</param>
    /// <param name="term">Election term</param>
    void LogLeaderImpersonation(
        string registerId,
        string fakeLeaderId,
        string actualLeaderId,
        long term);

    /// <summary>
    /// Get rejection count for a validator
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="validatorId">Validator ID</param>
    /// <param name="timeWindow">Time window to count</param>
    /// <returns>Number of rejections in window</returns>
    Task<int> GetRejectionCountAsync(
        string registerId,
        string validatorId,
        TimeSpan timeWindow);

    /// <summary>
    /// Get all incidents for a validator
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="validatorId">Validator ID</param>
    /// <param name="limit">Maximum incidents to return</param>
    /// <returns>List of incidents</returns>
    Task<IReadOnlyList<BadActorIncident>> GetIncidentsAsync(
        string registerId,
        string validatorId,
        int limit = 100);

    /// <summary>
    /// Check if a validator should be flagged for review
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="validatorId">Validator ID</param>
    /// <returns>True if validator has suspicious activity</returns>
    Task<bool> ShouldFlagForReviewAsync(
        string registerId,
        string validatorId);
}

/// <summary>
/// Record of a bad actor incident
/// </summary>
public record BadActorIncident
{
    /// <summary>Unique incident ID</summary>
    public required string IncidentId { get; init; }

    /// <summary>Register where incident occurred</summary>
    public required string RegisterId { get; init; }

    /// <summary>Validator involved</summary>
    public required string ValidatorId { get; init; }

    /// <summary>Type of incident</summary>
    public required BadActorIncidentType IncidentType { get; init; }

    /// <summary>Severity level</summary>
    public required IncidentSeverity Severity { get; init; }

    /// <summary>When incident occurred</summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>Related docket ID if applicable</summary>
    public string? DocketId { get; init; }

    /// <summary>Related transaction ID if applicable</summary>
    public string? TransactionId { get; init; }

    /// <summary>Additional details</summary>
    public string? Details { get; init; }
}

/// <summary>
/// Types of bad actor incidents
/// </summary>
public enum BadActorIncidentType
{
    /// <summary>Proposed an invalid docket</summary>
    InvalidDocketProposed,

    /// <summary>Submitted an invalid transaction</summary>
    InvalidTransactionSubmitted,

    /// <summary>Attempted to vote twice</summary>
    DoubleVoteAttempt,

    /// <summary>Claimed to be leader when not</summary>
    LeaderImpersonation,

    /// <summary>Signature verification failed</summary>
    SignatureForged,

    /// <summary>Excessive rejections</summary>
    ExcessiveRejections,

    /// <summary>Non-responsive during consensus</summary>
    NonResponsive
}

/// <summary>
/// Severity levels for incidents
/// </summary>
public enum IncidentSeverity
{
    /// <summary>Informational - may be transient</summary>
    Info,

    /// <summary>Warning - pattern emerging</summary>
    Warning,

    /// <summary>High - definite bad behavior</summary>
    High,

    /// <summary>Critical - immediate action needed</summary>
    Critical
}
