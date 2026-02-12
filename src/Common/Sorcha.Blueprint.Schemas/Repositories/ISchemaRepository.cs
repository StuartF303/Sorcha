// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Blueprint.Schemas.Models;

namespace Sorcha.Blueprint.Schemas.Repositories;

/// <summary>
/// Repository interface for persistent schema storage.
/// </summary>
public interface ISchemaRepository
{
    /// <summary>
    /// Gets a schema by identifier.
    /// </summary>
    Task<SchemaEntry?> GetByIdentifierAsync(
        string identifier,
        string? organizationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists schemas with filtering.
    /// </summary>
    Task<(IReadOnlyList<SchemaEntry> Schemas, int TotalCount, string? NextCursor)> ListAsync(
        SchemaCategory? category = null,
        SchemaStatus? status = null,
        string? search = null,
        string? organizationId = null,
        int limit = 50,
        string? cursor = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new schema.
    /// </summary>
    Task<SchemaEntry> CreateAsync(SchemaEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing schema.
    /// </summary>
    Task<SchemaEntry> UpdateAsync(SchemaEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a schema.
    /// </summary>
    Task<bool> DeleteAsync(
        string identifier,
        string organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a schema exists.
    /// </summary>
    Task<bool> ExistsAsync(
        string identifier,
        string? organizationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a schema identifier exists globally (published or External).
    /// </summary>
    Task<bool> ExistsGloballyAsync(
        string identifier,
        CancellationToken cancellationToken = default);
}
