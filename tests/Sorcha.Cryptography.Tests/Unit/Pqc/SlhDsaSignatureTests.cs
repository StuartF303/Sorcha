// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Enums;
using Xunit;

namespace Sorcha.Cryptography.Tests.Unit.Pqc;

public class SlhDsaSignatureTests : IDisposable
{
    private readonly PqcSignatureProvider _provider = new();

    public void Dispose() => _provider.Dispose();

    #region SLH-DSA-128s

    [Fact]
    public void GenerateSlhDsa128s_KeySizes_AreCorrect()
    {
        var result = _provider.GenerateSlhDsa128sKeyPair();

        result.IsSuccess.Should().BeTrue();
        result.Value.PrivateKey.Key.Should().HaveCount(64, "SLH-DSA-128s private key is 64 bytes");
        result.Value.PublicKey.Key.Should().HaveCount(32, "SLH-DSA-128s public key is 32 bytes");
        result.Value.PublicKey.Network.Should().Be(WalletNetworks.SLH_DSA_128s);
        result.Value.PrivateKey.Network.Should().Be(WalletNetworks.SLH_DSA_128s);
    }

    [Fact]
    public void SignSlhDsa128s_ProducesCorrectSignatureSize()
    {
        var keyPair = _provider.GenerateSlhDsa128sKeyPair().Value;
        var data = System.Text.Encoding.UTF8.GetBytes("SLH-DSA-128s signature size test");

        var signResult = _provider.SignSlhDsa128s(data, keyPair.PrivateKey.Key!);

        signResult.IsSuccess.Should().BeTrue();
        signResult.Value.Should().HaveCount(7856, "SLH-DSA-SHA2-128s signature is 7,856 bytes");
    }

    [Fact]
    public void SignVerifySlhDsa128s_RoundTrip_Succeeds()
    {
        var keyPair = _provider.GenerateSlhDsa128sKeyPair().Value;
        var data = System.Text.Encoding.UTF8.GetBytes("hash-based signature round trip");

        var signResult = _provider.SignSlhDsa128s(data, keyPair.PrivateKey.Key!);
        signResult.IsSuccess.Should().BeTrue();

        var verifyResult = _provider.VerifySlhDsa128s(data, signResult.Value!, keyPair.PublicKey.Key!);
        verifyResult.Should().Be(CryptoStatus.Success);
    }

    [Fact]
    public void VerifySlhDsa128s_TamperedSignature_Rejects()
    {
        var keyPair = _provider.GenerateSlhDsa128sKeyPair().Value;
        var data = System.Text.Encoding.UTF8.GetBytes("tamper test 128s");

        var signResult = _provider.SignSlhDsa128s(data, keyPair.PrivateKey.Key!);
        signResult.IsSuccess.Should().BeTrue();

        // Tamper with the signature
        var tampered = (byte[])signResult.Value!.Clone();
        tampered[0] ^= 0xFF;

        var verifyResult = _provider.VerifySlhDsa128s(data, tampered, keyPair.PublicKey.Key!);
        verifyResult.Should().Be(CryptoStatus.InvalidSignature);
    }

    [Fact]
    public void CalculateSlhDsa128sPublicKey_MatchesOriginal()
    {
        var keyPair = _provider.GenerateSlhDsa128sKeyPair().Value;

        var result = _provider.CalculateSlhDsa128sPublicKey(keyPair.PrivateKey.Key!);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(keyPair.PublicKey.Key);
    }

    #endregion

    #region SLH-DSA-192s

    [Fact]
    public void GenerateSlhDsa192s_KeySizes_AreCorrect()
    {
        var result = _provider.GenerateSlhDsa192sKeyPair();

        result.IsSuccess.Should().BeTrue();
        result.Value.PrivateKey.Key.Should().HaveCount(96, "SLH-DSA-192s private key is 96 bytes");
        result.Value.PublicKey.Key.Should().HaveCount(48, "SLH-DSA-192s public key is 48 bytes");
        result.Value.PublicKey.Network.Should().Be(WalletNetworks.SLH_DSA_192s);
        result.Value.PrivateKey.Network.Should().Be(WalletNetworks.SLH_DSA_192s);
    }

    [Fact]
    public void SignSlhDsa192s_ProducesCorrectSignatureSize()
    {
        var keyPair = _provider.GenerateSlhDsa192sKeyPair().Value;
        var data = System.Text.Encoding.UTF8.GetBytes("SLH-DSA-192s signature size test");

        var signResult = _provider.SignSlhDsa192s(data, keyPair.PrivateKey.Key!);

        signResult.IsSuccess.Should().BeTrue();
        signResult.Value.Should().HaveCount(16224, "SLH-DSA-SHA2-192s signature is 16,224 bytes");
    }

    [Fact]
    public void SignVerifySlhDsa192s_RoundTrip_Succeeds()
    {
        var keyPair = _provider.GenerateSlhDsa192sKeyPair().Value;
        var data = System.Text.Encoding.UTF8.GetBytes("192s hash-based round trip");

        var signResult = _provider.SignSlhDsa192s(data, keyPair.PrivateKey.Key!);
        signResult.IsSuccess.Should().BeTrue();

        var verifyResult = _provider.VerifySlhDsa192s(data, signResult.Value!, keyPair.PublicKey.Key!);
        verifyResult.Should().Be(CryptoStatus.Success);
    }

    [Fact]
    public void VerifySlhDsa192s_TamperedData_Rejects()
    {
        var keyPair = _provider.GenerateSlhDsa192sKeyPair().Value;
        var original = System.Text.Encoding.UTF8.GetBytes("original 192s data");

        var signResult = _provider.SignSlhDsa192s(original, keyPair.PrivateKey.Key!);
        signResult.IsSuccess.Should().BeTrue();

        var tampered = System.Text.Encoding.UTF8.GetBytes("tampered 192s data");
        var verifyResult = _provider.VerifySlhDsa192s(tampered, signResult.Value!, keyPair.PublicKey.Key!);
        verifyResult.Should().Be(CryptoStatus.InvalidSignature);
    }

    [Fact]
    public void VerifySlhDsa192s_WrongKey_Rejects()
    {
        var keyPair1 = _provider.GenerateSlhDsa192sKeyPair().Value;
        var keyPair2 = _provider.GenerateSlhDsa192sKeyPair().Value;
        var data = System.Text.Encoding.UTF8.GetBytes("wrong key test 192s");

        var signResult = _provider.SignSlhDsa192s(data, keyPair1.PrivateKey.Key!);
        signResult.IsSuccess.Should().BeTrue();

        var verifyResult = _provider.VerifySlhDsa192s(data, signResult.Value!, keyPair2.PublicKey.Key!);
        verifyResult.Should().Be(CryptoStatus.InvalidSignature);
    }

    [Fact]
    public void CalculateSlhDsa192sPublicKey_MatchesOriginal()
    {
        var keyPair = _provider.GenerateSlhDsa192sKeyPair().Value;

        var result = _provider.CalculateSlhDsa192sPublicKey(keyPair.PrivateKey.Key!);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(keyPair.PublicKey.Key);
    }

    #endregion

    #region CryptoModule Integration

    [Fact]
    public async Task CryptoModule_SignVerify_SlhDsa128s_Succeeds()
    {
        var module = new CryptoModule();
        var keyResult = await module.GenerateKeySetAsync(WalletNetworks.SLH_DSA_128s);
        keyResult.IsSuccess.Should().BeTrue();

        var data = System.Text.Encoding.UTF8.GetBytes("CryptoModule SLH-DSA-128s test");
        var signResult = await module.SignAsync(data, (byte)WalletNetworks.SLH_DSA_128s, keyResult.Value.PrivateKey.Key!);
        signResult.IsSuccess.Should().BeTrue();

        var verifyResult = await module.VerifyAsync(signResult.Value!, data, (byte)WalletNetworks.SLH_DSA_128s, keyResult.Value.PublicKey.Key!);
        verifyResult.Should().Be(CryptoStatus.Success);
    }

    [Fact]
    public async Task CryptoModule_SignVerify_SlhDsa192s_Succeeds()
    {
        var module = new CryptoModule();
        var keyResult = await module.GenerateKeySetAsync(WalletNetworks.SLH_DSA_192s);
        keyResult.IsSuccess.Should().BeTrue();

        var data = System.Text.Encoding.UTF8.GetBytes("CryptoModule SLH-DSA-192s test");
        var signResult = await module.SignAsync(data, (byte)WalletNetworks.SLH_DSA_192s, keyResult.Value.PrivateKey.Key!);
        signResult.IsSuccess.Should().BeTrue();

        var verifyResult = await module.VerifyAsync(signResult.Value!, data, (byte)WalletNetworks.SLH_DSA_192s, keyResult.Value.PublicKey.Key!);
        verifyResult.Should().Be(CryptoStatus.Success);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void SignSlhDsa128s_NullData_Fails()
    {
        var keyPair = _provider.GenerateSlhDsa128sKeyPair().Value;

        var result = _provider.SignSlhDsa128s(null!, keyPair.PrivateKey.Key!);
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(CryptoStatus.InvalidParameter);
    }

    [Fact]
    public void SignSlhDsa192s_NullData_Fails()
    {
        var keyPair = _provider.GenerateSlhDsa192sKeyPair().Value;

        var result = _provider.SignSlhDsa192s(null!, keyPair.PrivateKey.Key!);
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(CryptoStatus.InvalidParameter);
    }

    [Fact]
    public void SignSlhDsa128s_EmptyKey_Fails()
    {
        var result = _provider.SignSlhDsa128s(new byte[] { 1, 2, 3 }, Array.Empty<byte>());
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(CryptoStatus.InvalidKey);
    }

    [Fact]
    public void SignSlhDsa192s_EmptyKey_Fails()
    {
        var result = _provider.SignSlhDsa192s(new byte[] { 1, 2, 3 }, Array.Empty<byte>());
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(CryptoStatus.InvalidKey);
    }

    #endregion
}
