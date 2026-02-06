// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Peer.Service.Core;

namespace Sorcha.Peer.Service.Tests;

public class HeartbeatValidatorTests
{
    [Fact]
    public void IsHeartbeatTimedOut_RecentHeartbeat_ReturnsFalse()
    {
        // Arrange
        var recentHeartbeat = DateTime.UtcNow;

        // Act
        var result = HeartbeatValidator.IsHeartbeatTimedOut(recentHeartbeat);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsHeartbeatTimedOut_HeartbeatMoreThan30SecondsAgo_ReturnsTrue()
    {
        // Arrange
        var oldHeartbeat = DateTime.UtcNow.AddSeconds(-31);

        // Act
        var result = HeartbeatValidator.IsHeartbeatTimedOut(oldHeartbeat);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsHeartbeatTimedOut_HeartbeatExactlyAt30Seconds_ReturnsFalse()
    {
        // Arrange - exactly at threshold (not greater than)
        var heartbeat = DateTime.UtcNow.AddSeconds(-29);

        // Act
        var result = HeartbeatValidator.IsHeartbeatTimedOut(heartbeat);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ShouldFailover_BelowThreshold_ReturnsFalse(int missedHeartbeats)
    {
        // Act
        var result = HeartbeatValidator.ShouldFailover(missedHeartbeats);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public void ShouldFailover_AtOrAboveThreshold_ReturnsTrue(int missedHeartbeats)
    {
        // Act
        var result = HeartbeatValidator.ShouldFailover(missedHeartbeats);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CalculateNextHeartbeatTime_ReturnsApproximately30SecondsFromNow()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(HeartbeatValidator.HeartbeatIntervalSeconds);

        // Act
        var nextHeartbeat = HeartbeatValidator.CalculateNextHeartbeatTime();

        // Assert
        nextHeartbeat.Should().BeCloseTo(before, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void HeartbeatIntervalSeconds_MatchesPeerServiceConstants()
    {
        // Assert
        HeartbeatValidator.HeartbeatIntervalSeconds.Should().Be(PeerServiceConstants.HeartbeatIntervalSeconds);
        HeartbeatValidator.HeartbeatIntervalSeconds.Should().Be(30);
    }

    [Fact]
    public void HeartbeatTimeoutSeconds_MatchesPeerServiceConstants()
    {
        // Assert
        HeartbeatValidator.HeartbeatTimeoutSeconds.Should().Be(PeerServiceConstants.HeartbeatTimeoutSeconds);
        HeartbeatValidator.HeartbeatTimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void MaxMissedHeartbeats_MatchesPeerServiceConstants()
    {
        // Assert
        HeartbeatValidator.MaxMissedHeartbeats.Should().Be(PeerServiceConstants.MaxMissedHeartbeats);
        HeartbeatValidator.MaxMissedHeartbeats.Should().Be(2);
    }
}
