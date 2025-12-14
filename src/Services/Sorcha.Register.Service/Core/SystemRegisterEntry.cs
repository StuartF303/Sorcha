// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Sorcha.Register.Service.Core;

/// <summary>
/// Represents a blueprint document in the system register MongoDB collection
/// </summary>
public class SystemRegisterEntry
{
    /// <summary>
    /// Unique blueprint identifier (MongoDB _id)
    /// </summary>
    [BsonId]
    [Required]
    [MaxLength(255)]
    [RegularExpression(@"^[a-zA-Z0-9\-_]+$", ErrorMessage = "Blueprint ID must contain only alphanumeric characters, hyphens, and underscores")]
    public string BlueprintId { get; set; } = string.Empty;

    /// <summary>
    /// System register identifier (well-known constant: 00000000-0000-0000-0000-000000000000)
    /// </summary>
    [BsonElement("registerId")]
    [BsonRepresentation(BsonType.String)]
    [Required]
    public Guid RegisterId { get; set; } = Guid.Empty;

    /// <summary>
    /// Blueprint JSON document
    /// </summary>
    [BsonElement("document")]
    [Required]
    public BsonDocument Document { get; set; } = new();

    /// <summary>
    /// Timestamp when blueprint was published (UTC)
    /// </summary>
    [BsonElement("publishedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Required]
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Identity of publisher (user ID or "system")
    /// </summary>
    [BsonElement("publishedBy")]
    [Required]
    [MaxLength(255)]
    public string PublishedBy { get; set; } = string.Empty;

    /// <summary>
    /// Incrementing version number for sync (auto-increment)
    /// </summary>
    [BsonElement("version")]
    [Required]
    public long Version { get; set; }

    /// <summary>
    /// Whether blueprint is active/available
    /// </summary>
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Link to register transaction that published this blueprint (optional)
    /// </summary>
    [BsonElement("publicationTransactionId")]
    [BsonIgnoreIfNull]
    public string? PublicationTransactionId { get; set; }

    /// <summary>
    /// SHA-256 checksum of Document for integrity verification (optional)
    /// </summary>
    [BsonElement("checksum")]
    [BsonIgnoreIfNull]
    public string? Checksum { get; set; }

    /// <summary>
    /// Optional metadata key-value pairs
    /// </summary>
    [BsonElement("metadata")]
    [BsonIgnoreIfNull]
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Validates that RegisterId is the well-known system register ID
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when RegisterId is not the system register ID</exception>
    public void ValidateSystemRegister()
    {
        if (RegisterId != Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Invalid system register ID: {RegisterId}. Must be {Guid.Empty}");
        }
    }
}
