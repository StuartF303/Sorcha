// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Discovery;
using Sorcha.Peer.Service.Monitoring;
using Sorcha.Peer.Service.Network;

namespace Sorcha.Peer.Service.Tests.Monitoring;

public class HealthMonitorServiceTests : IDisposable
{
    private readonly PeerListManager _peerListManager;
    private readonly PeerDiscoveryService _peerDiscoveryService;
    private readonly HealthMonitorService _service;
    private readonly PeerServiceConfiguration _configuration;

    public HealthMonitorServiceTests()
    {
        _configuration = new PeerServiceConfiguration
        {
            NodeId = "test-node",
            PeerDiscovery = new PeerDiscoveryConfiguration
            {
                MinHealthyPeers = 5,
                MaxPeersInList = 100,
                MaxConcurrentDiscoveries = 10,
                RefreshIntervalMinutes = 15
            },
            NetworkAddress = new NetworkAddressConfiguration
            {
                ExternalAddress = "127.0.0.1",
                HttpLookupServices = new List<string>(),
                PreferredProtocol = "IPv4"
            },
            SeedNodes = new SeedNodeConfiguration()
        };
        var config = Options.Create(_configuration);

        _peerListManager = new PeerListManager(
            new Mock<ILogger<PeerListManager>>().Object,
            config);

        var stunClient = new StunClient(new Mock<ILogger<StunClient>>().Object);
        var networkAddressService = new NetworkAddressService(
            new Mock<ILogger<NetworkAddressService>>().Object,
            config,
            new HttpClient(),
            stunClient);

        _peerDiscoveryService = new PeerDiscoveryService(
            new Mock<ILogger<PeerDiscoveryService>>().Object,
            config,
            _peerListManager,
            networkAddressService);

        _service = new HealthMonitorService(
            new Mock<ILogger<HealthMonitorService>>().Object,
            config,
            _peerListManager,
            _peerDiscoveryService);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDependencies()
    {
        _service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        var config = Options.Create(_configuration);

        var act = () => new HealthMonitorService(
            null!,
            config,
            _peerListManager,
            _peerDiscoveryService);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DetermineServiceStatus_ShouldReturnOffline_WhenNoPeers()
    {
        // No peers added — healthy peer count is 0
        var status = _service.DetermineServiceStatus();

        status.Should().Be(PeerServiceStatus.Offline);
    }

    [Fact]
    public async Task DetermineServiceStatus_ShouldReturnDegraded_WhenBelowMinimum()
    {
        // Add 3 peers (below min of 5)
        for (int i = 0; i < 3; i++)
        {
            await _peerListManager.AddOrUpdatePeerAsync(new PeerNode
            {
                PeerId = $"peer-{i}",
                Address = $"192.168.1.{i + 1}",
                Port = 5001
            });
        }

        var status = _service.DetermineServiceStatus();

        status.Should().Be(PeerServiceStatus.Degraded);
    }

    [Fact]
    public async Task DetermineServiceStatus_ShouldReturnOnline_WhenAboveMinimum()
    {
        // Add 10 peers (above min of 5)
        for (int i = 0; i < 10; i++)
        {
            await _peerListManager.AddOrUpdatePeerAsync(new PeerNode
            {
                PeerId = $"peer-{i}",
                Address = $"192.168.1.{i + 1}",
                Port = 5001
            });
        }

        var status = _service.DetermineServiceStatus();

        status.Should().Be(PeerServiceStatus.Online);
    }

    [Fact]
    public async Task GetNetworkStatistics_ShouldReturnStatistics()
    {
        await _peerListManager.AddOrUpdatePeerAsync(new PeerNode
        {
            PeerId = "peer1",
            Address = "192.168.1.1",
            Port = 5001,
            AverageLatencyMs = 50
        });
        await _peerListManager.AddOrUpdatePeerAsync(new PeerNode
        {
            PeerId = "peer2",
            Address = "192.168.1.2",
            Port = 5001,
            AverageLatencyMs = 100
        });

        var stats = _service.GetNetworkStatistics();

        stats.Should().NotBeNull();
        stats.TotalPeers.Should().Be(2);
        stats.HealthyPeers.Should().Be(2);
        stats.AverageLatencyMs.Should().Be(75);
    }

    public void Dispose()
    {
        _peerListManager.Dispose();
    }
}
