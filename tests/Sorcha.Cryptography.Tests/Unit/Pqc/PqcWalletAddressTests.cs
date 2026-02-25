// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Enums;
using Sorcha.Cryptography.Utilities;
using Xunit;

namespace Sorcha.Cryptography.Tests.Unit.Pqc;

public class PqcWalletAddressTests
{
    private readonly WalletUtilities _walletUtilities = new();
    private readonly PqcSignatureProvider _provider = new();

    [Fact]
    public void PublicKeyToWallet_MlDsa65_ProducesWs2Address()
    {
        var keyPair = _provider.GenerateMlDsa65KeyPair().Value;

        var address = _walletUtilities.PublicKeyToWallet(keyPair.PublicKey.Key!, (byte)WalletNetworks.ML_DSA_65);

        address.Should().NotBeNull();
        address.Should().StartWith("ws2", "PQC wallets use ws2 prefix");
    }

    [Fact]
    public void PublicKeyToWallet_MlDsa65_AddressUnder100Chars()
    {
        var keyPair = _provider.GenerateMlDsa65KeyPair().Value;

        var address = _walletUtilities.PublicKeyToWallet(keyPair.PublicKey.Key!, (byte)WalletNetworks.ML_DSA_65);

        address.Should().NotBeNull();
        address!.Length.Should().BeLessThan(100, "PQC addresses must be compact (< 100 chars)");
    }

    [Fact]
    public void PublicKeyToWallet_SlhDsa128s_ProducesWs2Address()
    {
        var keyPair = _provider.GenerateSlhDsa128sKeyPair().Value;

        var address = _walletUtilities.PublicKeyToWallet(keyPair.PublicKey.Key!, (byte)WalletNetworks.SLH_DSA_128s);

        address.Should().NotBeNull();
        address.Should().StartWith("ws2");
    }

    [Fact]
    public void PublicKeyToWallet_SlhDsa192s_ProducesWs2Address()
    {
        var keyPair = _provider.GenerateSlhDsa192sKeyPair().Value;

        var address = _walletUtilities.PublicKeyToWallet(keyPair.PublicKey.Key!, (byte)WalletNetworks.SLH_DSA_192s);

        address.Should().NotBeNull();
        address.Should().StartWith("ws2");
        address!.Length.Should().BeLessThan(100);
    }

    [Fact]
    public void PublicKeyToWallet_Ed25519_StillProducesWs1Address()
    {
        // Classical keys should be unaffected
        var publicKey = new byte[32];
        Random.Shared.NextBytes(publicKey);

        var address = _walletUtilities.PublicKeyToWallet(publicKey, (byte)WalletNetworks.ED25519);

        address.Should().NotBeNull();
        address.Should().StartWith("ws1", "Classical wallets keep ws1 prefix");
    }

    [Fact]
    public void WalletToPublicKey_Ws2Address_ReturnsHashAndNetwork()
    {
        var keyPair = _provider.GenerateMlDsa65KeyPair().Value;
        var address = _walletUtilities.PublicKeyToWallet(keyPair.PublicKey.Key!, (byte)WalletNetworks.ML_DSA_65);

        var result = _walletUtilities.WalletToPublicKey(address!);

        result.Should().NotBeNull();
        result!.Value.Network.Should().Be((byte)WalletNetworks.ML_DSA_65);
        // Returns SHA-256 hash (32 bytes), NOT the full 1952-byte public key
        result.Value.PublicKey.Should().HaveCount(32, "ws2 address contains hash, not full key");
    }

    [Fact]
    public void WalletToPublicKey_Ws1Address_StillWorks()
    {
        var publicKey = new byte[32];
        Random.Shared.NextBytes(publicKey);
        var address = _walletUtilities.PublicKeyToWallet(publicKey, (byte)WalletNetworks.ED25519);

        var result = _walletUtilities.WalletToPublicKey(address!);

        result.Should().NotBeNull();
        result!.Value.Network.Should().Be((byte)WalletNetworks.ED25519);
        result.Value.PublicKey.Should().BeEquivalentTo(publicKey);
    }

    [Fact]
    public void AddressKeyBinding_HashOfPublicKeyMatchesAddress()
    {
        var keyPair = _provider.GenerateMlDsa65KeyPair().Value;
        var address = _walletUtilities.PublicKeyToWallet(keyPair.PublicKey.Key!, (byte)WalletNetworks.ML_DSA_65);

        // Decode the address
        var decoded = _walletUtilities.WalletToPublicKey(address!);
        decoded.Should().NotBeNull();

        // Manually compute the expected hash
        byte[] toHash = new byte[keyPair.PublicKey.Key!.Length + 1];
        toHash[0] = (byte)WalletNetworks.ML_DSA_65;
        Array.Copy(keyPair.PublicKey.Key, 0, toHash, 1, keyPair.PublicKey.Key.Length);
        var expectedHash = System.Security.Cryptography.SHA256.HashData(toHash);

        decoded!.Value.PublicKey.Should().BeEquivalentTo(expectedHash,
            "address hash must match SHA-256(network_byte + public_key)");
    }

    [Fact]
    public void Bech32m_RoundTrip_ProducesConsistentAddress()
    {
        var keyPair = _provider.GenerateMlDsa65KeyPair().Value;

        var address1 = _walletUtilities.PublicKeyToWallet(keyPair.PublicKey.Key!, (byte)WalletNetworks.ML_DSA_65);
        var address2 = _walletUtilities.PublicKeyToWallet(keyPair.PublicKey.Key!, (byte)WalletNetworks.ML_DSA_65);

        address1.Should().Be(address2, "same key should produce same address");
    }

    [Fact]
    public void DifferentPqcKeys_ProduceDifferentAddresses()
    {
        var keyPair1 = _provider.GenerateMlDsa65KeyPair().Value;
        var keyPair2 = _provider.GenerateMlDsa65KeyPair().Value;

        var address1 = _walletUtilities.PublicKeyToWallet(keyPair1.PublicKey.Key!, (byte)WalletNetworks.ML_DSA_65);
        var address2 = _walletUtilities.PublicKeyToWallet(keyPair2.PublicKey.Key!, (byte)WalletNetworks.ML_DSA_65);

        address1.Should().NotBe(address2);
    }
}
