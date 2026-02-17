// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;

namespace Sorcha.Blueprint.Service.Models;

/// <summary>
/// A subset of a formal schema created when a designer selects specific fields.
/// </summary>
public sealed class DerivedSchema
{
    /// <summary>
    /// MongoDB document ID.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Source provider of parent schema.
    /// </summary>
    public required string ParentSourceProvider { get; set; }

    /// <summary>
    /// Source URI of parent schema.
    /// </summary>
    public required string ParentSourceUri { get; set; }

    /// <summary>
    /// Title of parent schema.
    /// </summary>
    public required string ParentTitle { get; set; }

    /// <summary>
    /// Field names selected for inclusion.
    /// </summary>
    public required string[] IncludedFields { get; set; }

    /// <summary>
    /// Field names explicitly excluded.
    /// </summary>
    public string[]? ExcludedFields { get; set; }

    /// <summary>
    /// The subset JSON Schema.
    /// </summary>
    public required JsonDocument DerivedContent { get; set; }

    /// <summary>
    /// Owning organisation.
    /// </summary>
    public required string OrganizationId { get; set; }

    /// <summary>
    /// When derived.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// User who created the derivation.
    /// </summary>
    public required string CreatedByUserId { get; set; }
}
