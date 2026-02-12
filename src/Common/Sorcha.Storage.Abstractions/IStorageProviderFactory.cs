// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Storage.Abstractions;

/// <summary>
/// Factory interface for resolving storage providers by tier.
/// </summary>
public interface IStorageProviderFactory
{
    /// <summary>
    /// Gets the cache store for the hot tier.
    /// </summary>
    /// <returns>Cache store implementation.</returns>
    ICacheStore GetCacheStore();

    /// <summary>
    /// Gets a repository for the warm tier.
    /// </summary>
    /// <typeparam name="TEntity">Entity type.</typeparam>
    /// <typeparam name="TId">Primary key type.</typeparam>
    /// <returns>Repository implementation.</returns>
    IRepository<TEntity, TId> GetRepository<TEntity, TId>()
        where TEntity : class
        where TId : notnull;

    /// <summary>
    /// Gets a document store for the warm tier.
    /// </summary>
    /// <typeparam name="TDocument">Document type.</typeparam>
    /// <typeparam name="TId">Document identifier type.</typeparam>
    /// <returns>Document store implementation.</returns>
    IDocumentStore<TDocument, TId> GetDocumentStore<TDocument, TId>()
        where TDocument : class
        where TId : notnull;

    /// <summary>
    /// Gets a WORM store for the cold tier.
    /// </summary>
    /// <typeparam name="TDocument">Document type.</typeparam>
    /// <typeparam name="TId">Document identifier type.</typeparam>
    /// <returns>WORM store implementation.</returns>
    IWormStore<TDocument, TId> GetWormStore<TDocument, TId>()
        where TDocument : class
        where TId : notnull;

    /// <summary>
    /// Gets the health status of all storage tiers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health status for each tier.</returns>
    Task<StorageHealthStatus> GetHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Storage tier enumeration.
/// </summary>
public enum StorageTier
{
    /// <summary>
    /// Hot tier - ephemeral cache (Redis).
    /// </summary>
    Hot,

    /// <summary>
    /// Warm tier - operational data (PostgreSQL/MongoDB).
    /// </summary>
    Warm,

    /// <summary>
    /// Cold tier - immutable ledger (MongoDB WORM).
    /// </summary>
    Cold
}
