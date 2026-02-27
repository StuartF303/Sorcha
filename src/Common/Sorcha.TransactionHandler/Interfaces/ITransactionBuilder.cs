// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors
using System.Threading;
using System.Threading.Tasks;
using Sorcha.TransactionHandler.Enums;
using Sorcha.TransactionHandler.Models;

namespace Sorcha.TransactionHandler.Interfaces;

/// <summary>
/// Builder interface for creating transactions using a fluent API.
/// </summary>
public interface ITransactionBuilder
{
    /// <summary>
    /// Creates a new transaction with the specified version.
    /// </summary>
    /// <param name="version">The transaction version (default: V1)</param>
    /// <returns>The builder instance for chaining</returns>
    ITransactionBuilder Create(TransactionVersion version = TransactionVersion.V1);

    /// <summary>
    /// Sets the previous transaction hash for chaining.
    /// </summary>
    /// <param name="txHash">The previous transaction hash</param>
    /// <returns>The builder instance for chaining</returns>
    ITransactionBuilder WithPreviousTransaction(string txHash);

    /// <summary>
    /// Sets the recipient wallet addresses.
    /// </summary>
    /// <param name="walletAddresses">The recipient wallet addresses</param>
    /// <returns>The builder instance for chaining</returns>
    ITransactionBuilder WithRecipients(params string[] walletAddresses);

    /// <summary>
    /// Sets the transaction metadata from a JSON string.
    /// </summary>
    /// <param name="jsonMetadata">The JSON metadata</param>
    /// <returns>The builder instance for chaining</returns>
    ITransactionBuilder WithMetadata(string jsonMetadata);

    /// <summary>
    /// Sets the transaction metadata from an object.
    /// </summary>
    /// <typeparam name="T">The metadata type</typeparam>
    /// <param name="metadata">The metadata object</param>
    /// <returns>The builder instance for chaining</returns>
    ITransactionBuilder WithMetadata<T>(T metadata) where T : class;

    /// <summary>
    /// Adds a payload to the transaction.
    /// </summary>
    /// <param name="data">The payload data</param>
    /// <param name="recipientWallets">The recipient wallet addresses</param>
    /// <param name="options">Optional payload options</param>
    /// <returns>The builder instance for chaining</returns>
    ITransactionBuilder AddPayload(
        byte[] data,
        string[] recipientWallets,
        PayloadOptions? options = null);

    /// <summary>
    /// Signs the transaction with the sender's private key.
    /// </summary>
    /// <param name="wifPrivateKey">The WIF-encoded private key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The builder instance for chaining</returns>
    Task<ITransactionBuilder> SignAsync(
        string wifPrivateKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds and returns the final transaction.
    /// </summary>
    /// <returns>The transaction result</returns>
    TransactionResult<ITransaction> Build();
}
