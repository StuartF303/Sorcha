// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Microsoft.JSInterop;
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
/// Note: These tests mock IJSRuntime which is used to access localStorage via JavaScript interop.
/// </summary>
public class BrowserTokenCacheTests
{
    private readonly Mock<IEncryptionProvider> _mockEncryption;
    private readonly Mock<IJSRuntime> _mockJsRuntime;
    private readonly BrowserTokenCache _tokenCache;

    public BrowserTokenCacheTests()
    {
        _mockEncryption = new Mock<IEncryptionProvider>();
        _mockJsRuntime = new Mock<IJSRuntime>();
        _tokenCache = new BrowserTokenCache(_mockEncryption.Object, _mockJsRuntime.Object);
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Assert - just verify constructor doesn't throw
        _tokenCache.Should().NotBeNull();
    }

    [Fact]
    public async Task SetAsync_EncryptsToken()
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

        // Setup IJSRuntime mock for localStorage.setItem
        _mockJsRuntime
            .Setup(x => x.InvokeAsync<object>(
                "localStorage.setItem",
                It.IsAny<object[]>()))
            .ReturnsAsync((object)null!);

        // Act
        await _tokenCache.SetAsync(profile, entry);

        // Assert
        _mockEncryption.Verify(x => x.EncryptAsync(It.Is<string>(json =>
            json.Contains("test-access-token") &&
            json.Contains("test-refresh-token"))), Times.Once);
    }

    [Fact]
    public async Task GetAsync_ReturnsNullWhenTokenDoesNotExist()
    {
        // Arrange - localStorage.getItem returns null
        _mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>(
                "localStorage.getItem",
                It.IsAny<object[]>()))
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

        _mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>(
                "localStorage.getItem",
                It.IsAny<object[]>()))
            .ReturnsAsync(base64);

        _mockEncryption
            .Setup(x => x.DecryptAsync(It.IsAny<byte[]>()))
            .ReturnsAsync(json);

        // Act
        var result = await _tokenCache.GetAsync(profile);

        // Assert
        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("test-access-token");
        result.RefreshToken.Should().Be("test-refresh-token");
        result.Profile.Should().Be(profile);
    }

    [Fact]
    public async Task ClearAsync_RemovesTokenFromStorage()
    {
        // Arrange
        var profile = "docker";

        _mockJsRuntime
            .Setup(x => x.InvokeAsync<object>(
                "localStorage.removeItem",
                It.IsAny<object[]>()))
            .ReturnsAsync((object)null!);

        // Act
        await _tokenCache.ClearAsync(profile);

        // Assert
        _mockJsRuntime.Verify(
            x => x.InvokeAsync<object>(
                "localStorage.removeItem",
                It.Is<object[]>(args => args[0].ToString()!.Contains(profile))),
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

        _mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>(
                "localStorage.getItem",
                It.IsAny<object[]>()))
            .ReturnsAsync(base64);

        _mockEncryption
            .Setup(x => x.DecryptAsync(It.IsAny<byte[]>()))
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
        _mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>(
                "localStorage.getItem",
                It.IsAny<object[]>()))
            .ReturnsAsync((string?)null);

        // Act
        var exists = await _tokenCache.ExistsAsync("docker");

        // Assert
        exists.Should().BeFalse();
    }
}
