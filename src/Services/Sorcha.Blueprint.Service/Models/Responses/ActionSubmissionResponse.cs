// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Blueprint.Service.Models.Responses;

/// <summary>
/// Response from submitting an action
/// </summary>
public record ActionSubmissionResponse
{
    /// <summary>
    /// The transaction hash
    /// </summary>
    public required string TransactionHash { get; init; }

    /// <summary>
    /// The workflow instance ID
    /// </summary>
    public required string InstanceId { get; init; }

    /// <summary>
    /// The serialized transaction (for signing by wallet)
    /// </summary>
    public required string SerializedTransaction { get; init; }

    /// <summary>
    /// File transaction hashes (if files were attached)
    /// </summary>
    public List<string>? FileTransactionHashes { get; init; }

    /// <summary>
    /// Timestamp when the transaction was created
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
