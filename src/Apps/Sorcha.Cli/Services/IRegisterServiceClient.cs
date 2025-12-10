using Refit;
using Sorcha.Cli.Models;

namespace Sorcha.Cli.Services;

/// <summary>
/// Refit client interface for the Register Service API.
/// </summary>
public interface IRegisterServiceClient
{
    #region Registers

    /// <summary>
    /// Lists all registers.
    /// </summary>
    [Get("/api/registers")]
    Task<List<Register>> ListRegistersAsync([Header("Authorization")] string authorization);

    /// <summary>
    /// Gets a register by ID.
    /// </summary>
    [Get("/api/registers/{id}")]
    Task<Register> GetRegisterAsync(string id, [Header("Authorization")] string authorization);

    /// <summary>
    /// Creates a new register.
    /// </summary>
    [Post("/api/registers")]
    Task<Register> CreateRegisterAsync([Body] CreateRegisterRequest request, [Header("Authorization")] string authorization);

    /// <summary>
    /// Deletes a register.
    /// </summary>
    [Delete("/api/registers/{id}")]
    Task DeleteRegisterAsync(string id, [Header("Authorization")] string authorization);

    #endregion

    #region Transactions

    /// <summary>
    /// Lists all transactions in a register.
    /// </summary>
    [Get("/api/registers/{registerId}/transactions")]
    Task<List<Transaction>> ListTransactionsAsync(
        string registerId,
        [Query] int? skip,
        [Query] int? take,
        [Header("Authorization")] string authorization);

    /// <summary>
    /// Gets a transaction by ID.
    /// </summary>
    [Get("/api/registers/{registerId}/transactions/{transactionId}")]
    Task<Transaction> GetTransactionAsync(
        string registerId,
        string transactionId,
        [Header("Authorization")] string authorization);

    /// <summary>
    /// Submits a new transaction to a register.
    /// </summary>
    [Post("/api/registers/{registerId}/transactions")]
    Task<SubmitTransactionResponse> SubmitTransactionAsync(
        string registerId,
        [Body] SubmitTransactionRequest request,
        [Header("Authorization")] string authorization);

    /// <summary>
    /// Gets the status of a transaction.
    /// </summary>
    [Get("/api/registers/{registerId}/transactions/{transactionId}/status")]
    Task<SubmitTransactionResponse> GetTransactionStatusAsync(
        string registerId,
        string transactionId,
        [Header("Authorization")] string authorization);

    #endregion
}
