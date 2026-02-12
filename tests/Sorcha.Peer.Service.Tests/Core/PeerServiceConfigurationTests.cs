// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Peer.Service.Core;

namespace Sorcha.Peer.Service.Tests.Core;

public class PeerServiceConfigurationTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var config = new PeerServiceConfiguration();

        // Assert
        config.Enabled.Should().BeTrue();
        config.NodeId.Should().BeNull();
        config.ListenPort.Should().Be(5001);
        config.NetworkAddress.Should().NotBeNull();
        config.PeerDiscovery.Should().NotBeNull();
        config.Communication.Should().NotBeNull();
        config.TransactionDistribution.Should().NotBeNull();
        config.OfflineMode.Should().NotBeNull();
    }

    [Fact]
    public void Enabled_ShouldBeSettable()
    {
        // Arrange
        var config = new PeerServiceConfiguration();

        // Act
        config.Enabled = false;

        // Assert
        config.Enabled.Should().BeFalse();
    }

    [Fact]
    public void NodeId_ShouldBeSettable()
    {
        // Arrange
        var config = new PeerServiceConfiguration();

        // Act
        config.NodeId = "custom-node-id";

        // Assert
        config.NodeId.Should().Be("custom-node-id");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5001)]
    [InlineData(65535)]
    public void ListenPort_ShouldAcceptValidPorts(int port)
    {
        // Arrange
        var config = new PeerServiceConfiguration();

        // Act
        config.ListenPort = port;

        // Assert
        config.ListenPort.Should().Be(port);
    }
}

public class NetworkAddressConfigurationTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var config = new NetworkAddressConfiguration();

        // Assert
        config.ExternalAddress.Should().BeNull();
        config.StunServers.Should().ContainSingle().Which.Should().Be("stun.l.google.com:19302");
        config.HttpLookupServices.Should().ContainSingle().Which.Should().Be("https://api.ipify.org");
        config.PreferredProtocol.Should().Be("IPv4");
    }

    [Fact]
    public void ExternalAddress_ShouldBeSettable()
    {
        // Arrange
        var config = new NetworkAddressConfiguration();

        // Act
        config.ExternalAddress = "203.0.113.42";

        // Assert
        config.ExternalAddress.Should().Be("203.0.113.42");
    }

    [Fact]
    public void StunServers_ShouldBeModifiable()
    {
        // Arrange
        var config = new NetworkAddressConfiguration();

        // Act
        config.StunServers.Add("stun.example.com:3478");

        // Assert
        config.StunServers.Should().HaveCount(2)
            .And.Contain("stun.l.google.com:19302")
            .And.Contain("stun.example.com:3478");
    }

    [Fact]
    public void HttpLookupServices_ShouldBeModifiable()
    {
        // Arrange
        var config = new NetworkAddressConfiguration();

        // Act
        config.HttpLookupServices.Add("https://checkip.amazonaws.com");

        // Assert
        config.HttpLookupServices.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("IPv4")]
    [InlineData("IPv6")]
    public void PreferredProtocol_ShouldAcceptIPv4OrIPv6(string protocol)
    {
        // Arrange
        var config = new NetworkAddressConfiguration();

        // Act
        config.PreferredProtocol = protocol;

        // Assert
        config.PreferredProtocol.Should().Be(protocol);
    }
}

public class PeerDiscoveryConfigurationTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var config = new PeerDiscoveryConfiguration();

        // Assert
        config.BootstrapNodes.Should().NotBeNull().And.BeEmpty();
        config.RefreshIntervalMinutes.Should().Be(15);
        config.MaxPeersInList.Should().Be(1000);
        config.MinHealthyPeers.Should().Be(5);
        config.PeerTimeoutSeconds.Should().Be(30);
        config.MaxConcurrentDiscoveries.Should().Be(10);
    }

    [Fact]
    public void BootstrapNodes_ShouldBeModifiable()
    {
        // Arrange
        var config = new PeerDiscoveryConfiguration();

        // Act
        config.BootstrapNodes.Add("bootstrap1.sorcha.dev:5001");
        config.BootstrapNodes.Add("bootstrap2.sorcha.dev:5001");

        // Assert
        config.BootstrapNodes.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(60)]
    [InlineData(1440)]
    public void RefreshIntervalMinutes_ShouldAcceptValidValues(int minutes)
    {
        // Arrange
        var config = new PeerDiscoveryConfiguration();

        // Act
        config.RefreshIntervalMinutes = minutes;

        // Assert
        config.RefreshIntervalMinutes.Should().Be(minutes);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void MaxPeersInList_ShouldAcceptValidValues(int maxPeers)
    {
        // Arrange
        var config = new PeerDiscoveryConfiguration();

        // Act
        config.MaxPeersInList = maxPeers;

        // Assert
        config.MaxPeersInList.Should().Be(maxPeers);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(20)]
    public void MinHealthyPeers_ShouldAcceptValidValues(int minPeers)
    {
        // Arrange
        var config = new PeerDiscoveryConfiguration();

        // Act
        config.MinHealthyPeers = minPeers;

        // Assert
        config.MinHealthyPeers.Should().Be(minPeers);
    }
}

public class CommunicationConfigurationTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var config = new CommunicationConfiguration();

        // Assert
        config.PreferredProtocol.Should().Be("GrpcStream");
        config.ConnectionTimeout.Should().Be(30);
        config.MaxRetries.Should().Be(3);
        config.RetryDelaySeconds.Should().Be(5);
        config.CircuitBreakerThreshold.Should().Be(5);
        config.CircuitBreakerResetMinutes.Should().Be(5);
    }

    [Theory]
    [InlineData("GrpcStream")]
    [InlineData("Grpc")]
    [InlineData("Rest")]
    public void PreferredProtocol_ShouldAcceptValidProtocols(string protocol)
    {
        // Arrange
        var config = new CommunicationConfiguration();

        // Act
        config.PreferredProtocol = protocol;

        // Assert
        config.PreferredProtocol.Should().Be(protocol);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(120)]
    public void ConnectionTimeout_ShouldAcceptValidValues(int timeout)
    {
        // Arrange
        var config = new CommunicationConfiguration();

        // Act
        config.ConnectionTimeout = timeout;

        // Assert
        config.ConnectionTimeout.Should().Be(timeout);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void MaxRetries_ShouldAcceptValidValues(int retries)
    {
        // Arrange
        var config = new CommunicationConfiguration();

        // Act
        config.MaxRetries = retries;

        // Assert
        config.MaxRetries.Should().Be(retries);
    }
}

public class TransactionDistributionConfigurationTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var config = new TransactionDistributionConfiguration();

        // Assert
        config.FanoutFactor.Should().Be(3);
        config.GossipRounds.Should().Be(3);
        config.TransactionCacheTTL.Should().Be(3600);
        config.MaxTransactionSize.Should().Be(10 * 1024 * 1024);
        config.StreamingThreshold.Should().Be(1024 * 1024);
        config.EnableCompression.Should().BeTrue();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(20)]
    public void FanoutFactor_ShouldAcceptValidValues(int fanout)
    {
        // Arrange
        var config = new TransactionDistributionConfiguration();

        // Act
        config.FanoutFactor = fanout;

        // Assert
        config.FanoutFactor.Should().Be(fanout);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void GossipRounds_ShouldAcceptValidValues(int rounds)
    {
        // Arrange
        var config = new TransactionDistributionConfiguration();

        // Act
        config.GossipRounds = rounds;

        // Assert
        config.GossipRounds.Should().Be(rounds);
    }

    [Fact]
    public void EnableCompression_ShouldBeSettable()
    {
        // Arrange
        var config = new TransactionDistributionConfiguration();

        // Act
        config.EnableCompression = false;

        // Assert
        config.EnableCompression.Should().BeFalse();
    }
}

public class OfflineModeConfigurationTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var config = new OfflineModeConfiguration();

        // Assert
        config.MaxQueueSize.Should().Be(10000);
        config.MaxRetries.Should().Be(5);
        config.QueuePersistence.Should().BeTrue();
        config.PersistencePath.Should().Be("./data/tx_queue.db");
    }

    [Theory]
    [InlineData(100)]
    [InlineData(10000)]
    [InlineData(100000)]
    public void MaxQueueSize_ShouldAcceptValidValues(int size)
    {
        // Arrange
        var config = new OfflineModeConfiguration();

        // Act
        config.MaxQueueSize = size;

        // Assert
        config.MaxQueueSize.Should().Be(size);
    }

    [Fact]
    public void QueuePersistence_ShouldBeSettable()
    {
        // Arrange
        var config = new OfflineModeConfiguration();

        // Act
        config.QueuePersistence = false;

        // Assert
        config.QueuePersistence.Should().BeFalse();
    }

    [Fact]
    public void PersistencePath_ShouldBeSettable()
    {
        // Arrange
        var config = new OfflineModeConfiguration();

        // Act
        config.PersistencePath = "/custom/path/queue.db";

        // Assert
        config.PersistencePath.Should().Be("/custom/path/queue.db");
    }
}
