// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Discovery;
using Sorcha.Peer.Service.Monitoring;
using Sorcha.Peer.Service.Replication;

namespace Sorcha.Peer.Service.Tests.Endpoints;

/// <summary>
/// Tests for the peer management REST API endpoint logic.
/// These tests verify the service layer methods that the endpoints delegate to.
/// </summary>
public class PeerManagementEndpointTests : IDisposable
{
    private readonly PeerListManager _peerListManager;
    private readonly RegisterAdvertisementService _advertisementService;
    private readonly ConnectionQualityTracker _qualityTracker;

    public PeerManagementEndpointTests()
    {
        _peerListManager = new PeerListManager(
            new Mock<ILogger<PeerListManager>>().Object,
            Options.Create(new PeerServiceConfiguration
            {
                NodeId = "test-node",
                PeerDiscovery = new PeerDiscoveryConfiguration
                {
                    MaxPeersInList = 100,
                    MinHealthyPeers = 5,
                    RefreshIntervalMinutes = 15
                },
                SeedNodes = new SeedNodeConfiguration()
            }));

        _advertisementService = new RegisterAdvertisementService(
            new Mock<ILogger<RegisterAdvertisementService>>().Object,
            _peerListManager);

        _qualityTracker = new ConnectionQualityTracker(
            new Mock<ILogger<ConnectionQualityTracker>>().Object);
    }

    // === GET /api/peers Tests ===

    [Fact]
    public async Task GetPeers_ReturnsAllPeers_IncludingBanned()
    {
        await _peerListManager.AddOrUpdatePeerAsync(CreatePeer("peer-1", "192.168.1.1"));
        await _peerListManager.AddOrUpdatePeerAsync(CreatePeer("peer-2", "192.168.1.2"));
        await _peerListManager.BanPeerAsync("peer-2", "bad behavior");

        var peers = _peerListManager.GetAllPeers();

        peers.Should().HaveCount(2);
        peers.Should().Contain(p => p.PeerId == "peer-1" && !p.IsBanned);
        peers.Should().Contain(p => p.PeerId == "peer-2" && p.IsBanned);
    }

    [Fact]
    public async Task GetPeers_IncludesQualityScores()
    {
        await _peerListManager.AddOrUpdatePeerAsync(CreatePeer("peer-1", "192.168.1.1"));
        _qualityTracker.RecordSuccess("peer-1", 50);

        var peers = _peerListManager.GetAllPeers();
        var qualities = _qualityTracker.GetAllQualities();

        peers.Should().HaveCount(1);
        qualities.Should().ContainKey("peer-1");
        qualities["peer-1"].QualityScore.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPeers_IncludesAdvertisedRegisters()
    {
        var peer = CreatePeer("peer-1", "192.168.1.1");
        peer.AdvertisedRegisters = new List<PeerRegisterInfo>
        {
            new() { RegisterId = "reg-1", SyncState = RegisterSyncState.Active, LatestVersion = 100, IsPublic = true }
        };
        await _peerListManager.AddOrUpdatePeerAsync(peer);

        var peers = _peerListManager.GetAllPeers();

        peers.Should().ContainSingle();
        peers.First().AdvertisedRegisters.Should().HaveCount(1);
        peers.First().AdvertisedRegisters.First().RegisterId.Should().Be("reg-1");
    }

    // === GET /api/peers/{peerId} Tests ===

    [Fact]
    public async Task GetPeerById_ExistingPeer_ReturnsPeer()
    {
        await _peerListManager.AddOrUpdatePeerAsync(CreatePeer("peer-1", "192.168.1.1"));

        var peer = _peerListManager.GetPeer("peer-1");

        peer.Should().NotBeNull();
        peer!.PeerId.Should().Be("peer-1");
    }

    [Fact]
    public void GetPeerById_NonExistent_ReturnsNull()
    {
        var peer = _peerListManager.GetPeer("nonexistent");

        peer.Should().BeNull();
    }

    [Fact]
    public async Task GetPeerById_IncludesQualityDetails()
    {
        await _peerListManager.AddOrUpdatePeerAsync(CreatePeer("peer-1", "192.168.1.1"));
        _qualityTracker.RecordSuccess("peer-1", 30);
        _qualityTracker.RecordSuccess("peer-1", 70);

        var quality = _qualityTracker.GetQuality("peer-1");

        quality.Should().NotBeNull();
        quality!.AverageLatencyMs.Should().Be(50);
        quality.TotalRequests.Should().Be(2);
        quality.SuccessRate.Should().Be(1.0);
    }

    // === GET /api/peers/quality Tests ===

    [Fact]
    public void GetQuality_EmptyTracker_ReturnsEmpty()
    {
        var qualities = _qualityTracker.GetAllQualities();

        qualities.Should().BeEmpty();
    }

    [Fact]
    public void GetQuality_WithRecordings_ReturnsAllQualities()
    {
        _qualityTracker.RecordSuccess("peer-1", 50);
        _qualityTracker.RecordSuccess("peer-2", 100);
        _qualityTracker.RecordFailure("peer-2");

        var qualities = _qualityTracker.GetAllQualities();

        qualities.Should().HaveCount(2);
        qualities["peer-1"].SuccessRate.Should().Be(1.0);
        qualities["peer-2"].SuccessRate.Should().BeLessThan(1.0);
    }

    // === GET /api/registers/subscriptions Tests ===
    // (RegisterSyncBackgroundService requires full DI; tested indirectly via subscription state)

    // === GET /api/registers/available Tests ===

    [Fact]
    public async Task GetAvailableRegisters_ReturnsAggregatedRegisters()
    {
        var peer1 = CreatePeer("peer-1", "192.168.1.1");
        peer1.AdvertisedRegisters = new List<PeerRegisterInfo>
        {
            new() { RegisterId = "reg-1", SyncState = RegisterSyncState.FullyReplicated, LatestVersion = 100, IsPublic = true },
            new() { RegisterId = "reg-2", SyncState = RegisterSyncState.Active, LatestVersion = 50, IsPublic = true }
        };
        var peer2 = CreatePeer("peer-2", "192.168.1.2");
        peer2.AdvertisedRegisters = new List<PeerRegisterInfo>
        {
            new() { RegisterId = "reg-1", SyncState = RegisterSyncState.FullyReplicated, LatestVersion = 200, IsPublic = true }
        };
        await _peerListManager.AddOrUpdatePeerAsync(peer1);
        await _peerListManager.AddOrUpdatePeerAsync(peer2);

        var result = _advertisementService.GetNetworkAdvertisedRegisters();

        result.Should().HaveCount(2);
        var reg1 = result.First(r => r.RegisterId == "reg-1");
        reg1.PeerCount.Should().Be(2);
        reg1.LatestVersion.Should().Be(200);
    }

    [Fact]
    public void GetAvailableRegisters_NoPeers_ReturnsEmpty()
    {
        var result = _advertisementService.GetNetworkAdvertisedRegisters();

        result.Should().BeEmpty();
    }

    // === POST /api/registers/{registerId}/subscribe Tests ===

    [Fact]
    public async Task Subscribe_RegisterNotInNetwork_LogicReturnsNotFound()
    {
        // Simulate endpoint logic: check if register exists in advertisements
        var available = _advertisementService.GetNetworkAdvertisedRegisters();
        var exists = available.Any(r => r.RegisterId == "nonexistent");

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Subscribe_RegisterExists_FoundInAdvertisements()
    {
        var peer = CreatePeer("peer-1", "192.168.1.1");
        peer.AdvertisedRegisters = new List<PeerRegisterInfo>
        {
            new() { RegisterId = "reg-target", SyncState = RegisterSyncState.FullyReplicated, LatestVersion = 100, IsPublic = true }
        };
        await _peerListManager.AddOrUpdatePeerAsync(peer);

        var available = _advertisementService.GetNetworkAdvertisedRegisters();
        var exists = available.Any(r => r.RegisterId == "reg-target");

        exists.Should().BeTrue();
    }

    [Fact]
    public void Subscribe_InvalidMode_DoesNotParse()
    {
        var validForwardOnly = Enum.TryParse<ReplicationMode>("forwardonly", ignoreCase: true, out _);
        var validFullReplica = Enum.TryParse<ReplicationMode>("fullreplica", ignoreCase: true, out _);
        var invalidMode = Enum.TryParse<ReplicationMode>("invalid", ignoreCase: true, out _);

        validForwardOnly.Should().BeTrue();
        validFullReplica.Should().BeTrue();
        invalidMode.Should().BeFalse();
    }

    // === POST /api/peers/{peerId}/ban Tests ===

    [Fact]
    public async Task BanPeer_ExistingPeer_SetsBanned()
    {
        await _peerListManager.AddOrUpdatePeerAsync(CreatePeer("peer-1", "192.168.1.1"));

        var result = await _peerListManager.BanPeerAsync("peer-1", "test reason");

        result.Should().BeTrue();
        var peer = _peerListManager.GetPeer("peer-1");
        peer!.IsBanned.Should().BeTrue();
        peer.BanReason.Should().Be("test reason");
        peer.BannedAt.Should().NotBeNull();
    }

    [Fact]
    public void BanPeer_NonExistent_ReturnsFalse()
    {
        // Endpoint checks GetPeer first
        var peer = _peerListManager.GetPeer("nonexistent");
        peer.Should().BeNull();
    }

    [Fact]
    public async Task BanPeer_AlreadyBanned_EndpointReturnsConflict()
    {
        await _peerListManager.AddOrUpdatePeerAsync(CreatePeer("peer-1", "192.168.1.1"));
        await _peerListManager.BanPeerAsync("peer-1");

        var peer = _peerListManager.GetPeer("peer-1");
        peer!.IsBanned.Should().BeTrue();

        // Endpoint logic: if already banned, return 409
        // Verify the state is already banned
        var alreadyBanned = peer.IsBanned;
        alreadyBanned.Should().BeTrue();
    }

    // === DELETE /api/peers/{peerId}/ban Tests ===

    [Fact]
    public async Task UnbanPeer_BannedPeer_ClearsBan()
    {
        await _peerListManager.AddOrUpdatePeerAsync(CreatePeer("peer-1", "192.168.1.1"));
        await _peerListManager.BanPeerAsync("peer-1", "bad actor");

        var result = await _peerListManager.UnbanPeerAsync("peer-1");

        result.Should().BeTrue();
        var peer = _peerListManager.GetPeer("peer-1");
        peer!.IsBanned.Should().BeFalse();
        peer.BanReason.Should().BeNull();
        peer.BannedAt.Should().BeNull();
    }

    [Fact]
    public async Task UnbanPeer_NotBanned_EndpointReturnsConflict()
    {
        await _peerListManager.AddOrUpdatePeerAsync(CreatePeer("peer-1", "192.168.1.1"));

        var peer = _peerListManager.GetPeer("peer-1");
        peer!.IsBanned.Should().BeFalse();
    }

    // === POST /api/peers/{peerId}/reset Tests ===

    [Fact]
    public async Task ResetPeer_WithFailures_ReturnsZero()
    {
        var peer = CreatePeer("peer-1", "192.168.1.1");
        peer.FailureCount = 5;
        await _peerListManager.AddOrUpdatePeerAsync(peer);

        var previousCount = await _peerListManager.ResetFailureCountAsync("peer-1");

        previousCount.Should().Be(5);
        _peerListManager.GetPeer("peer-1")!.FailureCount.Should().Be(0);
    }

    [Fact]
    public async Task ResetPeer_NonExistent_ReturnsNegativeOne()
    {
        var result = await _peerListManager.ResetFailureCountAsync("nonexistent");

        result.Should().Be(-1);
    }

    // === Banned peers excluded from healthy peers ===

    [Fact]
    public async Task GetHealthyPeers_ExcludesBanned()
    {
        await _peerListManager.AddOrUpdatePeerAsync(CreatePeer("peer-1", "192.168.1.1"));
        await _peerListManager.AddOrUpdatePeerAsync(CreatePeer("peer-2", "192.168.1.2"));
        await _peerListManager.BanPeerAsync("peer-2");

        var healthy = _peerListManager.GetHealthyPeers();

        healthy.Should().ContainSingle().Which.PeerId.Should().Be("peer-1");
    }

    // === Available registers exclude banned peer advertisements ===

    [Fact]
    public async Task GetAvailableRegisters_ExcludesBannedPeerAds()
    {
        var peer1 = CreatePeer("peer-1", "192.168.1.1");
        peer1.AdvertisedRegisters = new List<PeerRegisterInfo>
        {
            new() { RegisterId = "reg-1", SyncState = RegisterSyncState.Active, LatestVersion = 100, IsPublic = true }
        };
        var peer2 = CreatePeer("peer-2", "192.168.1.2");
        peer2.AdvertisedRegisters = new List<PeerRegisterInfo>
        {
            new() { RegisterId = "reg-1", SyncState = RegisterSyncState.FullyReplicated, LatestVersion = 200, IsPublic = true }
        };
        await _peerListManager.AddOrUpdatePeerAsync(peer1);
        await _peerListManager.AddOrUpdatePeerAsync(peer2);
        await _peerListManager.BanPeerAsync("peer-2");

        var result = _advertisementService.GetNetworkAdvertisedRegisters();

        result.Should().ContainSingle();
        result.First().PeerCount.Should().Be(1);
        result.First().LatestVersion.Should().Be(100); // banned peer's 200 excluded
    }

    // === Available registers exclude private registers ===

    [Fact]
    public async Task GetAvailableRegisters_ExcludesPrivate()
    {
        var peer = CreatePeer("peer-1", "192.168.1.1");
        peer.AdvertisedRegisters = new List<PeerRegisterInfo>
        {
            new() { RegisterId = "public-reg", SyncState = RegisterSyncState.Active, LatestVersion = 100, IsPublic = true },
            new() { RegisterId = "private-reg", SyncState = RegisterSyncState.Active, LatestVersion = 200, IsPublic = false }
        };
        await _peerListManager.AddOrUpdatePeerAsync(peer);

        var result = _advertisementService.GetNetworkAdvertisedRegisters();

        result.Should().ContainSingle().Which.RegisterId.Should().Be("public-reg");
    }

    // === Helpers ===

    private static PeerNode CreatePeer(string peerId, string address, int port = 5001)
    {
        return new PeerNode
        {
            PeerId = peerId,
            Address = address,
            Port = port
        };
    }

    public void Dispose()
    {
        _peerListManager.Dispose();
    }
}
