// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Peer.Service.Core;

namespace Sorcha.Peer.Service.Tests.Core;

public class SeedNodeConfigurationTests
{
    [Fact]
    public void Constructor_ShouldInitializeEmptySeedNodes()
    {
        var config = new SeedNodeConfiguration();

        config.SeedNodes.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void SeedNodes_ShouldAcceptMultipleEndpoints()
    {
        var config = new SeedNodeConfiguration
        {
            SeedNodes =
            [
                new SeedNodeEndpoint { NodeId = "seed1", Hostname = "seed1.example.com", Port = 5000 },
                new SeedNodeEndpoint { NodeId = "seed2", Hostname = "seed2.example.com", Port = 5000 }
            ]
        };

        config.SeedNodes.Should().HaveCount(2);
    }
}

public class SeedNodeEndpointTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        var endpoint = new SeedNodeEndpoint();

        endpoint.NodeId.Should().BeEmpty();
        endpoint.Hostname.Should().BeEmpty();
        endpoint.Port.Should().Be(5000);
        endpoint.EnableTls.Should().BeFalse();
    }

    [Fact]
    public void GrpcChannelAddress_ShouldReturnHttpByDefault()
    {
        var endpoint = new SeedNodeEndpoint
        {
            Hostname = "seed1.example.com",
            Port = 5000
        };

        endpoint.GrpcChannelAddress.Should().Be("http://seed1.example.com:5000");
    }

    [Fact]
    public void GrpcChannelAddress_ShouldReturnHttpsWhenTlsEnabled()
    {
        var endpoint = new SeedNodeEndpoint
        {
            Hostname = "seed1.example.com",
            Port = 5000,
            EnableTls = true
        };

        endpoint.GrpcChannelAddress.Should().Be("https://seed1.example.com:5000");
    }

    [Theory]
    [InlineData("10.0.0.1", 5000, false, "http://10.0.0.1:5000")]
    [InlineData("seed.sorcha.dev", 443, true, "https://seed.sorcha.dev:443")]
    [InlineData("localhost", 8080, false, "http://localhost:8080")]
    public void GrpcChannelAddress_ShouldFormatCorrectly(string hostname, int port, bool tls, string expected)
    {
        var endpoint = new SeedNodeEndpoint
        {
            Hostname = hostname,
            Port = port,
            EnableTls = tls
        };

        endpoint.GrpcChannelAddress.Should().Be(expected);
    }
}
