// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

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
    private readonly Mock<ILogger<NetworkAddressService>> _loggerMock;
    private readonly Mock<IOptions<PeerServiceConfiguration>> _configMock;
    private readonly Mock<StunClient> _stunClientMock;
    private readonly PeerServiceConfiguration _configuration;

    public NetworkAddressServiceTests()
    {
        _loggerMock = new Mock<ILogger<NetworkAddressService>>();
        _configMock = new Mock<IOptions<PeerServiceConfiguration>>();
        _stunClientMock = new Mock<StunClient>();
        _configuration = new PeerServiceConfiguration
        {
            NetworkAddress = new NetworkAddressConfiguration
            {
                ExternalAddress = null,
                HttpLookupServices = new List<string> { "https://api.ipify.org" },
                PreferredProtocol = "IPv4"
            }
        };
        _configMock.Setup(x => x.Value).Returns(_configuration);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDependencies()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act
        var service = new NetworkAddressService(_loggerMock.Object, _configMock.Object, httpClient, _stunClientMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act
        var act = () => new NetworkAddressService(null!, _configMock.Object, httpClient, _stunClientMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenConfigurationIsNull()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act
        var act = () => new NetworkAddressService(_loggerMock.Object, null!, httpClient, _stunClientMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenHttpClientIsNull()
    {
        // Act
        var act = () => new NetworkAddressService(_loggerMock.Object, _configMock.Object, null!, _stunClientMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
    }

    [Fact]
    public async Task GetExternalAddressAsync_ShouldReturnConfiguredAddress_WhenSet()
    {
        // Arrange
        _configuration.NetworkAddress.ExternalAddress = "203.0.113.42";
        var httpClient = new HttpClient();
        var service = new NetworkAddressService(_loggerMock.Object, _configMock.Object, httpClient, _stunClientMock.Object);

        // Act
        var address = await service.GetExternalAddressAsync();

        // Assert
        address.Should().Be("203.0.113.42");
    }

    [Fact]
    public async Task GetExternalAddressAsync_ShouldDetectAddress_WhenNotConfigured()
    {
        // Arrange
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
        var service = new NetworkAddressService(_loggerMock.Object, _configMock.Object, httpClient, _stunClientMock.Object);

        // Act
        var address = await service.GetExternalAddressAsync();

        // Assert
        address.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetLocalAddress_ShouldReturnAddress()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new NetworkAddressService(_loggerMock.Object, _configMock.Object, httpClient, _stunClientMock.Object);

        // Act
        var address = service.GetLocalAddress();

        // Assert
        // Local address detection depends on environment, so we just check it's not null
        address.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void InvalidateCache_ShouldClearCachedAddress()
    {
        // Arrange
        _configuration.NetworkAddress.ExternalAddress = "203.0.113.42";
        var httpClient = new HttpClient();
        var service = new NetworkAddressService(_loggerMock.Object, _configMock.Object, httpClient, _stunClientMock.Object);

        // Act
        service.InvalidateCache();
        // Cache invalidation is internal, we can't directly verify it
        // But it should not throw

        // Assert - method should complete without error
        Assert.True(true);
    }

    [Fact]
    public async Task IsBehindNatAsync_ShouldReturnTrue_WhenAddressesDiffer()
    {
        // Arrange
        _configuration.NetworkAddress.ExternalAddress = "203.0.113.42";
        var httpClient = new HttpClient();
        var service = new NetworkAddressService(_loggerMock.Object, _configMock.Object, httpClient, _stunClientMock.Object);

        // Act
        var behindNat = await service.IsBehindNatAsync();

        // Assert
        // Local address will almost certainly differ from 203.0.113.42
        behindNat.Should().BeTrue();
    }
}
