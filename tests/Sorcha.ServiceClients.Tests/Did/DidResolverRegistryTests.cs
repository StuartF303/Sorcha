// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.Logging;
using Sorcha.ServiceClients.Did;

namespace Sorcha.ServiceClients.Tests.Did;

public class DidResolverRegistryTests
{
    private readonly Mock<ILogger<DidResolverRegistry>> _loggerMock = new();
    private readonly DidResolverRegistry _registry;

    public DidResolverRegistryTests()
    {
        _registry = new DidResolverRegistry(_loggerMock.Object);
    }

    [Fact]
    public async Task ResolveAsync_RegisteredMethod_DelegatesToResolver()
    {
        var resolverMock = new Mock<IDidResolver>();
        resolverMock.Setup(r => r.CanResolve("test")).Returns(true);
        resolverMock
            .Setup(r => r.ResolveAsync("did:test:123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DidDocument { Id = "did:test:123" });

        _registry.Register(resolverMock.Object);

        var doc = await _registry.ResolveAsync("did:test:123");

        doc.Should().NotBeNull();
        doc!.Id.Should().Be("did:test:123");
        resolverMock.Verify(r => r.ResolveAsync("did:test:123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_UnregisteredMethod_ReturnsNull()
    {
        var doc = await _registry.ResolveAsync("did:unknown:something");

        doc.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_MultipleResolvers_DispatchesCorrectly()
    {
        var sorchaResolverMock = new Mock<IDidResolver>();
        sorchaResolverMock.Setup(r => r.CanResolve("sorcha")).Returns(true);
        sorchaResolverMock
            .Setup(r => r.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DidDocument { Id = "did:sorcha:w:addr" });

        var webResolverMock = new Mock<IDidResolver>();
        webResolverMock.Setup(r => r.CanResolve("web")).Returns(true);
        webResolverMock
            .Setup(r => r.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DidDocument { Id = "did:web:example.com" });

        _registry.Register(sorchaResolverMock.Object);
        _registry.Register(webResolverMock.Object);

        var sorchaDoc = await _registry.ResolveAsync("did:sorcha:w:addr");
        var webDoc = await _registry.ResolveAsync("did:web:example.com");

        sorchaDoc!.Id.Should().Be("did:sorcha:w:addr");
        webDoc!.Id.Should().Be("did:web:example.com");
    }

    [Fact]
    public async Task ResolveAsync_InvalidDidFormat_ReturnsNull()
    {
        var doc = await _registry.ResolveAsync("not-a-did");
        doc.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_EmptyDid_ReturnsNull()
    {
        var doc = await _registry.ResolveAsync("");
        doc.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_NullDid_ReturnsNull()
    {
        var doc = await _registry.ResolveAsync(null!);
        doc.Should().BeNull();
    }

    [Fact]
    public void Register_MultipleResolversForSameMethod_LastWins()
    {
        var resolver1 = new Mock<IDidResolver>();
        resolver1.Setup(r => r.CanResolve("test")).Returns(true);

        var resolver2 = new Mock<IDidResolver>();
        resolver2.Setup(r => r.CanResolve("test")).Returns(true);

        // Should not throw
        _registry.Register(resolver1.Object);
        _registry.Register(resolver2.Object);
    }
}
