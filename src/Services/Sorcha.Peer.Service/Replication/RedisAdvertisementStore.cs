// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sorcha.Peer.Service.Core;
using StackExchange.Redis;

namespace Sorcha.Peer.Service.Replication;

/// <summary>
/// Redis-backed persistence layer for register advertisements.
/// Stores both local and remote advertisements with a 5-minute TTL.
/// All operations are designed to be fire-and-forget safe — Redis failures
/// are logged and do not block the caller (FR-010).
/// </summary>
public class RedisAdvertisementStore : IRedisAdvertisementStore
{
    private const string LocalKeyPrefix = "peer:advert:local:";
    private const string RemoteKeyPrefix = "peer:advert:remote:";
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
        try
        {
            var db = _redis.GetDatabase();
            var key = $"{LocalKeyPrefix}{advertisement.RegisterId}";
            var json = JsonSerializer.Serialize(advertisement, JsonOptions);
            await db.StringSetAsync(key, json, DefaultTtl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist local advertisement for register {RegisterId} to Redis", advertisement.RegisterId);
        }
    }

    public async Task SetRemoteAsync(string peerId, PeerRegisterInfo advertisement, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"{RemoteKeyPrefix}{peerId}:{advertisement.RegisterId}";
            var json = JsonSerializer.Serialize(advertisement, JsonOptions);
            await db.StringSetAsync(key, json, DefaultTtl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist remote advertisement for register {RegisterId} from peer {PeerId} to Redis",
                advertisement.RegisterId, peerId);
        }
    }

    public async Task<IReadOnlyList<LocalRegisterAdvertisement>> GetAllLocalAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<LocalRegisterAdvertisement>();

        try
        {
            var db = _redis.GetDatabase();
            var server = GetServer();
            if (server == null) return results;

            var keys = server.Keys(pattern: $"{LocalKeyPrefix}*").ToArray();

            foreach (var key in keys)
            {
                var json = await db.StringGetAsync(key);
                if (json.HasValue)
                {
                    var ad = JsonSerializer.Deserialize<LocalRegisterAdvertisement>((string)json!, JsonOptions);
                    if (ad != null)
                        results.Add(ad);
                }
            }

            _logger.LogInformation("Loaded {Count} local advertisements from Redis", results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load local advertisements from Redis");
        }

        return results;
    }

    public async Task<IReadOnlyDictionary<string, List<PeerRegisterInfo>>> GetAllRemoteAsync(CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, List<PeerRegisterInfo>>();

        try
        {
            var db = _redis.GetDatabase();
            var server = GetServer();
            if (server == null) return results;

            var keys = server.Keys(pattern: $"{RemoteKeyPrefix}*").ToArray();

            foreach (var key in keys)
            {
                var json = await db.StringGetAsync(key);
                if (!json.HasValue) continue;

                var ad = JsonSerializer.Deserialize<PeerRegisterInfo>((string)json!, JsonOptions);
                if (ad == null) continue;

                // Extract peerId from key: peer:advert:remote:{peerId}:{registerId}
                var keyStr = key.ToString();
                var afterPrefix = keyStr[RemoteKeyPrefix.Length..];
                var separatorIndex = afterPrefix.IndexOf(':');
                if (separatorIndex < 0) continue;

                var peerId = afterPrefix[..separatorIndex];

                if (!results.TryGetValue(peerId, out var peerAds))
                {
                    peerAds = [];
                    results[peerId] = peerAds;
                }
                peerAds.Add(ad);
            }

            _logger.LogInformation("Loaded {PeerCount} peers with remote advertisements from Redis",
                results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load remote advertisements from Redis");
        }

        return results;
    }

    public async Task RemoveLocalAsync(string registerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"{LocalKeyPrefix}{registerId}";
            await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove local advertisement for register {RegisterId} from Redis", registerId);
        }
    }

    public async Task<int> RemoveLocalExceptAsync(HashSet<string> registerIdsToKeep, CancellationToken cancellationToken = default)
    {
        var removed = 0;

        try
        {
            var db = _redis.GetDatabase();
            var server = GetServer();
            if (server == null) return 0;

            var keys = server.Keys(pattern: $"{LocalKeyPrefix}*").ToArray();

            foreach (var key in keys)
            {
                var registerId = key.ToString()[LocalKeyPrefix.Length..];
                if (!registerIdsToKeep.Contains(registerId))
                {
                    await db.KeyDeleteAsync(key);
                    removed++;
                }
            }

            if (removed > 0)
                _logger.LogInformation("Removed {Count} stale local advertisements from Redis (full-sync)", removed);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove stale local advertisements from Redis");
        }

        return removed;
    }

    public async Task RemoveRemoteByPeerAsync(string peerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var server = GetServer();
            if (server == null) return;

            var keys = server.Keys(pattern: $"{RemoteKeyPrefix}{peerId}:*").ToArray();
            if (keys.Length > 0)
            {
                await db.KeyDeleteAsync(keys);
                _logger.LogDebug("Removed {Count} remote advertisements for peer {PeerId} from Redis",
                    keys.Length, peerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove remote advertisements for peer {PeerId} from Redis", peerId);
        }
    }

    private IServer? GetServer()
    {
        try
        {
            return _redis.GetServers().FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Redis server for key scanning");
            return null;
        }
    }
}
