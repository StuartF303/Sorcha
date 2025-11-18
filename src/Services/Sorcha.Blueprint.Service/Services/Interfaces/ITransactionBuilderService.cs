// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Blueprint.Service.Models.Requests;
using Sorcha.TransactionHandler.Core;

namespace Sorcha.Blueprint.Service.Services.Interfaces;

/// <summary>
/// Service for building blockchain transactions
/// </summary>
public interface ITransactionBuilderService
{
    /// <summary>
    /// Builds an action transaction with metadata
    /// </summary>
    /// <param name="blueprintId">The blueprint ID</param>
    /// <param name="actionId">The action ID</param>
    /// <param name="instanceId">The workflow instance ID (generated if new)</param>
    /// <param name="previousTransactionHash">Optional hash of previous transaction in the workflow</param>
    /// <param name="encryptedPayloads">Dictionary mapping wallet addresses to encrypted payloads</param>
    /// <param name="senderWallet">The sender's wallet address</param>
    /// <param name="registerAddress">The register address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The built transaction</returns>
    Task<Transaction> BuildActionTransactionAsync(
        string blueprintId,
        string actionId,
        string? instanceId,
        string? previousTransactionHash,
        Dictionary<string, byte[]> encryptedPayloads,
        string senderWallet,
        string registerAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a rejection transaction
    /// </summary>
    /// <param name="originalTransactionHash">The transaction being rejected</param>
    /// <param name="reason">The reason for rejection</param>
    /// <param name="senderWallet">The sender's wallet address</param>
    /// <param name="registerAddress">The register address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The built rejection transaction</returns>
    Task<Transaction> BuildRejectionTransactionAsync(
        string originalTransactionHash,
        string reason,
        string senderWallet,
        string registerAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds file transactions for attachments
    /// </summary>
    /// <param name="files">The file attachments</param>
    /// <param name="parentTransactionHash">The parent transaction hash</param>
    /// <param name="senderWallet">The sender's wallet address</param>
    /// <param name="registerAddress">The register address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of file transactions</returns>
    Task<List<Transaction>> BuildFileTransactionsAsync(
        IEnumerable<FileAttachment> files,
        string parentTransactionHash,
        string senderWallet,
        string registerAddress,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a file attachment
/// </summary>
public record FileAttachment(
    string FileName,
    string ContentType,
    byte[] Content
);
