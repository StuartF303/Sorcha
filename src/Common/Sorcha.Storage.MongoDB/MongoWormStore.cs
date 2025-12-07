// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Linq.Expressions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Sorcha.Storage.Abstractions;

namespace Sorcha.Storage.MongoDB;

/// <summary>
/// MongoDB implementation of IWormStore for cold-tier immutable ledger storage.
/// Enforces Write-Once-Read-Many (WORM) semantics - no updates or deletes.
/// </summary>
/// <typeparam name="TDocument">Document type.</typeparam>
/// <typeparam name="TId">Document identifier type.</typeparam>
public class MongoWormStore<TDocument, TId> : IWormStore<TDocument, TId>
    where TDocument : class
    where TId : notnull
{
    private readonly IMongoCollection<TDocument> _collection;
    private readonly Func<TDocument, TId> _idSelector;
    private readonly Expression<Func<TDocument, TId>> _idExpression;

    /// <summary>
    /// Initializes a new instance of the MongoWormStore.
    /// </summary>
    /// <param name="database">MongoDB database.</param>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="idSelector">Function to extract ID from document.</param>
    /// <param name="idExpression">Expression to extract ID (for MongoDB filters).</param>
    public MongoWormStore(
        IMongoDatabase database,
        string collectionName,
        Func<TDocument, TId> idSelector,
        Expression<Func<TDocument, TId>> idExpression)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrEmpty(collectionName);

        _collection = database.GetCollection<TDocument>(collectionName);
        _idSelector = idSelector ?? throw new ArgumentNullException(nameof(idSelector));
        _idExpression = idExpression ?? throw new ArgumentNullException(nameof(idExpression));
    }

    /// <summary>
    /// Initializes a new instance of the MongoWormStore with configuration.
    /// </summary>
    /// <param name="options">Cold tier configuration options.</param>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="idSelector">Function to extract ID from document.</param>
    /// <param name="idExpression">Expression to extract ID.</param>
    public MongoWormStore(
        IOptions<ColdTierConfiguration> options,
        string collectionName,
        Func<TDocument, TId> idSelector,
        Expression<Func<TDocument, TId>> idExpression)
    {
        ArgumentNullException.ThrowIfNull(options?.Value);
        var config = options.Value;

        var client = new MongoClient(config.ConnectionString);
        var database = client.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<TDocument>(collectionName);
        _idSelector = idSelector ?? throw new ArgumentNullException(nameof(idSelector));
        _idExpression = idExpression ?? throw new ArgumentNullException(nameof(idExpression));
    }

    /// <inheritdoc/>
    public async Task<TDocument> AppendAsync(TDocument document, CancellationToken cancellationToken = default)
    {
        var id = _idSelector(document);

        // Check if document already exists - WORM semantics forbid updates
        var existing = await GetAsync(id, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException(
                $"Document with ID '{id}' already exists. WORM storage does not allow updates.");
        }

        await _collection.InsertOneAsync(document, new InsertOneOptions(), cancellationToken);
        return document;
    }

    /// <inheritdoc/>
    public async Task AppendBatchAsync(
        IEnumerable<TDocument> documents,
        CancellationToken cancellationToken = default)
    {
        var docList = documents.ToList();

        // Validate all documents don't exist first (WORM semantics)
        foreach (var doc in docList)
        {
            var id = _idSelector(doc);
            var exists = await ExistsAsync(id, cancellationToken);
            if (exists)
            {
                throw new InvalidOperationException(
                    $"Document with ID '{id}' already exists. WORM storage does not allow updates.");
            }
        }

        // All validation passed, insert the batch
        if (docList.Count > 0)
        {
            await _collection.InsertManyAsync(docList, new InsertManyOptions(), cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<TDocument?> GetAsync(TId id, CancellationToken cancellationToken = default)
    {
        var filter = CreateIdFilter(id);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TDocument>> GetRangeAsync(
        TId startId,
        TId endId,
        CancellationToken cancellationToken = default)
    {
        var builder = Builders<TDocument>.Filter;
        var filter = builder.And(
            builder.Gte(_idExpression, startId),
            builder.Lte(_idExpression, endId));

        var sortDefinition = Builders<TDocument>.Sort.Ascending(new ExpressionFieldDefinition<TDocument>(_idExpression));
        return await _collection
            .Find(filter)
            .Sort(sortDefinition)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TDocument>> QueryAsync(
        Expression<Func<TDocument, bool>> filter,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = _collection.Find(filter);

        if (limit.HasValue)
        {
            query = query.Limit(limit.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ulong> GetCurrentSequenceAsync(CancellationToken cancellationToken = default)
    {
        // For WORM stores, the sequence is typically the highest ID
        // This assumes TId is a numeric type representing the sequence
        var sortDefinition = Builders<TDocument>.Sort.Descending(new ExpressionFieldDefinition<TDocument>(_idExpression));
        var document = await _collection
            .Find(Builders<TDocument>.Filter.Empty)
            .Sort(sortDefinition)
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            return 0;
        }

        var id = _idSelector(document);

        // Convert ID to ulong if possible
        return id switch
        {
            ulong ulongId => ulongId,
            long longId => (ulong)longId,
            int intId => (ulong)intId,
            _ => 0
        };
    }

    /// <inheritdoc/>
    public async Task<long> CountAsync(
        Expression<Func<TDocument, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        if (filter is null)
        {
            return await _collection.EstimatedDocumentCountAsync(cancellationToken: cancellationToken);
        }

        return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
    {
        var filter = CreateIdFilter(id);
        var count = await _collection.CountDocumentsAsync(filter, new CountOptions { Limit = 1 }, cancellationToken);
        return count > 0;
    }

    /// <inheritdoc/>
    public async Task<IntegrityCheckResult> VerifyIntegrityAsync(
        TId? startId = default,
        TId? endId = default,
        CancellationToken cancellationToken = default)
    {
        // For MongoDB, we verify documents can be read and have valid structure
        var hasStartId = !EqualityComparer<TId?>.Default.Equals(startId, default);
        var hasEndId = !EqualityComparer<TId?>.Default.Equals(endId, default);

        FilterDefinition<TDocument> filter;
        if (hasStartId && hasEndId)
        {
            var builder = Builders<TDocument>.Filter;
            filter = builder.And(
                builder.Gte(_idExpression, startId!),
                builder.Lte(_idExpression, endId!));
        }
        else
        {
            filter = Builders<TDocument>.Filter.Empty;
        }

        long documentsChecked = 0;
        long corruptedDocuments = 0;
        var violations = new List<IntegrityViolation>();

        try
        {
            // Count documents in range
            documentsChecked = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

            // Verify documents can be deserialized by reading them
            using var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
            while (await cursor.MoveNextAsync(cancellationToken))
            {
                foreach (var doc in cursor.Current)
                {
                    try
                    {
                        // Verify the document has a valid ID
                        var id = _idSelector(doc);
                        if (id is null)
                        {
                            corruptedDocuments++;
                            violations.Add(new IntegrityViolation(
                                DocumentId: "unknown",
                                ViolationType: "NullId",
                                Details: "Document has null ID"));
                        }
                    }
                    catch (Exception ex)
                    {
                        corruptedDocuments++;
                        violations.Add(new IntegrityViolation(
                            DocumentId: "unknown",
                            ViolationType: "DeserializationError",
                            Details: $"Failed to extract ID: {ex.Message}"));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            violations.Add(new IntegrityViolation(
                DocumentId: "collection",
                ViolationType: "CollectionError",
                Details: $"Failed to verify integrity: {ex.Message}"));
        }

        return new IntegrityCheckResult(
            IsValid: corruptedDocuments == 0 && violations.Count == 0,
            DocumentsChecked: documentsChecked,
            CorruptedDocuments: corruptedDocuments,
            Violations: violations);
    }

    private FilterDefinition<TDocument> CreateIdFilter(TId id)
    {
        return Builders<TDocument>.Filter.Eq(_idExpression, id);
    }
}
