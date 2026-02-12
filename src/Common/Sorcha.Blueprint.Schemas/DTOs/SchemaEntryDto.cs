// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Blueprint.Schemas.DTOs;

/// <summary>
/// Data transfer object for schema entry metadata (without content).
/// </summary>
/// <param name="Identifier">Unique schema identifier.</param>
/// <param name="Title">Human-readable display name.</param>
/// <param name="Description">Detailed description.</param>
/// <param name="Version">Semantic version.</param>
/// <param name="Category">Schema category: System, External, Custom.</param>
/// <param name="Status">Lifecycle status: Active, Deprecated.</param>
/// <param name="Source">Origin information.</param>
/// <param name="OrganizationId">Owning organization (for Custom).</param>
/// <param name="IsGloballyPublished">Whether Custom schema is visible globally.</param>
/// <param name="DateAdded">When schema was added.</param>
/// <param name="DateModified">Last modification timestamp.</param>
/// <param name="DateDeprecated">When schema was deprecated.</param>
public sealed record SchemaEntryDto(
    string Identifier,
    string Title,
    string? Description,
    string Version,
    string Category,
    string Status,
    SchemaSourceDto Source,
    string? OrganizationId,
    bool IsGloballyPublished,
    DateTimeOffset DateAdded,
    DateTimeOffset DateModified,
    DateTimeOffset? DateDeprecated
);
