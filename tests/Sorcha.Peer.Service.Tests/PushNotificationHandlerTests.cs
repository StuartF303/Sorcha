// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.Peer.Service.Protos;
using Sorcha.Peer.Service.Replication;

namespace Sorcha.Peer.Service.Tests;

public class PushNotificationHandlerTests
{
    private readonly Mock<ILogger<PushNotificationHandler>> _loggerMock = new();

    private PushNotificationHandler CreateHandler()
    {
        return new PushNotificationHandler(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new PushNotificationHandler(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #region RegisterSubscriber

    [Fact]
    public void RegisterSubscriber_NewSubscriber_ReturnsTrue()
    {
        // Arrange
        var handler = CreateHandler();
        var streamMock = new Mock<IServerStreamWriter<BlueprintNotification>>();

        // Act
        var result = handler.RegisterSubscriber("peer-1", "session-1", streamMock.Object);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RegisterSubscriber_DuplicateKey_ReturnsFalse()
    {
        // Arrange
        var handler = CreateHandler();
        var streamMock = new Mock<IServerStreamWriter<BlueprintNotification>>();
        handler.RegisterSubscriber("peer-1", "session-1", streamMock.Object);

        // Act
        var result = handler.RegisterSubscriber("peer-1", "session-1", streamMock.Object);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RegisterSubscriber_DifferentSessions_BothSucceed()
    {
        // Arrange
        var handler = CreateHandler();
        var streamMock = new Mock<IServerStreamWriter<BlueprintNotification>>();

        // Act
        var result1 = handler.RegisterSubscriber("peer-1", "session-1", streamMock.Object);
        var result2 = handler.RegisterSubscriber("peer-1", "session-2", streamMock.Object);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterSubscriber_NullOrEmptyPeerId_ThrowsArgumentException(string? peerId)
    {
        // Arrange
        var handler = CreateHandler();
        var streamMock = new Mock<IServerStreamWriter<BlueprintNotification>>();

        // Act
        var act = () => handler.RegisterSubscriber(peerId!, "session-1", streamMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterSubscriber_NullOrEmptySessionId_ThrowsArgumentException(string? sessionId)
    {
        // Arrange
        var handler = CreateHandler();
        var streamMock = new Mock<IServerStreamWriter<BlueprintNotification>>();

        // Act
        var act = () => handler.RegisterSubscriber("peer-1", sessionId!, streamMock.Object);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RegisterSubscriber_NullStream_ThrowsArgumentNullException()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var act = () => handler.RegisterSubscriber("peer-1", "session-1", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region UnregisterSubscriber

    [Fact]
    public void UnregisterSubscriber_ExistingSubscriber_ReturnsTrue()
    {
        // Arrange
        var handler = CreateHandler();
        var streamMock = new Mock<IServerStreamWriter<BlueprintNotification>>();
        handler.RegisterSubscriber("peer-1", "session-1", streamMock.Object);

        // Act
        var result = handler.UnregisterSubscriber("peer-1", "session-1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void UnregisterSubscriber_NonExistentSubscriber_ReturnsFalse()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = handler.UnregisterSubscriber("peer-1", "session-1");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetSubscriberCount

    [Fact]
    public void GetSubscriberCount_Initially_ReturnsZero()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var count = handler.GetSubscriberCount();

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void GetSubscriberCount_AfterRegister_Increments()
    {
        // Arrange
        var handler = CreateHandler();
        var streamMock = new Mock<IServerStreamWriter<BlueprintNotification>>();

        // Act
        handler.RegisterSubscriber("peer-1", "session-1", streamMock.Object);

        // Assert
        handler.GetSubscriberCount().Should().Be(1);
    }

    [Fact]
    public void GetSubscriberCount_AfterRegisterAndUnregister_Decrements()
    {
        // Arrange
        var handler = CreateHandler();
        var streamMock = new Mock<IServerStreamWriter<BlueprintNotification>>();
        handler.RegisterSubscriber("peer-1", "session-1", streamMock.Object);

        // Act
        handler.UnregisterSubscriber("peer-1", "session-1");

        // Assert
        handler.GetSubscriberCount().Should().Be(0);
    }

    [Fact]
    public void GetSubscriberCount_MultipleSubscribers_ReturnsCorrectCount()
    {
        // Arrange
        var handler = CreateHandler();
        var streamMock = new Mock<IServerStreamWriter<BlueprintNotification>>();
        handler.RegisterSubscriber("peer-1", "session-1", streamMock.Object);
        handler.RegisterSubscriber("peer-2", "session-2", streamMock.Object);
        handler.RegisterSubscriber("peer-3", "session-3", streamMock.Object);

        // Assert
        handler.GetSubscriberCount().Should().Be(3);
    }

    #endregion

    #region NotifyBlueprintPublishedAsync

    [Fact]
    public async Task NotifyBlueprintPublishedAsync_NoSubscribers_CompletesSuccessfully()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var act = () => handler.NotifyBlueprintPublishedAsync(
            "blueprint-1", 1, DateTime.UtcNow, "publisher-1");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NotifyBlueprintPublishedAsync_NullOrEmptyBlueprintId_ThrowsArgumentException(string? blueprintId)
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var act = () => handler.NotifyBlueprintPublishedAsync(
            blueprintId!, 1, DateTime.UtcNow, "publisher-1");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NotifyBlueprintPublishedAsync_NullOrEmptyPublishedBy_ThrowsArgumentException(string? publishedBy)
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var act = () => handler.NotifyBlueprintPublishedAsync(
            "blueprint-1", 1, DateTime.UtcNow, publishedBy!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task NotifyBlueprintPublishedAsync_WithSubscribers_WritesToStream()
    {
        // Arrange
        var handler = CreateHandler();
        var streamMock = new Mock<IServerStreamWriter<BlueprintNotification>>();
        streamMock.Setup(s => s.WriteAsync(It.IsAny<BlueprintNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        handler.RegisterSubscriber("peer-1", "session-1", streamMock.Object);

        // Act
        await handler.NotifyBlueprintPublishedAsync(
            "blueprint-1", 1, DateTime.UtcNow, "publisher-1");

        // Assert
        streamMock.Verify(s => s.WriteAsync(
            It.Is<BlueprintNotification>(n => n.BlueprintId == "blueprint-1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyBlueprintPublishedAsync_IncrementsSequenceNumber()
    {
        // Arrange
        var handler = CreateHandler();
        var streamMock = new Mock<IServerStreamWriter<BlueprintNotification>>();
        var capturedNotifications = new List<BlueprintNotification>();
        streamMock.Setup(s => s.WriteAsync(It.IsAny<BlueprintNotification>(), It.IsAny<CancellationToken>()))
            .Callback<BlueprintNotification, CancellationToken>((n, _) => capturedNotifications.Add(n))
            .Returns(Task.CompletedTask);
        handler.RegisterSubscriber("peer-1", "session-1", streamMock.Object);

        // Act
        await handler.NotifyBlueprintPublishedAsync("bp-1", 1, DateTime.UtcNow, "pub-1");
        await handler.NotifyBlueprintPublishedAsync("bp-2", 2, DateTime.UtcNow, "pub-1");

        // Assert
        capturedNotifications.Should().HaveCount(2);
        capturedNotifications[1].SequenceNumber.Should().BeGreaterThan(capturedNotifications[0].SequenceNumber);
    }

    [Fact]
    public async Task NotifyBlueprintPublishedAsync_FailedStream_RemovesSubscriber()
    {
        // Arrange
        var handler = CreateHandler();
        var streamMock = new Mock<IServerStreamWriter<BlueprintNotification>>();
        streamMock.Setup(s => s.WriteAsync(It.IsAny<BlueprintNotification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RpcException(new Status(StatusCode.Unavailable, "stream closed")));
        handler.RegisterSubscriber("peer-1", "session-1", streamMock.Object);
        handler.GetSubscriberCount().Should().Be(1);

        // Act
        await handler.NotifyBlueprintPublishedAsync(
            "blueprint-1", 1, DateTime.UtcNow, "publisher-1");

        // Assert
        handler.GetSubscriberCount().Should().Be(0);
    }

    #endregion

    #region GetSubscriberStatistics

    [Fact]
    public void GetSubscriberStatistics_Initially_ReturnsEmptyDictionary()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var stats = handler.GetSubscriberStatistics();

        // Assert
        stats.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSubscriberStatistics_AfterNotifications_ReturnsStats()
    {
        // Arrange
        var handler = CreateHandler();
        var streamMock = new Mock<IServerStreamWriter<BlueprintNotification>>();
        streamMock.Setup(s => s.WriteAsync(It.IsAny<BlueprintNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        handler.RegisterSubscriber("peer-1", "session-1", streamMock.Object);

        // Act
        await handler.NotifyBlueprintPublishedAsync("bp-1", 1, DateTime.UtcNow, "pub-1");

        // Assert
        var stats = handler.GetSubscriberStatistics();
        stats.Should().ContainKey("peer-1:session-1");
        stats["peer-1:session-1"].Sent.Should().Be(1);
        stats["peer-1:session-1"].Failed.Should().Be(0);
    }

    #endregion
}
