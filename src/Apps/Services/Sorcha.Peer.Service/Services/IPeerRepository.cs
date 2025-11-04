// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Peer.Service.Models;

namespace Sorcha.Peer.Service.Services;

/// <summary>
/// Repository interface for peer node management
/// </summary>
public interface IPeerRepository
{
    Task<PeerNode?> GetPeerAsync(string peerId);
    Task<IEnumerable<PeerNode>> GetAllPeersAsync();
    Task<string> RegisterPeerAsync(string peerId, string endpoint, Dictionary<string, string> metadata);
    Task<bool> UnregisterPeerAsync(string peerId);
    Task<bool> UpdatePeerStatusAsync(string peerId, string status);
}

/// <summary>
/// In-memory implementation of peer repository
/// </summary>
public class InMemoryPeerRepository : IPeerRepository
{
    private readonly Dictionary<string, PeerNode> _peers = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<PeerNode?> GetPeerAsync(string peerId)
    {
        await _lock.WaitAsync();
        try
        {
            return _peers.TryGetValue(peerId, out var peer) ? peer : null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IEnumerable<PeerNode>> GetAllPeersAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return _peers.Values.ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string> RegisterPeerAsync(string peerId, string endpoint, Dictionary<string, string> metadata)
    {
        await _lock.WaitAsync();
        try
        {
            var actualPeerId = string.IsNullOrEmpty(peerId) ? Guid.NewGuid().ToString() : peerId;

            var peer = new PeerNode
            {
                PeerId = actualPeerId,
                Endpoint = endpoint,
                Status = "active",
                RegisteredAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Metadata = metadata
            };

            _peers[actualPeerId] = peer;
            return actualPeerId;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> UnregisterPeerAsync(string peerId)
    {
        await _lock.WaitAsync();
        try
        {
            return _peers.Remove(peerId);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> UpdatePeerStatusAsync(string peerId, string status)
    {
        await _lock.WaitAsync();
        try
        {
            if (_peers.TryGetValue(peerId, out var peer))
            {
                peer.Status = status;
                return true;
            }
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }
}
