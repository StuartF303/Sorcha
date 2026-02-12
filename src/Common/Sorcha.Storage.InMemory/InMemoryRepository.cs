// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Text.Json;
using Sorcha.Storage.Abstractions;

namespace Sorcha.Storage.InMemory;

/// <summary>
/// In-memory implementation of IRepository for development and testing.
/// </summary>
/// <typeparam name="TEntity">Entity type.</typeparam>
/// <typeparam name="TId">Primary key type.</typeparam>
public class InMemoryRepository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private readonly ConcurrentDictionary<TId, string> _store = new();
    private readonly Func<TEntity, TId> _idSelector;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the InMemoryRepository.
    /// </summary>
    /// <param name="idSelector">Function to extract ID from entity.</param>
    public InMemoryRepository(Func<TEntity, TId> idSelector)
    {
        _idSelector = idSelector ?? throw new ArgumentNullException(nameof(idSelector));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc/>
    public Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(id, out var json))
        {
            return Task.FromResult(JsonSerializer.Deserialize<TEntity>(json, _jsonOptions));
        }
        return Task.FromResult<TEntity?>(null);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = _store.Values
            .Select(json => JsonSerializer.Deserialize<TEntity>(json, _jsonOptions)!)
            .ToList();
        return Task.FromResult<IEnumerable<TEntity>>(entities);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TEntity>> QueryAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        var compiled = predicate.Compile();
        return all.Where(compiled).ToList();
    }

    /// <inheritdoc/>
    public async Task<PagedResult<TEntity>> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);

        if (predicate is not null)
        {
            var compiled = predicate.Compile();
            all = all.Where(compiled);
        }

        var totalCount = all.Count();
        var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<TEntity>(items, totalCount, page, pageSize);
    }

    /// <inheritdoc/>
    public Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var id = _idSelector(entity);
        var json = JsonSerializer.Serialize(entity, _jsonOptions);

        if (!_store.TryAdd(id, json))
        {
            throw new InvalidOperationException($"Entity with ID '{id}' already exists.");
        }

        return Task.FromResult(entity);
    }

    /// <inheritdoc/>
    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            await AddAsync(entity, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var id = _idSelector(entity);

        if (!_store.ContainsKey(id))
        {
            throw new InvalidOperationException($"Entity with ID '{id}' not found.");
        }

        var json = JsonSerializer.Serialize(entity, _jsonOptions);
        _store[id] = json;

        return Task.FromResult(entity);
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(TId id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.TryRemove(id, out _));
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.ContainsKey(id));
    }

    /// <inheritdoc/>
    public async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        if (predicate is null)
        {
            return _store.Count;
        }

        var all = await GetAllAsync(cancellationToken);
        var compiled = predicate.Compile();
        return all.Count(compiled);
    }

    /// <inheritdoc/>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // In-memory store saves immediately, no pending changes
        return Task.FromResult(0);
    }

    /// <summary>
    /// Clears all entities from the store.
    /// </summary>
    public void Clear()
    {
        _store.Clear();
    }
}
