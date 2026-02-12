// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Blueprint.Service.Models.Responses;

namespace Sorcha.Blueprint.Service.Storage;

/// <summary>
/// Storage interface for action transactions
/// </summary>
public interface IActionStore
{
    /// <summary>
    /// Store an action transaction
    /// </summary>
    Task<ActionDetailsResponse> StoreActionAsync(ActionDetailsResponse action);

    /// <summary>
    /// Get an action by transaction hash
    /// </summary>
    Task<ActionDetailsResponse?> GetActionAsync(string transactionHash);

    /// <summary>
    /// Get all actions for a wallet/register combination
    /// </summary>
    Task<IEnumerable<ActionDetailsResponse>> GetActionsAsync(
        string walletAddress,
        string registerAddress,
        int skip = 0,
        int take = 20);

    /// <summary>
    /// Get total count of actions for a wallet/register
    /// </summary>
    Task<int> GetActionCountAsync(string walletAddress, string registerAddress);

    /// <summary>
    /// Store file metadata
    /// </summary>
    Task StoreFileMetadataAsync(string transactionHash, string fileId, FileMetadata metadata);

    /// <summary>
    /// Get file metadata
    /// </summary>
    Task<FileMetadata?> GetFileMetadataAsync(string transactionHash, string fileId);

    /// <summary>
    /// Store file content
    /// </summary>
    Task StoreFileContentAsync(string fileId, byte[] content);

    /// <summary>
    /// Get file content
    /// </summary>
    Task<byte[]?> GetFileContentAsync(string fileId);
}
