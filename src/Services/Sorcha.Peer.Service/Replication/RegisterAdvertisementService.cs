// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Logging;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Discovery;
using System.Collections.Concurrent;

namespace Sorcha.Peer.Service.Replication;

/// <summary>
/// Manages register advertisements — tracking which registers this node holds,
/// building advertisement messages for peers, and processing incoming advertisements.
/// </summary>
public class RegisterAdvertisementService
{
    private readonly ILogger<RegisterAdvertisementService> _logger;
    private readonly PeerListManager _peerListManager;
    private readonly ConcurrentDictionary<string, LocalRegisterAdvertisement> _localAdvertisements = new();

    public RegisterAdvertisementService(
        ILogger<RegisterAdvertisementService> logger,
        PeerListManager peerListManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _peerListManager = peerListManager ?? throw new ArgumentNullException(nameof(peerListManager));
    }

    /// <summary>
    /// Advertises a register that this node holds or subscribes to.
    /// </summary>
    public void AdvertiseRegister(
        string registerId,
        RegisterSyncState syncState,
        long latestVersion = 0,
        long latestDocketVersion = 0,
        bool isPublic = false)
    {
        var ad = new LocalRegisterAdvertisement
        {
            RegisterId = registerId,
            SyncState = syncState,
            LatestVersion = latestVersion,
            LatestDocketVersion = latestDocketVersion,
            IsPublic = isPublic,
            LastUpdated = DateTimeOffset.UtcNow
        };

        _localAdvertisements[registerId] = ad;

        _logger.LogDebug(
            "Advertising register {RegisterId} with state {SyncState}, version {Version}",
            registerId, syncState, latestVersion);
    }

    /// <summary>
    /// Updates the version information for an already-advertised register.
    /// </summary>
    public void UpdateRegisterVersion(
        string registerId,
        long latestVersion,
        long latestDocketVersion)
    {
        if (_localAdvertisements.TryGetValue(registerId, out var ad))
        {
            ad.LatestVersion = latestVersion;
            ad.LatestDocketVersion = latestDocketVersion;
            ad.LastUpdated = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Removes an advertisement for a register (e.g., when unsubscribing).
    /// </summary>
    public void RemoveAdvertisement(string registerId)
    {
        if (_localAdvertisements.TryRemove(registerId, out _))
        {
            _logger.LogDebug("Removed advertisement for register {RegisterId}", registerId);
        }
    }

    /// <summary>
    /// Gets all local register advertisements to include in heartbeats/exchanges.
    /// </summary>
    public IReadOnlyCollection<LocalRegisterAdvertisement> GetLocalAdvertisements()
    {
        return _localAdvertisements.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets only public register advertisements (for sharing with unknown peers).
    /// </summary>
    public IReadOnlyCollection<LocalRegisterAdvertisement> GetPublicAdvertisements()
    {
        return _localAdvertisements.Values
            .Where(a => a.IsPublic)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets a specific local advertisement.
    /// </summary>
    public LocalRegisterAdvertisement? GetAdvertisement(string registerId)
    {
        _localAdvertisements.TryGetValue(registerId, out var ad);
        return ad;
    }

    /// <summary>
    /// Processes incoming register advertisements from a remote peer.
    /// Updates the peer's advertised registers in PeerListManager.
    /// </summary>
    public async Task ProcessRemoteAdvertisementsAsync(
        string sourcePeerId,
        IEnumerable<PeerRegisterInfo> remoteAdvertisements,
        CancellationToken cancellationToken = default)
    {
        var peer = _peerListManager.GetPeer(sourcePeerId);
        if (peer == null)
        {
            _logger.LogDebug(
                "Received advertisements from unknown peer {PeerId}, ignoring",
                sourcePeerId);
            return;
        }

        // Update the peer's advertised registers
        peer.AdvertisedRegisters = remoteAdvertisements.ToList();
        await _peerListManager.AddOrUpdatePeerAsync(peer);

        _logger.LogDebug(
            "Updated {Count} register advertisements from peer {PeerId}",
            peer.AdvertisedRegisters.Count, sourcePeerId);
    }

    /// <summary>
    /// Builds PeerRegisterInfo list from local advertisements for sharing with peers.
    /// </summary>
    public List<PeerRegisterInfo> BuildPeerRegisterInfoList()
    {
        return _localAdvertisements.Values
            .Select(a => new PeerRegisterInfo
            {
                RegisterId = a.RegisterId,
                SyncState = a.SyncState,
                LatestVersion = a.LatestVersion,
                IsPublic = a.IsPublic
            })
            .ToList();
    }

    /// <summary>
    /// Detects registers where the remote peer has a newer version than us.
    /// Returns register IDs that may need syncing.
    /// </summary>
    public IReadOnlyCollection<string> DetectVersionLag(
        IDictionary<string, long> remoteVersions)
    {
        var lagging = new List<string>();

        foreach (var (registerId, remoteVersion) in remoteVersions)
        {
            if (_localAdvertisements.TryGetValue(registerId, out var local))
            {
                if (remoteVersion > local.LatestVersion)
                {
                    lagging.Add(registerId);
                }
            }
        }

        return lagging.AsReadOnly();
    }
}

/// <summary>
/// Local register advertisement state tracked by this node.
/// </summary>
public class LocalRegisterAdvertisement
{
    public required string RegisterId { get; init; }
    public RegisterSyncState SyncState { get; set; }
    public long LatestVersion { get; set; }
    public long LatestDocketVersion { get; set; }
    public bool IsPublic { get; set; }
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}
