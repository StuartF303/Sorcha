// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Peer.Service.Core;

namespace Sorcha.Peer.Service.Tests.Core;

public class PeerNodeTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var peerNode = new PeerNode();

        // Assert
        peerNode.PeerId.Should().BeEmpty();
        peerNode.Address.Should().BeEmpty();
        peerNode.Port.Should().Be(0);
        peerNode.SupportedProtocols.Should().NotBeNull().And.BeEmpty();
        peerNode.FirstSeen.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        peerNode.LastSeen.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        peerNode.FailureCount.Should().Be(0);
        peerNode.IsBootstrapNode.Should().BeFalse();
        peerNode.Capabilities.Should().NotBeNull();
        peerNode.AverageLatencyMs.Should().Be(0);
    }

    [Fact]
    public void Constructor_ShouldAllowPropertyInitialization()
    {
        // Arrange & Act
        var peerNode = new PeerNode
        {
            PeerId = "peer123",
            Address = "192.168.1.100",
            Port = 5001,
            SupportedProtocols = new List<string> { "GrpcStream", "Rest" },
            IsBootstrapNode = true,
            AverageLatencyMs = 50
        };

        // Assert
        peerNode.PeerId.Should().Be("peer123");
        peerNode.Address.Should().Be("192.168.1.100");
        peerNode.Port.Should().Be(5001);
        peerNode.SupportedProtocols.Should().HaveCount(2).And.Contain(new[] { "GrpcStream", "Rest" });
        peerNode.IsBootstrapNode.Should().BeTrue();
        peerNode.AverageLatencyMs.Should().Be(50);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenPeerIdsMatch()
    {
        // Arrange
        var peer1 = new PeerNode { PeerId = "peer123", Address = "192.168.1.100", Port = 5001 };
        var peer2 = new PeerNode { PeerId = "peer123", Address = "192.168.1.101", Port = 5002 };

        // Act
        var result = peer1.Equals(peer2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenPeerIdsDiffer()
    {
        // Arrange
        var peer1 = new PeerNode { PeerId = "peer123", Address = "192.168.1.100", Port = 5001 };
        var peer2 = new PeerNode { PeerId = "peer456", Address = "192.168.1.100", Port = 5001 };

        // Act
        var result = peer1.Equals(peer2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparingWithNull()
    {
        // Arrange
        var peer = new PeerNode { PeerId = "peer123" };

        // Act
        var result = peer.Equals(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ShouldReturnSameValue_ForSamePeerId()
    {
        // Arrange
        var peer1 = new PeerNode { PeerId = "peer123", Address = "192.168.1.100" };
        var peer2 = new PeerNode { PeerId = "peer123", Address = "192.168.1.101" };

        // Act
        var hash1 = peer1.GetHashCode();
        var hash2 = peer2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void GetHashCode_ShouldReturnDifferentValues_ForDifferentPeerIds()
    {
        // Arrange
        var peer1 = new PeerNode { PeerId = "peer123" };
        var peer2 = new PeerNode { PeerId = "peer456" };

        // Act
        var hash1 = peer1.GetHashCode();
        var hash2 = peer2.GetHashCode();

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var peer = new PeerNode
        {
            PeerId = "peer123",
            Address = "192.168.1.100",
            Port = 5001
        };

        // Act
        var result = peer.ToString();

        // Assert
        result.Should().Be("Peer peer123 at 192.168.1.100:5001");
    }

    [Fact]
    public void FailureCount_ShouldBeIncrementable()
    {
        // Arrange
        var peer = new PeerNode { PeerId = "peer123" };

        // Act
        peer.FailureCount++;
        peer.FailureCount++;

        // Assert
        peer.FailureCount.Should().Be(2);
    }

    [Fact]
    public void LastSeen_ShouldBeUpdatable()
    {
        // Arrange
        var peer = new PeerNode { PeerId = "peer123" };
        var originalLastSeen = peer.LastSeen;

        // Act
        System.Threading.Thread.Sleep(10); // Small delay to ensure time difference
        peer.LastSeen = DateTimeOffset.UtcNow;

        // Assert
        peer.LastSeen.Should().BeAfter(originalLastSeen);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(65535)]
    public void Port_ShouldAcceptValidPortNumbers(int port)
    {
        // Arrange & Act
        var peer = new PeerNode { PeerId = "peer123", Port = port };

        // Assert
        peer.Port.Should().Be(port);
    }
}
