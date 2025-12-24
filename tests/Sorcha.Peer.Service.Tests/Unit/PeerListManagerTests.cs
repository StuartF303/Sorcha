// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

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
        _manager.UpdateLocalPeerStatus("n0.sorcha.dev", PeerConnectionStatus.Connected);

        // Assert
        var status = _manager.GetLocalPeerStatus();
        status.Should().NotBeNull();
        status!.PeerId.Should().NotBeNullOrEmpty();
        status.ConnectedHubNodeId.Should().Be("n0.sorcha.dev");
        status.Status.Should().Be(PeerConnectionStatus.Connected);
    }

    [Fact]
    public void UpdateLocalPeerStatus_UpdatesExistingStatus_WhenAlreadyInitialized()
    {
        // Arrange
        _manager.UpdateLocalPeerStatus("n0.sorcha.dev", PeerConnectionStatus.Connected);
        var initialStatus = _manager.GetLocalPeerStatus();

        // Act
        _manager.UpdateLocalPeerStatus("n1.sorcha.dev", PeerConnectionStatus.Connected);

        // Assert
        var updatedStatus = _manager.GetLocalPeerStatus();
        updatedStatus.Should().NotBeNull();
        updatedStatus!.ConnectedHubNodeId.Should().Be("n1.sorcha.dev");
        updatedStatus.Status.Should().Be(PeerConnectionStatus.Connected);
        updatedStatus.PeerId.Should().Be(initialStatus!.PeerId); // PeerId should remain same
    }

    [Fact]
    public void UpdateLocalPeerStatus_WithNullHubNodeId_SetsStatusToDisconnected()
    {
        // Arrange
        _manager.UpdateLocalPeerStatus("n0.sorcha.dev", PeerConnectionStatus.Connected);

        // Act
        _manager.UpdateLocalPeerStatus(null, PeerConnectionStatus.Disconnected);

        // Assert
        var status = _manager.GetLocalPeerStatus();
        status.Should().NotBeNull();
        status!.ConnectedHubNodeId.Should().BeNull();
        status.Status.Should().Be(PeerConnectionStatus.Disconnected);
    }

    [Fact]
    public void UpdateLocalPeerStatus_UpdatesLastHeartbeatTimestamp()
    {
        // Arrange
        var beforeUpdate = DateTime.UtcNow;
        System.Threading.Thread.Sleep(10); // Small delay to ensure timestamp difference

        // Act
        _manager.UpdateLocalPeerStatus("n0.sorcha.dev", PeerConnectionStatus.Connected);

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
        _manager.UpdateLocalPeerStatus("n0.sorcha.dev", status);

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

        _manager.UpdateLocalPeerStatus("n0.sorcha.dev", PeerConnectionStatus.Connecting);
        _manager.GetLocalPeerStatus()!.Status.Should().Be(PeerConnectionStatus.Connecting);

        _manager.UpdateLocalPeerStatus("n0.sorcha.dev", PeerConnectionStatus.Connected);
        _manager.GetLocalPeerStatus()!.Status.Should().Be(PeerConnectionStatus.Connected);

        _manager.UpdateLocalPeerStatus("n0.sorcha.dev", PeerConnectionStatus.HeartbeatTimeout);
        _manager.GetLocalPeerStatus()!.Status.Should().Be(PeerConnectionStatus.HeartbeatTimeout);

        _manager.UpdateLocalPeerStatus("n1.sorcha.dev", PeerConnectionStatus.Connected);
        var status = _manager.GetLocalPeerStatus();
        status!.Status.Should().Be(PeerConnectionStatus.Connected);
        status.ConnectedHubNodeId.Should().Be("n1.sorcha.dev");
    }

    [Fact]
    public void UpdateLocalPeerStatus_FailoverScenario_TracksCorrectly()
    {
        // Arrange & Act - Simulate failover from n0 to n1 to n2
        _manager.UpdateLocalPeerStatus("n0.sorcha.dev", PeerConnectionStatus.Connected);
        var n0Status = _manager.GetLocalPeerStatus();

        _manager.UpdateLocalPeerStatus("n0.sorcha.dev", PeerConnectionStatus.HeartbeatTimeout);
        var timeoutStatus = _manager.GetLocalPeerStatus();

        _manager.UpdateLocalPeerStatus("n1.sorcha.dev", PeerConnectionStatus.Connected);
        var n1Status = _manager.GetLocalPeerStatus();

        // Assert
        n0Status!.ConnectedHubNodeId.Should().Be("n0.sorcha.dev");
        n0Status.Status.Should().Be(PeerConnectionStatus.Connected);

        timeoutStatus!.Status.Should().Be(PeerConnectionStatus.HeartbeatTimeout);

        n1Status!.ConnectedHubNodeId.Should().Be("n1.sorcha.dev");
        n1Status.Status.Should().Be(PeerConnectionStatus.Connected);
    }

    [Fact]
    public void UpdateLocalPeerStatus_IsolatedMode_TracksCorrectly()
    {
        // Arrange
        _manager.UpdateLocalPeerStatus("n0.sorcha.dev", PeerConnectionStatus.Connected);

        // Act - All hub nodes unreachable
        _manager.UpdateLocalPeerStatus(null, PeerConnectionStatus.Isolated);

        // Assert
        var status = _manager.GetLocalPeerStatus();
        status.Should().NotBeNull();
        status!.Status.Should().Be(PeerConnectionStatus.Isolated);
        status.ConnectedHubNodeId.Should().BeNull();
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
