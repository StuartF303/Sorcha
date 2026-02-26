// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Enums;
using Sorcha.Cryptography.Models;
using Xunit;

namespace Sorcha.Cryptography.Tests.Unit.Pqc;

/// <summary>
/// Tests for hybrid signature verification — classical-only, PQC-only, both, and neither.
/// </summary>
public class HybridVerificationTests
{
    private readonly CryptoModule _cryptoModule = new();

    [Fact]
    public async Task Verify_ClassicalOnly_ShouldAccept()
    {
        var keySet = (await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ED25519)).Value;
        var data = System.Security.Cryptography.SHA256.HashData("classical only"u8.ToArray());
        var sig = await _cryptoModule.SignAsync(data, (byte)WalletNetworks.ED25519, keySet.PrivateKey.Key!);

        var hybrid = new HybridSignature
        {
            Classical = Convert.ToBase64String(sig.Value!),
            ClassicalAlgorithm = "ED25519"
        };

        hybrid.IsValid().Should().BeTrue();

        // Verify classical component
        var classicalSig = Convert.FromBase64String(hybrid.Classical!);
        var result = await _cryptoModule.VerifyAsync(classicalSig, data,
            (byte)WalletNetworks.ED25519, keySet.PublicKey.Key!);
        result.Should().Be(CryptoStatus.Success);
    }

    [Fact]
    public async Task Verify_PqcOnly_ShouldAccept()
    {
        var keySet = (await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ML_DSA_65)).Value;
        var data = System.Security.Cryptography.SHA256.HashData("pqc only"u8.ToArray());
        var sig = await _cryptoModule.SignAsync(data, (byte)WalletNetworks.ML_DSA_65, keySet.PrivateKey.Key!);

        var hybrid = new HybridSignature
        {
            Pqc = Convert.ToBase64String(sig.Value!),
            PqcAlgorithm = "ML-DSA-65",
            WitnessPublicKey = Convert.ToBase64String(keySet.PublicKey.Key!)
        };

        hybrid.IsValid().Should().BeTrue();

        // Verify PQC component
        var pqcSig = Convert.FromBase64String(hybrid.Pqc!);
        var result = await _cryptoModule.VerifyAsync(pqcSig, data,
            (byte)WalletNetworks.ML_DSA_65, keySet.PublicKey.Key!);
        result.Should().Be(CryptoStatus.Success);
    }

    [Fact]
    public async Task Verify_BothValid_ShouldAccept()
    {
        var classicalKeySet = (await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ED25519)).Value;
        var pqcKeySet = (await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ML_DSA_65)).Value;
        var data = System.Security.Cryptography.SHA256.HashData("both valid"u8.ToArray());

        var classicalSig = await _cryptoModule.SignAsync(data, (byte)WalletNetworks.ED25519, classicalKeySet.PrivateKey.Key!);
        var pqcSig = await _cryptoModule.SignAsync(data, (byte)WalletNetworks.ML_DSA_65, pqcKeySet.PrivateKey.Key!);

        var hybrid = new HybridSignature
        {
            Classical = Convert.ToBase64String(classicalSig.Value!),
            ClassicalAlgorithm = "ED25519",
            Pqc = Convert.ToBase64String(pqcSig.Value!),
            PqcAlgorithm = "ML-DSA-65",
            WitnessPublicKey = Convert.ToBase64String(pqcKeySet.PublicKey.Key!)
        };

        // Verify both
        var classicalVerify = await _cryptoModule.VerifyAsync(
            Convert.FromBase64String(hybrid.Classical!), data,
            (byte)WalletNetworks.ED25519, classicalKeySet.PublicKey.Key!);
        var pqcVerify = await _cryptoModule.VerifyAsync(
            Convert.FromBase64String(hybrid.Pqc!), data,
            (byte)WalletNetworks.ML_DSA_65, pqcKeySet.PublicKey.Key!);

        classicalVerify.Should().Be(CryptoStatus.Success);
        pqcVerify.Should().Be(CryptoStatus.Success);
    }

    [Fact]
    public async Task Verify_BothInvalid_ShouldReject()
    {
        var classicalKeySet = (await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ED25519)).Value;
        var pqcKeySet = (await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ML_DSA_65)).Value;
        var data = System.Security.Cryptography.SHA256.HashData("original"u8.ToArray());
        var wrongData = System.Security.Cryptography.SHA256.HashData("wrong"u8.ToArray());

        var classicalSig = await _cryptoModule.SignAsync(data, (byte)WalletNetworks.ED25519, classicalKeySet.PrivateKey.Key!);
        var pqcSig = await _cryptoModule.SignAsync(data, (byte)WalletNetworks.ML_DSA_65, pqcKeySet.PrivateKey.Key!);

        // Verify against wrong data — both should fail
        var classicalVerify = await _cryptoModule.VerifyAsync(
            classicalSig.Value!, wrongData,
            (byte)WalletNetworks.ED25519, classicalKeySet.PublicKey.Key!);
        var pqcVerify = await _cryptoModule.VerifyAsync(
            pqcSig.Value!, wrongData,
            (byte)WalletNetworks.ML_DSA_65, pqcKeySet.PublicKey.Key!);

        classicalVerify.Should().Be(CryptoStatus.InvalidSignature);
        pqcVerify.Should().Be(CryptoStatus.InvalidSignature);
    }

    [Fact]
    public async Task HybridVerifyAsync_BothValid_ShouldReturnSuccess()
    {
        var classicalKeySet = (await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ED25519)).Value;
        var pqcKeySet = (await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ML_DSA_65)).Value;
        var data = System.Security.Cryptography.SHA256.HashData("hybrid verify async both"u8.ToArray());

        var hybrid = (await _cryptoModule.HybridSignAsync(
            data,
            (byte)WalletNetworks.ED25519, classicalKeySet.PrivateKey.Key!,
            (byte)WalletNetworks.ML_DSA_65, pqcKeySet.PrivateKey.Key!, pqcKeySet.PublicKey.Key!)).Value!;

        var result = await _cryptoModule.HybridVerifyAsync(hybrid, data, classicalKeySet.PublicKey.Key!);

        result.Should().Be(CryptoStatus.Success);
    }

    [Fact]
    public async Task HybridVerifyAsync_ClassicalOnlyValid_ShouldAccept()
    {
        var classicalKeySet = (await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ED25519)).Value;
        var data = System.Security.Cryptography.SHA256.HashData("classical only verify"u8.ToArray());
        var sig = await _cryptoModule.SignAsync(data, (byte)WalletNetworks.ED25519, classicalKeySet.PrivateKey.Key!);

        var hybrid = new HybridSignature
        {
            Classical = Convert.ToBase64String(sig.Value!),
            ClassicalAlgorithm = "ED25519"
        };

        var result = await _cryptoModule.HybridVerifyAsync(hybrid, data, classicalKeySet.PublicKey.Key!,
            HybridVerificationMode.Permissive);
        result.Should().Be(CryptoStatus.Success);
    }

    [Fact]
    public async Task HybridVerifyAsync_PqcOnlyValid_ShouldAccept()
    {
        var pqcKeySet = (await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ML_DSA_65)).Value;
        var data = System.Security.Cryptography.SHA256.HashData("pqc only verify"u8.ToArray());
        var sig = await _cryptoModule.SignAsync(data, (byte)WalletNetworks.ML_DSA_65, pqcKeySet.PrivateKey.Key!);

        var hybrid = new HybridSignature
        {
            Pqc = Convert.ToBase64String(sig.Value!),
            PqcAlgorithm = "ML-DSA-65",
            WitnessPublicKey = Convert.ToBase64String(pqcKeySet.PublicKey.Key!)
        };

        // No classical key needed — PQC should suffice in Permissive mode
        var result = await _cryptoModule.HybridVerifyAsync(hybrid, data, null,
            HybridVerificationMode.Permissive);
        result.Should().Be(CryptoStatus.Success);
    }

    [Fact]
    public async Task HybridVerifyAsync_WrongData_ShouldReject()
    {
        var classicalKeySet = (await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ED25519)).Value;
        var pqcKeySet = (await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ML_DSA_65)).Value;
        var data = System.Security.Cryptography.SHA256.HashData("original data"u8.ToArray());
        var wrongData = System.Security.Cryptography.SHA256.HashData("wrong data"u8.ToArray());

        var hybrid = (await _cryptoModule.HybridSignAsync(
            data,
            (byte)WalletNetworks.ED25519, classicalKeySet.PrivateKey.Key!,
            (byte)WalletNetworks.ML_DSA_65, pqcKeySet.PrivateKey.Key!, pqcKeySet.PublicKey.Key!)).Value!;

        var result = await _cryptoModule.HybridVerifyAsync(hybrid, wrongData, classicalKeySet.PublicKey.Key!);
        result.Should().Be(CryptoStatus.InvalidSignature);
    }

    [Fact]
    public async Task HybridVerifyAsync_StrictMode_ClassicalOnlyValid_ShouldReject()
    {
        var classicalKeySet = (await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ED25519)).Value;
        var data = System.Security.Cryptography.SHA256.HashData("strict classical only"u8.ToArray());
        var sig = await _cryptoModule.SignAsync(data, (byte)WalletNetworks.ED25519, classicalKeySet.PrivateKey.Key!);

        var hybrid = new HybridSignature
        {
            Classical = Convert.ToBase64String(sig.Value!),
            ClassicalAlgorithm = "ED25519"
        };

        var result = await _cryptoModule.HybridVerifyAsync(
            hybrid, data, classicalKeySet.PublicKey.Key!,
            HybridVerificationMode.Strict);

        result.Should().Be(CryptoStatus.InvalidSignature,
            "Strict mode requires BOTH components — classical-only should fail");
    }

    [Fact]
    public async Task HybridVerifyAsync_StrictMode_PqcOnlyValid_ShouldReject()
    {
        var pqcKeySet = (await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ML_DSA_65)).Value;
        var data = System.Security.Cryptography.SHA256.HashData("strict pqc only"u8.ToArray());
        var sig = await _cryptoModule.SignAsync(data, (byte)WalletNetworks.ML_DSA_65, pqcKeySet.PrivateKey.Key!);

        var hybrid = new HybridSignature
        {
            Pqc = Convert.ToBase64String(sig.Value!),
            PqcAlgorithm = "ML-DSA-65",
            WitnessPublicKey = Convert.ToBase64String(pqcKeySet.PublicKey.Key!)
        };

        var result = await _cryptoModule.HybridVerifyAsync(
            hybrid, data, null,
            HybridVerificationMode.Strict);

        result.Should().Be(CryptoStatus.InvalidSignature,
            "Strict mode requires BOTH components — PQC-only should fail");
    }

    [Fact]
    public async Task HybridVerifyAsync_StrictMode_BothValid_ShouldAccept()
    {
        var classicalKeySet = (await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ED25519)).Value;
        var pqcKeySet = (await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ML_DSA_65)).Value;
        var data = System.Security.Cryptography.SHA256.HashData("strict both valid"u8.ToArray());

        var hybrid = (await _cryptoModule.HybridSignAsync(
            data,
            (byte)WalletNetworks.ED25519, classicalKeySet.PrivateKey.Key!,
            (byte)WalletNetworks.ML_DSA_65, pqcKeySet.PrivateKey.Key!, pqcKeySet.PublicKey.Key!)).Value!;

        var result = await _cryptoModule.HybridVerifyAsync(
            hybrid, data, classicalKeySet.PublicKey.Key!,
            HybridVerificationMode.Strict);

        result.Should().Be(CryptoStatus.Success);
    }

    [Fact]
    public async Task HybridVerifyAsync_PermissiveMode_ClassicalOnlyValid_ShouldAccept()
    {
        var classicalKeySet = (await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ED25519)).Value;
        var data = System.Security.Cryptography.SHA256.HashData("permissive classical"u8.ToArray());
        var sig = await _cryptoModule.SignAsync(data, (byte)WalletNetworks.ED25519, classicalKeySet.PrivateKey.Key!);

        var hybrid = new HybridSignature
        {
            Classical = Convert.ToBase64String(sig.Value!),
            ClassicalAlgorithm = "ED25519"
        };

        var result = await _cryptoModule.HybridVerifyAsync(
            hybrid, data, classicalKeySet.PublicKey.Key!,
            HybridVerificationMode.Permissive);

        result.Should().Be(CryptoStatus.Success,
            "Permissive mode accepts classical-only");
    }

    [Fact]
    public async Task HybridVerifyAsync_DefaultMode_IsStrict()
    {
        var classicalKeySet = (await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ED25519)).Value;
        var data = System.Security.Cryptography.SHA256.HashData("default mode test"u8.ToArray());
        var sig = await _cryptoModule.SignAsync(data, (byte)WalletNetworks.ED25519, classicalKeySet.PrivateKey.Key!);

        var hybrid = new HybridSignature
        {
            Classical = Convert.ToBase64String(sig.Value!),
            ClassicalAlgorithm = "ED25519"
        };

        // Call WITHOUT explicit mode — should default to Strict
        var result = await _cryptoModule.HybridVerifyAsync(
            hybrid, data, classicalKeySet.PublicKey.Key!);

        result.Should().Be(CryptoStatus.InvalidSignature,
            "Default mode should be Strict, rejecting classical-only");
    }

    [Fact]
    public async Task Verify_WitnessPublicKey_ShouldMatchSignerKey()
    {
        var pqcKeySet = (await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ML_DSA_65)).Value;
        var data = System.Security.Cryptography.SHA256.HashData("witness key test"u8.ToArray());
        var sig = await _cryptoModule.SignAsync(data, (byte)WalletNetworks.ML_DSA_65, pqcKeySet.PrivateKey.Key!);

        var hybrid = new HybridSignature
        {
            Pqc = Convert.ToBase64String(sig.Value!),
            PqcAlgorithm = "ML-DSA-65",
            WitnessPublicKey = Convert.ToBase64String(pqcKeySet.PublicKey.Key!)
        };

        // Extract witness key and verify signature using it
        var witnessKey = Convert.FromBase64String(hybrid.WitnessPublicKey!);
        var pqcVerify = await _cryptoModule.VerifyAsync(
            Convert.FromBase64String(hybrid.Pqc!), data,
            (byte)WalletNetworks.ML_DSA_65, witnessKey);

        pqcVerify.Should().Be(CryptoStatus.Success,
            "signature must verify with the witness public key from the hybrid signature");
    }
}
