// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Sorcha.ServiceClients.Did;
using Xunit;

namespace Sorcha.ServiceClients.Tests.Did;

public class WebDidResolverSsrfTests
{
    private static WebDidResolver CreateResolver(bool allowPrivate = false)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DidResolver:AllowPrivateAddresses"] = allowPrivate.ToString()
            })
            .Build();

        return new WebDidResolver(
            new HttpClient(),
            NullLogger<WebDidResolver>.Instance,
            config);
    }

    [Theory]
    [InlineData("did:web:127.0.0.1")]
    [InlineData("did:web:169.254.169.254")]
    [InlineData("did:web:10.0.0.1")]
    [InlineData("did:web:172.16.0.1")]
    [InlineData("did:web:192.168.1.1")]
    [InlineData("did:web:localhost")]
    public async Task ResolveAsync_PrivateAddress_ShouldReturnNull(string did)
    {
        var resolver = CreateResolver(allowPrivate: false);
        var result = await resolver.ResolveAsync(did);
        result.Should().BeNull("SSRF protection should block private/reserved addresses");
    }

    [Fact]
    public void IsPrivateOrReservedAddress_Loopback_ReturnsTrue()
    {
        WebDidResolver.IsPrivateOrReservedAddress(System.Net.IPAddress.Loopback)
            .Should().BeTrue();
    }

    [Fact]
    public void IsPrivateOrReservedAddress_LinkLocal_ReturnsTrue()
    {
        WebDidResolver.IsPrivateOrReservedAddress(System.Net.IPAddress.Parse("169.254.1.1"))
            .Should().BeTrue();
    }

    [Fact]
    public void IsPrivateOrReservedAddress_PublicAddress_ReturnsFalse()
    {
        WebDidResolver.IsPrivateOrReservedAddress(System.Net.IPAddress.Parse("8.8.8.8"))
            .Should().BeFalse();
    }

    [Theory]
    [InlineData("10.0.0.1", true)]
    [InlineData("172.16.0.1", true)]
    [InlineData("172.31.255.255", true)]
    [InlineData("172.32.0.1", false)]
    [InlineData("192.168.0.1", true)]
    [InlineData("8.8.8.8", false)]
    [InlineData("1.1.1.1", false)]
    public void IsPrivateOrReservedAddress_IPv4Ranges(string ip, bool expected)
    {
        WebDidResolver.IsPrivateOrReservedAddress(System.Net.IPAddress.Parse(ip))
            .Should().Be(expected);
    }
}
