// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Peer.Service.Core;

namespace Sorcha.Peer.Service.Tests;

public class HubNodeValidatorTests
{
    [Theory]
    [InlineData("n0.sorcha.dev")]
    [InlineData("n1.sorcha.dev")]
    [InlineData("n2.sorcha.dev")]
    public void IsValidHubNodeHostname_ValidHostnames_ReturnsTrue(string hostname)
    {
        // Act
        var result = HubNodeValidator.IsValidHubNodeHostname(hostname);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("n3.sorcha.dev")]
    [InlineData("n0.example.com")]
    [InlineData("random-host")]
    [InlineData("n0.sorcha.dev.extra")]
    [InlineData("xn0.sorcha.dev")]
    [InlineData("N0.sorcha.dev")]
    public void IsValidHubNodeHostname_InvalidHostnames_ReturnsFalse(string hostname)
    {
        // Act
        var result = HubNodeValidator.IsValidHubNodeHostname(hostname);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidHubNodeHostname_NullHostname_ReturnsFalse()
    {
        // Act
        var result = HubNodeValidator.IsValidHubNodeHostname(null);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidHubNodeHostname_EmptyOrWhitespace_ReturnsFalse(string hostname)
    {
        // Act
        var result = HubNodeValidator.IsValidHubNodeHostname(hostname);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("n0.sorcha.dev")]
    [InlineData("n1.sorcha.dev")]
    [InlineData("n2.sorcha.dev")]
    public void ValidateHostname_ValidHostname_DoesNotThrow(string hostname)
    {
        // Act
        var act = () => HubNodeValidator.ValidateHostname(hostname);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("n3.sorcha.dev")]
    [InlineData("random-host")]
    [InlineData("n0.example.com")]
    public void ValidateHostname_InvalidHostname_ThrowsArgumentException(string hostname)
    {
        // Act
        var act = () => HubNodeValidator.ValidateHostname(hostname);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("hostname");
    }

    [Fact]
    public void ValidateHostname_NullHostname_ThrowsArgumentException()
    {
        // Act
        var act = () => HubNodeValidator.ValidateHostname(null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("hostname");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateHostname_EmptyOrWhitespace_ThrowsArgumentException(string hostname)
    {
        // Act
        var act = () => HubNodeValidator.ValidateHostname(hostname);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("hostname");
    }
}
