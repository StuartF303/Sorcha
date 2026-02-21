// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using MongoDB.Bson.Serialization.Attributes;
using Sorcha.Register.Storage.MongoDB.Serialization;

namespace Sorcha.Register.Storage.MongoDB.Models;

/// <summary>
/// MongoDB-specific payload document with native BSON Binary storage for data and hash fields.
/// Maps 1:1 to <see cref="Register.Models.PayloadModel"/>.
/// ~33% storage reduction for binary fields compared to Base64 string storage.
/// </summary>
public class MongoPayloadDocument
{
    /// <summary>
    /// Wallet addresses authorized to decrypt this payload.
    /// </summary>
    public string[] WalletAccess { get; set; } = [];

    /// <summary>
    /// Size of payload in bytes.
    /// </summary>
    public ulong PayloadSize { get; set; }

    /// <summary>
    /// SHA-256 hash of payload (BSON Binary).
    /// Legacy documents may contain Base64/Base64url strings.
    /// </summary>
    [BsonSerializer(typeof(Base64UrlBinarySerializer))]
    public byte[] Hash { get; set; } = [];

    /// <summary>
    /// Payload data (BSON Binary).
    /// Legacy documents may contain Base64/Base64url strings.
    /// </summary>
    [BsonSerializer(typeof(Base64UrlBinarySerializer))]
    public byte[] Data { get; set; } = [];

    /// <summary>
    /// Encryption metadata flags.
    /// </summary>
    public string? PayloadFlags { get; set; }

    /// <summary>
    /// Initialization vector for encryption.
    /// </summary>
    public MongoChallengeDocument? IV { get; set; }

    /// <summary>
    /// Per-wallet encryption challenges.
    /// </summary>
    public MongoChallengeDocument[]? Challenges { get; set; }

    /// <summary>
    /// MIME type describing the plaintext data format (e.g., "application/json").
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Encoding scheme for the Data field (e.g., "base64url", "identity", "br+base64url").
    /// </summary>
    public string? ContentEncoding { get; set; }
}
