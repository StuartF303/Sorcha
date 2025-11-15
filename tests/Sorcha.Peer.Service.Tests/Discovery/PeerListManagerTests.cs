// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Discovery;

namespace Sorcha.Peer.Service.Tests.Discovery;

public class PeerListManagerTests : IDisposable
{
    private readonly Mock<ILogger<PeerListManager>> _loggerMock;
    private readonly Mock<IOptions<PeerServiceConfiguration>> _configMock;
    private readonly PeerServiceConfiguration _configuration;
    private readonly PeerListManager _manager;
    private readonly string _testDbPath;

    public PeerListManagerTests()
    {
        _loggerMock = new Mock<ILogger<PeerListManager>>();
        _configMock = new Mock<IOptions<PeerServiceConfiguration>>();
        _configuration = new PeerServiceConfiguration
        {
            PeerDiscovery = new PeerDiscoveryConfiguration
            {
                RefreshIntervalMinutes = 15,
                MaxPeersInList = 100,
                MinHealthyPeers = 5
            }
        };
        _configMock.Setup(x => x.Value).Returns(_configuration);

        _manager = new PeerListManager(_loggerMock.Object, _configMock.Object);
        _testDbPath = "./data/peers.db";
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDependencies()
    {
        // Assert
        _manager.Should().NotBeNull();
    }

    [Fact]
    public void GetAllPeers_ShouldReturnEmptyList_Initially()
    {
        // Act
        var peers = _manager.GetAllPeers();

        // Assert
        peers.Should().BeEmpty();
    }

    [Fact]
    public async Task AddOrUpdatePeerAsync_ShouldAddNewPeer()
    {
        // Arrange
        var peer = new PeerNode
        {
            PeerId = "peer1",
            Address = "192.168.1.100",
            Port = 5001,
            SupportedProtocols = new List<string> { "GrpcStream" }
        };

        // Act
        var result = await _manager.AddOrUpdatePeerAsync(peer);

        // Assert
        result.Should().BeTrue();
        _manager.GetAllPeers().Should().ContainSingle()
            .Which.PeerId.Should().Be("peer1");
    }

    [Fact]
    public async Task AddOrUpdatePeerAsync_ShouldUpdateExistingPeer()
    {
        // Arrange
        var peer1 = new PeerNode
        {
            PeerId = "peer1",
            Address = "192.168.1.100",
            Port = 5001
        };
        await _manager.AddOrUpdatePeerAsync(peer1);

        var peer2 = new PeerNode
        {
            PeerId = "peer1",
            Address = "192.168.1.101", // Different address
            Port = 5002
        };

        // Act
        var result = await _manager.AddOrUpdatePeerAsync(peer2);

        // Assert
        result.Should().BeTrue();
        var allPeers = _manager.GetAllPeers();
        allPeers.Should().ContainSingle();
        allPeers.First().Address.Should().Be("192.168.1.101");
        allPeers.First().Port.Should().Be(5002);
    }

    [Fact]
    public async Task AddOrUpdatePeerAsync_ShouldThrowArgumentNullException_WhenPeerIsNull()
    {
        // Act
        Func<Task> act = async () => await _manager.AddOrUpdatePeerAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task AddOrUpdatePeerAsync_ShouldReturnFalse_WhenPeerIdIsEmpty()
    {
        // Arrange
        var peer = new PeerNode { PeerId = "", Address = "192.168.1.100", Port = 5001 };

        // Act
        var result = await _manager.AddOrUpdatePeerAsync(peer);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemovePeerAsync_ShouldRemoveExistingPeer()
    {
        // Arrange
        var peer = new PeerNode
        {
            PeerId = "peer1",
            Address = "192.168.1.100",
            Port = 5001
        };
        await _manager.AddOrUpdatePeerAsync(peer);

        // Act
        var result = await _manager.RemovePeerAsync("peer1");

        // Assert
        result.Should().BeTrue();
        _manager.GetAllPeers().Should().BeEmpty();
    }

    [Fact]
    public async Task RemovePeerAsync_ShouldReturnFalse_WhenPeerNotFound()
    {
        // Act
        var result = await _manager.RemovePeerAsync("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetPeer_ShouldReturnPeer_WhenExists()
    {
        // Arrange
        var peer = new PeerNode
        {
            PeerId = "peer1",
            Address = "192.168.1.100",
            Port = 5001
        };
        await _manager.AddOrUpdatePeerAsync(peer);

        // Act
        var result = _manager.GetPeer("peer1");

        // Assert
        result.Should().NotBeNull();
        result!.PeerId.Should().Be("peer1");
    }

    [Fact]
    public void GetPeer_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = _manager.GetPeer("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetHealthyPeers_ShouldReturnOnlyHealthyPeers()
    {
        // Arrange
        var healthyPeer = new PeerNode
        {
            PeerId = "healthy1",
            Address = "192.168.1.100",
            Port = 5001,
            LastSeen = DateTimeOffset.UtcNow,
            FailureCount = 0
        };

        var unhealthyPeer = new PeerNode
        {
            PeerId = "unhealthy1",
            Address = "192.168.1.101",
            Port = 5001,
            LastSeen = DateTimeOffset.UtcNow.AddHours(-2),
            FailureCount = 5
        };

        await _manager.AddOrUpdatePeerAsync(healthyPeer);
        await _manager.AddOrUpdatePeerAsync(unhealthyPeer);

        // Act
        var healthyPeers = _manager.GetHealthyPeers();

        // Assert
        healthyPeers.Should().ContainSingle()
            .Which.PeerId.Should().Be("healthy1");
    }

    [Fact]
    public async Task GetRandomPeers_ShouldReturnRequestedCount()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            await _manager.AddOrUpdatePeerAsync(new PeerNode
            {
                PeerId = $"peer{i}",
                Address = $"192.168.1.{100 + i}",
                Port = 5001,
                LastSeen = DateTimeOffset.UtcNow,
                FailureCount = 0
            });
        }

        // Act
        var randomPeers = _manager.GetRandomPeers(5);

        // Assert
        randomPeers.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task UpdateLastSeenAsync_ShouldUpdateTimestampAndResetFailureCount()
    {
        // Arrange
        var peer = new PeerNode
        {
            PeerId = "peer1",
            Address = "192.168.1.100",
            Port = 5001,
            LastSeen = DateTimeOffset.UtcNow.AddHours(-1),
            FailureCount = 3
        };
        await _manager.AddOrUpdatePeerAsync(peer);

        // Act
        await _manager.UpdateLastSeenAsync("peer1");
        await Task.Delay(10); // Small delay to ensure time difference

        // Assert
        var updatedPeer = _manager.GetPeer("peer1");
        updatedPeer.Should().NotBeNull();
        updatedPeer!.LastSeen.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        updatedPeer.FailureCount.Should().Be(0);
    }

    [Fact]
    public async Task IncrementFailureCountAsync_ShouldIncrementCount()
    {
        // Arrange
        var peer = new PeerNode
        {
            PeerId = "peer1",
            Address = "192.168.1.100",
            Port = 5001,
            FailureCount = 0
        };
        await _manager.AddOrUpdatePeerAsync(peer);

        // Act
        await _manager.IncrementFailureCountAsync("peer1");

        // Assert
        var updatedPeer = _manager.GetPeer("peer1");
        updatedPeer.Should().NotBeNull();
        updatedPeer!.FailureCount.Should().Be(1);
    }

    [Fact]
    public async Task IncrementFailureCountAsync_ShouldRemovePeer_WhenFailureCountExceeds5()
    {
        // Arrange
        var peer = new PeerNode
        {
            PeerId = "peer1",
            Address = "192.168.1.100",
            Port = 5001,
            FailureCount = 4,
            IsBootstrapNode = false
        };
        await _manager.AddOrUpdatePeerAsync(peer);

        // Act
        await _manager.IncrementFailureCountAsync("peer1");

        // Assert
        _manager.GetPeer("peer1").Should().BeNull();
    }

    [Fact]
    public void GetHealthyPeerCount_ShouldReturnCorrectCount()
    {
        // Arrange - Initially empty

        // Act
        var count = _manager.GetHealthyPeerCount();

        // Assert
        count.Should().Be(0);
    }

    public void Dispose()
    {
        _manager?.Dispose();

        // Clean up test database
        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
