// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Enums;
using Xunit;

namespace Sorcha.Cryptography.Tests.Unit.Pqc;

public class PqcSignatureProviderTests : IDisposable
{
    private readonly PqcSignatureProvider _provider = new();

    public void Dispose() => _provider.Dispose();

    [Fact]
    public void GenerateMlDsa65KeyPair_ShouldProduceValidKeyPair()
    {
        var result = _provider.GenerateMlDsa65KeyPair();

        result.IsSuccess.Should().BeTrue();
        result.Value.PublicKey.Key.Should().HaveCount(1952, "ML-DSA-65 public key is 1952 bytes");
        result.Value.PrivateKey.Key.Should().HaveCount(4032, "ML-DSA-65 private key is 4032 bytes");
        result.Value.PublicKey.Network.Should().Be(WalletNetworks.ML_DSA_65);
        result.Value.PrivateKey.Network.Should().Be(WalletNetworks.ML_DSA_65);
    }

    [Fact]
    public void SignMlDsa65_ShouldProduceVerifiableSignature()
    {
        var keyPair = _provider.GenerateMlDsa65KeyPair().Value;
        var data = System.Text.Encoding.UTF8.GetBytes("test data for ML-DSA-65 signing");

        var signResult = _provider.SignMlDsa65(data, keyPair.PrivateKey.Key!);

        signResult.IsSuccess.Should().BeTrue();
        signResult.Value.Should().HaveCount(3309, "ML-DSA-65 signature is 3309 bytes");

        var verifyResult = _provider.VerifyMlDsa65(data, signResult.Value!, keyPair.PublicKey.Key!);
        verifyResult.Should().Be(CryptoStatus.Success);
    }

    [Fact]
    public void VerifyMlDsa65_TamperedData_ShouldReject()
    {
        var keyPair = _provider.GenerateMlDsa65KeyPair().Value;
        var data = System.Text.Encoding.UTF8.GetBytes("original data");

        var signResult = _provider.SignMlDsa65(data, keyPair.PrivateKey.Key!);
        signResult.IsSuccess.Should().BeTrue();

        var tampered = System.Text.Encoding.UTF8.GetBytes("tampered data");
        var verifyResult = _provider.VerifyMlDsa65(tampered, signResult.Value!, keyPair.PublicKey.Key!);
        verifyResult.Should().Be(CryptoStatus.InvalidSignature);
    }

    [Fact]
    public void VerifyMlDsa65_WrongKey_ShouldReject()
    {
        var keyPair1 = _provider.GenerateMlDsa65KeyPair().Value;
        var keyPair2 = _provider.GenerateMlDsa65KeyPair().Value;
        var data = System.Text.Encoding.UTF8.GetBytes("test data");

        var signResult = _provider.SignMlDsa65(data, keyPair1.PrivateKey.Key!);

        var verifyResult = _provider.VerifyMlDsa65(data, signResult.Value!, keyPair2.PublicKey.Key!);
        verifyResult.Should().Be(CryptoStatus.InvalidSignature);
    }

    [Fact]
    public void SignMlDsa65_NullData_ShouldFail()
    {
        var keyPair = _provider.GenerateMlDsa65KeyPair().Value;

        var result = _provider.SignMlDsa65(null!, keyPair.PrivateKey.Key!);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(CryptoStatus.InvalidParameter);
    }

    [Fact]
    public void CalculateMlDsa65PublicKey_ShouldMatchOriginal()
    {
        var keyPair = _provider.GenerateMlDsa65KeyPair().Value;

        var result = _provider.CalculateMlDsa65PublicKey(keyPair.PrivateKey.Key!);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(keyPair.PublicKey.Key);
    }

    [Fact]
    public void GenerateSlhDsa128sKeyPair_ShouldProduceValidKeyPair()
    {
        var result = _provider.GenerateSlhDsa128sKeyPair();

        result.IsSuccess.Should().BeTrue();
        result.Value.PublicKey.Key.Should().HaveCount(32, "SLH-DSA-SHA2-128s public key is 32 bytes");
        result.Value.PrivateKey.Key.Should().HaveCount(64, "SLH-DSA-SHA2-128s private key is 64 bytes");
        result.Value.PublicKey.Network.Should().Be(WalletNetworks.SLH_DSA_128s);
    }

    [Fact]
    public void SignSlhDsa128s_ShouldProduceVerifiableSignature()
    {
        var keyPair = _provider.GenerateSlhDsa128sKeyPair().Value;
        var data = System.Text.Encoding.UTF8.GetBytes("test data for SLH-DSA");

        var signResult = _provider.SignSlhDsa128s(data, keyPair.PrivateKey.Key!);

        signResult.IsSuccess.Should().BeTrue();
        signResult.Value.Should().HaveCount(7856, "SLH-DSA-SHA2-128s signature is 7856 bytes");

        var verifyResult = _provider.VerifySlhDsa128s(data, signResult.Value!, keyPair.PublicKey.Key!);
        verifyResult.Should().Be(CryptoStatus.Success);
    }

    [Fact]
    public void VerifySlhDsa128s_TamperedData_ShouldReject()
    {
        var keyPair = _provider.GenerateSlhDsa128sKeyPair().Value;
        var data = System.Text.Encoding.UTF8.GetBytes("original SLH-DSA data");

        var signResult = _provider.SignSlhDsa128s(data, keyPair.PrivateKey.Key!);
        signResult.IsSuccess.Should().BeTrue();

        var tampered = System.Text.Encoding.UTF8.GetBytes("tampered SLH-DSA data");
        var verifyResult = _provider.VerifySlhDsa128s(tampered, signResult.Value!, keyPair.PublicKey.Key!);
        verifyResult.Should().Be(CryptoStatus.InvalidSignature);
    }
}
