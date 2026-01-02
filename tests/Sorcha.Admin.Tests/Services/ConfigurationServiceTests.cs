// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Blazored.LocalStorage;
using FluentAssertions;
using Moq;
using Sorcha.Admin.Models.Configuration;
using Sorcha.Admin.Services.Configuration;
using System.Text.Json;
using Xunit;

namespace Sorcha.Admin.Tests.Services;

/// <summary>
/// Tests for ConfigurationService to ensure proper LocalStorage interaction
/// and profile management.
/// </summary>
public class ConfigurationServiceTests
{
    private readonly Mock<ILocalStorageService> _mockLocalStorage;
    private readonly ConfigurationService _service;
    private const string CONFIG_KEY = "sorcha:config";

    public ConfigurationServiceTests()
    {
        _mockLocalStorage = new Mock<ILocalStorageService>();
        _service = new ConfigurationService(_mockLocalStorage.Object);
    }

    [Fact]
    public async Task GetConfigurationAsync_WhenNoConfigExists_CreatesDefaultConfiguration()
    {
        // Arrange
        _mockLocalStorage
            .Setup(x => x.GetItemAsStringAsync(CONFIG_KEY))
            .ReturnsAsync((string?)null);

        string? savedJson = null;
        _mockLocalStorage
            .Setup(x => x.SetItemAsStringAsync(CONFIG_KEY, It.IsAny<string>()))
            .Callback<string, string, CancellationToken>((key, json, ct) => savedJson = json)
            .Returns(ValueTask.CompletedTask);

        // Act
        var config = await _service.GetConfigurationAsync();

        // Assert
        config.Should().NotBeNull();
        config.ActiveProfile.Should().Be("docker", "docker should be the default active profile");
        config.Profiles.Should().ContainKey("docker");
        config.Profiles.Should().ContainKey("local");
        config.Profiles.Should().ContainKey("production");
        config.Profiles.Count.Should().Be(3, "should have exactly 3 default profiles");

        // Verify it was saved to LocalStorage
        _mockLocalStorage.Verify(x => x.SetItemAsStringAsync(CONFIG_KEY, It.IsAny<string>()), Times.Once);
        savedJson.Should().NotBeNull();

        // Verify saved JSON structure
        var savedConfig = JsonSerializer.Deserialize<AdminConfiguration>(savedJson!);
        savedConfig.Should().NotBeNull();
        savedConfig!.ActiveProfile.Should().Be("docker");
    }

    [Fact]
    public async Task GetConfigurationAsync_WhenConfigExists_ReturnsStoredConfiguration()
    {
        // Arrange
        var existingConfig = new AdminConfiguration
        {
            ActiveProfile = "local",
            Profiles = new Dictionary<string, Profile>
            {
                ["local"] = new Profile { Name = "local", TenantServiceUrl = "https://localhost:7110" }
            }
        };

        var json = JsonSerializer.Serialize(existingConfig);
        _mockLocalStorage
            .Setup(x => x.GetItemAsStringAsync(CONFIG_KEY))
            .ReturnsAsync(json);

        // Act
        var config = await _service.GetConfigurationAsync();

        // Assert
        config.Should().NotBeNull();
        config.ActiveProfile.Should().Be("local");
        config.Profiles.Should().ContainKey("local");
        config.Profiles["local"].TenantServiceUrl.Should().Be("https://localhost:7110");

        // Should NOT save when config already exists
        _mockLocalStorage.Verify(x => x.SetItemAsStringAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetConfigurationAsync_UsesCache_AfterFirstLoad()
    {
        // Arrange
        _mockLocalStorage
            .Setup(x => x.GetItemAsStringAsync(CONFIG_KEY))
            .ReturnsAsync((string?)null);

        _mockLocalStorage
            .Setup(x => x.SetItemAsStringAsync(CONFIG_KEY, It.IsAny<string>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        var config1 = await _service.GetConfigurationAsync();
        var config2 = await _service.GetConfigurationAsync();

        // Assert
        config1.Should().BeSameAs(config2, "should return cached instance");
        _mockLocalStorage.Verify(x => x.GetItemAsStringAsync(CONFIG_KEY), Times.Once, "should only load from storage once");
    }

    [Fact]
    public async Task SetActiveProfileAsync_UpdatesActiveProfile_AndSavesToStorage()
    {
        // Arrange
        _mockLocalStorage
            .Setup(x => x.GetItemAsStringAsync(CONFIG_KEY))
            .ReturnsAsync((string?)null);

        string? savedJson = null;
        _mockLocalStorage
            .Setup(x => x.SetItemAsStringAsync(CONFIG_KEY, It.IsAny<string>()))
            .Callback<string, string, CancellationToken>((key, json, ct) => savedJson = json)
            .Returns(ValueTask.CompletedTask);

        // Load initial config
        await _service.GetConfigurationAsync();

        // Act
        await _service.SetActiveProfileAsync("local");

        // Assert
        var config = await _service.GetConfigurationAsync();
        config.ActiveProfile.Should().Be("local");

        // Verify saved to storage
        _mockLocalStorage.Verify(x => x.SetItemAsStringAsync(CONFIG_KEY, It.IsAny<string>()), Times.AtLeast(2));

        var savedConfig = JsonSerializer.Deserialize<AdminConfiguration>(savedJson!);
        savedConfig!.ActiveProfile.Should().Be("local");
    }

    [Fact]
    public async Task SetActiveProfileAsync_ThrowsException_WhenProfileDoesNotExist()
    {
        // Arrange
        _mockLocalStorage
            .Setup(x => x.GetItemAsStringAsync(CONFIG_KEY))
            .ReturnsAsync((string?)null);

        _mockLocalStorage
            .Setup(x => x.SetItemAsStringAsync(CONFIG_KEY, It.IsAny<string>()))
            .Returns(ValueTask.CompletedTask);

        await _service.GetConfigurationAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.SetActiveProfileAsync("nonexistent"));
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsCorrectProfile()
    {
        // Arrange
        _mockLocalStorage
            .Setup(x => x.GetItemAsStringAsync(CONFIG_KEY))
            .ReturnsAsync((string?)null);

        _mockLocalStorage
            .Setup(x => x.SetItemAsStringAsync(CONFIG_KEY, It.IsAny<string>()))
            .Returns(ValueTask.CompletedTask);

        await _service.GetConfigurationAsync();

        // Act
        var dockerProfile = await _service.GetProfileAsync("docker");
        var nonexistentProfile = await _service.GetProfileAsync("nonexistent");

        // Assert
        dockerProfile.Should().NotBeNull();
        dockerProfile!.Name.Should().Be("docker");
        dockerProfile.AuthTokenUrl.Should().Be("http://localhost/api/service-auth/token");
        dockerProfile.TenantServiceUrl.Should().Be("http://localhost/api/tenant");

        nonexistentProfile.Should().BeNull();
    }

    [Fact]
    public async Task SaveConfigurationAsync_SerializesWithIndentation()
    {
        // Arrange
        var config = new AdminConfiguration
        {
            ActiveProfile = "docker",
            Profiles = ProfileDefaults.GetDefaultProfiles()
        };

        string? savedJson = null;
        _mockLocalStorage
            .Setup(x => x.SetItemAsStringAsync(CONFIG_KEY, It.IsAny<string>()))
            .Callback<string, string, CancellationToken>((key, json, ct) => savedJson = json)
            .Returns(ValueTask.CompletedTask);

        // Act
        await _service.SaveConfigurationAsync(config);

        // Assert
        savedJson.Should().NotBeNull();
        savedJson.Should().Contain("\n", "should be indented for readability");
        savedJson.Should().Contain("\"ActiveProfile\"");
        savedJson.Should().Contain("\"docker\"");
    }

    [Fact]
    public async Task DockerProfile_HasCorrectAuthenticationUrl()
    {
        // Arrange
        _mockLocalStorage
            .Setup(x => x.GetItemAsStringAsync(CONFIG_KEY))
            .ReturnsAsync((string?)null);

        _mockLocalStorage
            .Setup(x => x.SetItemAsStringAsync(CONFIG_KEY, It.IsAny<string>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        var config = await _service.GetConfigurationAsync();
        var dockerProfile = config.Profiles["docker"];

        // Assert - Verify critical authentication paths
        dockerProfile.AuthTokenUrl.Should().Be(
            "http://localhost/api/service-auth/token",
            "docker profile should use API Gateway auth endpoint");

        dockerProfile.TenantServiceUrl.Should().StartWith("http://localhost/api/tenant");
        dockerProfile.WalletServiceUrl.Should().StartWith("http://localhost/api/wallet");
        dockerProfile.RegisterServiceUrl.Should().StartWith("http://localhost/api/register");
        dockerProfile.DefaultClientId.Should().Be("sorcha-admin");
        dockerProfile.VerifySsl.Should().BeFalse("local Docker doesn't use SSL");
    }

    [Fact]
    public async Task DefaultActiveProfile_IsDocker()
    {
        // Arrange
        _mockLocalStorage
            .Setup(x => x.GetItemAsStringAsync(CONFIG_KEY))
            .ReturnsAsync((string?)null);

        _mockLocalStorage
            .Setup(x => x.SetItemAsStringAsync(CONFIG_KEY, It.IsAny<string>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        var config = await _service.GetConfigurationAsync();

        // Assert
        config.ActiveProfile.Should().Be("docker", "docker should be the default for new installations");
        ProfileDefaults.DefaultActiveProfile.Should().Be("docker");
    }

    [Fact]
    public async Task LocalStorageKey_IsConsistent()
    {
        // Arrange
        _mockLocalStorage
            .Setup(x => x.GetItemAsStringAsync(CONFIG_KEY))
            .ReturnsAsync((string?)null);

        _mockLocalStorage
            .Setup(x => x.SetItemAsStringAsync(CONFIG_KEY, It.IsAny<string>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _service.GetConfigurationAsync();

        // Assert
        _mockLocalStorage.Verify(
            x => x.GetItemAsStringAsync("sorcha:config"),
            Times.Once,
            "should use 'sorcha:config' as the LocalStorage key");
    }
}
