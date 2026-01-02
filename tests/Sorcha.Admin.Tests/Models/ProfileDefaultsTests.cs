// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Admin.Models.Configuration;
using Xunit;

namespace Sorcha.Admin.Tests.Models;

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
        profiles.Should().HaveCount(3, "should have docker, local, and production profiles");
        profiles.Should().ContainKey("docker");
        profiles.Should().ContainKey("local");
        profiles.Should().ContainKey("production");
    }

    [Fact]
    public void DockerProfile_HasCorrectConfiguration()
    {
        // Act
        var profiles = ProfileDefaults.GetDefaultProfiles();
        var dockerProfile = profiles["docker"];

        // Assert
        dockerProfile.Name.Should().Be("docker");
        dockerProfile.TenantServiceUrl.Should().Be("http://localhost/api/tenant");
        dockerProfile.RegisterServiceUrl.Should().Be("http://localhost/api/register");
        dockerProfile.PeerServiceUrl.Should().Be("http://localhost/api/peer");
        dockerProfile.WalletServiceUrl.Should().Be("http://localhost/api/wallet");
        dockerProfile.BlueprintServiceUrl.Should().Be("http://localhost/api/blueprint");
        dockerProfile.AuthTokenUrl.Should().Be("http://localhost/api/service-auth/token",
            "should use API Gateway auth endpoint");
        dockerProfile.DefaultClientId.Should().Be("sorcha-admin");
        dockerProfile.VerifySsl.Should().BeFalse("local Docker doesn't use SSL");
        dockerProfile.TimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void LocalProfile_HasCorrectConfiguration()
    {
        // Act
        var profiles = ProfileDefaults.GetDefaultProfiles();
        var localProfile = profiles["local"];

        // Assert
        localProfile.Name.Should().Be("local");
        localProfile.TenantServiceUrl.Should().Be("https://localhost:7110");
        localProfile.RegisterServiceUrl.Should().Be("https://localhost:7290");
        localProfile.PeerServiceUrl.Should().Be("https://localhost:7002");
        localProfile.WalletServiceUrl.Should().Be("https://localhost:7001");
        localProfile.BlueprintServiceUrl.Should().Be("https://localhost:7000");
        localProfile.AuthTokenUrl.Should().Be("https://localhost:7110/api/service-auth/token",
            "should use direct Tenant Service endpoint for Aspire");
        localProfile.DefaultClientId.Should().Be("sorcha-admin");
        localProfile.VerifySsl.Should().BeFalse("local Aspire uses self-signed certificates");
        localProfile.TimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void ProductionProfile_HasCorrectConfiguration()
    {
        // Act
        var profiles = ProfileDefaults.GetDefaultProfiles();
        var productionProfile = profiles["production"];

        // Assert
        productionProfile.Name.Should().Be("production");
        productionProfile.TenantServiceUrl.Should().Be("https://tenant.sorcha.io");
        productionProfile.RegisterServiceUrl.Should().Be("https://register.sorcha.io");
        productionProfile.PeerServiceUrl.Should().Be("https://peer.sorcha.io");
        productionProfile.WalletServiceUrl.Should().Be("https://wallet.sorcha.io");
        productionProfile.BlueprintServiceUrl.Should().Be("https://blueprint.sorcha.io");
        productionProfile.AuthTokenUrl.Should().Be("https://tenant.sorcha.io/api/service-auth/token");
        productionProfile.DefaultClientId.Should().Be("sorcha-admin");
        productionProfile.VerifySsl.Should().BeTrue("production should verify SSL certificates");
        productionProfile.TimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void DockerProfile_AuthTokenUrl_DoesNotIncludeTenantPrefix()
    {
        // This test specifically ensures we don't regress to the wrong URL
        // Act
        var profiles = ProfileDefaults.GetDefaultProfiles();
        var dockerProfile = profiles["docker"];

        // Assert
        dockerProfile.AuthTokenUrl.Should().Be("http://localhost/api/service-auth/token",
            "should NOT be '/api/tenant/service-auth/token'");

        dockerProfile.AuthTokenUrl.Should().NotContain("/api/tenant/service-auth",
            "auth endpoint should be at root /api/service-auth, not under tenant");
    }

    [Fact]
    public void AllProfiles_HaveRequiredFields()
    {
        // Act
        var profiles = ProfileDefaults.GetDefaultProfiles();

        // Assert
        foreach (var kvp in profiles)
        {
            var profile = kvp.Value;
            profile.Name.Should().NotBeNullOrEmpty($"profile {kvp.Key} should have a name");
            profile.TenantServiceUrl.Should().NotBeNullOrEmpty($"profile {kvp.Key} should have TenantServiceUrl");
            profile.RegisterServiceUrl.Should().NotBeNullOrEmpty($"profile {kvp.Key} should have RegisterServiceUrl");
            profile.PeerServiceUrl.Should().NotBeNullOrEmpty($"profile {kvp.Key} should have PeerServiceUrl");
            profile.WalletServiceUrl.Should().NotBeNullOrEmpty($"profile {kvp.Key} should have WalletServiceUrl");
            profile.BlueprintServiceUrl.Should().NotBeNullOrEmpty($"profile {kvp.Key} should have BlueprintServiceUrl");
            profile.AuthTokenUrl.Should().NotBeNullOrEmpty($"profile {kvp.Key} should have AuthTokenUrl");
            profile.DefaultClientId.Should().NotBeNullOrEmpty($"profile {kvp.Key} should have DefaultClientId");
            profile.TimeoutSeconds.Should().BeGreaterThan(0, $"profile {kvp.Key} should have positive timeout");
        }
    }

    [Fact]
    public void DockerProfile_UsesHttpNotHttps()
    {
        // Act
        var profiles = ProfileDefaults.GetDefaultProfiles();
        var dockerProfile = profiles["docker"];

        // Assert
        dockerProfile.TenantServiceUrl.Should().StartWith("http://", "Docker uses HTTP via API Gateway");
        dockerProfile.TenantServiceUrl.Should().NotStartWith("https://");
        dockerProfile.AuthTokenUrl.Should().StartWith("http://");
    }

    [Fact]
    public void LocalProfile_UsesHttps()
    {
        // Act
        var profiles = ProfileDefaults.GetDefaultProfiles();
        var localProfile = profiles["local"];

        // Assert
        localProfile.TenantServiceUrl.Should().StartWith("https://", "Aspire uses HTTPS with self-signed certs");
        localProfile.AuthTokenUrl.Should().StartWith("https://");
    }

    [Fact]
    public void ProductionProfile_UsesHttps()
    {
        // Act
        var profiles = ProfileDefaults.GetDefaultProfiles();
        var productionProfile = profiles["production"];

        // Assert
        productionProfile.TenantServiceUrl.Should().StartWith("https://", "Production must use HTTPS");
        productionProfile.AuthTokenUrl.Should().StartWith("https://");
    }
}
