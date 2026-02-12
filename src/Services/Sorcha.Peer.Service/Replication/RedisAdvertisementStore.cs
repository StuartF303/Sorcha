// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sorcha.Peer.Service.Core;
using StackExchange.Redis;

namespace Sorcha.Peer.Service.Replication;

/// <summary>
/// Redis-backed persistence layer for register advertisements.
/// Stores both local and remote advertisements with a 5-minute TTL.
/// Uses Redis Set secondary indexes instead of KEYS scanning.
/// All operations are designed to be fire-and-forget safe — Redis failures
/// are logged and do not block the caller (FR-010).
/// </summary>
public class RedisAdvertisementStore : IRedisAdvertisementStore
{
    private const string LocalKeyPrefix = "peer:advert:local:";
    private const string RemoteKeyPrefix = "peer:advert:remote:";
    private const string LocalIndexKey = "peer:advert:local:_index";
    private const string RemotePeersIndexKey = "peer:advert:remote:_peers";
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisAdvertisementStore> _logger;

    public RedisAdvertisementStore(
        IConnectionMultiplexer redis,
        ILogger<RedisAdvertisementStore> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SetLocalAsync(LocalRegisterAdvertisement advertisement, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var db = _redis.GetDatabase();
            var key = $"{LocalKeyPrefix}{advertisement.RegisterId}";
            var json = JsonSerializer.Serialize(advertisement, JsonOptions);
            await db.StringSetAsync(key, json, DefaultTtl);

            // Add to local index set and refresh its TTL
            await db.SetAddAsync(LocalIndexKey, advertisement.RegisterId);
            await db.KeyExpireAsync(LocalIndexKey, DefaultTtl);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist local advertisement for register {RegisterId} to Redis", advertisement.RegisterId);
        }
    }

    public async Task SetRemoteAsync(string peerId, PeerRegisterInfo advertisement, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var db = _redis.GetDatabase();
            var key = $"{RemoteKeyPrefix}{peerId}:{advertisement.RegisterId}";
            var json = JsonSerializer.Serialize(advertisement, JsonOptions);
            await db.StringSetAsync(key, json, DefaultTtl);

            // Add to per-peer index set + master peer set, refresh TTLs
            var peerIndexKey = $"{RemoteKeyPrefix}{peerId}:_index";
            await db.SetAddAsync(peerIndexKey, advertisement.RegisterId);
            await db.KeyExpireAsync(peerIndexKey, DefaultTtl);
            await db.SetAddAsync(RemotePeersIndexKey, peerId);
            await db.KeyExpireAsync(RemotePeersIndexKey, DefaultTtl);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist remote advertisement for register {RegisterId} from peer {PeerId} to Redis",
                advertisement.RegisterId, peerId);
        }
    }

    public async Task<IReadOnlyList<LocalRegisterAdvertisement>> GetAllLocalAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var results = new List<LocalRegisterAdvertisement>();

        try
        {
            var db = _redis.GetDatabase();

            // Use SMEMBERS on the local index instead of KEYS scan
            var members = await db.SetMembersAsync(LocalIndexKey);
            if (members.Length == 0) return results;

            // Batch read all keys at once
            var keys = members
                .Where(m => m.HasValue)
                .Select(m => (RedisKey)$"{LocalKeyPrefix}{m}")
                .ToArray();

            var values = await db.StringGetAsync(keys);

            for (var i = 0; i < values.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!values[i].HasValue) continue;

                var ad = JsonSerializer.Deserialize<LocalRegisterAdvertisement>((string)values[i]!, JsonOptions);
                if (ad != null)
                    results.Add(ad);
            }

            _logger.LogInformation("Loaded {Count} local advertisements from Redis", results.Count);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load local advertisements from Redis");
        }

        return results;
    }

    public async Task<IReadOnlyDictionary<string, List<PeerRegisterInfo>>> GetAllRemoteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var results = new Dictionary<string, List<PeerRegisterInfo>>();

        try
        {
            var db = _redis.GetDatabase();

            // Get all known peer IDs from the master peer set
            var peerIds = await db.SetMembersAsync(RemotePeersIndexKey);
            if (peerIds.Length == 0) return results;

            foreach (var peerIdValue in peerIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!peerIdValue.HasValue) continue;
                var peerId = (string)peerIdValue!;

                // Get register IDs for this peer from per-peer index
                var peerIndexKey = $"{RemoteKeyPrefix}{peerId}:_index";
                var registerIds = await db.SetMembersAsync(peerIndexKey);
                if (registerIds.Length == 0) continue;

                // Batch read all keys for this peer
                var keys = registerIds
                    .Where(m => m.HasValue)
                    .Select(m => (RedisKey)$"{RemoteKeyPrefix}{peerId}:{m}")
                    .ToArray();

                var values = await db.StringGetAsync(keys);
                var peerAds = new List<PeerRegisterInfo>();

                for (var i = 0; i < values.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!values[i].HasValue) continue;

                    var ad = JsonSerializer.Deserialize<PeerRegisterInfo>((string)values[i]!, JsonOptions);
                    if (ad != null)
                        peerAds.Add(ad);
                }

                if (peerAds.Count > 0)
                    results[peerId] = peerAds;
            }

            _logger.LogInformation("Loaded {PeerCount} peers with remote advertisements from Redis",
                results.Count);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load remote advertisements from Redis");
        }

        return results;
    }

    public async Task RemoveLocalAsync(string registerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var db = _redis.GetDatabase();
            var key = $"{LocalKeyPrefix}{registerId}";
            await db.KeyDeleteAsync(key);

            // Remove from local index set
            await db.SetRemoveAsync(LocalIndexKey, registerId);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove local advertisement for register {RegisterId} from Redis", registerId);
        }
    }

    public async Task<int> RemoveLocalExceptAsync(HashSet<string> registerIdsToKeep, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var removed = 0;

        try
        {
            var db = _redis.GetDatabase();

            // Use SMEMBERS on local index instead of KEYS scan
            var members = await db.SetMembersAsync(LocalIndexKey);

            foreach (var member in members)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!member.HasValue) continue;
                var registerId = (string)member!;

                if (!registerIdsToKeep.Contains(registerId))
                {
                    var key = $"{LocalKeyPrefix}{registerId}";
                    await db.KeyDeleteAsync(key);
                    await db.SetRemoveAsync(LocalIndexKey, registerId);
                    removed++;
                }
            }

            if (removed > 0)
                _logger.LogInformation("Removed {Count} stale local advertisements from Redis (full-sync)", removed);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove stale local advertisements from Redis");
        }

        return removed;
    }

    public async Task RemoveRemoteByPeerAsync(string peerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var db = _redis.GetDatabase();

            // Get register IDs from per-peer index
            var peerIndexKey = $"{RemoteKeyPrefix}{peerId}:_index";
            var registerIds = await db.SetMembersAsync(peerIndexKey);

            if (registerIds.Length > 0)
            {
                // Build all data keys and delete in batch
                var keys = registerIds
                    .Where(m => m.HasValue)
                    .Select(m => (RedisKey)$"{RemoteKeyPrefix}{peerId}:{m}")
                    .ToArray();

                await db.KeyDeleteAsync(keys);

                _logger.LogDebug("Removed {Count} remote advertisements for peer {PeerId} from Redis",
                    keys.Length, peerId);
            }

            // Clean up both index sets
            await db.KeyDeleteAsync(peerIndexKey);
            await db.SetRemoveAsync(RemotePeersIndexKey, peerId);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove remote advertisements for peer {PeerId} from Redis", peerId);
        }
    }
}
