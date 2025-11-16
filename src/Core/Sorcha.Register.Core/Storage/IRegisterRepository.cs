// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Linq.Expressions;
using Sorcha.Register.Models;

namespace Sorcha.Register.Core.Storage;

/// <summary>
/// Repository interface for register operations
/// </summary>
public interface IRegisterRepository
{
    // ===========================
    // Register Operations
    // ===========================

    /// <summary>
    /// Checks if a register exists locally
    /// </summary>
    /// <param name="registerId">Register identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if register exists locally</returns>
    Task<bool> IsLocalRegisterAsync(string registerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registers
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all registers</returns>
    Task<IEnumerable<Models.Register>> GetRegistersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries registers using a predicate
    /// </summary>
    /// <param name="predicate">Filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered list of registers</returns>
    Task<IEnumerable<Models.Register>> QueryRegistersAsync(
        Func<Models.Register, bool> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific register by ID
    /// </summary>
    /// <param name="registerId">Register identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Register if found, null otherwise</returns>
    Task<Models.Register?> GetRegisterAsync(string registerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a new register
    /// </summary>
    /// <param name="newRegister">Register to insert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Inserted register</returns>
    Task<Models.Register> InsertRegisterAsync(Models.Register newRegister, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing register
    /// </summary>
    /// <param name="register">Register to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated register</returns>
    Task<Models.Register> UpdateRegisterAsync(Models.Register register, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a register
    /// </summary>
    /// <param name="registerId">Register identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteRegisterAsync(string registerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts total number of registers
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total count of registers</returns>
    Task<int> CountRegistersAsync(CancellationToken cancellationToken = default);

    // ===========================
    // Docket Operations
    // ===========================

    /// <summary>
    /// Gets all dockets for a register
    /// </summary>
    /// <param name="registerId">Register identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of dockets</returns>
    Task<IEnumerable<Docket>> GetDocketsAsync(
        string registerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific docket by register and docket ID
    /// </summary>
    /// <param name="registerId">Register identifier</param>
    /// <param name="docketId">Docket identifier (block height)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Docket if found, null otherwise</returns>
    Task<Docket?> GetDocketAsync(
        string registerId,
        ulong docketId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a new docket
    /// </summary>
    /// <param name="docket">Docket to insert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Inserted docket</returns>
    Task<Docket> InsertDocketAsync(Docket docket, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the register height atomically
    /// </summary>
    /// <param name="registerId">Register identifier</param>
    /// <param name="newHeight">New height value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateRegisterHeightAsync(
        string registerId,
        uint newHeight,
        CancellationToken cancellationToken = default);

    // ===========================
    // Transaction Operations
    // ===========================

    /// <summary>
    /// Gets all transactions for a register (queryable)
    /// </summary>
    /// <param name="registerId">Register identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Queryable collection of transactions</returns>
    Task<IQueryable<TransactionModel>> GetTransactionsAsync(
        string registerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific transaction by register and transaction ID
    /// </summary>
    /// <param name="registerId">Register identifier</param>
    /// <param name="transactionId">Transaction identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transaction if found, null otherwise</returns>
    Task<TransactionModel?> GetTransactionAsync(
        string registerId,
        string transactionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a new transaction
    /// </summary>
    /// <param name="transaction">Transaction to insert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Inserted transaction</returns>
    Task<TransactionModel> InsertTransactionAsync(
        TransactionModel transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries transactions using LINQ expression
    /// </summary>
    /// <param name="registerId">Register identifier</param>
    /// <param name="predicate">LINQ expression predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered list of transactions</returns>
    Task<IEnumerable<TransactionModel>> QueryTransactionsAsync(
        string registerId,
        Expression<Func<TransactionModel, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all transactions in a specific docket
    /// </summary>
    /// <param name="registerId">Register identifier</param>
    /// <param name="docketId">Docket identifier (block height)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transactions in the docket</returns>
    Task<IEnumerable<TransactionModel>> GetTransactionsByDocketAsync(
        string registerId,
        ulong docketId,
        CancellationToken cancellationToken = default);

    // ===========================
    // Advanced Queries
    // ===========================

    /// <summary>
    /// Gets all transactions where the address is a recipient
    /// </summary>
    /// <param name="registerId">Register identifier</param>
    /// <param name="address">Wallet address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transactions</returns>
    Task<IEnumerable<TransactionModel>> GetAllTransactionsByRecipientAddressAsync(
        string registerId,
        string address,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all transactions where the address is the sender
    /// </summary>
    /// <param name="registerId">Register identifier</param>
    /// <param name="address">Wallet address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transactions</returns>
    Task<IEnumerable<TransactionModel>> GetAllTransactionsBySenderAddressAsync(
        string registerId,
        string address,
        CancellationToken cancellationToken = default);
}
