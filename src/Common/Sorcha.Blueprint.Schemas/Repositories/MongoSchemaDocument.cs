// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Sorcha.Blueprint.Schemas.Repositories;

/// <summary>
/// MongoDB document representation of a schema entry.
/// JsonDocument is stored as BsonDocument for efficient querying.
/// </summary>
internal class MongoSchemaDocument
{
    /// <summary>
    /// MongoDB document ID (uses Identifier as the key).
    /// </summary>
    [BsonId]
    public required string Id { get; set; }

    /// <summary>
    /// Schema identifier (same as Id for consistency).
    /// </summary>
    public required string Identifier { get; set; }

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Detailed description of the schema's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Semantic version (major.minor.patch).
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// Classification: System, External, or Custom.
    /// </summary>
    public required string Category { get; set; }

    /// <summary>
    /// Lifecycle state: Active or Deprecated.
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Schema source type.
    /// </summary>
    public required string SourceType { get; set; }

    /// <summary>
    /// Schema source URI.
    /// </summary>
    public string? SourceUri { get; set; }

    /// <summary>
    /// Schema source provider name.
    /// </summary>
    public string? SourceProvider { get; set; }

    /// <summary>
    /// Owning organization ID for Custom schemas.
    /// </summary>
    public string? OrganizationId { get; set; }

    /// <summary>
    /// Whether the schema is globally published.
    /// </summary>
    public bool IsGloballyPublished { get; set; }

    /// <summary>
    /// The actual JSON schema content stored as BsonDocument.
    /// </summary>
    public required BsonDocument Content { get; set; }

    /// <summary>
    /// When the schema was added to the store.
    /// </summary>
    public DateTimeOffset DateAdded { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTimeOffset DateModified { get; set; }

    /// <summary>
    /// When the schema was deprecated (if applicable).
    /// </summary>
    public DateTimeOffset? DateDeprecated { get; set; }
}
