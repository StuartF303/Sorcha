// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Blueprint.Service.Models;
using Sorcha.Blueprint.Service.Models.Requests;
using Sorcha.TransactionHandler.Core;
using ActionModel = Sorcha.Blueprint.Models.Action;
using BlueprintModel = Sorcha.Blueprint.Models.Blueprint;

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

/// <summary>
/// Extended transaction builder service for orchestration
/// </summary>
public static class TransactionBuilderServiceExtensions
{
    /// <summary>
    /// Builds an action transaction using orchestration context
    /// </summary>
    public static Task<BuiltTransaction> BuildActionTransactionAsync(
        this ITransactionBuilderService service,
        BlueprintModel blueprint,
        Instance instance,
        ActionModel action,
        Dictionary<string, object> payloadData,
        Dictionary<string, Dictionary<string, object>> disclosedPayloads,
        string? previousTransactionId,
        CancellationToken cancellationToken = default)
    {
        // This extension method bridges the new orchestration model to the existing interface
        // The actual implementation would encrypt payloads per recipient
        return Task.FromResult(new BuiltTransaction
        {
            TransactionData = [],
            TxId = Guid.NewGuid().ToString(),
            Metadata = new Dictionary<string, object>
            {
                ["blueprintId"] = blueprint.Id,
                ["actionId"] = action.Id,
                ["instanceId"] = instance.Id,
                ["previousTxId"] = previousTransactionId ?? ""
            }
        });
    }

    /// <summary>
    /// Builds a rejection transaction using orchestration context
    /// </summary>
    public static Task<BuiltTransaction> BuildRejectionTransactionAsync(
        this ITransactionBuilderService service,
        BlueprintModel blueprint,
        Instance instance,
        ActionModel action,
        Dictionary<string, object> rejectionData,
        string? previousTransactionId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new BuiltTransaction
        {
            TransactionData = [],
            TxId = Guid.NewGuid().ToString(),
            TransactionType = "rejection",
            Metadata = new Dictionary<string, object>
            {
                ["blueprintId"] = blueprint.Id,
                ["actionId"] = action.Id,
                ["instanceId"] = instance.Id,
                ["previousTxId"] = previousTransactionId ?? "",
                ["rejectionReason"] = rejectionData.GetValueOrDefault("rejectionReason", "")!
            }
        });
    }
}

/// <summary>
/// Represents a built transaction ready for signing and submission
/// </summary>
public class BuiltTransaction
{
    /// <summary>
    /// The raw transaction data for signing
    /// </summary>
    public required byte[] TransactionData { get; init; }

    /// <summary>
    /// The transaction ID
    /// </summary>
    public required string TxId { get; init; }

    /// <summary>
    /// Transaction type (action, rejection, file)
    /// </summary>
    public string TransactionType { get; init; } = "action";

    /// <summary>
    /// The signature (populated after signing)
    /// </summary>
    public byte[]? Signature { get; set; }

    /// <summary>
    /// Transaction metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Converts to a TransactionModel for submission
    /// </summary>
    public Sorcha.Register.Models.TransactionModel ToTransactionModel()
    {
        var metaData = new Sorcha.Register.Models.TransactionMetaData
        {
            BlueprintId = Metadata.GetValueOrDefault("blueprintId")?.ToString(),
            InstanceId = Metadata.GetValueOrDefault("instanceId")?.ToString()
        };

        if (Metadata.TryGetValue("actionId", out var actionIdObj) && actionIdObj is int actionId)
        {
            metaData.ActionId = (uint)actionId;
        }

        return new Sorcha.Register.Models.TransactionModel
        {
            TxId = TxId,
            MetaData = metaData,
            TimeStamp = DateTime.UtcNow
        };
    }
}
