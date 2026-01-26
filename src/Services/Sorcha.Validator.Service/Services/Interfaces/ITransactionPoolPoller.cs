// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Validator.Service.Models;

namespace Sorcha.Validator.Service.Services.Interfaces;

/// <summary>
/// Polls unverified transactions from Redis for validation.
/// Part of the validator's initiator role infrastructure.
/// </summary>
public interface ITransactionPoolPoller
{
    /// <summary>
    /// Submit a transaction to the unverified pool for a register
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="transaction">Transaction to submit</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if submitted successfully</returns>
    Task<bool> SubmitTransactionAsync(
        string registerId,
        Transaction transaction,
        CancellationToken ct = default);

    /// <summary>
    /// Poll transactions from the unverified pool for a register
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="maxCount">Maximum transactions to poll</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of polled transactions</returns>
    Task<IReadOnlyList<Transaction>> PollTransactionsAsync(
        string registerId,
        int maxCount,
        CancellationToken ct = default);

    /// <summary>
    /// Return transactions to the unverified pool (e.g., after validation failure or consensus failure)
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="transactions">Transactions to return</param>
    /// <param name="ct">Cancellation token</param>
    Task ReturnTransactionsAsync(
        string registerId,
        IReadOnlyList<Transaction> transactions,
        CancellationToken ct = default);

    /// <summary>
    /// Get the count of transactions in the unverified pool for a register
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of transactions in the pool</returns>
    Task<long> GetUnverifiedCountAsync(
        string registerId,
        CancellationToken ct = default);

    /// <summary>
    /// Get statistics for the unverified pool
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Pool statistics</returns>
    Task<UnverifiedPoolStats> GetStatsAsync(
        string registerId,
        CancellationToken ct = default);

    /// <summary>
    /// Check if a transaction exists in the unverified pool
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="transactionId">Transaction ID to check</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if transaction exists</returns>
    Task<bool> ExistsAsync(
        string registerId,
        string transactionId,
        CancellationToken ct = default);

    /// <summary>
    /// Remove a specific transaction from the unverified pool
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="transactionId">Transaction ID to remove</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if removed</returns>
    Task<bool> RemoveTransactionAsync(
        string registerId,
        string transactionId,
        CancellationToken ct = default);

    /// <summary>
    /// Remove expired transactions from the unverified pool
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of expired transactions removed</returns>
    Task<int> CleanupExpiredAsync(
        string registerId,
        CancellationToken ct = default);
}

/// <summary>
/// Statistics for the unverified transaction pool
/// </summary>
public record UnverifiedPoolStats
{
    /// <summary>Register ID</summary>
    public required string RegisterId { get; init; }

    /// <summary>Total transactions in pool</summary>
    public long TotalTransactions { get; init; }

    /// <summary>Transactions submitted since service start</summary>
    public long TotalSubmitted { get; init; }

    /// <summary>Transactions polled since service start</summary>
    public long TotalPolled { get; init; }

    /// <summary>Transactions returned after failure since service start</summary>
    public long TotalReturned { get; init; }

    /// <summary>Expired transactions removed since service start</summary>
    public long TotalExpired { get; init; }

    /// <summary>Oldest transaction timestamp in pool</summary>
    public DateTimeOffset? OldestTransactionTime { get; init; }

    /// <summary>Average age of transactions in pool (ms)</summary>
    public double AverageAgeMs { get; init; }
}
