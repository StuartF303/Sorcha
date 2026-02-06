// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Peer.Service.Core;

namespace Sorcha.Peer.Service.Tests;

public class HubNodeInfoTests
{
    [Theory]
    [InlineData("n0.sorcha.dev")]
    [InlineData("n1.sorcha.dev")]
    [InlineData("n2.sorcha.dev")]
    public void IsValidHostname_ValidHostnames_ReturnsTrue(string hostname)
    {
        // Act
        var result = HubNodeInfo.IsValidHostname(hostname);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("n3.sorcha.dev")]
    [InlineData("random-host")]
    [InlineData("")]
    public void IsValidHostname_InvalidHostnames_ReturnsFalse(string hostname)
    {
        // Act
        var result = HubNodeInfo.IsValidHostname(hostname);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidHostname_NullHostname_ReturnsFalse()
    {
        // Act
        var result = HubNodeInfo.IsValidHostname(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ResetConnectionState_ClearsFailuresAndSetsConnected()
    {
        // Arrange
        var node = new HubNodeInfo
        {
            NodeId = "n0.sorcha.dev",
            Hostname = "n0.sorcha.dev",
            ConsecutiveFailures = 5,
            ConnectionStatus = HubNodeConnectionStatus.Failed
        };

        // Act
        node.ResetConnectionState();

        // Assert
        node.ConsecutiveFailures.Should().Be(0);
        node.ConnectionStatus.Should().Be(HubNodeConnectionStatus.Connected);
        node.LastSuccessfulConnection.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void RecordFailure_IncrementsConsecutiveFailures()
    {
        // Arrange
        var node = new HubNodeInfo
        {
            NodeId = "n0.sorcha.dev",
            Hostname = "n0.sorcha.dev",
            ConsecutiveFailures = 0
        };

        // Act
        node.RecordFailure();

        // Assert
        node.ConsecutiveFailures.Should().Be(1);
    }

    [Fact]
    public void RecordFailure_SetsStatusToFailed()
    {
        // Arrange
        var node = new HubNodeInfo
        {
            NodeId = "n0.sorcha.dev",
            Hostname = "n0.sorcha.dev",
            ConnectionStatus = HubNodeConnectionStatus.Connected
        };

        // Act
        node.RecordFailure();

        // Assert
        node.ConnectionStatus.Should().Be(HubNodeConnectionStatus.Failed);
    }

    [Fact]
    public void RecordFailure_SetsLastConnectionAttempt()
    {
        // Arrange
        var node = new HubNodeInfo
        {
            NodeId = "n0.sorcha.dev",
            Hostname = "n0.sorcha.dev"
        };

        // Act
        node.RecordFailure();

        // Assert
        node.LastConnectionAttempt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void RecordFailure_MultipleTimes_AccumulatesFailures()
    {
        // Arrange
        var node = new HubNodeInfo
        {
            NodeId = "n0.sorcha.dev",
            Hostname = "n0.sorcha.dev"
        };

        // Act
        node.RecordFailure();
        node.RecordFailure();
        node.RecordFailure();

        // Assert
        node.ConsecutiveFailures.Should().Be(3);
    }

    [Fact]
    public void DefaultPort_Is5000()
    {
        // Arrange & Act
        var node = new HubNodeInfo();

        // Assert
        node.Port.Should().Be(5000);
    }

    [Fact]
    public void DefaultEnableTls_IsFalse()
    {
        // Arrange & Act
        var node = new HubNodeInfo();

        // Assert
        node.EnableTls.Should().BeFalse();
    }

    [Fact]
    public void DefaultIsActive_IsFalse()
    {
        // Arrange & Act
        var node = new HubNodeInfo();

        // Assert
        node.IsActive.Should().BeFalse();
    }

    [Fact]
    public void DefaultConnectionStatus_IsDisconnected()
    {
        // Arrange & Act
        var node = new HubNodeInfo();

        // Assert
        node.ConnectionStatus.Should().Be(HubNodeConnectionStatus.Disconnected);
    }

    [Fact]
    public void DefaultConsecutiveFailures_IsZero()
    {
        // Arrange & Act
        var node = new HubNodeInfo();

        // Assert
        node.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public void GrpcChannelAddress_WithoutTls_ReturnsHttpAddress()
    {
        // Arrange
        var node = new HubNodeInfo
        {
            Hostname = "n0.sorcha.dev",
            Port = 5000,
            EnableTls = false
        };

        // Act & Assert
        node.GrpcChannelAddress.Should().Be("http://n0.sorcha.dev:5000");
    }

    [Fact]
    public void GrpcChannelAddress_WithTls_ReturnsHttpsAddress()
    {
        // Arrange
        var node = new HubNodeInfo
        {
            Hostname = "n0.sorcha.dev",
            Port = 5000,
            EnableTls = true
        };

        // Act & Assert
        node.GrpcChannelAddress.Should().Be("https://n0.sorcha.dev:5000");
    }
}
