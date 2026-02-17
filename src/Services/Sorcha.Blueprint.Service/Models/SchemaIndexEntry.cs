// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Blueprint.Schemas.Models;

namespace Sorcha.Blueprint.Service.Models;

/// <summary>
/// Server-side index entry for a schema from any source.
/// Lightweight metadata — full schema content fetched on demand.
/// </summary>
public sealed class SchemaIndexEntry
{
    /// <summary>
    /// MongoDB document ID.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Provider name (e.g., "SchemaStore.org", "HL7 FHIR").
    /// </summary>
    public required string SourceProvider { get; set; }

    /// <summary>
    /// Original schema URL or identifier at source.
    /// </summary>
    public required string SourceUri { get; set; }

    /// <summary>
    /// Human-readable schema title.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Plain-language description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Platform-curated sector labels.
    /// </summary>
    public required string[] SectorTags { get; set; }

    /// <summary>
    /// Search keywords extracted from schema content.
    /// </summary>
    public string[]? Keywords { get; set; }

    /// <summary>
    /// Number of top-level properties.
    /// </summary>
    public int FieldCount { get; set; }

    /// <summary>
    /// Names of top-level properties.
    /// </summary>
    public string[]? FieldNames { get; set; }

    /// <summary>
    /// Names of required properties.
    /// </summary>
    public string[]? RequiredFields { get; set; }

    /// <summary>
    /// Version from source (e.g., "R5", "2.3", "1.0.0").
    /// </summary>
    public string SchemaVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Target JSON Schema draft after normalisation.
    /// </summary>
    public string JsonSchemaDraft { get; set; } = "2020-12";

    /// <summary>
    /// SHA-256 hex hash of normalised schema content for change detection.
    /// </summary>
    public string? ContentHash { get; set; }

    /// <summary>
    /// Index status: Active, Deprecated, or Unavailable.
    /// </summary>
    public SchemaIndexStatus Status { get; set; } = SchemaIndexStatus.Active;

    /// <summary>
    /// When this entry was last refreshed from source.
    /// </summary>
    public DateTimeOffset LastFetchedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When first indexed.
    /// </summary>
    public DateTimeOffset DateAdded { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When last modified.
    /// </summary>
    public DateTimeOffset DateModified { get; set; } = DateTimeOffset.UtcNow;
}
