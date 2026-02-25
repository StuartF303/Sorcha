// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Enums;
using Xunit;

namespace Sorcha.Cryptography.Tests.Unit.Pqc;

public class PqcEncapsulationProviderTests : IDisposable
{
    private readonly PqcEncapsulationProvider _provider = new();

    public void Dispose() => _provider.Dispose();

    [Fact]
    public void GenerateMlKem768KeyPair_ShouldProduceValidKeyPair()
    {
        var result = _provider.GenerateMlKem768KeyPair();

        result.IsSuccess.Should().BeTrue();
        result.Value.PublicKey.Key.Should().HaveCount(1184, "ML-KEM-768 public key is 1184 bytes");
        result.Value.PrivateKey.Key.Should().HaveCount(2400, "ML-KEM-768 private key is 2400 bytes");
        result.Value.PublicKey.Network.Should().Be(WalletNetworks.ML_KEM_768);
        result.Value.PrivateKey.Network.Should().Be(WalletNetworks.ML_KEM_768);
    }

    [Fact]
    public void Encapsulate_ShouldProduceCiphertextAndSharedSecret()
    {
        var keyPair = _provider.GenerateMlKem768KeyPair().Value;

        var result = _provider.Encapsulate(keyPair.PublicKey.Key!);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Ciphertext.Should().HaveCount(PqcEncapsulationProvider.CiphertextSize,
            "ML-KEM-768 ciphertext is 1088 bytes");
        result.Value.SharedSecret.Should().HaveCount(PqcEncapsulationProvider.SharedSecretSize,
            "ML-KEM-768 shared secret is 32 bytes");
    }

    [Fact]
    public void EncapsulateAndDecapsulate_ShouldRecoverSameSharedSecret()
    {
        var keyPair = _provider.GenerateMlKem768KeyPair().Value;

        var encResult = _provider.Encapsulate(keyPair.PublicKey.Key!);
        encResult.IsSuccess.Should().BeTrue();

        var decResult = _provider.Decapsulate(encResult.Value!.Ciphertext, keyPair.PrivateKey.Key!);
        decResult.IsSuccess.Should().BeTrue();

        decResult.Value.Should().BeEquivalentTo(encResult.Value.SharedSecret,
            "decapsulated secret must match encapsulated secret");
    }

    [Fact]
    public void Decapsulate_WrongPrivateKey_ShouldProduceDifferentSecret()
    {
        var keyPair1 = _provider.GenerateMlKem768KeyPair().Value;
        var keyPair2 = _provider.GenerateMlKem768KeyPair().Value;

        var encResult = _provider.Encapsulate(keyPair1.PublicKey.Key!);
        encResult.IsSuccess.Should().BeTrue();

        // ML-KEM decapsulation with wrong key produces a pseudorandom secret (implicit rejection)
        var decResult = _provider.Decapsulate(encResult.Value!.Ciphertext, keyPair2.PrivateKey.Key!);
        decResult.IsSuccess.Should().BeTrue("ML-KEM uses implicit rejection");
        decResult.Value.Should().NotBeEquivalentTo(encResult.Value.SharedSecret,
            "wrong key should produce different shared secret");
    }

    [Fact]
    public void Encapsulate_NullKey_ShouldFail()
    {
        var result = _provider.Encapsulate(null!);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(CryptoStatus.InvalidKey);
    }

    [Fact]
    public void Decapsulate_NullCiphertext_ShouldFail()
    {
        var keyPair = _provider.GenerateMlKem768KeyPair().Value;

        var result = _provider.Decapsulate(null!, keyPair.PrivateKey.Key!);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(CryptoStatus.InvalidParameter);
    }

    [Fact]
    public void CalculateMlKem768PublicKey_ShouldMatchOriginal()
    {
        var keyPair = _provider.GenerateMlKem768KeyPair().Value;

        var result = _provider.CalculateMlKem768PublicKey(keyPair.PrivateKey.Key!);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(keyPair.PublicKey.Key);
    }

    [Fact]
    public void EncapsulationResult_Dispose_ShouldZeroSharedSecret()
    {
        var keyPair = _provider.GenerateMlKem768KeyPair().Value;
        var encResult = _provider.Encapsulate(keyPair.PublicKey.Key!);
        var secret = encResult.Value!;

        // Capture reference before dispose
        var secretBytes = secret.SharedSecret;
        secretBytes.Any(b => b != 0).Should().BeTrue("secret should have non-zero bytes before dispose");

        secret.Dispose();

        secretBytes.All(b => b == 0).Should().BeTrue("shared secret should be zeroed after dispose");
    }
}
