// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using Sorcha.Blueprint.Schemas.DTOs;
using Sorcha.Blueprint.Schemas.Models;

namespace Sorcha.Blueprint.Schemas.Mappers;

/// <summary>
/// Maps between SchemaEntry entities and DTOs.
/// </summary>
public static class SchemaEntryMapper
{
    /// <summary>
    /// Maps a SchemaEntry to SchemaEntryDto (without content).
    /// </summary>
    public static SchemaEntryDto ToEntryDto(this SchemaEntry entry) => new(
        Identifier: entry.Identifier,
        Title: entry.Title,
        Description: entry.Description,
        Version: entry.Version,
        Category: entry.Category.ToString(),
        Status: entry.Status.ToString(),
        Source: entry.Source.ToDto(),
        OrganizationId: entry.OrganizationId,
        IsGloballyPublished: entry.IsGloballyPublished,
        DateAdded: entry.DateAdded,
        DateModified: entry.DateModified,
        DateDeprecated: entry.DateDeprecated
    );

    /// <summary>
    /// Maps a SchemaEntry to SchemaContentDto (with content).
    /// </summary>
    public static SchemaContentDto ToContentDto(this SchemaEntry entry) => new(
        Identifier: entry.Identifier,
        Title: entry.Title,
        Description: entry.Description,
        Version: entry.Version,
        Category: entry.Category.ToString(),
        Status: entry.Status.ToString(),
        Source: entry.Source.ToDto(),
        OrganizationId: entry.OrganizationId,
        IsGloballyPublished: entry.IsGloballyPublished,
        DateAdded: entry.DateAdded,
        DateModified: entry.DateModified,
        DateDeprecated: entry.DateDeprecated,
        Content: entry.Content.RootElement.Clone()
    );

    /// <summary>
    /// Maps a SchemaSource to SchemaSourceDto.
    /// </summary>
    public static SchemaSourceDto ToDto(this SchemaSource source) => new(
        Type: source.Type.ToString(),
        Uri: source.Uri,
        Provider: source.Provider,
        FetchedAt: source.FetchedAt
    );

    /// <summary>
    /// Creates a SchemaEntry from a CreateSchemaRequest.
    /// </summary>
    public static SchemaEntry ToEntity(
        this CreateSchemaRequest request,
        string organizationId)
    {
        return new SchemaEntry
        {
            Identifier = request.Identifier,
            Title = request.Title,
            Description = request.Description,
            Version = request.Version,
            Category = SchemaCategory.Custom,
            Source = SchemaSource.Custom(),
            Status = SchemaStatus.Active,
            OrganizationId = organizationId,
            IsGloballyPublished = false,
            Content = JsonDocument.Parse(request.Content.GetRawText()),
            DateAdded = DateTimeOffset.UtcNow,
            DateModified = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Applies updates from UpdateSchemaRequest to an existing SchemaEntry.
    /// </summary>
    public static void ApplyUpdates(
        this SchemaEntry entry,
        UpdateSchemaRequest request)
    {
        if (request.Title is not null)
            entry.Title = request.Title;

        if (request.Description is not null)
            entry.Description = request.Description;

        if (request.Version is not null)
            entry.Version = request.Version;

        if (request.Content.HasValue)
            entry.Content = JsonDocument.Parse(request.Content.Value.GetRawText());

        entry.DateModified = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Maps a collection of SchemaEntry to SchemaListResponse.
    /// </summary>
    public static SchemaListResponse ToListResponse(
        this IReadOnlyList<SchemaEntry> schemas,
        int totalCount,
        string? nextCursor) => new(
        Schemas: schemas.Select(s => s.ToEntryDto()).ToList(),
        TotalCount: totalCount,
        NextCursor: nextCursor
    );
}
