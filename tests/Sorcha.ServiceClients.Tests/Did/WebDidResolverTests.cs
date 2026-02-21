// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Sorcha.ServiceClients.Did;

namespace Sorcha.ServiceClients.Tests.Did;

public class WebDidResolverTests
{
    private readonly Mock<ILogger<WebDidResolver>> _loggerMock = new();
    private readonly Mock<HttpMessageHandler> _httpHandlerMock = new();
    private readonly WebDidResolver _resolver;

    public WebDidResolverTests()
    {
        var httpClient = new HttpClient(_httpHandlerMock.Object);
        _resolver = new WebDidResolver(httpClient, _loggerMock.Object);
    }

    [Fact]
    public void CanResolve_Web_ReturnsTrue()
    {
        _resolver.CanResolve("web").Should().BeTrue();
    }

    [Fact]
    public void CanResolve_OtherMethod_ReturnsFalse()
    {
        _resolver.CanResolve("sorcha").Should().BeFalse();
        _resolver.CanResolve("key").Should().BeFalse();
    }

    [Fact]
    public async Task ResolveAsync_ValidDomain_ReturnsDidDocument()
    {
        var expectedDoc = new DidDocument
        {
            Id = "did:web:example.com",
            VerificationMethod =
            [
                new VerificationMethod
                {
                    Id = "did:web:example.com#key-1",
                    Type = "Ed25519VerificationKey2020",
                    Controller = "did:web:example.com"
                }
            ]
        };

        SetupHttpResponse(
            "https://example.com/.well-known/did.json",
            HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedDoc));

        var doc = await _resolver.ResolveAsync("did:web:example.com");

        doc.Should().NotBeNull();
        doc!.Id.Should().Be("did:web:example.com");
        doc.VerificationMethod.Should().HaveCount(1);
    }

    [Fact]
    public async Task ResolveAsync_PathBased_FetchesCorrectUrl()
    {
        var expectedDoc = new DidDocument { Id = "did:web:example.com:users:alice" };

        SetupHttpResponse(
            "https://example.com/users/alice/did.json",
            HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedDoc));

        var doc = await _resolver.ResolveAsync("did:web:example.com:users:alice");

        doc.Should().NotBeNull();
        doc!.Id.Should().Be("did:web:example.com:users:alice");
    }

    [Fact]
    public async Task ResolveAsync_NotFound_ReturnsNull()
    {
        SetupHttpResponse(
            "https://missing.example/.well-known/did.json",
            HttpStatusCode.NotFound,
            "");

        var doc = await _resolver.ResolveAsync("did:web:missing.example");

        doc.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_InvalidJson_ReturnsNull()
    {
        SetupHttpResponse(
            "https://invalid.example/.well-known/did.json",
            HttpStatusCode.OK,
            "not valid json {{{");

        var doc = await _resolver.ResolveAsync("did:web:invalid.example");

        doc.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_IdMismatch_ReturnsNull()
    {
        var wrongDoc = new DidDocument { Id = "did:web:other.com" };

        SetupHttpResponse(
            "https://example.com/.well-known/did.json",
            HttpStatusCode.OK,
            JsonSerializer.Serialize(wrongDoc));

        var doc = await _resolver.ResolveAsync("did:web:example.com");

        doc.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_EmptyDid_ReturnsNull()
    {
        var doc = await _resolver.ResolveAsync("");
        doc.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_TooFewParts_ReturnsNull()
    {
        var doc = await _resolver.ResolveAsync("did:web");
        doc.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_NetworkError_ReturnsNull()
    {
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var doc = await _resolver.ResolveAsync("did:web:unreachable.example");

        doc.Should().BeNull();
    }

    private void SetupHttpResponse(string expectedUrl, HttpStatusCode statusCode, string content)
    {
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m =>
                    m.RequestUri != null && m.RequestUri.ToString() == expectedUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
    }
}
