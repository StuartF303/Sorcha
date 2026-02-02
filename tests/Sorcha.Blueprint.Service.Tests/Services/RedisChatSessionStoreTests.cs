// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.Blueprint.Service.Models.Chat;
using Sorcha.Blueprint.Service.Services;

namespace Sorcha.Blueprint.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="RedisChatSessionStore"/>.
/// </summary>
public class RedisChatSessionStoreTests
{
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ILogger<RedisChatSessionStore>> _loggerMock;
    private readonly RedisChatSessionStore _store;

    public RedisChatSessionStoreTests()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<RedisChatSessionStore>>();
        _store = new RedisChatSessionStore(_cacheMock.Object, _loggerMock.Object);
    }

    #region CreateSessionAsync Tests

    [Fact]
    public async Task CreateSessionAsync_CreatesNewSession()
    {
        // Arrange
        var userId = "user-123";
        var orgId = "org-456";

        // Act
        var session = await _store.CreateSessionAsync(userId, orgId);

        // Assert
        session.Should().NotBeNull();
        session.Id.Should().NotBeNullOrEmpty();
        session.UserId.Should().Be(userId);
        session.OrganizationId.Should().Be(orgId);
        session.Status.Should().Be(SessionStatus.Active);
        session.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateSessionAsync_WithBlueprintId_StoresExistingBlueprintId()
    {
        // Arrange
        var userId = "user-123";
        var orgId = "org-456";
        var blueprintId = "blueprint-789";

        // Act
        var session = await _store.CreateSessionAsync(userId, orgId, blueprintId);

        // Assert
        session.ExistingBlueprintId.Should().Be(blueprintId);
    }

    [Fact]
    public async Task CreateSessionAsync_StoresSessionInCache()
    {
        // Arrange
        var userId = "user-123";
        var orgId = "org-456";
        byte[]? capturedData = null;

        _cacheMock
            .Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>((key, data, _, _) =>
            {
                if (key.StartsWith("chat:session:"))
                    capturedData = data;
            })
            .Returns(Task.CompletedTask);

        // Act
        await _store.CreateSessionAsync(userId, orgId);

        // Assert
        capturedData.Should().NotBeNull();
        var json = Encoding.UTF8.GetString(capturedData!);
        json.Should().Contain(userId);
    }

    [Fact]
    public async Task CreateSessionAsync_SetsActiveSession()
    {
        // Arrange
        var userId = "user-123";
        var orgId = "org-456";
        string? activeSessionKey = null;

        _cacheMock
            .Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>((key, _, _, _) =>
            {
                if (key.Contains(":active"))
                    activeSessionKey = key;
            })
            .Returns(Task.CompletedTask);

        // Act
        await _store.CreateSessionAsync(userId, orgId);

        // Assert
        activeSessionKey.Should().Contain(userId);
    }

    #endregion

    #region GetSessionAsync Tests

    [Fact]
    public async Task GetSessionAsync_ReturnsSession_WhenExists()
    {
        // Arrange
        var sessionId = "session-123";
        var session = new ChatSession
        {
            Id = sessionId,
            UserId = "user-123",
            OrganizationId = "org-456",
            Status = SessionStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            LastActivityAt = DateTimeOffset.UtcNow
        };
        var json = JsonSerializer.Serialize(session, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _cacheMock
            .Setup(c => c.GetAsync($"chat:session:{sessionId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(json));

        _cacheMock
            .Setup(c => c.GetAsync($"chat:messages:{sessionId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _store.GetSessionAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(sessionId);
        result.UserId.Should().Be("user-123");
    }

    [Fact]
    public async Task GetSessionAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        var sessionId = "nonexistent";

        _cacheMock
            .Setup(c => c.GetAsync($"chat:session:{sessionId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _store.GetSessionAsync(sessionId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateSessionAsync Tests

    [Fact]
    public async Task UpdateSessionAsync_UpdatesLastActivityTime()
    {
        // Arrange
        var originalTime = DateTimeOffset.UtcNow.AddMinutes(-10);
        var session = new ChatSession
        {
            Id = "session-123",
            UserId = "user-123",
            OrganizationId = "org-456",
            Status = SessionStatus.Active,
            CreatedAt = originalTime,
            LastActivityAt = originalTime
        };

        byte[]? capturedData = null;
        _cacheMock
            .Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>((_, data, _, _) =>
            {
                capturedData = data;
            })
            .Returns(Task.CompletedTask);

        // Act
        await _store.UpdateSessionAsync(session);

        // Assert
        capturedData.Should().NotBeNull();
        var json = Encoding.UTF8.GetString(capturedData!);
        var updated = JsonSerializer.Deserialize<ChatSession>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        updated!.LastActivityAt.Should().BeAfter(originalTime);
    }

    #endregion

    #region AddMessageAsync Tests

    [Fact]
    public async Task AddMessageAsync_AppendsMessageToList()
    {
        // Arrange
        var sessionId = "session-123";
        var message = new ChatMessage
        {
            SessionId = sessionId,
            Role = MessageRole.User,
            Content = "Hello, AI!"
        };

        byte[]? capturedData = null;
        _cacheMock
            .Setup(c => c.GetAsync($"chat:messages:{sessionId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        _cacheMock
            .Setup(c => c.SetAsync(
                $"chat:messages:{sessionId}",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>((_, data, _, _) =>
            {
                capturedData = data;
            })
            .Returns(Task.CompletedTask);

        // Act
        await _store.AddMessageAsync(sessionId, message);

        // Assert
        capturedData.Should().NotBeNull();
        var json = Encoding.UTF8.GetString(capturedData!);
        json.Should().Contain("Hello, AI!");
    }

    #endregion

    #region GetMessagesAsync Tests

    [Fact]
    public async Task GetMessagesAsync_ReturnsEmptyList_WhenNoMessages()
    {
        // Arrange
        var sessionId = "session-123";

        _cacheMock
            .Setup(c => c.GetAsync($"chat:messages:{sessionId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _store.GetMessagesAsync(sessionId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMessagesAsync_ReturnsMessages_WhenExist()
    {
        // Arrange
        var sessionId = "session-123";
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var messages = new List<ChatMessage>
        {
            new() { SessionId = sessionId, Role = MessageRole.User, Content = "Hello" },
            new() { SessionId = sessionId, Role = MessageRole.Assistant, Content = "Hi there!" }
        };

        // The implementation stores messages as List<string> where each string is a serialized ChatMessage
        var messageStrings = messages.Select(m => JsonSerializer.Serialize(m, jsonOptions)).ToList();
        var json = JsonSerializer.Serialize(messageStrings, jsonOptions);

        _cacheMock
            .Setup(c => c.GetAsync($"chat:messages:{sessionId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(json));

        // Act
        var result = await _store.GetMessagesAsync(sessionId);

        // Assert
        result.Should().HaveCount(2);
        result[0].Content.Should().Be("Hello");
        result[1].Content.Should().Be("Hi there!");
    }

    #endregion

    #region GetActiveSessionForUserAsync Tests

    [Fact]
    public async Task GetActiveSessionForUserAsync_ReturnsSession_WhenActive()
    {
        // Arrange
        var userId = "user-123";
        var sessionId = "session-456";
        var session = new ChatSession
        {
            Id = sessionId,
            UserId = userId,
            OrganizationId = "org-789",
            Status = SessionStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            LastActivityAt = DateTimeOffset.UtcNow
        };
        var sessionJson = JsonSerializer.Serialize(session, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _cacheMock
            .Setup(c => c.GetAsync($"chat:user:{userId}:active", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(sessionId));

        _cacheMock
            .Setup(c => c.GetAsync($"chat:session:{sessionId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(sessionJson));

        _cacheMock
            .Setup(c => c.GetAsync($"chat:messages:{sessionId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _store.GetActiveSessionForUserAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(sessionId);
    }

    [Fact]
    public async Task GetActiveSessionForUserAsync_ReturnsNull_WhenNoActiveSession()
    {
        // Arrange
        var userId = "user-123";

        _cacheMock
            .Setup(c => c.GetAsync($"chat:user:{userId}:active", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _store.GetActiveSessionForUserAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region DeleteSessionAsync Tests

    [Fact]
    public async Task DeleteSessionAsync_RemovesSessionFromCache()
    {
        // Arrange
        var sessionId = "session-123";
        var removedKeys = new List<string>();

        _cacheMock
            .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((key, _) => removedKeys.Add(key))
            .Returns(Task.CompletedTask);

        // Act
        await _store.DeleteSessionAsync(sessionId);

        // Assert
        removedKeys.Should().Contain($"chat:session:{sessionId}");
        removedKeys.Should().Contain($"chat:messages:{sessionId}");
    }

    #endregion

    #region ClearActiveSessionForUserAsync Tests

    [Fact]
    public async Task ClearActiveSessionForUserAsync_RemovesActiveKey()
    {
        // Arrange
        var userId = "user-123";
        string? removedKey = null;

        _cacheMock
            .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((key, _) => removedKey = key)
            .Returns(Task.CompletedTask);

        // Act
        await _store.ClearActiveSessionForUserAsync(userId);

        // Assert
        removedKey.Should().Be($"chat:user:{userId}:active");
    }

    #endregion
}
