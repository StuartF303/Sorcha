// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Represents a transaction notification for gossip protocol distribution
/// </summary>
public class TransactionNotification
{
    /// <summary>
    /// Unique identifier for the transaction
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// ID of the peer that originated this transaction
    /// </summary>
    [Required]
    public string OriginPeerId { get; set; } = string.Empty;

    /// <summary>
    /// When the transaction was created
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Size of the transaction data in bytes
    /// </summary>
    public int DataSize { get; set; }

    /// <summary>
    /// Hash of the transaction data for integrity verification
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string DataHash { get; set; } = string.Empty;

    /// <summary>
    /// Current gossip round number
    /// </summary>
    public int GossipRound { get; set; } = 0;

    /// <summary>
    /// Number of hops this notification has traveled
    /// </summary>
    public int HopCount { get; set; } = 0;

    /// <summary>
    /// Time to live for this notification (in seconds)
    /// </summary>
    public int TTL { get; set; } = 3600; // 1 hour default

    /// <summary>
    /// Whether this notification includes the full transaction data
    /// </summary>
    public bool HasFullData { get; set; } = false;

    /// <summary>
    /// The actual transaction data (may be null if not included)
    /// </summary>
    public byte[]? TransactionData { get; set; }
}
