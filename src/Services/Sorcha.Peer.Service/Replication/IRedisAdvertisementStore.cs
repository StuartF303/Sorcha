// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Peer.Service.Replication;

/// <summary>
/// Abstraction for Redis-backed advertisement persistence.
/// Provides durable storage for both local and remote register advertisements
/// with a 5-minute TTL that naturally expires stale entries.
/// </summary>
public interface IRedisAdvertisementStore
{
    /// <summary>
    /// Persists a local register advertisement to Redis with a 5-minute TTL.
    /// Key pattern: peer:advert:local:{registerId}
    /// </summary>
    Task SetLocalAsync(LocalRegisterAdvertisement advertisement, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a remote peer's register advertisement to Redis with a 5-minute TTL.
    /// Key pattern: peer:advert:remote:{peerId}:{registerId}
    /// </summary>
    Task SetRemoteAsync(string peerId, Core.PeerRegisterInfo advertisement, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads all local advertisements from Redis.
    /// Used on startup to restore in-memory state.
    /// </summary>
    Task<IReadOnlyList<LocalRegisterAdvertisement>> GetAllLocalAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads all remote advertisements from Redis, grouped by peer ID.
    /// Used on startup to restore peer advertisement state.
    /// </summary>
    Task<IReadOnlyDictionary<string, List<Core.PeerRegisterInfo>>> GetAllRemoteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a local advertisement from Redis.
    /// Key pattern: peer:advert:local:{registerId}
    /// </summary>
    Task RemoveLocalAsync(string registerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all local advertisements EXCEPT those in the provided set of register IDs.
    /// Used by the full-sync bulk endpoint to clean up stale local advertisements.
    /// Returns the number of entries removed.
    /// </summary>
    Task<int> RemoveLocalExceptAsync(HashSet<string> registerIdsToKeep, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all remote advertisements for a specific peer.
    /// Key pattern: peer:advert:remote:{peerId}:*
    /// </summary>
    Task RemoveRemoteByPeerAsync(string peerId, CancellationToken cancellationToken = default);
}
