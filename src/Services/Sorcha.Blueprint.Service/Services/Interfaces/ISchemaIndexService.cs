// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using Sorcha.Blueprint.Schemas.Models;
using Sorcha.Blueprint.Service.Models;

namespace Sorcha.Blueprint.Service.Services.Interfaces;

/// <summary>
/// Service for managing the schema index — search, CRUD, and provider orchestration.
/// </summary>
public interface ISchemaIndexService
{
    /// <summary>
    /// Searches the schema index with optional sector filtering for the requesting org.
    /// </summary>
    Task<SchemaIndexSearchResponse> SearchAsync(
        string? search = null,
        string[]? sectors = null,
        string? provider = null,
        SchemaIndexStatus? status = null,
        int limit = 25,
        string? cursor = null,
        string? organizationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single index entry with full content.
    /// </summary>
    Task<SchemaIndexEntryDetail?> GetByProviderAndUriAsync(
        string sourceProvider,
        string sourceUri,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the raw JSON Schema content for a schema.
    /// </summary>
    Task<JsonDocument?> GetContentAsync(
        string sourceProvider,
        string sourceUri,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts index entries from a provider catalog refresh.
    /// </summary>
    Task<int> UpsertFromProviderAsync(
        string providerName,
        IEnumerable<ProviderSchemaEntry> entries,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all provider statuses.
    /// </summary>
    IReadOnlyList<SchemaProviderStatusDto> GetProviderStatuses();

    /// <summary>
    /// Gets the names of providers still loading during cold start.
    /// </summary>
    string[] GetLoadingProviders();
}

/// <summary>
/// Schema entry from a provider during refresh.
/// </summary>
public sealed record ProviderSchemaEntry(
    string SourceUri,
    string Title,
    string? Description,
    string[] SectorTags,
    string SchemaVersion,
    JsonDocument Content,
    string[]? Keywords = null);
