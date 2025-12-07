// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors
// CONTRACT: This file defines the interface specification for implementation

using System.Linq.Expressions;

namespace Sorcha.Storage.Abstractions;

/// <summary>
/// Interface for document storage operations.
/// Implementations: MongoDB, InMemory
/// </summary>
/// <typeparam name="TDocument">Document type</typeparam>
/// <typeparam name="TId">Document identifier type</typeparam>
/// <remarks>
/// Designed for flexible schema documents (JSON) with complex nested structures.
/// Supports schema evolution without migrations.
/// </remarks>
public interface IDocumentStore<TDocument, TId> where TDocument : class
{
    /// <summary>
    /// Gets a document by identifier.
    /// </summary>
    /// <param name="id">Document identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document if found, null otherwise</returns>
    Task<TDocument?> GetAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple documents by identifiers.
    /// </summary>
    /// <param name="ids">Document identifiers</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Found documents (may be fewer than requested)</returns>
    Task<IEnumerable<TDocument>> GetManyAsync(
        IEnumerable<TId> ids,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries documents using a filter expression.
    /// </summary>
    /// <param name="filter">Filter expression</param>
    /// <param name="limit">Maximum documents to return</param>
    /// <param name="skip">Documents to skip (for pagination)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matching documents</returns>
    Task<IEnumerable<TDocument>> QueryAsync(
        Expression<Func<TDocument, bool>> filter,
        int? limit = null,
        int? skip = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a new document.
    /// </summary>
    /// <param name="document">Document to insert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Inserted document with generated ID</returns>
    Task<TDocument> InsertAsync(TDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts multiple documents in a batch.
    /// </summary>
    /// <param name="documents">Documents to insert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InsertManyAsync(
        IEnumerable<TDocument> documents,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces an existing document (full replacement).
    /// </summary>
    /// <param name="id">Document identifier</param>
    /// <param name="document">New document content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Replaced document</returns>
    Task<TDocument> ReplaceAsync(
        TId id,
        TDocument document,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts a document (insert if not exists, replace if exists).
    /// </summary>
    /// <param name="id">Document identifier</param>
    /// <param name="document">Document content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upserted document</returns>
    Task<TDocument> UpsertAsync(
        TId id,
        TDocument document,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document by identifier.
    /// </summary>
    /// <param name="id">Document identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if document existed and was deleted</returns>
    Task<bool> DeleteAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes documents matching a filter.
    /// </summary>
    /// <param name="filter">Filter expression</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of documents deleted</returns>
    Task<long> DeleteManyAsync(
        Expression<Func<TDocument, bool>> filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts documents matching a filter.
    /// </summary>
    /// <param name="filter">Optional filter expression</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of matching documents</returns>
    Task<long> CountAsync(
        Expression<Func<TDocument, bool>>? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a document exists.
    /// </summary>
    /// <param name="id">Document identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if document exists</returns>
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);
}
