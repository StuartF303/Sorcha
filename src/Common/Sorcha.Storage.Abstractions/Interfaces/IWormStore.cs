// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Linq.Expressions;

namespace Sorcha.Storage.Abstractions;

/// <summary>
/// Interface for Write-Once-Read-Many (WORM) storage.
/// Used for immutable ledger data.
/// Implementations: MongoDB, InMemory
/// </summary>
/// <typeparam name="TDocument">Document type</typeparam>
/// <typeparam name="TId">Document identifier type</typeparam>
/// <remarks>
/// CRITICAL: This store enforces append-only semantics.
/// - Documents CANNOT be updated after append
/// - Documents CANNOT be deleted
/// - Update/Delete methods are intentionally NOT provided
/// - Implementations must enforce immutability at both application and database level
/// </remarks>
public interface IWormStore<TDocument, TId> where TDocument : class
{
    /// <summary>
    /// Appends a new document to the store. Document becomes immutable after append.
    /// </summary>
    /// <param name="document">Document to append</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Appended document with sealed timestamp and hash</returns>
    /// <exception cref="InvalidOperationException">If document ID already exists</exception>
    Task<TDocument> AppendAsync(TDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends multiple documents in a batch. All documents become immutable.
    /// </summary>
    /// <param name="documents">Documents to append</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">If any document ID already exists</exception>
    Task AppendBatchAsync(
        IEnumerable<TDocument> documents,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document by identifier.
    /// </summary>
    /// <param name="id">Document identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document if found, null otherwise</returns>
    Task<TDocument?> GetAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents in a sequential range (for ledger traversal).
    /// </summary>
    /// <param name="startId">Start of range (inclusive)</param>
    /// <param name="endId">End of range (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Documents in range, ordered by ID</returns>
    Task<IEnumerable<TDocument>> GetRangeAsync(
        TId startId,
        TId endId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries documents (read-only).
    /// </summary>
    /// <param name="filter">Filter expression</param>
    /// <param name="limit">Maximum documents to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matching documents</returns>
    Task<IEnumerable<TDocument>> QueryAsync(
        Expression<Func<TDocument, bool>> filter,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current sequence/height of the store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current sequence number (0 if empty)</returns>
    Task<ulong> GetCurrentSequenceAsync(CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Verifies integrity of stored documents.
    /// </summary>
    /// <param name="startId">Optional start of range to verify</param>
    /// <param name="endId">Optional end of range to verify</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Integrity check result</returns>
    Task<IntegrityCheckResult> VerifyIntegrityAsync(
        TId? startId = default,
        TId? endId = default,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of integrity verification.
/// </summary>
public record IntegrityCheckResult(
    bool IsValid,
    long DocumentsChecked,
    long CorruptedDocuments,
    IReadOnlyList<IntegrityViolation> Violations);

/// <summary>
/// Details of an integrity violation.
/// </summary>
public record IntegrityViolation(
    string DocumentId,
    string ViolationType,
    string Details);
