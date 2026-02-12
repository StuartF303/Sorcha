// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Validator.Service.Models;

namespace Sorcha.Validator.Service.Services.Interfaces;

/// <summary>
/// Receives transactions from external sources (Peer Service, API endpoints).
/// Handles:
/// - Transaction reception from peer network via gRPC
/// - Transaction submission to memory pool for validation
/// - Transaction acknowledgment back to sender
/// </summary>
public interface ITransactionReceiver
{
    /// <summary>
    /// Receives a transaction from the peer network.
    /// </summary>
    /// <param name="transactionHash">Hash of the incoming transaction</param>
    /// <param name="transactionData">Serialized transaction data</param>
    /// <param name="senderPeerId">ID of the sending peer</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Reception result indicating success and any validation errors</returns>
    Task<TransactionReceptionResult> ReceiveTransactionAsync(
        string transactionHash,
        byte[] transactionData,
        string senderPeerId,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a transaction is already known (in mempool or confirmed).
    /// </summary>
    /// <param name="transactionHash">Transaction hash to check</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if transaction is already known</returns>
    Task<bool> IsTransactionKnownAsync(
        string transactionHash,
        CancellationToken ct = default);

    /// <summary>
    /// Gets reception statistics.
    /// </summary>
    /// <returns>Reception statistics</returns>
    TransactionReceiverStats GetStats();
}

/// <summary>
/// Result of receiving a transaction.
/// </summary>
public record TransactionReceptionResult
{
    /// <summary>Whether the transaction was accepted</summary>
    public bool Accepted { get; init; }

    /// <summary>Whether the transaction was already known</summary>
    public bool AlreadyKnown { get; init; }

    /// <summary>Validation errors if transaction was rejected</summary>
    public IReadOnlyList<string> ValidationErrors { get; init; } = Array.Empty<string>();

    /// <summary>Transaction ID if successfully parsed</summary>
    public string? TransactionId { get; init; }

    /// <summary>When the transaction was received</summary>
    public DateTimeOffset ReceivedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Statistics for transaction reception.
/// </summary>
public record TransactionReceiverStats
{
    /// <summary>Total transactions received</summary>
    public long TotalReceived { get; init; }

    /// <summary>Total transactions accepted</summary>
    public long TotalAccepted { get; init; }

    /// <summary>Total transactions rejected</summary>
    public long TotalRejected { get; init; }

    /// <summary>Total duplicate transactions received</summary>
    public long TotalDuplicates { get; init; }

    /// <summary>Transactions received per second (rolling average)</summary>
    public double TransactionsPerSecond { get; init; }

    /// <summary>Last transaction received at</summary>
    public DateTimeOffset? LastReceivedAt { get; init; }
}
