// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Peer.Service.Core;

namespace Sorcha.Peer.Service.Tests;

public class SystemRegisterValidatorTests
{
    [Fact]
    public void SystemRegisterId_EqualsGuidEmpty()
    {
        // Assert
        SystemRegisterValidator.SystemRegisterId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void IsSystemRegister_GuidEmpty_ReturnsTrue()
    {
        // Act
        var result = SystemRegisterValidator.IsSystemRegister(Guid.Empty);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSystemRegister_RandomGuid_ReturnsFalse()
    {
        // Act
        var result = SystemRegisterValidator.IsSystemRegister(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateSystemRegisterId_GuidEmpty_DoesNotThrow()
    {
        // Act
        var act = () => SystemRegisterValidator.ValidateSystemRegisterId(Guid.Empty);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateSystemRegisterId_RandomGuid_ThrowsArgumentException()
    {
        // Arrange
        var randomId = Guid.NewGuid();

        // Act
        var act = () => SystemRegisterValidator.ValidateSystemRegisterId(randomId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("registerId");
    }

    [Fact]
    public void SystemRegisterId_MatchesPeerServiceConstants()
    {
        // Assert
        SystemRegisterValidator.SystemRegisterId.Should().Be(PeerServiceConstants.SystemRegisterId);
    }
}
