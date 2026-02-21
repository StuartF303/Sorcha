// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using MongoDB.Bson.Serialization.Attributes;
using Sorcha.Register.Storage.MongoDB.Serialization;

namespace Sorcha.Register.Storage.MongoDB.Models;

/// <summary>
/// MongoDB-specific challenge document with native BSON Binary storage for key data.
/// Maps 1:1 to <see cref="Register.Models.Challenge"/>.
/// </summary>
public class MongoChallengeDocument
{
    /// <summary>
    /// Encrypted symmetric key bytes (BSON Binary subtype 0x00).
    /// Legacy documents may contain Base64/Base64url strings — the custom serializer handles both.
    /// </summary>
    [BsonSerializer(typeof(NullableBase64UrlBinarySerializer))]
    public byte[]? Data { get; set; }

    /// <summary>
    /// Wallet address this challenge targets.
    /// </summary>
    public string? Address { get; set; }
}
