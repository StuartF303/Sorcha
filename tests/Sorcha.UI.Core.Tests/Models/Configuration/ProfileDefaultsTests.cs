// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.UI.Core.Models.Configuration;
using Xunit;

namespace Sorcha.UI.Core.Tests.Models.Configuration;

/// <summary>
/// Tests for ProfileDefaults to ensure correct default configuration.
/// </summary>
public class ProfileDefaultsTests
{
    [Fact]
    public void DefaultActiveProfile_IsDocker()
    {
        // Act
        var defaultProfile = ProfileDefaults.DefaultActiveProfile;

        // Assert
        defaultProfile.Should().Be("docker", "docker should be the default for Docker Compose deployments");
    }

    [Fact]
    public void GetDefaultProfiles_ReturnsThreeProfiles()
    {
        // Act
        var profiles = ProfileDefaults.GetDefaultProfiles();

        // Assert
        profiles.Should().HaveCount(3, "should have local, docker, and production profiles");
        profiles.Select(p => p.Name).Should().Contain("local");
        profiles.Select(p => p.Name).Should().Contain("docker");
        profiles.Select(p => p.Name).Should().Contain("production");
    }

    [Fact]
    public void LocalProfile_HasIndividualServiceUrls()
    {
        // Act
        var profiles = ProfileDefaults.GetDefaultProfiles();
        var localProfile = profiles.First(p => p.Name == "local");

        // Assert - Local profile uses individual service URLs for Aspire
        localProfile.SorchaServiceUrl.Should().BeEmpty("local profile doesn't use a base URL");
        localProfile.TenantServiceUrl.Should().Be("https://localhost:7110");
        localProfile.RegisterServiceUrl.Should().Be("https://localhost:7290");
        localProfile.BlueprintServiceUrl.Should().Be("https://localhost:7000");
        localProfile.WalletServiceUrl.Should().Be("https://localhost:7001");
        localProfile.PeerServiceUrl.Should().Be("https://localhost:7002");
        localProfile.AuthTokenUrl.Should().Be("https://localhost:7110/api/service-auth/token");
        localProfile.VerifySsl.Should().BeFalse("local Aspire uses self-signed certificates");
        localProfile.IsSystemProfile.Should().BeTrue();
    }

    [Fact]
    public void DockerProfile_UsesSameOriginRelativeUrls()
    {
        // Act
        var profiles = ProfileDefaults.GetDefaultProfiles();
        var dockerProfile = profiles.First(p => p.Name == "docker");

        // Assert - Docker profile uses empty base URL for same-origin relative URLs via API Gateway
        dockerProfile.SorchaServiceUrl.Should().BeEmpty("Docker uses same-origin relative URLs via gateway");
        dockerProfile.TenantServiceUrl.Should().BeNull("should use relative URL via gateway");
        dockerProfile.RegisterServiceUrl.Should().BeNull("should use relative URL via gateway");
        dockerProfile.BlueprintServiceUrl.Should().BeNull("should use relative URL via gateway");
        dockerProfile.WalletServiceUrl.Should().BeNull("should use relative URL via gateway");
        dockerProfile.PeerServiceUrl.Should().BeNull("should use relative URL via gateway");
        dockerProfile.AuthTokenUrl.Should().BeNull("should use relative URL via gateway");
        dockerProfile.VerifySsl.Should().BeFalse("local Docker doesn't use SSL");
        dockerProfile.IsSystemProfile.Should().BeTrue();
    }

    [Fact]
    public void DockerProfile_ResolvesUrlsToRelativePaths()
    {
        // Act
        var profiles = ProfileDefaults.GetDefaultProfiles();
        var dockerProfile = profiles.First(p => p.Name == "docker");

        // Assert - With empty base URL and null individual URLs, resolved URLs are empty
        // The HttpClient in ConfigurationService handles this by using relative paths
        dockerProfile.GetTenantServiceUrl().Should().BeEmpty("empty base + null individual = relative URL");
        dockerProfile.GetRegisterServiceUrl().Should().BeEmpty("empty base + null individual = relative URL");
        dockerProfile.GetBlueprintServiceUrl().Should().BeEmpty("empty base + null individual = relative URL");
        dockerProfile.GetWalletServiceUrl().Should().BeEmpty("empty base + null individual = relative URL");
        dockerProfile.GetPeerServiceUrl().Should().BeEmpty("empty base + null individual = relative URL");
    }

    [Fact]
    public void ProductionProfile_UsesBaseUrl()
    {
        // Act
        var profiles = ProfileDefaults.GetDefaultProfiles();
        var productionProfile = profiles.First(p => p.Name == "production");

        // Assert
        productionProfile.SorchaServiceUrl.Should().Be("https://api.sorcha.io");
        productionProfile.TenantServiceUrl.Should().BeNull("should derive from base URL");
        productionProfile.VerifySsl.Should().BeTrue("production must verify SSL certificates");
        productionProfile.IsSystemProfile.Should().BeTrue();
    }

    [Fact]
    public void ProductionProfile_ResolvesUrlsCorrectly()
    {
        // Act
        var profiles = ProfileDefaults.GetDefaultProfiles();
        var productionProfile = profiles.First(p => p.Name == "production");

        // Assert
        productionProfile.GetTenantServiceUrl().Should().Be("https://api.sorcha.io/api/tenant");
        productionProfile.GetBlueprintServiceUrl().Should().Be("https://api.sorcha.io/api/blueprint");
    }

    [Fact]
    public void AllProfiles_AreValid()
    {
        // Act
        var profiles = ProfileDefaults.GetDefaultProfiles();

        // Assert
        foreach (var profile in profiles)
        {
            profile.IsValid().Should().BeTrue($"profile '{profile.Name}' should be valid");
        }
    }

    [Fact]
    public void AllProfiles_AreSystemProfiles()
    {
        // Act
        var profiles = ProfileDefaults.GetDefaultProfiles();

        // Assert
        foreach (var profile in profiles)
        {
            profile.IsSystemProfile.Should().BeTrue($"profile '{profile.Name}' should be a system profile");
        }
    }

    [Fact]
    public void AllProfiles_HaveDefaultClientId()
    {
        // Act
        var profiles = ProfileDefaults.GetDefaultProfiles();

        // Assert
        foreach (var profile in profiles)
        {
            profile.DefaultClientId.Should().Be("sorcha-ui", $"profile '{profile.Name}' should use sorcha-ui client ID");
        }
    }

    [Fact]
    public void AllProfiles_HaveDefaultTimeout()
    {
        // Act
        var profiles = ProfileDefaults.GetDefaultProfiles();

        // Assert
        foreach (var profile in profiles)
        {
            profile.TimeoutSeconds.Should().Be(30, $"profile '{profile.Name}' should have 30 second timeout");
        }
    }

    [Fact]
    public void GetDefaultProfile_ReturnsCorrectProfile()
    {
        // Act & Assert
        ProfileDefaults.GetDefaultProfile("local").Should().NotBeNull();
        ProfileDefaults.GetDefaultProfile("docker").Should().NotBeNull();
        ProfileDefaults.GetDefaultProfile("production").Should().NotBeNull();
        ProfileDefaults.GetDefaultProfile("nonexistent").Should().BeNull();
    }

    [Fact]
    public void GetDefaultProfile_IsCaseInsensitive()
    {
        // Act & Assert
        ProfileDefaults.GetDefaultProfile("LOCAL")?.Name.Should().Be("local");
        ProfileDefaults.GetDefaultProfile("Docker")?.Name.Should().Be("docker");
        ProfileDefaults.GetDefaultProfile("PRODUCTION")?.Name.Should().Be("production");
    }

    [Fact]
    public void LocalProfile_UsesHttps()
    {
        // Act
        var profiles = ProfileDefaults.GetDefaultProfiles();
        var localProfile = profiles.First(p => p.Name == "local");

        // Assert - Aspire uses HTTPS with self-signed certs
        localProfile.TenantServiceUrl.Should().StartWith("https://");
        localProfile.GetTenantServiceUrl().Should().StartWith("https://");
        localProfile.AuthTokenUrl.Should().StartWith("https://");
    }

    [Fact]
    public void DockerProfile_UsesSameOriginRequests()
    {
        // Act
        var profiles = ProfileDefaults.GetDefaultProfiles();
        var dockerProfile = profiles.First(p => p.Name == "docker");

        // Assert - Docker uses same-origin relative URLs via API Gateway
        // Empty base URL + null individual URLs = relative URL pattern (e.g., /api/tenant)
        dockerProfile.SorchaServiceUrl.Should().BeEmpty("uses same-origin relative URLs");
        dockerProfile.GetTenantServiceUrl().Should().BeEmpty("resolved URL is empty for relative path handling");
        dockerProfile.IsValid().Should().BeTrue("empty base + null individuals is valid for same-origin");
    }

    [Fact]
    public void ProductionProfile_UsesHttps()
    {
        // Act
        var profiles = ProfileDefaults.GetDefaultProfiles();
        var productionProfile = profiles.First(p => p.Name == "production");

        // Assert - Production must use HTTPS
        productionProfile.SorchaServiceUrl.Should().StartWith("https://");
        productionProfile.GetTenantServiceUrl().Should().StartWith("https://");
    }
}
