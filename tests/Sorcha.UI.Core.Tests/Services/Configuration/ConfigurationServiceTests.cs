// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using FluentAssertions;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;
using Sorcha.UI.Core.Models.Configuration;
using Sorcha.UI.Core.Services.Configuration;
using Xunit;

namespace Sorcha.UI.Core.Tests.Services.Configuration;

/// <summary>
/// Tests for ConfigurationService with mocked IJSRuntime.
/// </summary>
public class ConfigurationServiceTests
{
    #region GetActiveProfileNameAsync Tests

    [Fact]
    public async Task GetActiveProfileNameAsync_WithStoredName_ReturnsStoredName()
    {
        // Arrange
        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime
            .Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:active-profile")))
            .ReturnsAsync("production");

        var service = new ConfigurationService(jsRuntime.Object);

        // Act
        var result = await service.GetActiveProfileNameAsync();

        // Assert
        result.Should().Be("production");
    }

    [Fact]
    public async Task GetActiveProfileNameAsync_WithNoStoredName_ReturnsDefault()
    {
        // Arrange
        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime
            .Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:active-profile")))
            .ReturnsAsync((string?)null);

        var service = new ConfigurationService(jsRuntime.Object);

        // Act
        var result = await service.GetActiveProfileNameAsync();

        // Assert
        result.Should().Be(ProfileDefaults.DefaultActiveProfile);
    }

    #endregion

    #region GetProfilesAsync Tests

    [Fact]
    public async Task GetProfilesAsync_WithExistingProfiles_ReturnsProfiles()
    {
        // Arrange
        var profiles = new List<Profile>
        {
            new() { Name = "test1", SorchaServiceUrl = "http://localhost:80" },
            new() { Name = "test2", SorchaServiceUrl = "http://localhost:8080" }
        };
        var json = JsonSerializer.Serialize(profiles);

        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime
            .Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:profiles")))
            .ReturnsAsync(json);

        var service = new ConfigurationService(jsRuntime.Object);

        // Act
        var result = await service.GetProfilesAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("test1");
        result[1].Name.Should().Be("test2");
    }

    [Fact]
    public async Task GetProfilesAsync_WithNoProfiles_InitializesDefaults()
    {
        // Arrange
        var callCount = 0;
        string? storedProfiles = null;
        var defaultProfiles = ProfileDefaults.GetDefaultProfiles();
        var defaultJson = JsonSerializer.Serialize(defaultProfiles);

        var jsRuntime = new Mock<IJSRuntime>();

        // First call returns null (no profiles), subsequent calls return the saved profiles
        jsRuntime
            .Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:profiles")))
            .Returns(() =>
            {
                callCount++;
                return ValueTask.FromResult(callCount == 1 ? null : storedProfiles);
            });

        // Capture what gets saved
        jsRuntime
            .Setup(js => js.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:profiles")))
            .Callback<string, object[]>((_, args) => storedProfiles = args[1]?.ToString())
            .ReturnsAsync((IJSVoidResult)null!);

        jsRuntime
            .Setup(js => js.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:active-profile")))
            .ReturnsAsync((IJSVoidResult)null!);

        var service = new ConfigurationService(jsRuntime.Object);

        // Act
        var result = await service.GetProfilesAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Select(p => p.Name).Should().Contain("local");
        result.Select(p => p.Name).Should().Contain("docker");
        result.Select(p => p.Name).Should().Contain("production");
    }

    #endregion

    #region GetProfileAsync Tests

    [Fact]
    public async Task GetProfileAsync_ExistingProfile_ReturnsProfile()
    {
        // Arrange
        var profiles = new List<Profile>
        {
            new() { Name = "myprofile", SorchaServiceUrl = "http://test:80" }
        };
        var json = JsonSerializer.Serialize(profiles);

        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime
            .Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:profiles")))
            .ReturnsAsync(json);

        var service = new ConfigurationService(jsRuntime.Object);

        // Act
        var result = await service.GetProfileAsync("myprofile");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("myprofile");
        result.SorchaServiceUrl.Should().Be("http://test:80");
    }

    [Fact]
    public async Task GetProfileAsync_NonExistingProfile_ReturnsNull()
    {
        // Arrange
        var profiles = new List<Profile>
        {
            new() { Name = "existing", SorchaServiceUrl = "http://test:80" }
        };
        var json = JsonSerializer.Serialize(profiles);

        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime
            .Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:profiles")))
            .ReturnsAsync(json);

        var service = new ConfigurationService(jsRuntime.Object);

        // Act
        var result = await service.GetProfileAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProfileAsync_IsCaseInsensitive()
    {
        // Arrange
        var profiles = new List<Profile>
        {
            new() { Name = "MyProfile", SorchaServiceUrl = "http://test:80" }
        };
        var json = JsonSerializer.Serialize(profiles);

        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime
            .Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:profiles")))
            .ReturnsAsync(json);

        var service = new ConfigurationService(jsRuntime.Object);

        // Act
        var result = await service.GetProfileAsync("myprofile");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("MyProfile");
    }

    #endregion

    #region SaveProfileAsync Tests

    [Fact]
    public async Task SaveProfileAsync_NewProfile_AddsToList()
    {
        // Arrange
        var existingProfiles = new List<Profile>
        {
            new() { Name = "existing", SorchaServiceUrl = "http://localhost:80" }
        };
        var existingJson = JsonSerializer.Serialize(existingProfiles);
        string? savedJson = null;

        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime
            .Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:profiles")))
            .ReturnsAsync(existingJson);

        jsRuntime
            .Setup(js => js.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:profiles")))
            .Callback<string, object[]>((_, args) => savedJson = args[1]?.ToString())
            .ReturnsAsync((IJSVoidResult)null!);

        var service = new ConfigurationService(jsRuntime.Object);

        var newProfile = new Profile
        {
            Name = "newprofile",
            SorchaServiceUrl = "http://newhost:8080"
        };

        // Act
        await service.SaveProfileAsync(newProfile);

        // Assert
        savedJson.Should().NotBeNullOrEmpty();
        var savedProfiles = JsonSerializer.Deserialize<List<Profile>>(savedJson!);
        savedProfiles.Should().HaveCount(2);
        savedProfiles.Should().Contain(p => p.Name == "newprofile");
    }

    [Fact]
    public async Task SaveProfileAsync_ExistingProfile_UpdatesExisting()
    {
        // Arrange
        var existingProfiles = new List<Profile>
        {
            new() { Name = "myprofile", SorchaServiceUrl = "http://old:80" }
        };
        var existingJson = JsonSerializer.Serialize(existingProfiles);
        string? savedJson = null;

        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime
            .Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:profiles")))
            .ReturnsAsync(existingJson);

        jsRuntime
            .Setup(js => js.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:profiles")))
            .Callback<string, object[]>((_, args) => savedJson = args[1]?.ToString())
            .ReturnsAsync((IJSVoidResult)null!);

        var service = new ConfigurationService(jsRuntime.Object);

        var updatedProfile = new Profile
        {
            Name = "myprofile",
            SorchaServiceUrl = "http://new:8080"
        };

        // Act
        await service.SaveProfileAsync(updatedProfile);

        // Assert
        savedJson.Should().NotBeNullOrEmpty();
        var savedProfiles = JsonSerializer.Deserialize<List<Profile>>(savedJson!);
        savedProfiles.Should().HaveCount(1);
        savedProfiles![0].SorchaServiceUrl.Should().Be("http://new:8080");
    }

    [Fact]
    public async Task SaveProfileAsync_NullProfile_ThrowsArgumentNullException()
    {
        // Arrange
        var jsRuntime = new Mock<IJSRuntime>();
        var service = new ConfigurationService(jsRuntime.Object);

        // Act
        var act = () => service.SaveProfileAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SaveProfileAsync_InvalidProfile_ThrowsArgumentException()
    {
        // Arrange
        var jsRuntime = new Mock<IJSRuntime>();
        var service = new ConfigurationService(jsRuntime.Object);
        var invalidProfile = new Profile { Name = "" };

        // Act
        var act = () => service.SaveProfileAsync(invalidProfile);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid profile configuration*");
    }

    #endregion

    #region DeleteProfileAsync Tests

    [Fact]
    public async Task DeleteProfileAsync_ExistingUserProfile_DeletesAndReturnsTrue()
    {
        // Arrange
        var profiles = new List<Profile>
        {
            new() { Name = "userprofile", SorchaServiceUrl = "http://localhost:80", IsSystemProfile = false }
        };
        var existingJson = JsonSerializer.Serialize(profiles);
        string? savedJson = null;

        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime
            .Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:profiles")))
            .ReturnsAsync(existingJson);

        jsRuntime
            .Setup(js => js.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:profiles")))
            .Callback<string, object[]>((_, args) => savedJson = args[1]?.ToString())
            .ReturnsAsync((IJSVoidResult)null!);

        var service = new ConfigurationService(jsRuntime.Object);

        // Act
        var result = await service.DeleteProfileAsync("userprofile");

        // Assert
        result.Should().BeTrue();
        var savedProfiles = JsonSerializer.Deserialize<List<Profile>>(savedJson!);
        savedProfiles.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteProfileAsync_SystemProfile_ReturnsFalse()
    {
        // Arrange
        var profiles = new List<Profile>
        {
            new() { Name = "systemprofile", SorchaServiceUrl = "http://localhost:80", IsSystemProfile = true }
        };
        var existingJson = JsonSerializer.Serialize(profiles);

        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime
            .Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:profiles")))
            .ReturnsAsync(existingJson);

        var service = new ConfigurationService(jsRuntime.Object);

        // Act
        var result = await service.DeleteProfileAsync("systemprofile");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProfileAsync_NonExistentProfile_ReturnsFalse()
    {
        // Arrange
        var profiles = new List<Profile>
        {
            new() { Name = "existing", SorchaServiceUrl = "http://localhost:80" }
        };
        var existingJson = JsonSerializer.Serialize(profiles);

        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime
            .Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:profiles")))
            .ReturnsAsync(existingJson);

        var service = new ConfigurationService(jsRuntime.Object);

        // Act
        var result = await service.DeleteProfileAsync("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProfileAsync_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var jsRuntime = new Mock<IJSRuntime>();
        var service = new ConfigurationService(jsRuntime.Object);

        // Act
        var act = () => service.DeleteProfileAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Profile name cannot be null or empty*");
    }

    #endregion

    #region SetActiveProfileAsync Tests

    [Fact]
    public async Task SetActiveProfileAsync_ExistingProfile_SetsActiveProfile()
    {
        // Arrange
        var profiles = new List<Profile>
        {
            new() { Name = "docker", SorchaServiceUrl = "http://localhost:80" }
        };
        var existingJson = JsonSerializer.Serialize(profiles);
        string? savedProfileName = null;

        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime
            .Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:profiles")))
            .ReturnsAsync(existingJson);

        jsRuntime
            .Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:active-profile")))
            .ReturnsAsync("local");

        jsRuntime
            .Setup(js => js.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:active-profile")))
            .Callback<string, object[]>((_, args) => savedProfileName = args[1]?.ToString())
            .ReturnsAsync((IJSVoidResult)null!);

        var service = new ConfigurationService(jsRuntime.Object);

        // Act
        await service.SetActiveProfileAsync("docker");

        // Assert
        savedProfileName.Should().Be("docker");
    }

    [Fact]
    public async Task SetActiveProfileAsync_NonExistentProfile_ThrowsInvalidOperationException()
    {
        // Arrange
        var profiles = new List<Profile>
        {
            new() { Name = "docker", SorchaServiceUrl = "http://localhost:80" }
        };
        var existingJson = JsonSerializer.Serialize(profiles);

        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime
            .Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:profiles")))
            .ReturnsAsync(existingJson);

        var service = new ConfigurationService(jsRuntime.Object);

        // Act
        var act = () => service.SetActiveProfileAsync("nonexistent");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Profile 'nonexistent' not found*");
    }

    [Fact]
    public async Task SetActiveProfileAsync_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var jsRuntime = new Mock<IJSRuntime>();
        var service = new ConfigurationService(jsRuntime.Object);

        // Act
        var act = () => service.SetActiveProfileAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Profile name cannot be null or empty*");
    }

    [Fact]
    public async Task SetActiveProfileAsync_RaisesActiveProfileChangedEvent()
    {
        // Arrange
        var profiles = new List<Profile>
        {
            new() { Name = "local", SorchaServiceUrl = "" },
            new() { Name = "docker", SorchaServiceUrl = "http://localhost:80" }
        };
        var existingJson = JsonSerializer.Serialize(profiles);

        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime
            .Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:profiles")))
            .ReturnsAsync(existingJson);

        jsRuntime
            .Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:active-profile")))
            .ReturnsAsync("local");

        jsRuntime
            .Setup(js => js.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:active-profile")))
            .ReturnsAsync((IJSVoidResult)null!);

        var service = new ConfigurationService(jsRuntime.Object);

        var eventRaised = false;
        string? previousProfileName = null;
        Profile? newProfile = null;

        service.ActiveProfileChanged += (sender, args) =>
        {
            eventRaised = true;
            previousProfileName = args.PreviousProfileName;
            newProfile = args.NewProfile;
        };

        // Act
        await service.SetActiveProfileAsync("docker");

        // Assert
        eventRaised.Should().BeTrue();
        previousProfileName.Should().Be("local");
        newProfile.Should().NotBeNull();
        newProfile!.Name.Should().Be("docker");
    }

    #endregion

    #region UiConfiguration Tests

    [Fact]
    public async Task GetUiConfigurationAsync_WithStoredConfig_ReturnsStoredConfig()
    {
        // Arrange
        var config = new UiConfiguration
        {
            Theme = "Dark",
            DefaultPageSize = 50
        };
        var json = JsonSerializer.Serialize(config);

        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime
            .Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:ui-config")))
            .ReturnsAsync(json);

        var service = new ConfigurationService(jsRuntime.Object);

        // Act
        var result = await service.GetUiConfigurationAsync();

        // Assert
        result.Theme.Should().Be("Dark");
        result.DefaultPageSize.Should().Be(50);
    }

    [Fact]
    public async Task GetUiConfigurationAsync_WithNoStoredConfig_ReturnsDefault()
    {
        // Arrange
        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime
            .Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:ui-config")))
            .ReturnsAsync((string?)null);

        var service = new ConfigurationService(jsRuntime.Object);

        // Act
        var result = await service.GetUiConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result.Theme.Should().Be("Auto");
        result.DefaultPageSize.Should().Be(20);
    }

    [Fact]
    public async Task SaveUiConfigurationAsync_SavesConfiguration()
    {
        // Arrange
        string? savedJson = null;

        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime
            .Setup(js => js.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:ui-config")))
            .Callback<string, object[]>((_, args) => savedJson = args[1]?.ToString())
            .ReturnsAsync((IJSVoidResult)null!);

        var service = new ConfigurationService(jsRuntime.Object);

        var config = new UiConfiguration
        {
            Theme = "Light",
            EnableAnimations = false
        };

        // Act
        await service.SaveUiConfigurationAsync(config);

        // Assert
        savedJson.Should().NotBeNullOrEmpty();
        var savedConfig = JsonSerializer.Deserialize<UiConfiguration>(savedJson!);
        savedConfig!.Theme.Should().Be("Light");
        savedConfig.EnableAnimations.Should().BeFalse();
    }

    [Fact]
    public async Task SaveUiConfigurationAsync_NullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        var jsRuntime = new Mock<IJSRuntime>();
        var service = new ConfigurationService(jsRuntime.Object);

        // Act
        var act = () => service.SaveUiConfigurationAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region InitializeDefaultProfilesAsync Tests

    [Fact]
    public async Task InitializeDefaultProfilesAsync_InitializesDefaultProfiles()
    {
        // Arrange
        string? savedProfilesJson = null;
        string? savedActiveProfile = null;

        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime
            .Setup(js => js.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:profiles")))
            .Callback<string, object[]>((_, args) => savedProfilesJson = args[1]?.ToString())
            .ReturnsAsync((IJSVoidResult)null!);

        jsRuntime
            .Setup(js => js.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:active-profile")))
            .Callback<string, object[]>((_, args) => savedActiveProfile = args[1]?.ToString())
            .ReturnsAsync((IJSVoidResult)null!);

        var service = new ConfigurationService(jsRuntime.Object);

        // Act
        await service.InitializeDefaultProfilesAsync();

        // Assert
        savedProfilesJson.Should().NotBeNullOrEmpty();
        var savedProfiles = JsonSerializer.Deserialize<List<Profile>>(savedProfilesJson!);
        savedProfiles.Should().HaveCount(3);
        savedProfiles.Should().Contain(p => p.Name == "local");
        savedProfiles.Should().Contain(p => p.Name == "docker");
        savedProfiles.Should().Contain(p => p.Name == "production");

        savedActiveProfile.Should().Be(ProfileDefaults.DefaultActiveProfile);
    }

    #endregion

    #region GetActiveProfileAsync Tests

    [Fact]
    public async Task GetActiveProfileAsync_WithActiveProfile_ReturnsProfile()
    {
        // Arrange
        var profiles = new List<Profile>
        {
            new() { Name = "docker", SorchaServiceUrl = "http://localhost:80" }
        };
        var profilesJson = JsonSerializer.Serialize(profiles);

        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime
            .Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:profiles")))
            .ReturnsAsync(profilesJson);

        jsRuntime
            .Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(a => a[0].ToString() == "sorcha:active-profile")))
            .ReturnsAsync("docker");

        var service = new ConfigurationService(jsRuntime.Object);

        // Act
        var result = await service.GetActiveProfileAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("docker");
    }

    #endregion
}
