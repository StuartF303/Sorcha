using System;

namespace Sorcha.TransactionHandler.Models;

/// <summary>
/// Represents transaction metadata.
/// </summary>
public sealed class TransactionMetadata
{
    /// <summary>
    /// Gets or sets the hash of the previous transaction in the chain.
    /// </summary>
    public string? PreviousTxHash { get; set; }

    /// <summary>
    /// Gets or sets the wallet addresses of transaction recipients.
    /// </summary>
    public string[]? Recipients { get; set; }

    /// <summary>
    /// Gets or sets the JSON metadata associated with the transaction.
    /// </summary>
    public string? JsonMetadata { get; set; }

    /// <summary>
    /// Gets or sets the transaction timestamp.
    /// </summary>
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the sender's wallet address.
    /// </summary>
    public string? SenderWallet { get; set; }

    /// <summary>
    /// Gets or sets the transaction signature.
    /// </summary>
    public byte[]? Signature { get; set; }
}
