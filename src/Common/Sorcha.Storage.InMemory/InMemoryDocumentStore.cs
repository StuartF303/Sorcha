// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Text.Json;
using Sorcha.Storage.Abstractions;

namespace Sorcha.Storage.InMemory;

/// <summary>
/// In-memory implementation of IDocumentStore for development and testing.
/// </summary>
/// <typeparam name="TDocument">Document type.</typeparam>
/// <typeparam name="TId">Document identifier type.</typeparam>
public class InMemoryDocumentStore<TDocument, TId> : IDocumentStore<TDocument, TId>
    where TDocument : class
    where TId : notnull
{
    private readonly ConcurrentDictionary<TId, string> _store = new();
    private readonly Func<TDocument, TId> _idSelector;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the InMemoryDocumentStore.
    /// </summary>
    /// <param name="idSelector">Function to extract ID from document.</param>
    public InMemoryDocumentStore(Func<TDocument, TId> idSelector)
    {
        _idSelector = idSelector ?? throw new ArgumentNullException(nameof(idSelector));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc/>
    public Task<TDocument?> GetAsync(TId id, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(id, out var json))
        {
            return Task.FromResult(JsonSerializer.Deserialize<TDocument>(json, _jsonOptions));
        }
        return Task.FromResult<TDocument?>(null);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TDocument>> GetManyAsync(
        IEnumerable<TId> ids,
        CancellationToken cancellationToken = default)
    {
        var results = new List<TDocument>();
        foreach (var id in ids)
        {
            var doc = await GetAsync(id, cancellationToken);
            if (doc is not null)
            {
                results.Add(doc);
            }
        }
        return results;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<TDocument>> QueryAsync(
        Expression<Func<TDocument, bool>> filter,
        int? limit = null,
        int? skip = null,
        CancellationToken cancellationToken = default)
    {
        var compiled = filter.Compile();
        var query = _store.Values
            .Select(json => JsonSerializer.Deserialize<TDocument>(json, _jsonOptions)!)
            .Where(compiled);

        if (skip.HasValue)
        {
            query = query.Skip(skip.Value);
        }

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return Task.FromResult<IEnumerable<TDocument>>(query.ToList());
    }

    /// <inheritdoc/>
    public Task<TDocument> InsertAsync(TDocument document, CancellationToken cancellationToken = default)
    {
        var id = _idSelector(document);
        var json = JsonSerializer.Serialize(document, _jsonOptions);

        if (!_store.TryAdd(id, json))
        {
            throw new InvalidOperationException($"Document with ID '{id}' already exists.");
        }

        return Task.FromResult(document);
    }

    /// <inheritdoc/>
    public async Task InsertManyAsync(
        IEnumerable<TDocument> documents,
        CancellationToken cancellationToken = default)
    {
        foreach (var doc in documents)
        {
            await InsertAsync(doc, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public Task<TDocument> ReplaceAsync(
        TId id,
        TDocument document,
        CancellationToken cancellationToken = default)
    {
        if (!_store.ContainsKey(id))
        {
            throw new InvalidOperationException($"Document with ID '{id}' not found.");
        }

        var json = JsonSerializer.Serialize(document, _jsonOptions);
        _store[id] = json;

        return Task.FromResult(document);
    }

    /// <inheritdoc/>
    public Task<TDocument> UpsertAsync(
        TId id,
        TDocument document,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(document, _jsonOptions);
        _store.AddOrUpdate(id, json, (_, _) => json);
        return Task.FromResult(document);
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(TId id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.TryRemove(id, out _));
    }

    /// <inheritdoc/>
    public Task<long> DeleteManyAsync(
        Expression<Func<TDocument, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        var compiled = filter.Compile();
        var toDelete = _store
            .Where(kvp => compiled(JsonSerializer.Deserialize<TDocument>(kvp.Value, _jsonOptions)!))
            .Select(kvp => kvp.Key)
            .ToList();

        long deleted = 0;
        foreach (var id in toDelete)
        {
            if (_store.TryRemove(id, out _))
            {
                deleted++;
            }
        }

        return Task.FromResult(deleted);
    }

    /// <inheritdoc/>
    public Task<long> CountAsync(
        Expression<Func<TDocument, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        if (filter is null)
        {
            return Task.FromResult((long)_store.Count);
        }

        var compiled = filter.Compile();
        var count = _store.Values
            .Select(json => JsonSerializer.Deserialize<TDocument>(json, _jsonOptions)!)
            .Count(compiled);

        return Task.FromResult((long)count);
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.ContainsKey(id));
    }

    /// <summary>
    /// Clears all documents from the store.
    /// </summary>
    public void Clear()
    {
        _store.Clear();
    }
}
