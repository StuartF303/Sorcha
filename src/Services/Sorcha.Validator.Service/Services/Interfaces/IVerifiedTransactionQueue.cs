// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Validator.Service.Models;

namespace Sorcha.Validator.Service.Services.Interfaces;

/// <summary>
/// In-memory queue for validated transactions ready for docket building.
/// Transactions are dequeued by the leader during docket construction.
/// </summary>
public interface IVerifiedTransactionQueue
{
    /// <summary>
    /// Enqueue a validated transaction for docket building
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="transaction">Validated transaction</param>
    /// <param name="priority">Transaction priority (higher = processed first)</param>
    /// <returns>True if enqueued successfully</returns>
    bool Enqueue(string registerId, Transaction transaction, int priority = 0);

    /// <summary>
    /// Dequeue transactions for docket building (leader only)
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="maxCount">Maximum transactions to dequeue</param>
    /// <returns>Transactions ready for docket building</returns>
    IReadOnlyList<VerifiedTransaction> Dequeue(string registerId, int maxCount);

    /// <summary>
    /// Peek at transactions without removing them
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="maxCount">Maximum transactions to peek</param>
    /// <returns>Preview of queued transactions</returns>
    IReadOnlyList<VerifiedTransaction> Peek(string registerId, int maxCount);

    /// <summary>
    /// Return transactions to the queue (docket build failed)
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="transactions">Transactions to return</param>
    void ReturnToQueue(string registerId, IReadOnlyList<VerifiedTransaction> transactions);

    /// <summary>
    /// Remove a specific transaction from the queue
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="transactionId">Transaction ID to remove</param>
    /// <returns>True if removed</returns>
    bool Remove(string registerId, string transactionId);

    /// <summary>
    /// Check if a transaction is in the queue
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="transactionId">Transaction ID</param>
    /// <returns>True if transaction is queued</returns>
    bool Contains(string registerId, string transactionId);

    /// <summary>
    /// Get the count of queued transactions for a register
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <returns>Transaction count</returns>
    int GetCount(string registerId);

    /// <summary>
    /// Get total count of queued transactions across all registers
    /// </summary>
    /// <returns>Total transaction count</returns>
    int GetTotalCount();

    /// <summary>
    /// Get queue statistics
    /// </summary>
    /// <returns>Queue statistics</returns>
    VerifiedQueueStats GetStats();

    /// <summary>
    /// Get queue statistics for a specific register
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <returns>Register-specific queue statistics</returns>
    RegisterQueueStats GetRegisterStats(string registerId);

    /// <summary>
    /// Clear all transactions for a register
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <returns>Number of transactions cleared</returns>
    int Clear(string registerId);

    /// <summary>
    /// Clear all transactions across all registers
    /// </summary>
    /// <returns>Number of transactions cleared</returns>
    int ClearAll();

    /// <summary>
    /// Remove expired transactions from all queues
    /// </summary>
    /// <returns>Number of expired transactions removed</returns>
    int CleanupExpired();
}

/// <summary>
/// A verified transaction with metadata
/// </summary>
public record VerifiedTransaction
{
    /// <summary>The validated transaction</summary>
    public required Transaction Transaction { get; init; }

    /// <summary>When the transaction was validated and enqueued</summary>
    public required DateTimeOffset EnqueuedAt { get; init; }

    /// <summary>Priority for docket ordering (higher = processed first)</summary>
    public required int Priority { get; init; }

    /// <summary>When this entry expires and should be removed</summary>
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Transaction ID (convenience accessor)</summary>
    public string TransactionId => Transaction.TransactionId;
}

/// <summary>
/// Queue statistics across all registers
/// </summary>
public record VerifiedQueueStats
{
    /// <summary>Total transactions in queue</summary>
    public int TotalTransactions { get; init; }

    /// <summary>Number of active registers with queued transactions</summary>
    public int ActiveRegisters { get; init; }

    /// <summary>Average transactions per register</summary>
    public double AverageTransactionsPerRegister { get; init; }

    /// <summary>Oldest transaction in queue (null if empty)</summary>
    public DateTimeOffset? OldestTransaction { get; init; }

    /// <summary>Newest transaction in queue (null if empty)</summary>
    public DateTimeOffset? NewestTransaction { get; init; }

    /// <summary>Total transactions enqueued since service start</summary>
    public long TotalEnqueued { get; init; }

    /// <summary>Total transactions dequeued since service start</summary>
    public long TotalDequeued { get; init; }

    /// <summary>Total transactions expired since service start</summary>
    public long TotalExpired { get; init; }
}

/// <summary>
/// Queue statistics for a specific register
/// </summary>
public record RegisterQueueStats
{
    /// <summary>Register ID</summary>
    public required string RegisterId { get; init; }

    /// <summary>Current transaction count</summary>
    public int TransactionCount { get; init; }

    /// <summary>Oldest transaction in queue (null if empty)</summary>
    public DateTimeOffset? OldestTransaction { get; init; }

    /// <summary>Newest transaction in queue (null if empty)</summary>
    public DateTimeOffset? NewestTransaction { get; init; }

    /// <summary>Average priority of queued transactions</summary>
    public double AveragePriority { get; init; }
}
