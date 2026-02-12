// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Discovery;
using Xunit;

namespace Sorcha.Peer.Service.Tests.Unit;

/// <summary>
/// Unit tests for PeerListManager local peer status tracking
/// </summary>
public class PeerListManagerTests : IDisposable
{
    private readonly PeerListManager _manager;
    private readonly Mock<ILogger<PeerListManager>> _loggerMock;
    private readonly string _testDbPath;

    public PeerListManagerTests()
    {
        _loggerMock = new Mock<ILogger<PeerListManager>>();

        // Use unique test database path
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_peers_{Guid.NewGuid()}.db");
        Directory.CreateDirectory(Path.GetDirectoryName(_testDbPath)!);

        var configuration = new PeerServiceConfiguration
        {
            PeerDiscovery = new PeerDiscoveryConfiguration
            {
                MaxPeersInList = 100,
                MinHealthyPeers = 5,
                RefreshIntervalMinutes = 15
            }
        };

        _manager = new PeerListManager(
            _loggerMock.Object,
            Options.Create(configuration));
    }

    [Fact]
    public void UpdateLocalPeerStatus_InitializesActivePeerInfo_WhenNull()
    {
        // Act
        _manager.UpdateLocalPeerStatus("peer-1", PeerConnectionStatus.Connected);

        // Assert
        var status = _manager.GetLocalPeerStatus();
        status.Should().NotBeNull();
        status!.PeerId.Should().NotBeNullOrEmpty();
        status.ConnectedPeerIds.Should().Contain("peer-1");
        status.Status.Should().Be(PeerConnectionStatus.Connected);
    }

    [Fact]
    public void UpdateLocalPeerStatus_UpdatesExistingStatus_WhenAlreadyInitialized()
    {
        // Arrange
        _manager.UpdateLocalPeerStatus("peer-1", PeerConnectionStatus.Connected);
        var initialStatus = _manager.GetLocalPeerStatus();

        // Act
        _manager.UpdateLocalPeerStatus("peer-2", PeerConnectionStatus.Connected);

        // Assert
        var updatedStatus = _manager.GetLocalPeerStatus();
        updatedStatus.Should().NotBeNull();
        updatedStatus!.ConnectedPeerIds.Should().Contain("peer-2");
        updatedStatus.Status.Should().Be(PeerConnectionStatus.Connected);
        updatedStatus.PeerId.Should().Be(initialStatus!.PeerId); // PeerId should remain same
    }

    [Fact]
    public void UpdateLocalPeerStatus_WithNullPeerId_SetsStatusToDisconnected()
    {
        // Arrange
        _manager.UpdateLocalPeerStatus("peer-1", PeerConnectionStatus.Connected);

        // Act
        _manager.UpdateLocalPeerStatus(null, PeerConnectionStatus.Disconnected);

        // Assert
        var status = _manager.GetLocalPeerStatus();
        status.Should().NotBeNull();
        status!.ConnectedPeerIds.Should().BeEmpty();
        status.Status.Should().Be(PeerConnectionStatus.Disconnected);
    }

    [Fact]
    public void UpdateLocalPeerStatus_UpdatesLastHeartbeatTimestamp()
    {
        // Arrange
        var beforeUpdate = DateTime.UtcNow;
        System.Threading.Thread.Sleep(10); // Small delay to ensure timestamp difference

        // Act
        _manager.UpdateLocalPeerStatus("peer-1", PeerConnectionStatus.Connected);

        // Assert
        var status = _manager.GetLocalPeerStatus();
        status.Should().NotBeNull();
        status!.LastHeartbeat.Should().BeAfter(beforeUpdate);
    }

    [Theory]
    [InlineData(PeerConnectionStatus.Disconnected)]
    [InlineData(PeerConnectionStatus.Connecting)]
    [InlineData(PeerConnectionStatus.Connected)]
    [InlineData(PeerConnectionStatus.HeartbeatTimeout)]
    [InlineData(PeerConnectionStatus.Isolated)]
    public void UpdateLocalPeerStatus_HandlesAllConnectionStatusTransitions(PeerConnectionStatus status)
    {
        // Act
        _manager.UpdateLocalPeerStatus("peer-1", status);

        // Assert
        var peerStatus = _manager.GetLocalPeerStatus();
        peerStatus.Should().NotBeNull();
        peerStatus!.Status.Should().Be(status);
    }

    [Fact]
    public void GetLocalPeerStatus_ReturnsNull_WhenNeverUpdated()
    {
        // Act
        var status = _manager.GetLocalPeerStatus();

        // Assert
        status.Should().BeNull();
    }

    [Fact]
    public void UpdateLocalPeerStatus_SequentialUpdates_MaintainsCorrectState()
    {
        // Act & Assert - Simulate connection lifecycle
        _manager.UpdateLocalPeerStatus(null, PeerConnectionStatus.Disconnected);
        _manager.GetLocalPeerStatus()!.Status.Should().Be(PeerConnectionStatus.Disconnected);

        _manager.UpdateLocalPeerStatus("peer-1", PeerConnectionStatus.Connecting);
        _manager.GetLocalPeerStatus()!.Status.Should().Be(PeerConnectionStatus.Connecting);

        _manager.UpdateLocalPeerStatus("peer-1", PeerConnectionStatus.Connected);
        _manager.GetLocalPeerStatus()!.Status.Should().Be(PeerConnectionStatus.Connected);

        _manager.UpdateLocalPeerStatus("peer-1", PeerConnectionStatus.HeartbeatTimeout);
        _manager.GetLocalPeerStatus()!.Status.Should().Be(PeerConnectionStatus.HeartbeatTimeout);

        _manager.UpdateLocalPeerStatus("peer-2", PeerConnectionStatus.Connected);
        var status = _manager.GetLocalPeerStatus();
        status!.Status.Should().Be(PeerConnectionStatus.Connected);
        status.ConnectedPeerIds.Should().Contain("peer-2");
    }

    [Fact]
    public void UpdateLocalPeerStatus_FailoverScenario_TracksCorrectly()
    {
        // Note: GetLocalPeerStatus() returns a mutable reference,
        // so we assert at each step before mutating further.

        // Step 1: Connect to peer-1
        _manager.UpdateLocalPeerStatus("peer-1", PeerConnectionStatus.Connected);
        var status = _manager.GetLocalPeerStatus();
        status!.ConnectedPeerIds.Should().Contain("peer-1");
        status.Status.Should().Be(PeerConnectionStatus.Connected);

        // Step 2: peer-1 heartbeat timeout
        _manager.UpdateLocalPeerStatus("peer-1", PeerConnectionStatus.HeartbeatTimeout);
        status = _manager.GetLocalPeerStatus();
        status!.Status.Should().Be(PeerConnectionStatus.HeartbeatTimeout);

        // Step 3: Failover to peer-2
        _manager.UpdateLocalPeerStatus("peer-2", PeerConnectionStatus.Connected);
        status = _manager.GetLocalPeerStatus();
        status!.ConnectedPeerIds.Should().Contain("peer-2");
        status.Status.Should().Be(PeerConnectionStatus.Connected);
    }

    [Fact]
    public void UpdateLocalPeerStatus_IsolatedMode_TracksCorrectly()
    {
        // Arrange
        _manager.UpdateLocalPeerStatus("peer-1", PeerConnectionStatus.Connected);

        // Act - All peers unreachable
        _manager.UpdateLocalPeerStatus(null, PeerConnectionStatus.Isolated);

        // Assert
        var status = _manager.GetLocalPeerStatus();
        status.Should().NotBeNull();
        status!.Status.Should().Be(PeerConnectionStatus.Isolated);
        status.ConnectedPeerIds.Should().BeEmpty();
    }

    public void Dispose()
    {
        _manager?.Dispose();

        // Clean up test database file
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
