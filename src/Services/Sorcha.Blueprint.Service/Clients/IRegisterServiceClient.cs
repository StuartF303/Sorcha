// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Register.Models;

namespace Sorcha.Blueprint.Service.Clients;

/// <summary>
/// Client interface for interacting with the Register Service
/// </summary>
public interface IRegisterServiceClient
{
    /// <summary>
    /// Submits a transaction to a register
    /// </summary>
    /// <param name="registerId">The register ID to submit to</param>
    /// <param name="transaction">The transaction to submit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The stored transaction with confirmation</returns>
    Task<TransactionModel> SubmitTransactionAsync(
        string registerId,
        TransactionModel transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a transaction by ID from a register
    /// </summary>
    /// <param name="registerId">The register ID</param>
    /// <param name="transactionId">The transaction ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transaction, or null if not found</returns>
    Task<TransactionModel?> GetTransactionAsync(
        string registerId,
        string transactionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list of transactions from a register
    /// </summary>
    /// <param name="registerId">The register ID</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of transactions per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated transaction list</returns>
    Task<TransactionPage> GetTransactionsAsync(
        string registerId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transactions for a specific wallet address
    /// </summary>
    /// <param name="registerId">The register ID</param>
    /// <param name="walletAddress">The wallet address to query</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of transactions per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated transaction list</returns>
    Task<TransactionPage> GetTransactionsByWalletAsync(
        string registerId,
        string walletAddress,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets register information by ID
    /// </summary>
    /// <param name="registerId">The register ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Register information, or null if not found</returns>
    Task<Sorcha.Register.Models.Register?> GetRegisterAsync(
        string registerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all transactions associated with a workflow instance.
    /// Used for state reconstruction during action execution.
    /// </summary>
    /// <param name="registerId">The register ID</param>
    /// <param name="instanceId">The workflow instance ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transactions for the instance, ordered by execution time</returns>
    Task<List<TransactionModel>> GetTransactionsByInstanceIdAsync(
        string registerId,
        string instanceId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Paginated transaction results
/// </summary>
public class TransactionPage
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<TransactionModel> Transactions { get; set; } = new();
}
