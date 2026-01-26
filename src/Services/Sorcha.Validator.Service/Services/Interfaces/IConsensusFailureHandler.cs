// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Validator.Service.Models;

namespace Sorcha.Validator.Service.Services.Interfaces;

/// <summary>
/// Handles consensus failures for dockets by implementing retry and abandon logic.
/// When signature collection fails to meet threshold, this handler decides whether
/// to retry or abandon the docket.
/// </summary>
public interface IConsensusFailureHandler
{
    /// <summary>
    /// Handle a failed consensus attempt for a docket
    /// </summary>
    /// <param name="docket">The docket that failed consensus</param>
    /// <param name="result">The signature collection result</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Action to take: retry, abandon, or succeed if threshold met on retry</returns>
    Task<ConsensusRecoveryResult> HandleFailureAsync(
        Docket docket,
        SignatureCollectionResult result,
        CancellationToken ct = default);

    /// <summary>
    /// Abandon a docket permanently after max retries exceeded
    /// </summary>
    /// <param name="docket">The docket to abandon</param>
    /// <param name="reason">Reason for abandonment</param>
    /// <param name="ct">Cancellation token</param>
    Task AbandonDocketAsync(
        Docket docket,
        string reason,
        CancellationToken ct = default);

    /// <summary>
    /// Return transactions from an abandoned docket to the memory pool
    /// </summary>
    /// <param name="docket">The abandoned docket</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of transactions returned to pool</returns>
    Task<int> ReturnTransactionsToPoolAsync(
        Docket docket,
        CancellationToken ct = default);

    /// <summary>
    /// Get statistics about consensus failure handling
    /// </summary>
    ConsensusFailureStats GetStats();
}

/// <summary>
/// Result of consensus recovery attempt
/// </summary>
public record ConsensusRecoveryResult
{
    /// <summary>Action taken</summary>
    public required ConsensusRecoveryAction Action { get; init; }

    /// <summary>Whether recovery succeeded (threshold met on retry)</summary>
    public required bool Succeeded { get; init; }

    /// <summary>Number of retry attempts made</summary>
    public required int RetryAttempts { get; init; }

    /// <summary>Reason for the result</summary>
    public required string Reason { get; init; }

    /// <summary>Updated docket if signatures were collected</summary>
    public Docket? UpdatedDocket { get; init; }

    /// <summary>Transactions returned to pool (if abandoned)</summary>
    public int TransactionsReturnedToPool { get; init; }

    /// <summary>Total time spent in recovery</summary>
    public TimeSpan RecoveryDuration { get; init; }
}

/// <summary>
/// Actions the consensus failure handler can take
/// </summary>
public enum ConsensusRecoveryAction
{
    /// <summary>Retry signature collection</summary>
    Retry,

    /// <summary>Abandon the docket permanently</summary>
    Abandon,

    /// <summary>No action needed (threshold met)</summary>
    NoActionNeeded,

    /// <summary>Cannot recover (fatal error)</summary>
    Unrecoverable
}

/// <summary>
/// Statistics about consensus failure handling
/// </summary>
public record ConsensusFailureStats
{
    /// <summary>Total failures handled</summary>
    public long TotalFailures { get; init; }

    /// <summary>Successful recoveries (threshold met on retry)</summary>
    public long SuccessfulRecoveries { get; init; }

    /// <summary>Dockets abandoned after max retries</summary>
    public long DocketsAbandoned { get; init; }

    /// <summary>Total retry attempts</summary>
    public long TotalRetryAttempts { get; init; }

    /// <summary>Transactions returned to pool</summary>
    public long TransactionsReturnedToPool { get; init; }

    /// <summary>Recovery success rate (0-1)</summary>
    public double RecoveryRate => TotalFailures > 0
        ? (double)SuccessfulRecoveries / TotalFailures
        : 0;
}
