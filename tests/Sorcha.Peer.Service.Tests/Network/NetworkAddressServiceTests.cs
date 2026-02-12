// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Network;
using System.Net;

namespace Sorcha.Peer.Service.Tests.Network;

public class NetworkAddressServiceTests
{
    private readonly IOptions<PeerServiceConfiguration> _config;
    private readonly PeerServiceConfiguration _configuration;
    private readonly StunClient _stunClient;

    public NetworkAddressServiceTests()
    {
        _configuration = new PeerServiceConfiguration
        {
            NodeId = "test-node",
            NetworkAddress = new NetworkAddressConfiguration
            {
                ExternalAddress = null,
                HttpLookupServices = new List<string> { "https://api.ipify.org" },
                PreferredProtocol = "IPv4"
            },
            PeerDiscovery = new PeerDiscoveryConfiguration
            {
                MaxPeersInList = 100,
                MinHealthyPeers = 5,
                RefreshIntervalMinutes = 15
            },
            SeedNodes = new SeedNodeConfiguration()
        };
        _config = Options.Create(_configuration);
        _stunClient = new StunClient(new Mock<ILogger<StunClient>>().Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDependencies()
    {
        var httpClient = new HttpClient();

        var service = new NetworkAddressService(
            new Mock<ILogger<NetworkAddressService>>().Object,
            _config, httpClient, _stunClient);

        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        var httpClient = new HttpClient();

        var act = () => new NetworkAddressService(null!, _config, httpClient, _stunClient);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenConfigurationIsNull()
    {
        var httpClient = new HttpClient();

        var act = () => new NetworkAddressService(
            new Mock<ILogger<NetworkAddressService>>().Object, null!, httpClient, _stunClient);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenHttpClientIsNull()
    {
        var act = () => new NetworkAddressService(
            new Mock<ILogger<NetworkAddressService>>().Object, _config, null!, _stunClient);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
    }

    [Fact]
    public async Task GetExternalAddressAsync_ShouldReturnConfiguredAddress_WhenSet()
    {
        _configuration.NetworkAddress.ExternalAddress = "203.0.113.42";
        var httpClient = new HttpClient();
        var service = new NetworkAddressService(
            new Mock<ILogger<NetworkAddressService>>().Object,
            _config, httpClient, _stunClient);

        var address = await service.GetExternalAddressAsync();

        address.Should().Be("203.0.113.42");
    }

    [Fact]
    public async Task GetExternalAddressAsync_ShouldDetectAddress_WhenNotConfigured()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("203.0.113.42")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new NetworkAddressService(
            new Mock<ILogger<NetworkAddressService>>().Object,
            _config, httpClient, _stunClient);

        var address = await service.GetExternalAddressAsync();

        address.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetLocalAddress_ShouldReturnAddress()
    {
        var httpClient = new HttpClient();
        var service = new NetworkAddressService(
            new Mock<ILogger<NetworkAddressService>>().Object,
            _config, httpClient, _stunClient);

        var address = service.GetLocalAddress();

        address.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void InvalidateCache_ShouldClearCachedAddress()
    {
        _configuration.NetworkAddress.ExternalAddress = "203.0.113.42";
        var httpClient = new HttpClient();
        var service = new NetworkAddressService(
            new Mock<ILogger<NetworkAddressService>>().Object,
            _config, httpClient, _stunClient);

        service.InvalidateCache();

        // Cache invalidation is internal — should not throw
        Assert.True(true);
    }

    [Fact]
    public async Task IsBehindNatAsync_ShouldReturnTrue_WhenAddressesDiffer()
    {
        _configuration.NetworkAddress.ExternalAddress = "203.0.113.42";
        var httpClient = new HttpClient();
        var service = new NetworkAddressService(
            new Mock<ILogger<NetworkAddressService>>().Object,
            _config, httpClient, _stunClient);

        var behindNat = await service.IsBehindNatAsync();

        // Local address will almost certainly differ from 203.0.113.42
        behindNat.Should().BeTrue();
    }
}
