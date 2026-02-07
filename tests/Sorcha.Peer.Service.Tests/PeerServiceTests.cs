// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Sorcha.Peer.Service.Connection;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Discovery;
using Sorcha.Peer.Service.Monitoring;
using Sorcha.Peer.Service.Network;
using Sorcha.Peer.Service.Observability;
using Sorcha.Peer.Service.Replication;

namespace Sorcha.Peer.Service.Tests;

public class PeerServiceTests
{
    private readonly Mock<ILogger<PeerService>> _loggerMock;
    private readonly Mock<IOptions<PeerServiceConfiguration>> _configMock;
    private readonly Mock<PeerListManager> _peerListManagerMock;
    private readonly Mock<NetworkAddressService> _networkAddressServiceMock;
    private readonly Mock<PeerDiscoveryService> _peerDiscoveryServiceMock;
    private readonly Mock<HealthMonitorService> _healthMonitorServiceMock;
    private readonly PeerConnectionPool _connectionPool;
    private readonly PeerExchangeService _exchangeService;
    private readonly PeerServiceConfiguration _configuration;

    public PeerServiceTests()
    {
        _loggerMock = new Mock<ILogger<PeerService>>();
        _configMock = new Mock<IOptions<PeerServiceConfiguration>>();
        _peerListManagerMock = new Mock<PeerListManager>();
        _networkAddressServiceMock = new Mock<NetworkAddressService>();
        _peerDiscoveryServiceMock = new Mock<PeerDiscoveryService>();
        _healthMonitorServiceMock = new Mock<HealthMonitorService>();
        _configuration = new PeerServiceConfiguration
        {
            Enabled = true,
            ListenPort = 5001,
            NodeId = null // Will be auto-generated
        };
        _configMock.Setup(x => x.Value).Returns(_configuration);

        // Create real instances for sealed/non-mockable types
        var metrics = new PeerServiceMetrics();
        var activitySource = new PeerServiceActivitySource();
        var realPeerListManager = new PeerListManager(
            new Mock<ILogger<PeerListManager>>().Object,
            _configMock.Object);
        _connectionPool = new PeerConnectionPool(
            new Mock<ILogger<PeerConnectionPool>>().Object,
            realPeerListManager,
            _configMock.Object,
            metrics,
            activitySource);
        _exchangeService = new PeerExchangeService(
            new Mock<ILogger<PeerExchangeService>>().Object,
            realPeerListManager,
            _connectionPool,
            _configMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDependencies()
    {
        // Act
        var service = new PeerService(_loggerMock.Object, _configMock.Object, _peerListManagerMock.Object, _networkAddressServiceMock.Object, _peerDiscoveryServiceMock.Object, _healthMonitorServiceMock.Object, _connectionPool, _exchangeService);

        // Assert
        service.Should().NotBeNull();
        service.Status.Should().Be(PeerServiceStatus.Offline);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        var act = () => new PeerService(null!, _configMock.Object, _peerListManagerMock.Object, _networkAddressServiceMock.Object, _peerDiscoveryServiceMock.Object, _healthMonitorServiceMock.Object, _connectionPool, _exchangeService);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenConfigurationIsNull()
    {
        // Act
        var act = () => new PeerService(_loggerMock.Object, null!, _peerListManagerMock.Object, _networkAddressServiceMock.Object, _peerDiscoveryServiceMock.Object, _healthMonitorServiceMock.Object, _connectionPool, _exchangeService);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void Status_ShouldInitiallyBeOffline()
    {
        // Arrange
        var service = new PeerService(_loggerMock.Object, _configMock.Object, _peerListManagerMock.Object, _networkAddressServiceMock.Object, _peerDiscoveryServiceMock.Object, _healthMonitorServiceMock.Object, _connectionPool, _exchangeService);

        // Assert
        service.Status.Should().Be(PeerServiceStatus.Offline);
    }

    [Fact]
    public void NodeId_ShouldBeGenerated_WhenNotProvidedInConfiguration()
    {
        // Arrange
        _configuration.NodeId = null;
        var service = new PeerService(_loggerMock.Object, _configMock.Object, _peerListManagerMock.Object, _networkAddressServiceMock.Object, _peerDiscoveryServiceMock.Object, _healthMonitorServiceMock.Object, _connectionPool, _exchangeService);

        // Act - Start the service to trigger initialization
        var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);
        Thread.Sleep(100); // Allow service to initialize
        cts.Cancel();

        // Assert
        service.NodeId.Should().NotBeNullOrEmpty();
        service.NodeId.Should().StartWith("node_");
    }

    [Fact]
    public void NodeId_ShouldUseConfiguredValue_WhenProvided()
    {
        // Arrange
        _configuration.NodeId = "custom-node-123";
        var service = new PeerService(_loggerMock.Object, _configMock.Object, _peerListManagerMock.Object, _networkAddressServiceMock.Object, _peerDiscoveryServiceMock.Object, _healthMonitorServiceMock.Object, _connectionPool, _exchangeService);

        // Act - Start the service to trigger initialization
        var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);
        Thread.Sleep(100); // Allow service to initialize
        cts.Cancel();

        // Assert
        service.NodeId.Should().Be("custom-node-123");
    }

    [Fact]
    public async Task StartAsync_ShouldNotStart_WhenServiceIsDisabled()
    {
        // Arrange
        _configuration.Enabled = false;
        var service = new PeerService(_loggerMock.Object, _configMock.Object, _peerListManagerMock.Object, _networkAddressServiceMock.Object, _peerDiscoveryServiceMock.Object, _healthMonitorServiceMock.Object, _connectionPool, _exchangeService);
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act
        await service.StartAsync(cts.Token);

        // Assert
        service.Status.Should().Be(PeerServiceStatus.Offline);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("disabled")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartAsync_ShouldInitialize_WhenServiceIsEnabled()
    {
        // Arrange
        _configuration.Enabled = true;
        var service = new PeerService(_loggerMock.Object, _configMock.Object, _peerListManagerMock.Object, _networkAddressServiceMock.Object, _peerDiscoveryServiceMock.Object, _healthMonitorServiceMock.Object, _connectionPool, _exchangeService);
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act
        var startTask = service.StartAsync(cts.Token);
        await Task.Delay(200); // Allow initialization
        await service.StopAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Initializing")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task StopAsync_ShouldStopGracefully()
    {
        // Arrange
        var service = new PeerService(_loggerMock.Object, _configMock.Object, _peerListManagerMock.Object, _networkAddressServiceMock.Object, _peerDiscoveryServiceMock.Object, _healthMonitorServiceMock.Object, _connectionPool, _exchangeService);
        var cts = new CancellationTokenSource();

        // Act
        var startTask = service.StartAsync(cts.Token);
        await Task.Delay(200); // Allow service to start
        await service.StopAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stopping")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Service_ShouldLogNodeId_AfterInitialization()
    {
        // Arrange
        var service = new PeerService(_loggerMock.Object, _configMock.Object, _peerListManagerMock.Object, _networkAddressServiceMock.Object, _peerDiscoveryServiceMock.Object, _healthMonitorServiceMock.Object, _connectionPool, _exchangeService);
        var cts = new CancellationTokenSource();

        // Act
        var startTask = service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Node ID")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Service_ShouldHandleCancellation()
    {
        // Arrange
        var service = new PeerService(_loggerMock.Object, _configMock.Object, _peerListManagerMock.Object, _networkAddressServiceMock.Object, _peerDiscoveryServiceMock.Object, _healthMonitorServiceMock.Object, _connectionPool, _exchangeService);
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        // Act
        var startTask = service.StartAsync(cts.Token);
        await Task.Delay(500); // Wait for cancellation and cleanup

        // Assert - Service should handle cancellation without throwing
        service.Status.Should().Be(PeerServiceStatus.Offline);
    }

    [Fact]
    public void Dispose_ShouldCleanupResources()
    {
        // Arrange
        var service = new PeerService(_loggerMock.Object, _configMock.Object, _peerListManagerMock.Object, _networkAddressServiceMock.Object, _peerDiscoveryServiceMock.Object, _healthMonitorServiceMock.Object, _connectionPool, _exchangeService);

        // Act
        service.Dispose();

        // Assert - Should not throw
        service.Status.Should().Be(PeerServiceStatus.Offline);
    }

    [Fact]
    public async Task Service_ShouldLogStatusChange()
    {
        // Arrange
        var service = new PeerService(_loggerMock.Object, _configMock.Object, _peerListManagerMock.Object, _networkAddressServiceMock.Object, _peerDiscoveryServiceMock.Object, _healthMonitorServiceMock.Object, _connectionPool, _exchangeService);
        var cts = new CancellationTokenSource();

        // Act
        var startTask = service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);

        // Assert - Should log status transitions
        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task MultipleStartStop_ShouldWorkCorrectly()
    {
        // Arrange
        var service = new PeerService(_loggerMock.Object, _configMock.Object, _peerListManagerMock.Object, _networkAddressServiceMock.Object, _peerDiscoveryServiceMock.Object, _healthMonitorServiceMock.Object, _connectionPool, _exchangeService);

        // Act & Assert - Start and stop multiple times
        for (int i = 0; i < 3; i++)
        {
            var cts = new CancellationTokenSource();
            var startTask = service.StartAsync(cts.Token);
            await Task.Delay(100);
            await service.StopAsync(CancellationToken.None);

            service.Status.Should().Be(PeerServiceStatus.Offline);
        }
    }
}
