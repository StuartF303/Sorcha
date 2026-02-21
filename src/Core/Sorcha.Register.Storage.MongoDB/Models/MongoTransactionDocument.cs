// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Sorcha.Register.Models;
using Sorcha.Register.Storage.MongoDB.Serialization;

namespace Sorcha.Register.Storage.MongoDB.Models;

/// <summary>
/// MongoDB-specific transaction document with native BSON Binary storage for signature and payload fields.
/// Maps 1:1 to <see cref="TransactionModel"/> but stores binary fields as BSON Binary subtype 0x00
/// instead of Base64/Base64url strings, providing ~33% storage reduction.
/// </summary>
/// <remarks>
/// Internal to Sorcha.Register.Storage.MongoDB — not exposed via IRegisterRepository.
/// Legacy documents with string-format binary fields are automatically handled by
/// <see cref="Base64UrlBinarySerializer"/> during deserialization.
/// </remarks>
public class MongoTransactionDocument
{
    /// <summary>
    /// MongoDB document ID.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfDefault]
    public string? MongoId { get; set; }

    /// <summary>
    /// JSON-LD context for semantic web integration.
    /// </summary>
    [BsonElement("Context")]
    public string? Context { get; set; }

    /// <summary>
    /// JSON-LD type designation.
    /// </summary>
    [BsonElement("Type")]
    public string? Type { get; set; }

    /// <summary>
    /// JSON-LD universal identifier (DID URI).
    /// </summary>
    [BsonElement("Id")]
    public string? Id { get; set; }

    /// <summary>
    /// Register identifier this transaction belongs to.
    /// </summary>
    public string RegisterId { get; set; } = string.Empty;

    /// <summary>
    /// Transaction identifier (64 character hex hash).
    /// </summary>
    public string TxId { get; set; } = string.Empty;

    /// <summary>
    /// Previous transaction ID for chain linking.
    /// </summary>
    public string PrevTxId { get; set; } = string.Empty;

    /// <summary>
    /// Docket number this transaction is sealed in.
    /// </summary>
    public ulong? DocketNumber { get; set; }

    /// <summary>
    /// Transaction format version.
    /// </summary>
    public uint Version { get; set; } = 1;

    /// <summary>
    /// Sender wallet address (Base58 encoded).
    /// </summary>
    public string SenderWallet { get; set; } = string.Empty;

    /// <summary>
    /// Recipient wallet addresses.
    /// </summary>
    public IEnumerable<string> RecipientsWallets { get; set; } = [];

    /// <summary>
    /// Transaction timestamp (UTC).
    /// </summary>
    public DateTime TimeStamp { get; set; }

    /// <summary>
    /// Blueprint and workflow metadata.
    /// </summary>
    public TransactionMetaData? MetaData { get; set; }

    /// <summary>
    /// Number of payloads in transaction.
    /// </summary>
    public ulong PayloadCount { get; set; }

    /// <summary>
    /// Payload documents with native BSON Binary fields.
    /// </summary>
    public MongoPayloadDocument[] Payloads { get; set; } = [];

    /// <summary>
    /// Cryptographic signature (BSON Binary).
    /// Legacy documents may contain Base64/Base64url strings.
    /// </summary>
    [BsonSerializer(typeof(Base64UrlBinarySerializer))]
    public byte[] Signature { get; set; } = [];
}
