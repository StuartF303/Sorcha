// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Discovery;
using Sorcha.Peer.Service.Replication;

namespace Sorcha.Peer.Service.Tests.Replication;

public class RegisterAdvertisementServiceTests : IDisposable
{
    private readonly RegisterAdvertisementService _service;
    private readonly PeerListManager _peerListManager;

    public RegisterAdvertisementServiceTests()
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

        _service = new RegisterAdvertisementService(
            new Mock<ILogger<RegisterAdvertisementService>>().Object,
            _peerListManager);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Action act = () => new RegisterAdvertisementService(null!, _peerListManager);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullPeerListManager_ThrowsArgumentNullException()
    {
        Action act = () => new RegisterAdvertisementService(
            new Mock<ILogger<RegisterAdvertisementService>>().Object, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AdvertiseRegister_AddsToLocalAdvertisements()
    {
        _service.AdvertiseRegister("reg-1", RegisterSyncState.Active, 100, 10, true);

        var ad = _service.GetAdvertisement("reg-1");
        ad.Should().NotBeNull();
        ad!.RegisterId.Should().Be("reg-1");
        ad.SyncState.Should().Be(RegisterSyncState.Active);
        ad.LatestVersion.Should().Be(100);
        ad.LatestDocketVersion.Should().Be(10);
        ad.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void AdvertiseRegister_OverwritesExisting()
    {
        _service.AdvertiseRegister("reg-1", RegisterSyncState.Syncing, 50);
        _service.AdvertiseRegister("reg-1", RegisterSyncState.FullyReplicated, 100);

        var ad = _service.GetAdvertisement("reg-1");
        ad!.SyncState.Should().Be(RegisterSyncState.FullyReplicated);
        ad.LatestVersion.Should().Be(100);
    }

    [Fact]
    public void UpdateRegisterVersion_ExistingRegister_UpdatesVersion()
    {
        _service.AdvertiseRegister("reg-1", RegisterSyncState.Active, 100);

        _service.UpdateRegisterVersion("reg-1", 200, 20);

        var ad = _service.GetAdvertisement("reg-1");
        ad!.LatestVersion.Should().Be(200);
        ad.LatestDocketVersion.Should().Be(20);
    }

    [Fact]
    public void UpdateRegisterVersion_NonExistent_NoOp()
    {
        // Should not throw
        _service.UpdateRegisterVersion("nonexistent", 200, 20);

        _service.GetAdvertisement("nonexistent").Should().BeNull();
    }

    [Fact]
    public void RemoveAdvertisement_ExistingRegister_Removes()
    {
        _service.AdvertiseRegister("reg-1", RegisterSyncState.Active);

        _service.RemoveAdvertisement("reg-1");

        _service.GetAdvertisement("reg-1").Should().BeNull();
    }

    [Fact]
    public void RemoveAdvertisement_NonExistent_NoOp()
    {
        // Should not throw
        _service.RemoveAdvertisement("nonexistent");
    }

    [Fact]
    public void GetLocalAdvertisements_ReturnsAllAdvertisements()
    {
        _service.AdvertiseRegister("reg-1", RegisterSyncState.Active);
        _service.AdvertiseRegister("reg-2", RegisterSyncState.FullyReplicated);

        var ads = _service.GetLocalAdvertisements();

        ads.Should().HaveCount(2);
    }

    [Fact]
    public void GetPublicAdvertisements_FiltersNonPublic()
    {
        _service.AdvertiseRegister("reg-1", RegisterSyncState.Active, isPublic: true);
        _service.AdvertiseRegister("reg-2", RegisterSyncState.Active, isPublic: false);
        _service.AdvertiseRegister("reg-3", RegisterSyncState.Active, isPublic: true);

        var ads = _service.GetPublicAdvertisements();

        ads.Should().HaveCount(2);
        ads.Select(a => a.RegisterId).Should().BeEquivalentTo(["reg-1", "reg-3"]);
    }

    [Fact]
    public void GetAdvertisement_NonExistent_ReturnsNull()
    {
        _service.GetAdvertisement("nonexistent").Should().BeNull();
    }

    [Fact]
    public void BuildPeerRegisterInfoList_ConvertsToRegisterInfo()
    {
        _service.AdvertiseRegister("reg-1", RegisterSyncState.Active, 100, isPublic: true);
        _service.AdvertiseRegister("reg-2", RegisterSyncState.FullyReplicated, 200, isPublic: false);

        var infos = _service.BuildPeerRegisterInfoList();

        infos.Should().HaveCount(2);
        var reg1 = infos.First(i => i.RegisterId == "reg-1");
        reg1.SyncState.Should().Be(RegisterSyncState.Active);
        reg1.LatestVersion.Should().Be(100);
        reg1.IsPublic.Should().BeTrue();

        var reg2 = infos.First(i => i.RegisterId == "reg-2");
        reg2.SyncState.Should().Be(RegisterSyncState.FullyReplicated);
        reg2.CanServeFullReplica.Should().BeTrue();
    }

    [Fact]
    public void DetectVersionLag_RemoteAhead_ReturnsLaggingRegisters()
    {
        _service.AdvertiseRegister("reg-1", RegisterSyncState.Active, 100);
        _service.AdvertiseRegister("reg-2", RegisterSyncState.Active, 200);

        var remoteVersions = new Dictionary<string, long>
        {
            { "reg-1", 150 }, // remote ahead
            { "reg-2", 100 }, // we're ahead
        };

        var lagging = _service.DetectVersionLag(remoteVersions);

        lagging.Should().ContainSingle().Which.Should().Be("reg-1");
    }

    [Fact]
    public void DetectVersionLag_AllUpToDate_ReturnsEmpty()
    {
        _service.AdvertiseRegister("reg-1", RegisterSyncState.Active, 100);

        var remoteVersions = new Dictionary<string, long>
        {
            { "reg-1", 100 }
        };

        _service.DetectVersionLag(remoteVersions).Should().BeEmpty();
    }

    [Fact]
    public void DetectVersionLag_UnknownRegister_Ignored()
    {
        _service.AdvertiseRegister("reg-1", RegisterSyncState.Active, 100);

        var remoteVersions = new Dictionary<string, long>
        {
            { "reg-1", 50 },
            { "reg-unknown", 500 } // not in local ads
        };

        _service.DetectVersionLag(remoteVersions).Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessRemoteAdvertisementsAsync_KnownPeer_UpdatesRegisters()
    {
        // Add a peer first
        var peer = new PeerNode
        {
            PeerId = "peer-1",
            Address = "192.168.1.100",
            Port = 5001
        };
        await _peerListManager.AddOrUpdatePeerAsync(peer);

        // Process advertisements from that peer
        var ads = new List<PeerRegisterInfo>
        {
            new() { RegisterId = "reg-1", SyncState = RegisterSyncState.Active, LatestVersion = 100 },
            new() { RegisterId = "reg-2", SyncState = RegisterSyncState.FullyReplicated, LatestVersion = 200 }
        };

        await _service.ProcessRemoteAdvertisementsAsync("peer-1", ads);

        var updatedPeer = _peerListManager.GetPeer("peer-1");
        updatedPeer!.AdvertisedRegisters.Should().HaveCount(2);
    }

    [Fact]
    public async Task ProcessRemoteAdvertisementsAsync_UnknownPeer_Ignored()
    {
        var ads = new List<PeerRegisterInfo>
        {
            new() { RegisterId = "reg-1", SyncState = RegisterSyncState.Active }
        };

        // Should not throw
        await _service.ProcessRemoteAdvertisementsAsync("unknown-peer", ads);
    }

    public void Dispose()
    {
        _peerListManager.Dispose();
    }
}
