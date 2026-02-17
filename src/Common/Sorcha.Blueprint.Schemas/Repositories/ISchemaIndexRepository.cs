// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Blueprint.Schemas.Models;

namespace Sorcha.Blueprint.Schemas.Repositories;

/// <summary>
/// Repository for the schema index collection.
/// Stores lightweight metadata entries for all indexed schemas.
/// </summary>
public interface ISchemaIndexRepository
{
    /// <summary>
    /// Searches the schema index with full-text search, sector filtering, and cursor pagination.
    /// </summary>
    Task<SchemaIndexSearchResult> SearchAsync(
        string? search = null,
        string[]? sectors = null,
        string? provider = null,
        SchemaIndexStatus? status = null,
        int limit = 25,
        string? cursor = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single entry by source provider and source URI (composite key).
    /// </summary>
    Task<SchemaIndexEntryDocument?> GetByProviderAndUriAsync(
        string sourceProvider,
        string sourceUri,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts an entry. If content hash is unchanged, skips the update.
    /// Returns true if the entry was inserted or updated, false if unchanged.
    /// </summary>
    Task<bool> UpsertAsync(
        SchemaIndexEntryDocument entry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch upserts entries from a provider refresh.
    /// Returns the number of entries inserted or updated.
    /// </summary>
    Task<int> BatchUpsertAsync(
        IEnumerable<SchemaIndexEntryDocument> entries,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of all entries for a given provider.
    /// </summary>
    Task UpdateProviderStatusAsync(
        string sourceProvider,
        SchemaIndexStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of entries for a given provider.
    /// </summary>
    Task<int> GetCountByProviderAsync(
        string sourceProvider,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of all indexed entries.
    /// </summary>
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Search result from the schema index.
/// </summary>
public sealed record SchemaIndexSearchResult(
    IReadOnlyList<SchemaIndexEntryDocument> Results,
    int TotalCount,
    string? NextCursor);

/// <summary>
/// BSON-mapped schema index document for MongoDB.
/// </summary>
public sealed class SchemaIndexEntryDocument
{
    public string? Id { get; set; }
    public required string SourceProvider { get; set; }
    public required string SourceUri { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string[] SectorTags { get; set; }
    public string[]? Keywords { get; set; }
    public int FieldCount { get; set; }
    public string[]? FieldNames { get; set; }
    public string[]? RequiredFields { get; set; }
    public string SchemaVersion { get; set; } = "1.0.0";
    public string JsonSchemaDraft { get; set; } = "2020-12";
    public string? ContentHash { get; set; }
    public string Status { get; set; } = nameof(SchemaIndexStatus.Active);
    public DateTimeOffset LastFetchedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset DateAdded { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset DateModified { get; set; } = DateTimeOffset.UtcNow;
}
