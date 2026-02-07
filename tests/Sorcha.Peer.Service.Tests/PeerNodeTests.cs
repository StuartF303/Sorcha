// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Peer.Service.Core;

namespace Sorcha.Peer.Service.Tests;

public class PeerNodeTests
{
    [Fact]
    public void Equals_SamePeerId_ReturnsTrue()
    {
        // Arrange
        var peer1 = new PeerNode { PeerId = "peer-1", Address = "10.0.0.1", Port = 5001 };
        var peer2 = new PeerNode { PeerId = "peer-1", Address = "10.0.0.2", Port = 5002 };

        // Act & Assert
        peer1.Equals(peer2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentPeerId_ReturnsFalse()
    {
        // Arrange
        var peer1 = new PeerNode { PeerId = "peer-1", Address = "10.0.0.1", Port = 5001 };
        var peer2 = new PeerNode { PeerId = "peer-2", Address = "10.0.0.1", Port = 5001 };

        // Act & Assert
        peer1.Equals(peer2).Should().BeFalse();
    }

    [Fact]
    public void Equals_NullPeerNode_ReturnsFalse()
    {
        // Arrange
        var peer = new PeerNode { PeerId = "peer-1" };

        // Act & Assert
        peer.Equals((PeerNode?)null).Should().BeFalse();
    }

    [Fact]
    public void Equals_NullObject_ReturnsFalse()
    {
        // Arrange
        var peer = new PeerNode { PeerId = "peer-1" };

        // Act & Assert
        peer.Equals((object?)null).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        // Arrange
        var peer = new PeerNode { PeerId = "peer-1" };

        // Act & Assert
        peer.Equals("not-a-peer").Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SamePeerId_ReturnsSameHash()
    {
        // Arrange
        var peer1 = new PeerNode { PeerId = "peer-1", Address = "10.0.0.1" };
        var peer2 = new PeerNode { PeerId = "peer-1", Address = "10.0.0.2" };

        // Act & Assert
        peer1.GetHashCode().Should().Be(peer2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentPeerId_ReturnsDifferentHash()
    {
        // Arrange
        var peer1 = new PeerNode { PeerId = "peer-1" };
        var peer2 = new PeerNode { PeerId = "peer-2" };

        // Act & Assert
        peer1.GetHashCode().Should().NotBe(peer2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var peer = new PeerNode { PeerId = "peer-1", Address = "10.0.0.1", Port = 5001 };

        // Act
        var result = peer.ToString();

        // Assert
        result.Should().Be("Peer peer-1 at 10.0.0.1:5001");
    }

    [Fact]
    public void DefaultCapabilities_SupportsStreaming()
    {
        // Arrange & Act
        var peer = new PeerNode();

        // Assert
        peer.Capabilities.SupportsStreaming.Should().BeTrue();
    }

    [Fact]
    public void DefaultCapabilities_SupportsTransactionDistribution()
    {
        // Arrange & Act
        var peer = new PeerNode();

        // Assert
        peer.Capabilities.SupportsTransactionDistribution.Should().BeTrue();
    }

    [Fact]
    public void DefaultCapabilities_MaxTransactionSize_Is10MB()
    {
        // Arrange & Act
        var peer = new PeerNode();

        // Assert
        peer.Capabilities.MaxTransactionSize.Should().Be(10 * 1024 * 1024);
    }

    [Fact]
    public void DefaultFailureCount_IsZero()
    {
        // Arrange & Act
        var peer = new PeerNode();

        // Assert
        peer.FailureCount.Should().Be(0);
    }

    [Fact]
    public void DefaultIsSeedNode_IsFalse()
    {
        // Arrange & Act
        var peer = new PeerNode();

        // Assert
        peer.IsSeedNode.Should().BeFalse();
    }

    [Fact]
    public void DefaultAverageLatencyMs_IsZero()
    {
        // Arrange & Act
        var peer = new PeerNode();

        // Assert
        peer.AverageLatencyMs.Should().Be(0);
    }

    [Fact]
    public void FirstSeen_DefaultsToApproximatelyNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var peer = new PeerNode();

        // Assert
        peer.FirstSeen.Should().BeCloseTo(before, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void LastSeen_DefaultsToApproximatelyNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var peer = new PeerNode();

        // Assert
        peer.LastSeen.Should().BeCloseTo(before, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void CanBeUsedInHashSet()
    {
        // Arrange
        var set = new HashSet<PeerNode>();
        var peer1 = new PeerNode { PeerId = "peer-1", Address = "10.0.0.1", Port = 5001 };
        var peer2 = new PeerNode { PeerId = "peer-1", Address = "10.0.0.2", Port = 5002 };
        var peer3 = new PeerNode { PeerId = "peer-2", Address = "10.0.0.3", Port = 5003 };

        // Act
        set.Add(peer1);
        set.Add(peer2); // Same PeerId as peer1
        set.Add(peer3);

        // Assert
        set.Should().HaveCount(2);
    }
}
