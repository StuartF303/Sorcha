// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;

namespace Sorcha.Blueprint.Schemas.Models;

/// <summary>
/// Represents a JSON schema entry with metadata.
/// </summary>
public sealed class SchemaEntry
{
    /// <summary>
    /// Gets or sets the unique identifier (lowercase, alphanumeric with hyphens).
    /// </summary>
    public required string Identifier { get; set; }

    /// <summary>
    /// Gets or sets the human-readable display name.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Gets or sets the detailed description of the schema's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the semantic version (major.minor.patch).
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// Gets or sets the classification: System, External, or Custom.
    /// </summary>
    public required SchemaCategory Category { get; set; }

    /// <summary>
    /// Gets or sets the origin information.
    /// </summary>
    public required SchemaSource Source { get; set; }

    /// <summary>
    /// Gets or sets the lifecycle state: Active or Deprecated.
    /// </summary>
    public SchemaStatus Status { get; set; } = SchemaStatus.Active;

    /// <summary>
    /// Gets or sets the owning organization ID for Custom schemas.
    /// Null for System and External schemas.
    /// </summary>
    public string? OrganizationId { get; set; }

    /// <summary>
    /// Gets or sets whether a Custom schema is visible globally.
    /// </summary>
    public bool IsGloballyPublished { get; set; }

    /// <summary>
    /// Gets or sets the actual JSON schema content.
    /// </summary>
    public required JsonDocument Content { get; set; }

    /// <summary>
    /// Gets or sets when the schema was added to the store.
    /// </summary>
    public DateTimeOffset DateAdded { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the last modification timestamp.
    /// </summary>
    public DateTimeOffset DateModified { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets when the schema was deprecated.
    /// </summary>
    public DateTimeOffset? DateDeprecated { get; set; }

    /// <summary>
    /// Marks the schema as deprecated.
    /// </summary>
    public void Deprecate()
    {
        Status = SchemaStatus.Deprecated;
        DateDeprecated = DateTimeOffset.UtcNow;
        DateModified = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Reactivates a deprecated schema.
    /// </summary>
    public void Activate()
    {
        Status = SchemaStatus.Active;
        DateDeprecated = null;
        DateModified = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Publishes a custom schema globally.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if schema is not a Custom category.</exception>
    public void PublishGlobally()
    {
        if (Category != SchemaCategory.Custom)
        {
            throw new InvalidOperationException("Only Custom schemas can be published globally.");
        }

        IsGloballyPublished = true;
        DateModified = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Generates an ETag for cache validation.
    /// </summary>
    public string GetETag() => $"\"{Identifier}-{Version}-{DateModified.Ticks}\"";
}
