// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.Logging;
using SimpleBase;
using Sorcha.ServiceClients.Did;

namespace Sorcha.ServiceClients.Tests.Did;

public class KeyDidResolverTests
{
    private readonly Mock<ILogger<KeyDidResolver>> _loggerMock = new();
    private readonly KeyDidResolver _resolver;

    public KeyDidResolverTests()
    {
        _resolver = new KeyDidResolver(_loggerMock.Object);
    }

    [Fact]
    public void CanResolve_Key_ReturnsTrue()
    {
        _resolver.CanResolve("key").Should().BeTrue();
    }

    [Fact]
    public void CanResolve_OtherMethod_ReturnsFalse()
    {
        _resolver.CanResolve("sorcha").Should().BeFalse();
        _resolver.CanResolve("web").Should().BeFalse();
    }

    [Fact]
    public async Task ResolveAsync_Ed25519Key_ReturnsDidDocument()
    {
        // Build a valid ED25519 did:key
        // Multicodec: 0xed01 + 32 bytes of key
        var keyBytes = new byte[32];
        Array.Fill(keyBytes, (byte)0xAB);
        var encoded = new byte[] { 0xed, 0x01 }.Concat(keyBytes).ToArray();
        var multibase = "z" + Base58.Bitcoin.Encode(encoded);
        var did = $"did:key:{multibase}";

        var doc = await _resolver.ResolveAsync(did);

        doc.Should().NotBeNull();
        doc!.Id.Should().Be(did);
        doc.VerificationMethod.Should().HaveCount(1);
        doc.VerificationMethod[0].Type.Should().Be("Ed25519VerificationKey2020");
        doc.VerificationMethod[0].PublicKeyMultibase.Should().Be(multibase);
        doc.Authentication.Should().HaveCount(1);
        doc.AssertionMethod.Should().HaveCount(1);
    }

    [Fact]
    public async Task ResolveAsync_P256Key_ReturnsJsonWebKey2020()
    {
        // Build a valid P-256 did:key
        // Multicodec: 0x1200 + 33 bytes of compressed key
        var keyBytes = new byte[33];
        keyBytes[0] = 0x02; // Compressed point prefix
        Array.Fill(keyBytes, (byte)0xCD, 1, 32);
        var encoded = new byte[] { 0x12, 0x00 }.Concat(keyBytes).ToArray();
        var multibase = "z" + Base58.Bitcoin.Encode(encoded);
        var did = $"did:key:{multibase}";

        var doc = await _resolver.ResolveAsync(did);

        doc.Should().NotBeNull();
        doc!.Id.Should().Be(did);
        doc.VerificationMethod[0].Type.Should().Be("JsonWebKey2020");
    }

    [Fact]
    public async Task ResolveAsync_InvalidMultibasePrefix_ReturnsNull()
    {
        var doc = await _resolver.ResolveAsync("did:key:f0123456789abcdef");
        doc.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_TooShortKey_ReturnsNull()
    {
        var doc = await _resolver.ResolveAsync("did:key:z1");
        doc.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_EmptyDid_ReturnsNull()
    {
        var doc = await _resolver.ResolveAsync("");
        doc.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WrongPrefix_ReturnsNull()
    {
        var doc = await _resolver.ResolveAsync("did:web:example.com");
        doc.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_UnsupportedMulticodec_ReturnsNull()
    {
        // Unknown multicodec prefix 0xFF01 + some bytes
        var keyBytes = new byte[32];
        var encoded = new byte[] { 0xFF, 0x01 }.Concat(keyBytes).ToArray();
        var multibase = "z" + Base58.Bitcoin.Encode(encoded);
        var did = $"did:key:{multibase}";

        var doc = await _resolver.ResolveAsync(did);
        doc.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_Ed25519WrongKeyLength_ReturnsNull()
    {
        // ED25519 multicodec but only 16 bytes of key (should be 32)
        var shortKey = new byte[16];
        var encoded = new byte[] { 0xed, 0x01 }.Concat(shortKey).ToArray();
        var multibase = "z" + Base58.Bitcoin.Encode(encoded);
        var did = $"did:key:{multibase}";

        var doc = await _resolver.ResolveAsync(did);
        doc.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_NoNetworkCall_SynchronousExecution()
    {
        // Verify that did:key resolution is purely local (no async I/O)
        var keyBytes = new byte[32];
        var encoded = new byte[] { 0xed, 0x01 }.Concat(keyBytes).ToArray();
        var multibase = "z" + Base58.Bitcoin.Encode(encoded);
        var did = $"did:key:{multibase}";

        // Should complete without any await (Task.FromResult)
        var task = _resolver.ResolveAsync(did);
        task.IsCompletedSuccessfully.Should().BeTrue("did:key resolution requires no network calls");
    }
}
