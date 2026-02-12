// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Peer.Service.Core;

namespace Sorcha.Peer.Service.Tests;

public class SyncValidatorTests
{
    [Fact]
    public void CalculateNextSyncTime_Adds5Minutes()
    {
        // Arrange
        var lastSync = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var nextSync = SyncValidator.CalculateNextSyncTime(lastSync);

        // Assert
        nextSync.Should().Be(new DateTime(2026, 1, 15, 12, 5, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void IsSyncDue_PastTime_ReturnsTrue()
    {
        // Arrange
        var pastTime = DateTime.UtcNow.AddMinutes(-1);

        // Act
        var result = SyncValidator.IsSyncDue(pastTime);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSyncDue_FutureTime_ReturnsFalse()
    {
        // Arrange
        var futureTime = DateTime.UtcNow.AddMinutes(5);

        // Act
        var result = SyncValidator.IsSyncDue(futureTime);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TimeUntilNextSync_FutureTime_ReturnsPositiveTimeSpan()
    {
        // Arrange
        var futureTime = DateTime.UtcNow.AddMinutes(3);

        // Act
        var result = SyncValidator.TimeUntilNextSync(futureTime);

        // Assert
        result.Should().BeGreaterThan(TimeSpan.Zero);
        result.Should().BeCloseTo(TimeSpan.FromMinutes(3), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void TimeUntilNextSync_PastTime_ReturnsZero()
    {
        // Arrange
        var pastTime = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var result = SyncValidator.TimeUntilNextSync(pastTime);

        // Assert
        result.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void PeriodicSyncIntervalMinutes_IsFive()
    {
        // Assert
        SyncValidator.PeriodicSyncIntervalMinutes.Should().Be(5);
        SyncValidator.PeriodicSyncIntervalMinutes.Should().Be(PeerServiceConstants.PeriodicSyncIntervalMinutes);
    }

    [Fact]
    public void PeriodicSyncIntervalSeconds_Is300()
    {
        // Assert
        SyncValidator.PeriodicSyncIntervalSeconds.Should().Be(300);
    }
}
