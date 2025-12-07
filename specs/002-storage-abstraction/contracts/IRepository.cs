// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors
// CONTRACT: This file defines the interface specification for implementation

using System.Linq.Expressions;

namespace Sorcha.Storage.Abstractions;

/// <summary>
/// Generic repository interface for warm-tier relational data.
/// Implementations: EF Core (PostgreSQL), InMemory
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TId">Primary key type</typeparam>
/// <remarks>
/// Provides CRUD operations with ACID transaction guarantees.
/// Designed for relational data with strong consistency requirements.
/// </remarks>
public interface IRepository<TEntity, TId> where TEntity : class
{
    /// <summary>
    /// Gets an entity by its identifier.
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Entity if found, null otherwise</returns>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities (use with caution - prefer queries for large datasets).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All entities</returns>
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries entities using a predicate.
    /// </summary>
    /// <param name="predicate">Filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matching entities</returns>
    Task<IEnumerable<TEntity>> QueryAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged result.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="predicate">Optional filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result</returns>
    Task<PagedResult<TEntity>> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity.
    /// </summary>
    /// <param name="entity">Entity to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Added entity with generated ID</returns>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities in a batch.
    /// </summary>
    /// <param name="entities">Entities to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="entity">Entity to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated entity</returns>
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity by identifier.
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if entity existed and was deleted</returns>
    Task<bool> DeleteAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an entity exists.
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if entity exists</returns>
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities matching a predicate.
    /// </summary>
    /// <param name="predicate">Optional filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of matching entities</returns>
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes (for unit of work pattern).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entities affected</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Paged result wrapper.
/// </summary>
/// <typeparam name="T">Item type</typeparam>
public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}
