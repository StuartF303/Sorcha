// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Discovery;
using Sorcha.Peer.Service.Monitoring;
using Sorcha.Peer.Service.Network;

namespace Sorcha.Peer.Service.Tests.Monitoring;

public class HealthMonitorServiceTests
{
    private readonly Mock<ILogger<HealthMonitorService>> _loggerMock;
    private readonly Mock<IOptions<PeerServiceConfiguration>> _configMock;
    private readonly Mock<PeerListManager> _peerListManagerMock;
    private readonly Mock<PeerDiscoveryService> _peerDiscoveryServiceMock;
    private readonly PeerServiceConfiguration _configuration;

    public HealthMonitorServiceTests()
    {
        _loggerMock = new Mock<ILogger<HealthMonitorService>>();
        _configMock = new Mock<IOptions<PeerServiceConfiguration>>();
        _configuration = new PeerServiceConfiguration
        {
            PeerDiscovery = new PeerDiscoveryConfiguration
            {
                MinHealthyPeers = 5,
                MaxConcurrentDiscoveries = 10
            }
        };
        _configMock.Setup(x => x.Value).Returns(_configuration);

        // Create mock dependencies
        var peerListLoggerMock = new Mock<ILogger<PeerListManager>>();
        _peerListManagerMock = new Mock<PeerListManager>(
            peerListLoggerMock.Object,
            _configMock.Object);

        var discoveryLoggerMock = new Mock<ILogger<PeerDiscoveryService>>();
        var stunClientMock = new Mock<StunClient>();
        var networkServiceMock = new Mock<NetworkAddressService>(
            Mock.Of<ILogger<NetworkAddressService>>(),
            _configMock.Object,
            new HttpClient(),
            stunClientMock.Object);

        _peerDiscoveryServiceMock = new Mock<PeerDiscoveryService>(
            discoveryLoggerMock.Object,
            _configMock.Object,
            _peerListManagerMock.Object,
            networkServiceMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDependencies()
    {
        // Act
        var service = new HealthMonitorService(
            _loggerMock.Object,
            _configMock.Object,
            _peerListManagerMock.Object,
            _peerDiscoveryServiceMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        var act = () => new HealthMonitorService(
            null!,
            _configMock.Object,
            _peerListManagerMock.Object,
            _peerDiscoveryServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DetermineServiceStatus_ShouldReturnOffline_WhenNoPeers()
    {
        // Arrange
        _peerListManagerMock.Setup(x => x.GetHealthyPeerCount()).Returns(0);
        var service = new HealthMonitorService(
            _loggerMock.Object,
            _configMock.Object,
            _peerListManagerMock.Object,
            _peerDiscoveryServiceMock.Object);

        // Act
        var status = service.DetermineServiceStatus();

        // Assert
        status.Should().Be(PeerServiceStatus.Offline);
    }

    [Fact]
    public void DetermineServiceStatus_ShouldReturnDegraded_WhenBelowMinimum()
    {
        // Arrange
        _peerListManagerMock.Setup(x => x.GetHealthyPeerCount()).Returns(3); // Below min of 5
        var service = new HealthMonitorService(
            _loggerMock.Object,
            _configMock.Object,
            _peerListManagerMock.Object,
            _peerDiscoveryServiceMock.Object);

        // Act
        var status = service.DetermineServiceStatus();

        // Assert
        status.Should().Be(PeerServiceStatus.Degraded);
    }

    [Fact]
    public void DetermineServiceStatus_ShouldReturnOnline_WhenAboveMinimum()
    {
        // Arrange
        _peerListManagerMock.Setup(x => x.GetHealthyPeerCount()).Returns(10);
        var service = new HealthMonitorService(
            _loggerMock.Object,
            _configMock.Object,
            _peerListManagerMock.Object,
            _peerDiscoveryServiceMock.Object);

        // Act
        var status = service.DetermineServiceStatus();

        // Assert
        status.Should().Be(PeerServiceStatus.Online);
    }

    [Fact]
    public void GetNetworkStatistics_ShouldReturnStatistics()
    {
        // Arrange
        var peers = new List<PeerNode>
        {
            new PeerNode { PeerId = "peer1", AverageLatencyMs = 50 },
            new PeerNode { PeerId = "peer2", AverageLatencyMs = 100 }
        }.AsReadOnly();

        _peerListManagerMock.Setup(x => x.GetAllPeers()).Returns(peers);
        _peerListManagerMock.Setup(x => x.GetHealthyPeers()).Returns(peers);

        var service = new HealthMonitorService(
            _loggerMock.Object,
            _configMock.Object,
            _peerListManagerMock.Object,
            _peerDiscoveryServiceMock.Object);

        // Act
        var stats = service.GetNetworkStatistics();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalPeers.Should().Be(2);
        stats.HealthyPeers.Should().Be(2);
        stats.AverageLatencyMs.Should().Be(75);
    }
}
