// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.Peer.Service.Network;

namespace Sorcha.Peer.Service.Tests.Network;

public class StunClientTests
{
    private readonly Mock<ILogger<StunClient>> _loggerMock;
    private readonly StunClient _client;

    public StunClientTests()
    {
        _loggerMock = new Mock<ILogger<StunClient>>();
        _client = new StunClient(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDependencies()
    {
        // Assert
        _client.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        var act = () => new StunClient(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task QueryAsync_ShouldHandleInvalidServer()
    {
        // Act
        var result = await _client.QueryAsync("invalid-server-that-does-not-exist.com");

        // Assert - Should return null for unreachable servers
        result.Should().BeNull();
    }

    [Fact]
    public async Task QueryAsync_ShouldHandleTimeout()
    {
        // Arrange - Use a non-routable IP to trigger timeout
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act
        var result = await _client.QueryAsync("192.0.2.1:3478", cts.Token); // TEST-NET-1 (RFC 5737)

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DetermineNatTypeAsync_ShouldReturnUnknown_ForInvalidServer()
    {
        // Act
        var natType = await _client.DetermineNatTypeAsync("invalid-server.com");

        // Assert
        natType.Should().Be(NatType.Unknown);
    }

    // Note: Testing against real STUN servers would require network access
    // and would be flaky in CI/CD environments. These tests verify the
    // client handles errors gracefully.
}

public class StunResultTests
{
    [Fact]
    public void StunResult_ShouldInitialize()
    {
        // Act
        var result = new StunResult
        {
            PublicAddress = "203.0.113.1",
            PublicPort = 5001,
            NatType = NatType.FullCone
        };

        // Assert
        result.PublicAddress.Should().Be("203.0.113.1");
        result.PublicPort.Should().Be(5001);
        result.NatType.Should().Be(NatType.FullCone);
    }
}

public class NatTypeTests
{
    [Fact]
    public void NatType_ShouldHaveExpectedValues()
    {
        // Assert - Verify enum values exist
        var unknown = NatType.Unknown;
        var none = NatType.None;
        var fullCone = NatType.FullCone;
        var restrictedCone = NatType.RestrictedCone;
        var portRestricted = NatType.PortRestricted;
        var symmetric = NatType.Symmetric;

        unknown.Should().Be(NatType.Unknown);
        none.Should().Be(NatType.None);
        fullCone.Should().Be(NatType.FullCone);
        restrictedCone.Should().Be(NatType.RestrictedCone);
        portRestricted.Should().Be(NatType.PortRestricted);
        symmetric.Should().Be(NatType.Symmetric);
    }
}
