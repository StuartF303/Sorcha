// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Blazored.LocalStorage;
using FluentAssertions;
using Moq;
using Sorcha.Admin.Models.Authentication;
using Sorcha.Admin.Services.Authentication;
using Sorcha.Admin.Services.Encryption;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Sorcha.Admin.Tests.Services;

/// <summary>
/// Tests for BrowserTokenCache to ensure proper token storage and encryption.
/// </summary>
public class BrowserTokenCacheTests
{
    private readonly Mock<ILocalStorageService> _mockLocalStorage;
    private readonly Mock<IEncryptionProvider> _mockEncryption;
    private readonly BrowserTokenCache _tokenCache;

    public BrowserTokenCacheTests()
    {
        _mockLocalStorage = new Mock<ILocalStorageService>();
        _mockEncryption = new Mock<IEncryptionProvider>();
        _tokenCache = new BrowserTokenCache(_mockLocalStorage.Object, _mockEncryption.Object);
    }

    [Fact]
    public async Task SetAsync_EncryptsAndStoresToken()
    {
        // Arrange
        var profile = "docker";
        var entry = new TokenCacheEntry
        {
            AccessToken = "test-access-token",
            RefreshToken = "test-refresh-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            Profile = profile,
            Subject = "admin@sorcha.local"
        };

        var encryptedBytes = Encoding.UTF8.GetBytes("encrypted-data");
        _mockEncryption
            .Setup(x => x.EncryptAsync(It.IsAny<string>()))
            .ReturnsAsync(encryptedBytes);

        string? savedKey = null;
        string? savedValue = null;
        _mockLocalStorage
            .Setup(x => x.SetItemAsStringAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, CancellationToken>((key, value, ct) =>
            {
                savedKey = key;
                savedValue = value;
            })
            .Returns(ValueTask.CompletedTask);

        // Act
        await _tokenCache.SetAsync(profile, entry);

        // Assert
        savedKey.Should().Be("sorcha:tokens:docker", "should use correct key format");
        savedValue.Should().NotBeNull();

        // Verify encryption was called with serialized token
        _mockEncryption.Verify(x => x.EncryptAsync(It.Is<string>(json =>
            json.Contains("test-access-token") &&
            json.Contains("test-refresh-token"))), Times.Once);

        // Verify base64 encoding
        var decodedBytes = Convert.FromBase64String(savedValue!);
        decodedBytes.Should().Equal(encryptedBytes);
    }

    [Fact]
    public async Task GetAsync_ReturnsNullWhenTokenDoesNotExist()
    {
        // Arrange
        _mockLocalStorage
            .Setup(x => x.GetItemAsStringAsync("sorcha:tokens:docker"))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _tokenCache.GetAsync("docker");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_DecryptsAndReturnsToken()
    {
        // Arrange
        var profile = "docker";
        var entry = new TokenCacheEntry
        {
            AccessToken = "test-access-token",
            RefreshToken = "test-refresh-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            Profile = profile,
            Subject = "admin@sorcha.local"
        };

        var json = JsonSerializer.Serialize(entry);
        var encryptedBytes = Encoding.UTF8.GetBytes("encrypted-data");
        var base64 = Convert.ToBase64String(encryptedBytes);

        _mockLocalStorage
            .Setup(x => x.GetItemAsStringAsync("sorcha:tokens:docker"))
            .ReturnsAsync(base64);

        _mockEncryption
            .Setup(x => x.DecryptAsync(encryptedBytes))
            .ReturnsAsync(json);

        // Act
        var result = await _tokenCache.GetAsync(profile);

        // Assert
        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("test-access-token");
        result.RefreshToken.Should().Be("test-refresh-token");
        result.Profile.Should().Be(profile);
        result.Subject.Should().Be("admin@sorcha.local");
    }

    [Fact]
    public async Task GetAsync_ReturnsNullAndClearsExpiredToken()
    {
        // Arrange
        var profile = "docker";
        var entry = new TokenCacheEntry
        {
            AccessToken = "test-access-token",
            RefreshToken = "test-refresh-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1), // Expired
            Profile = profile,
            Subject = "admin@sorcha.local"
        };

        var json = JsonSerializer.Serialize(entry);
        var encryptedBytes = Encoding.UTF8.GetBytes("encrypted-data");
        var base64 = Convert.ToBase64String(encryptedBytes);

        _mockLocalStorage
            .Setup(x => x.GetItemAsStringAsync("sorcha:tokens:docker"))
            .ReturnsAsync(base64);

        _mockEncryption
            .Setup(x => x.DecryptAsync(encryptedBytes))
            .ReturnsAsync(json);

        _mockLocalStorage
            .Setup(x => x.RemoveItemAsync("sorcha:tokens:docker"))
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _tokenCache.GetAsync(profile);

        // Assert
        result.Should().BeNull("expired tokens should be removed");

        _mockLocalStorage.Verify(
            x => x.RemoveItemAsync("sorcha:tokens:docker"),
            Times.Once,
            "expired token should be deleted from storage");
    }

    [Fact]
    public async Task ClearAsync_RemovesTokenFromStorage()
    {
        // Arrange
        var profile = "docker";

        _mockLocalStorage
            .Setup(x => x.RemoveItemAsync("sorcha:tokens:docker"))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _tokenCache.ClearAsync(profile);

        // Assert
        _mockLocalStorage.Verify(
            x => x.RemoveItemAsync("sorcha:tokens:docker"),
            Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrueWhenValidTokenExists()
    {
        // Arrange
        var profile = "docker";
        var entry = new TokenCacheEntry
        {
            AccessToken = "test-access-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            Profile = profile
        };

        var json = JsonSerializer.Serialize(entry);
        var encryptedBytes = Encoding.UTF8.GetBytes("encrypted-data");
        var base64 = Convert.ToBase64String(encryptedBytes);

        _mockLocalStorage
            .Setup(x => x.GetItemAsStringAsync("sorcha:tokens:docker"))
            .ReturnsAsync(base64);

        _mockEncryption
            .Setup(x => x.DecryptAsync(encryptedBytes))
            .ReturnsAsync(json);

        // Act
        var exists = await _tokenCache.ExistsAsync(profile);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalseWhenTokenDoesNotExist()
    {
        // Arrange
        _mockLocalStorage
            .Setup(x => x.GetItemAsStringAsync("sorcha:tokens:docker"))
            .ReturnsAsync((string?)null);

        // Act
        var exists = await _tokenCache.ExistsAsync("docker");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task SetAsync_ThrowsExceptionWithHelpfulMessage_OnEncryptionFailure()
    {
        // Arrange
        var entry = new TokenCacheEntry
        {
            AccessToken = "test-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            Profile = "docker"
        };

        _mockEncryption
            .Setup(x => x.EncryptAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Encryption module not loaded"));

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _tokenCache.SetAsync("docker", entry));

        // Assert
        exception.Message.Should().Contain("Failed to store token for profile 'docker'");
        exception.InnerException.Should().NotBeNull();
        exception.InnerException!.Message.Should().Contain("Encryption module not loaded");
    }

    [Fact]
    public async Task TokenCacheKey_FollowsCorrectFormat()
    {
        // Arrange
        var profiles = new[] { "docker", "local", "production", "custom-env" };

        foreach (var profile in profiles)
        {
            _mockLocalStorage
                .Setup(x => x.GetItemAsStringAsync($"sorcha:tokens:{profile}"))
                .ReturnsAsync((string?)null);

            // Act
            await _tokenCache.GetAsync(profile);

            // Assert
            _mockLocalStorage.Verify(
                x => x.GetItemAsStringAsync($"sorcha:tokens:{profile}"),
                Times.Once,
                $"should use 'sorcha:tokens:{profile}' format");
        }
    }

    [Theory]
    [InlineData("docker")]
    [InlineData("local")]
    [InlineData("production")]
    public async Task ClearAllAsync_RemovesAllTokens(string profileName)
    {
        // Arrange
        var allKeys = new List<string>
        {
            "sorcha:tokens:docker",
            "sorcha:tokens:local",
            "sorcha:tokens:production",
            "sorcha:config",
            "other:key"
        };

        _mockLocalStorage
            .Setup(x => x.KeysAsync())
            .ReturnsAsync(allKeys);

        _mockLocalStorage
            .Setup(x => x.RemoveItemAsync(It.IsAny<string>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _tokenCache.ClearAllAsync();

        // Assert
        _mockLocalStorage.Verify(x => x.RemoveItemAsync("sorcha:tokens:docker"), Times.Once);
        _mockLocalStorage.Verify(x => x.RemoveItemAsync("sorcha:tokens:local"), Times.Once);
        _mockLocalStorage.Verify(x => x.RemoveItemAsync("sorcha:tokens:production"), Times.Once);

        // Should NOT remove non-token keys
        _mockLocalStorage.Verify(x => x.RemoveItemAsync("sorcha:config"), Times.Never);
        _mockLocalStorage.Verify(x => x.RemoveItemAsync("other:key"), Times.Never);
    }
}
