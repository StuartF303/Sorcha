// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.UI.Core.Models.Registers;

namespace Sorcha.UI.Core.Services;

/// <summary>
/// Service for interacting with the Transaction API.
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// Gets paginated transactions for a register.
    /// </summary>
    /// <param name="registerId">Register identifier.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page (max 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated transaction list.</returns>
    Task<TransactionListResponse> GetTransactionsAsync(
        string registerId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single transaction by ID.
    /// </summary>
    /// <param name="registerId">Register identifier.</param>
    /// <param name="txId">Transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transaction details or null if not found.</returns>
    Task<TransactionViewModel?> GetTransactionAsync(
        string registerId,
        string txId,
        CancellationToken cancellationToken = default);
}
