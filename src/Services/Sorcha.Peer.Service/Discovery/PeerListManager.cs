// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sorcha.Peer.Service.Core;
using System.Collections.Concurrent;

namespace Sorcha.Peer.Service.Discovery;

/// <summary>
/// Manages the list of known peers with SQLite persistence
/// </summary>
public class PeerListManager : IDisposable
{
    private readonly ILogger<PeerListManager> _logger;
    private readonly PeerDiscoveryConfiguration _configuration;
    private readonly ConcurrentDictionary<string, PeerNode> _peers;
    private readonly SqliteConnection _dbConnection;
    private readonly SemaphoreSlim _dbLock = new(1, 1);
    private bool _disposed;

    public PeerListManager(
        ILogger<PeerListManager> logger,
        IOptions<PeerServiceConfiguration> configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration?.Value?.PeerDiscovery ?? throw new ArgumentNullException(nameof(configuration));
        _peers = new ConcurrentDictionary<string, PeerNode>();

        // Initialize SQLite database
        var dbPath = "./data/peers.db";
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _dbConnection = new SqliteConnection($"Data Source={dbPath}");
        _dbConnection.Open();
        InitializeDatabaseAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets all known peers
    /// </summary>
    public IReadOnlyCollection<PeerNode> GetAllPeers()
    {
        return _peers.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets healthy peers (low failure count, recently seen)
    /// </summary>
    public IReadOnlyCollection<PeerNode> GetHealthyPeers()
    {
        var cutoffTime = DateTimeOffset.UtcNow.AddMinutes(-_configuration.RefreshIntervalMinutes * 2);

        return _peers.Values
            .Where(p => p.FailureCount < 3 && p.LastSeen > cutoffTime)
            .OrderBy(p => p.FailureCount)
            .ThenByDescending(p => p.LastSeen)
            .Take(_configuration.MinHealthyPeers * 2)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets a random selection of peers for gossip
    /// </summary>
    public IReadOnlyCollection<PeerNode> GetRandomPeers(int count)
    {
        var healthyPeers = GetHealthyPeers();
        return healthyPeers
            .OrderBy(_ => Random.Shared.Next())
            .Take(count)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Adds or updates a peer in the list
    /// </summary>
    public async Task<bool> AddOrUpdatePeerAsync(PeerNode peer, CancellationToken cancellationToken = default)
    {
        if (peer == null)
            throw new ArgumentNullException(nameof(peer));

        if (string.IsNullOrEmpty(peer.PeerId))
        {
            _logger.LogWarning("Cannot add peer with empty PeerId");
            return false;
        }

        // Check if we've reached the maximum peer count
        if (_peers.Count >= _configuration.MaxPeersInList && !_peers.ContainsKey(peer.PeerId))
        {
            _logger.LogWarning("Peer list is full ({Count}/{Max}), cannot add new peer",
                _peers.Count, _configuration.MaxPeersInList);
            return false;
        }

        var isNew = _peers.TryAdd(peer.PeerId, peer);
        if (!isNew)
        {
            _peers[peer.PeerId] = peer;
        }

        _logger.LogDebug("{Action} peer: {PeerId} at {Address}:{Port}",
            isNew ? "Added" : "Updated", peer.PeerId, peer.Address, peer.Port);

        // Persist to database
        await PersistPeerAsync(peer, cancellationToken);

        return true;
    }

    /// <summary>
    /// Removes a peer from the list
    /// </summary>
    public async Task<bool> RemovePeerAsync(string peerId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(peerId))
            return false;

        var removed = _peers.TryRemove(peerId, out _);

        if (removed)
        {
            _logger.LogInformation("Removed peer: {PeerId}", peerId);
            await DeletePeerAsync(peerId, cancellationToken);
        }

        return removed;
    }

    /// <summary>
    /// Gets a specific peer by ID
    /// </summary>
    public PeerNode? GetPeer(string peerId)
    {
        _peers.TryGetValue(peerId, out var peer);
        return peer;
    }

    /// <summary>
    /// Updates the last seen time for a peer
    /// </summary>
    public async Task UpdateLastSeenAsync(string peerId, CancellationToken cancellationToken = default)
    {
        if (_peers.TryGetValue(peerId, out var peer))
        {
            peer.LastSeen = DateTimeOffset.UtcNow;
            peer.FailureCount = 0; // Reset failure count on successful contact
            await PersistPeerAsync(peer, cancellationToken);
        }
    }

    /// <summary>
    /// Increments the failure count for a peer
    /// </summary>
    public async Task IncrementFailureCountAsync(string peerId, CancellationToken cancellationToken = default)
    {
        if (_peers.TryGetValue(peerId, out var peer))
        {
            peer.FailureCount++;
            _logger.LogWarning("Peer {PeerId} failure count: {Count}", peerId, peer.FailureCount);

            // Remove peer if failure count is too high
            if (peer.FailureCount >= 5 && !peer.IsBootstrapNode)
            {
                _logger.LogWarning("Removing peer {PeerId} due to excessive failures", peerId);
                await RemovePeerAsync(peerId, cancellationToken);
            }
            else
            {
                await PersistPeerAsync(peer, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Gets the count of healthy peers
    /// </summary>
    public int GetHealthyPeerCount()
    {
        return GetHealthyPeers().Count;
    }

    /// <summary>
    /// Loads peers from database on startup
    /// </summary>
    public async Task LoadPeersFromDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await _dbLock.WaitAsync(cancellationToken);
        try
        {
            using var command = _dbConnection.CreateCommand();
            command.CommandText = @"
                SELECT peer_id, address, port, protocols, first_seen, last_seen,
                       failure_count, is_bootstrap, avg_latency_ms
                FROM peers";

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var loadedCount = 0;

            while (await reader.ReadAsync(cancellationToken))
            {
                var peer = new PeerNode
                {
                    PeerId = reader.GetString(0),
                    Address = reader.GetString(1),
                    Port = reader.GetInt32(2),
                    SupportedProtocols = reader.GetString(3).Split(',').ToList(),
                    FirstSeen = DateTimeOffset.Parse(reader.GetString(4)),
                    LastSeen = DateTimeOffset.Parse(reader.GetString(5)),
                    FailureCount = reader.GetInt32(6),
                    IsBootstrapNode = reader.GetBoolean(7),
                    AverageLatencyMs = reader.GetInt32(8)
                };

                _peers.TryAdd(peer.PeerId, peer);
                loadedCount++;
            }

            _logger.LogInformation("Loaded {Count} peers from database", loadedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading peers from database");
        }
        finally
        {
            _dbLock.Release();
        }
    }

    /// <summary>
    /// Initializes the database schema
    /// </summary>
    private async Task InitializeDatabaseAsync()
    {
        await _dbLock.WaitAsync();
        try
        {
            using var command = _dbConnection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS peers (
                    peer_id TEXT PRIMARY KEY,
                    address TEXT NOT NULL,
                    port INTEGER NOT NULL,
                    protocols TEXT NOT NULL,
                    first_seen TEXT NOT NULL,
                    last_seen TEXT NOT NULL,
                    failure_count INTEGER NOT NULL DEFAULT 0,
                    is_bootstrap INTEGER NOT NULL DEFAULT 0,
                    avg_latency_ms INTEGER NOT NULL DEFAULT 0
                )";

            await command.ExecuteNonQueryAsync();
            _logger.LogDebug("Database initialized");
        }
        finally
        {
            _dbLock.Release();
        }
    }

    /// <summary>
    /// Persists a peer to the database
    /// </summary>
    private async Task PersistPeerAsync(PeerNode peer, CancellationToken cancellationToken)
    {
        await _dbLock.WaitAsync(cancellationToken);
        try
        {
            using var command = _dbConnection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO peers
                (peer_id, address, port, protocols, first_seen, last_seen, failure_count, is_bootstrap, avg_latency_ms)
                VALUES ($peerId, $address, $port, $protocols, $firstSeen, $lastSeen, $failureCount, $isBootstrap, $avgLatency)";

            command.Parameters.AddWithValue("$peerId", peer.PeerId);
            command.Parameters.AddWithValue("$address", peer.Address);
            command.Parameters.AddWithValue("$port", peer.Port);
            command.Parameters.AddWithValue("$protocols", string.Join(",", peer.SupportedProtocols));
            command.Parameters.AddWithValue("$firstSeen", peer.FirstSeen.ToString("o"));
            command.Parameters.AddWithValue("$lastSeen", peer.LastSeen.ToString("o"));
            command.Parameters.AddWithValue("$failureCount", peer.FailureCount);
            command.Parameters.AddWithValue("$isBootstrap", peer.IsBootstrapNode ? 1 : 0);
            command.Parameters.AddWithValue("$avgLatency", peer.AverageLatencyMs);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting peer {PeerId}", peer.PeerId);
        }
        finally
        {
            _dbLock.Release();
        }
    }

    /// <summary>
    /// Deletes a peer from the database
    /// </summary>
    private async Task DeletePeerAsync(string peerId, CancellationToken cancellationToken)
    {
        await _dbLock.WaitAsync(cancellationToken);
        try
        {
            using var command = _dbConnection.CreateCommand();
            command.CommandText = "DELETE FROM peers WHERE peer_id = $peerId";
            command.Parameters.AddWithValue("$peerId", peerId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting peer {PeerId}", peerId);
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _dbLock.Dispose();
            _dbConnection.Dispose();
            _disposed = true;
        }
    }
}
