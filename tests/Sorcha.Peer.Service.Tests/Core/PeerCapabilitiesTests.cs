// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Peer.Service.Core;

namespace Sorcha.Peer.Service.Tests.Core;

public class PeerCapabilitiesTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var capabilities = new PeerCapabilities();

        // Assert
        capabilities.SupportsStreaming.Should().BeTrue();
        capabilities.SupportsTransactionDistribution.Should().BeTrue();
        capabilities.MaxTransactionSize.Should().Be(10 * 1024 * 1024); // 10 MB
    }

    [Fact]
    public void Constructor_ShouldAllowPropertyInitialization()
    {
        // Arrange & Act
        var capabilities = new PeerCapabilities
        {
            SupportsStreaming = false,
            SupportsTransactionDistribution = false,
            MaxTransactionSize = 5 * 1024 * 1024
        };

        // Assert
        capabilities.SupportsStreaming.Should().BeFalse();
        capabilities.SupportsTransactionDistribution.Should().BeFalse();
        capabilities.MaxTransactionSize.Should().Be(5 * 1024 * 1024);
    }

    [Theory]
    [InlineData(1024)]
    [InlineData(1024 * 1024)]
    [InlineData(10 * 1024 * 1024)]
    [InlineData(100 * 1024 * 1024)]
    public void MaxTransactionSize_ShouldAcceptVariousSizes(int size)
    {
        // Arrange & Act
        var capabilities = new PeerCapabilities { MaxTransactionSize = size };

        // Assert
        capabilities.MaxTransactionSize.Should().Be(size);
    }

    [Fact]
    public void Properties_ShouldBeIndependentlyMutable()
    {
        // Arrange
        var capabilities = new PeerCapabilities();

        // Act
        capabilities.SupportsStreaming = false;

        // Assert
        capabilities.SupportsStreaming.Should().BeFalse();
        capabilities.SupportsTransactionDistribution.Should().BeTrue(); // Other properties unchanged
        capabilities.MaxTransactionSize.Should().Be(10 * 1024 * 1024);
    }
}
