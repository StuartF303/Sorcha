// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sorcha.Register.Models;

/// <summary>
/// Represents a signed transaction in a register with JSON-LD support
/// </summary>
public class TransactionModel
{
    /// <summary>
    /// JSON-LD context for semantic web integration
    /// </summary>
    [JsonPropertyName("@context")]
    public string? Context { get; set; } = "https://sorcha.dev/contexts/blockchain/v1.jsonld";

    /// <summary>
    /// JSON-LD type designation
    /// </summary>
    [JsonPropertyName("@type")]
    public string? Type { get; set; } = "Transaction";

    /// <summary>
    /// JSON-LD universal identifier (DID URI)
    /// Format: did:sorcha:register:{registerId}/tx/{txId}
    /// </summary>
    [JsonPropertyName("@id")]
    public string? Id { get; set; }

    /// <summary>
    /// Register identifier this transaction belongs to
    /// </summary>
    [Required]
    public string RegisterId { get; set; } = string.Empty;

    /// <summary>
    /// Transaction identifier (64 character hex hash)
    /// </summary>
    [Required]
    [StringLength(64, MinimumLength = 64)]
    public string TxId { get; set; } = string.Empty;

    /// <summary>
    /// Previous transaction ID for blockchain chain
    /// </summary>
    [StringLength(64, MinimumLength = 64)]
    public string PrevTxId { get; set; } = string.Empty;

    /// <summary>
    /// Block number (docket ID) this transaction is sealed in
    /// </summary>
    public ulong? BlockNumber { get; set; }

    /// <summary>
    /// Transaction format version
    /// </summary>
    public uint Version { get; set; } = 1;

    /// <summary>
    /// Sender wallet address (Base58 encoded)
    /// </summary>
    [Required]
    public string SenderWallet { get; set; } = string.Empty;

    /// <summary>
    /// Recipient wallet addresses
    /// </summary>
    public IEnumerable<string> RecipientsWallets { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Transaction timestamp (UTC)
    /// </summary>
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Blueprint and workflow metadata
    /// </summary>
    public TransactionMetaData? MetaData { get; set; }

    /// <summary>
    /// Number of payloads in transaction
    /// </summary>
    public ulong PayloadCount { get; set; }

    /// <summary>
    /// Encrypted data payloads
    /// </summary>
    public PayloadModel[] Payloads { get; set; } = Array.Empty<PayloadModel>();

    /// <summary>
    /// Cryptographic signature of transaction
    /// </summary>
    [Required]
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Generates the DID URI for this transaction
    /// </summary>
    public string GenerateDidUri() => $"did:sorcha:register:{RegisterId}/tx/{TxId}";
}
