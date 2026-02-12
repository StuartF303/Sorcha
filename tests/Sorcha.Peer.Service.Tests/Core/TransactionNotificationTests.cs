// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Peer.Service.Core;

namespace Sorcha.Peer.Service.Tests.Core;

public class TransactionNotificationTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var notification = new TransactionNotification();

        // Assert
        notification.TransactionId.Should().BeEmpty();
        notification.OriginPeerId.Should().BeEmpty();
        notification.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        notification.DataSize.Should().Be(0);
        notification.DataHash.Should().BeEmpty();
        notification.GossipRound.Should().Be(0);
        notification.HopCount.Should().Be(0);
        notification.TTL.Should().Be(3600);
        notification.HasFullData.Should().BeFalse();
        notification.TransactionData.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldAllowPropertyInitialization()
    {
        // Arrange
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        var timestamp = DateTimeOffset.UtcNow.AddMinutes(-5);

        // Act
        var notification = new TransactionNotification
        {
            TransactionId = "tx123",
            OriginPeerId = "peer456",
            Timestamp = timestamp,
            DataSize = 1024,
            DataHash = "abc123def456",
            GossipRound = 2,
            HopCount = 3,
            TTL = 7200,
            HasFullData = true,
            TransactionData = testData
        };

        // Assert
        notification.TransactionId.Should().Be("tx123");
        notification.OriginPeerId.Should().Be("peer456");
        notification.Timestamp.Should().Be(timestamp);
        notification.DataSize.Should().Be(1024);
        notification.DataHash.Should().Be("abc123def456");
        notification.GossipRound.Should().Be(2);
        notification.HopCount.Should().Be(3);
        notification.TTL.Should().Be(7200);
        notification.HasFullData.Should().BeTrue();
        notification.TransactionData.Should().BeEquivalentTo(testData);
    }

    [Fact]
    public void GossipRound_ShouldBeIncrementable()
    {
        // Arrange
        var notification = new TransactionNotification
        {
            TransactionId = "tx123",
            GossipRound = 0
        };

        // Act
        notification.GossipRound++;
        notification.GossipRound++;

        // Assert
        notification.GossipRound.Should().Be(2);
    }

    [Fact]
    public void HopCount_ShouldBeIncrementable()
    {
        // Arrange
        var notification = new TransactionNotification
        {
            TransactionId = "tx123",
            HopCount = 0
        };

        // Act
        notification.HopCount++;
        notification.HopCount++;
        notification.HopCount++;

        // Assert
        notification.HopCount.Should().Be(3);
    }

    [Fact]
    public void TTL_ShouldBeDecrementable()
    {
        // Arrange
        var notification = new TransactionNotification
        {
            TransactionId = "tx123",
            TTL = 3600
        };

        // Act
        notification.TTL -= 60;

        // Assert
        notification.TTL.Should().Be(3540);
    }

    [Theory]
    [InlineData(300)]
    [InlineData(3600)]
    [InlineData(86400)]
    public void TTL_ShouldAcceptValidValues(int ttl)
    {
        // Arrange & Act
        var notification = new TransactionNotification { TTL = ttl };

        // Assert
        notification.TTL.Should().Be(ttl);
    }

    [Fact]
    public void TransactionData_ShouldHandleLargeData()
    {
        // Arrange
        var largeData = new byte[1024 * 1024]; // 1 MB
        for (int i = 0; i < largeData.Length; i++)
        {
            largeData[i] = (byte)(i % 256);
        }

        // Act
        var notification = new TransactionNotification
        {
            TransactionId = "tx_large",
            TransactionData = largeData,
            DataSize = largeData.Length,
            HasFullData = true
        };

        // Assert
        notification.TransactionData.Should().NotBeNull();
        notification.TransactionData!.Length.Should().Be(1024 * 1024);
        notification.DataSize.Should().Be(1024 * 1024);
        notification.HasFullData.Should().BeTrue();
    }

    [Fact]
    public void HasFullData_ShouldBeFalse_WhenTransactionDataIsNull()
    {
        // Arrange
        var notification = new TransactionNotification
        {
            TransactionId = "tx123",
            DataHash = "abc123",
            DataSize = 1024,
            HasFullData = false,
            TransactionData = null
        };

        // Assert
        notification.HasFullData.Should().BeFalse();
        notification.TransactionData.Should().BeNull();
    }

    [Fact]
    public void HasFullData_ShouldBeTrue_WhenTransactionDataIsProvided()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3 };
        var notification = new TransactionNotification
        {
            TransactionId = "tx123",
            TransactionData = data,
            HasFullData = true
        };

        // Assert
        notification.HasFullData.Should().BeTrue();
        notification.TransactionData.Should().NotBeNull();
    }

    [Fact]
    public void Timestamp_ShouldBeSettable()
    {
        // Arrange
        var pastTime = DateTimeOffset.UtcNow.AddHours(-2);
        var notification = new TransactionNotification();

        // Act
        notification.Timestamp = pastTime;

        // Assert
        notification.Timestamp.Should().Be(pastTime);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1024)]
    [InlineData(1024 * 1024)]
    [InlineData(10 * 1024 * 1024)]
    public void DataSize_ShouldAcceptVariousSizes(int size)
    {
        // Arrange & Act
        var notification = new TransactionNotification { DataSize = size };

        // Assert
        notification.DataSize.Should().Be(size);
    }
}
