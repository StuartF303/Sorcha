// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.UI.Core.Models.Configuration;
using Xunit;

namespace Sorcha.UI.Core.Tests.Models.Configuration;

/// <summary>
/// Tests for Profile model with hybrid URL pattern.
/// </summary>
public class ProfileTests
{
    #region URL Resolution Tests

    [Fact]
    public void GetTenantServiceUrl_WithOverride_ReturnsOverrideUrl()
    {
        // Arrange
        var profile = new Profile
        {
            Name = "test",
            SorchaServiceUrl = "http://gateway:80",
            TenantServiceUrl = "https://localhost:7110"
        };

        // Act
        var url = profile.GetTenantServiceUrl();

        // Assert
        url.Should().Be("https://localhost:7110");
    }

    [Fact]
    public void GetTenantServiceUrl_WithoutOverride_DerivesFromBaseUrl()
    {
        // Arrange
        var profile = new Profile
        {
            Name = "test",
            SorchaServiceUrl = "http://localhost:80"
        };

        // Act
        var url = profile.GetTenantServiceUrl();

        // Assert
        url.Should().Be("http://localhost:80/api/tenant");
    }

    [Fact]
    public void GetTenantServiceUrl_WithEmptyBaseUrl_ReturnsEmpty()
    {
        // Arrange
        var profile = new Profile
        {
            Name = "test",
            SorchaServiceUrl = ""
        };

        // Act
        var url = profile.GetTenantServiceUrl();

        // Assert
        url.Should().BeEmpty();
    }

    [Fact]
    public void GetBlueprintServiceUrl_WithOverride_ReturnsOverrideUrl()
    {
        // Arrange
        var profile = new Profile
        {
            Name = "test",
            SorchaServiceUrl = "http://gateway:80",
            BlueprintServiceUrl = "https://localhost:7000"
        };

        // Act
        var url = profile.GetBlueprintServiceUrl();

        // Assert
        url.Should().Be("https://localhost:7000");
    }

    [Fact]
    public void GetBlueprintServiceUrl_WithoutOverride_DerivesFromBaseUrl()
    {
        // Arrange
        var profile = new Profile
        {
            Name = "test",
            SorchaServiceUrl = "https://api.sorcha.io"
        };

        // Act
        var url = profile.GetBlueprintServiceUrl();

        // Assert
        url.Should().Be("https://api.sorcha.io/api/blueprint");
    }

    [Fact]
    public void GetRegisterServiceUrl_DerivesCorrectly()
    {
        // Arrange
        var profile = new Profile
        {
            Name = "test",
            SorchaServiceUrl = "http://localhost:80"
        };

        // Act
        var url = profile.GetRegisterServiceUrl();

        // Assert
        url.Should().Be("http://localhost:80/api/register");
    }

    [Fact]
    public void GetWalletServiceUrl_DerivesCorrectly()
    {
        // Arrange
        var profile = new Profile
        {
            Name = "test",
            SorchaServiceUrl = "http://localhost:80"
        };

        // Act
        var url = profile.GetWalletServiceUrl();

        // Assert
        url.Should().Be("http://localhost:80/api/wallet");
    }

    [Fact]
    public void GetPeerServiceUrl_DerivesCorrectly()
    {
        // Arrange
        var profile = new Profile
        {
            Name = "test",
            SorchaServiceUrl = "http://localhost:80"
        };

        // Act
        var url = profile.GetPeerServiceUrl();

        // Assert
        url.Should().Be("http://localhost:80/api/peer");
    }

    [Fact]
    public void GetAuthTokenUrl_WithOverride_ReturnsOverrideUrl()
    {
        // Arrange
        var profile = new Profile
        {
            Name = "test",
            SorchaServiceUrl = "http://gateway:80",
            AuthTokenUrl = "https://auth.sorcha.io/token"
        };

        // Act
        var url = profile.GetAuthTokenUrl();

        // Assert
        url.Should().Be("https://auth.sorcha.io/token");
    }

    [Fact]
    public void GetAuthTokenUrl_WithoutOverride_UsesDirectGatewayRoute()
    {
        // Arrange - When using gateway (no explicit TenantServiceUrl),
        // use the direct /api/service-auth/token route
        var profile = new Profile
        {
            Name = "test",
            SorchaServiceUrl = "http://localhost:80"
        };

        // Act
        var url = profile.GetAuthTokenUrl();

        // Assert - Uses gateway's direct route, not tenant prefix
        url.Should().Be("http://localhost:80/api/service-auth/token");
    }

    [Fact]
    public void GetAuthTokenUrl_WithExplicitTenantServiceUrl_DerivesFromTenantService()
    {
        // Arrange - When TenantServiceUrl is explicitly set (e.g., Aspire),
        // derive auth URL from it
        var profile = new Profile
        {
            Name = "test",
            SorchaServiceUrl = "http://gateway:80",
            TenantServiceUrl = "https://localhost:7110"
        };

        // Act
        var url = profile.GetAuthTokenUrl();

        // Assert
        url.Should().Be("https://localhost:7110/api/service-auth/token");
    }

    [Fact]
    public void UrlResolution_TrimsTrailingSlashes()
    {
        // Arrange
        var profile = new Profile
        {
            Name = "test",
            SorchaServiceUrl = "http://localhost:80/",
            TenantServiceUrl = "https://localhost:7110/"
        };

        // Act & Assert
        profile.GetTenantServiceUrl().Should().Be("https://localhost:7110");
        profile.GetBlueprintServiceUrl().Should().Be("http://localhost:80/api/blueprint");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void IsValid_WithBaseUrl_ReturnsTrue()
    {
        // Arrange
        var profile = new Profile
        {
            Name = "test",
            SorchaServiceUrl = "http://localhost:80"
        };

        // Act & Assert
        profile.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithAllServiceUrls_ReturnsTrue()
    {
        // Arrange
        var profile = new Profile
        {
            Name = "test",
            SorchaServiceUrl = "", // No base URL
            TenantServiceUrl = "https://localhost:7110",
            RegisterServiceUrl = "https://localhost:7290",
            BlueprintServiceUrl = "https://localhost:7000",
            WalletServiceUrl = "https://localhost:7001",
            PeerServiceUrl = "https://localhost:7002"
        };

        // Act & Assert
        profile.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithoutName_ReturnsFalse()
    {
        // Arrange
        var profile = new Profile
        {
            Name = "",
            SorchaServiceUrl = "http://localhost:80"
        };

        // Act & Assert
        profile.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithInvalidBaseUrl_ReturnsFalse()
    {
        // Arrange
        var profile = new Profile
        {
            Name = "test",
            SorchaServiceUrl = "not-a-valid-url"
        };

        // Act & Assert
        profile.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithPartialServiceUrls_ReturnsFalse()
    {
        // Arrange - has base URL empty and only some service URLs
        var profile = new Profile
        {
            Name = "test",
            SorchaServiceUrl = "",
            TenantServiceUrl = "https://localhost:7110",
            RegisterServiceUrl = "https://localhost:7290"
            // Missing Blueprint, Wallet, Peer URLs
        };

        // Act & Assert
        profile.IsValid().Should().BeFalse();
    }

    #endregion

    #region Record Behavior Tests

    [Fact]
    public void WithUpdatedTimestamp_CreatesNewInstanceWithUpdatedTime()
    {
        // Arrange
        var originalTime = DateTime.UtcNow.AddHours(-1);
        var profile = new Profile
        {
            Name = "test",
            SorchaServiceUrl = "http://localhost:80",
            CreatedAt = originalTime,
            UpdatedAt = originalTime
        };

        // Act
        var updated = profile.WithUpdatedTimestamp();

        // Assert
        updated.Should().NotBeSameAs(profile);
        updated.Name.Should().Be(profile.Name);
        updated.SorchaServiceUrl.Should().Be(profile.SorchaServiceUrl);
        updated.CreatedAt.Should().Be(originalTime);
        updated.UpdatedAt.Should().BeAfter(originalTime);
    }

    [Fact]
    public void Profile_HasCorrectDefaults()
    {
        // Arrange & Act
        var profile = new Profile { Name = "test" };

        // Assert
        profile.SorchaServiceUrl.Should().BeEmpty();
        profile.TenantServiceUrl.Should().BeNull();
        profile.RegisterServiceUrl.Should().BeNull();
        profile.BlueprintServiceUrl.Should().BeNull();
        profile.WalletServiceUrl.Should().BeNull();
        profile.PeerServiceUrl.Should().BeNull();
        profile.AuthTokenUrl.Should().BeNull();
        profile.DefaultClientId.Should().Be("sorcha-ui");
        profile.VerifySsl.Should().BeTrue();
        profile.TimeoutSeconds.Should().Be(30);
        profile.IsSystemProfile.Should().BeFalse();
    }

    #endregion
}
