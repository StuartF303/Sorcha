// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Peer.Service.Core;

namespace Sorcha.Peer.Service.Tests;

public class TransactionNotificationTests
{
    [Fact]
    public void DefaultTTL_Is3600()
    {
        // Arrange & Act
        var notification = new TransactionNotification();

        // Assert
        notification.TTL.Should().Be(3600);
    }

    [Fact]
    public void DefaultGossipRound_IsZero()
    {
        // Arrange & Act
        var notification = new TransactionNotification();

        // Assert
        notification.GossipRound.Should().Be(0);
    }

    [Fact]
    public void DefaultHopCount_IsZero()
    {
        // Arrange & Act
        var notification = new TransactionNotification();

        // Assert
        notification.HopCount.Should().Be(0);
    }

    [Fact]
    public void DefaultHasFullData_IsFalse()
    {
        // Arrange & Act
        var notification = new TransactionNotification();

        // Assert
        notification.HasFullData.Should().BeFalse();
    }

    [Fact]
    public void DefaultTransactionData_IsNull()
    {
        // Arrange & Act
        var notification = new TransactionNotification();

        // Assert
        notification.TransactionData.Should().BeNull();
    }

    [Fact]
    public void DefaultTimestamp_IsApproximatelyNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var notification = new TransactionNotification();

        // Assert
        notification.Timestamp.Should().BeCloseTo(before, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void DefaultTransactionId_IsEmpty()
    {
        // Arrange & Act
        var notification = new TransactionNotification();

        // Assert
        notification.TransactionId.Should().BeEmpty();
    }

    [Fact]
    public void DefaultOriginPeerId_IsEmpty()
    {
        // Arrange & Act
        var notification = new TransactionNotification();

        // Assert
        notification.OriginPeerId.Should().BeEmpty();
    }

    [Fact]
    public void DefaultDataHash_IsEmpty()
    {
        // Arrange & Act
        var notification = new TransactionNotification();

        // Assert
        notification.DataHash.Should().BeEmpty();
    }

    [Fact]
    public void DefaultDataSize_IsZero()
    {
        // Arrange & Act
        var notification = new TransactionNotification();

        // Assert
        notification.DataSize.Should().Be(0);
    }

    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3 };

        // Act
        var notification = new TransactionNotification
        {
            TransactionId = "tx-123",
            OriginPeerId = "peer-1",
            DataSize = 1024,
            DataHash = "abc123",
            GossipRound = 2,
            HopCount = 3,
            TTL = 1800,
            HasFullData = true,
            TransactionData = data
        };

        // Assert
        notification.TransactionId.Should().Be("tx-123");
        notification.OriginPeerId.Should().Be("peer-1");
        notification.DataSize.Should().Be(1024);
        notification.DataHash.Should().Be("abc123");
        notification.GossipRound.Should().Be(2);
        notification.HopCount.Should().Be(3);
        notification.TTL.Should().Be(1800);
        notification.HasFullData.Should().BeTrue();
        notification.TransactionData.Should().BeEquivalentTo(data);
    }
}
