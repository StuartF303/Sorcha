// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Blueprint.Schemas.Models;

namespace Sorcha.Blueprint.Schemas.Services;

/// <summary>
/// Interface for schema storage and retrieval operations.
/// </summary>
public interface ISchemaStore
{
    /// <summary>
    /// Gets all system schemas (Installation, Organisation, Participant, Register).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of system schema entries.</returns>
    Task<IReadOnlyList<SchemaEntry>> GetSystemSchemasAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a schema by its identifier.
    /// </summary>
    /// <param name="identifier">Schema identifier.</param>
    /// <param name="organizationId">Optional organization ID for Custom schema access.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The schema entry, or null if not found.</returns>
    Task<SchemaEntry?> GetByIdentifierAsync(
        string identifier,
        string? organizationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists schemas with optional filtering.
    /// </summary>
    /// <param name="category">Filter by category.</param>
    /// <param name="status">Filter by status (default: Active).</param>
    /// <param name="search">Search term for title/description.</param>
    /// <param name="organizationId">Organization ID for Custom schema visibility.</param>
    /// <param name="limit">Maximum results (default: 50, max: 100).</param>
    /// <param name="cursor">Pagination cursor.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching schemas with total count and next cursor.</returns>
    Task<(IReadOnlyList<SchemaEntry> Schemas, int TotalCount, string? NextCursor)> ListAsync(
        SchemaCategory? category = null,
        SchemaStatus? status = SchemaStatus.Active,
        string? search = null,
        string? organizationId = null,
        int limit = 50,
        string? cursor = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new custom schema.
    /// </summary>
    /// <param name="entry">Schema entry to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created schema entry.</returns>
    /// <exception cref="InvalidOperationException">Thrown if identifier already exists.</exception>
    Task<SchemaEntry> CreateAsync(SchemaEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing custom schema.
    /// </summary>
    /// <param name="entry">Schema entry with updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated schema entry.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if schema not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown if schema is not Custom category.</exception>
    Task<SchemaEntry> UpdateAsync(SchemaEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a custom schema.
    /// </summary>
    /// <param name="identifier">Schema identifier.</param>
    /// <param name="organizationId">Organization ID for authorization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    /// <exception cref="InvalidOperationException">Thrown if schema is not Custom category.</exception>
    Task<bool> DeleteAsync(
        string identifier,
        string organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deprecates a schema.
    /// </summary>
    /// <param name="identifier">Schema identifier.</param>
    /// <param name="organizationId">Organization ID for Custom schema authorization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated schema entry.</returns>
    Task<SchemaEntry> DeprecateAsync(
        string identifier,
        string? organizationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reactivates a deprecated schema.
    /// </summary>
    /// <param name="identifier">Schema identifier.</param>
    /// <param name="organizationId">Organization ID for Custom schema authorization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated schema entry.</returns>
    Task<SchemaEntry> ActivateAsync(
        string identifier,
        string? organizationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a custom schema globally.
    /// </summary>
    /// <param name="identifier">Schema identifier.</param>
    /// <param name="organizationId">Organization ID (owner must match).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated schema entry.</returns>
    /// <exception cref="InvalidOperationException">Thrown if identifier conflicts with existing global schema.</exception>
    Task<SchemaEntry> PublishGloballyAsync(
        string identifier,
        string organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a schema identifier exists.
    /// </summary>
    /// <param name="identifier">Schema identifier.</param>
    /// <param name="organizationId">Organization ID for Custom schemas.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if exists.</returns>
    Task<bool> ExistsAsync(
        string identifier,
        string? organizationId = null,
        CancellationToken cancellationToken = default);
}
