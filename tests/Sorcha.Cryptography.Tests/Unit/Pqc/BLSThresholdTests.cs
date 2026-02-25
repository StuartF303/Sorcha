// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Models;
using Xunit;

namespace Sorcha.Cryptography.Tests.Unit.Pqc;

public class BLSThresholdTests : IDisposable
{
    private readonly BLSThresholdProvider _provider = new();

    public void Dispose() => _provider.Dispose();

    #region Key Generation

    [Fact]
    public void GenerateThresholdKeyShares_2of3_ProducesValidKeySet()
    {
        var validatorIds = new[] { "v1", "v2", "v3" };

        var result = _provider.GenerateThresholdKeyShares(2, 3, validatorIds);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Threshold.Should().Be(2);
        result.Value.TotalSigners.Should().Be(3);
        result.Value.GroupPublicKey.Should().HaveCount(BLSThresholdProvider.PublicKeySize);
        result.Value.KeyShares.Should().HaveCount(3);
    }

    [Fact]
    public void GenerateThresholdKeyShares_ProducesUniqueShares()
    {
        var validatorIds = new[] { "v1", "v2", "v3" };

        var result = _provider.GenerateThresholdKeyShares(2, 3, validatorIds);

        result.IsSuccess.Should().BeTrue();
        var shares = result.Value!.KeyShares;

        // Each share should be unique
        for (int i = 0; i < shares.Count; i++)
        {
            for (int j = i + 1; j < shares.Count; j++)
            {
                shares[i].SecretShare.Should().NotBeEquivalentTo(shares[j].SecretShare);
                shares[i].PublicShare.Should().NotBeEquivalentTo(shares[j].PublicShare);
            }
        }
    }

    [Fact]
    public void GenerateThresholdKeyShares_ShareSizes_MatchSpec()
    {
        var validatorIds = new[] { "v1", "v2", "v3" };

        var result = _provider.GenerateThresholdKeyShares(2, 3, validatorIds);

        result.IsSuccess.Should().BeTrue();
        foreach (var share in result.Value!.KeyShares)
        {
            share.SecretShare.Should().HaveCount(BLSThresholdProvider.SecretKeySize,
                "Fr scalar should be 32 bytes");
            share.PublicShare.Should().HaveCount(BLSThresholdProvider.PublicKeySize,
                "G2 public key share should be 96 bytes");
        }
    }

    [Fact]
    public void GenerateThresholdKeyShares_1of1_ValidDegenerateCase()
    {
        var result = _provider.GenerateThresholdKeyShares(1, 1, ["solo"]);

        result.IsSuccess.Should().BeTrue();
        result.Value!.KeyShares.Should().HaveCount(1);
    }

    [Fact]
    public void GenerateThresholdKeyShares_ZeroThreshold_Fails()
    {
        var result = _provider.GenerateThresholdKeyShares(0, 3, ["v1", "v2", "v3"]);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void GenerateThresholdKeyShares_ThresholdExceedsTotal_Fails()
    {
        var result = _provider.GenerateThresholdKeyShares(4, 3, ["v1", "v2", "v3"]);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void GenerateThresholdKeyShares_WrongValidatorCount_Fails()
    {
        var result = _provider.GenerateThresholdKeyShares(2, 3, ["v1", "v2"]);
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region Partial Signing

    [Fact]
    public void SignPartial_ProducesSignatureOfExpectedSize()
    {
        var keySet = GenerateTestKeySet(2, 3);
        var docketHash = System.Text.Encoding.UTF8.GetBytes("test-docket-hash-001");

        var result = _provider.SignPartial(keySet.KeyShares[0].SecretShare, docketHash);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(BLSThresholdProvider.SignatureSize,
            "G1 signature should be 48 bytes");
    }

    [Fact]
    public void SignPartial_DifferentShares_ProduceDifferentSignatures()
    {
        var keySet = GenerateTestKeySet(2, 3);
        var docketHash = System.Text.Encoding.UTF8.GetBytes("test-docket-hash-001");

        var sig1 = _provider.SignPartial(keySet.KeyShares[0].SecretShare, docketHash);
        var sig2 = _provider.SignPartial(keySet.KeyShares[1].SecretShare, docketHash);

        sig1.IsSuccess.Should().BeTrue();
        sig2.IsSuccess.Should().BeTrue();
        sig1.Value.Should().NotBeEquivalentTo(sig2.Value);
    }

    [Fact]
    public void SignPartial_NullSecretShare_Fails()
    {
        var result = _provider.SignPartial(null!, System.Text.Encoding.UTF8.GetBytes("hash"));
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void SignPartial_EmptyDocketHash_Fails()
    {
        var keySet = GenerateTestKeySet(2, 3);
        var result = _provider.SignPartial(keySet.KeyShares[0].SecretShare, Array.Empty<byte>());
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region Aggregation and Verification

    [Fact]
    public void AggregateSignatures_2of3_ProducesValidSignature()
    {
        var keySet = GenerateTestKeySet(2, 3);
        var docketHash = System.Text.Encoding.UTF8.GetBytes("test-docket-hash-001");

        // Sign with shares 1 and 2 (meeting threshold of 2)
        var sig1 = _provider.SignPartial(keySet.KeyShares[0].SecretShare, docketHash).Value!;
        var sig2 = _provider.SignPartial(keySet.KeyShares[1].SecretShare, docketHash).Value!;

        var result = _provider.AggregateSignatures(
            [sig1, sig2],
            [keySet.KeyShares[0].ShareIndex, keySet.KeyShares[1].ShareIndex],
            2, 3);

        result.IsSuccess.Should().BeTrue();
        result.AggregateSignature!.Signature.Should().HaveCount(BLSThresholdProvider.SignatureSize);
        result.AggregateSignature.Threshold.Should().Be(2);
        result.AggregateSignature.TotalSigners.Should().Be(3);
    }

    [Fact]
    public void AggregateSignatures_VerifiesAgainstGroupPublicKey()
    {
        var keySet = GenerateTestKeySet(2, 3);
        var docketHash = System.Text.Encoding.UTF8.GetBytes("test-docket-hash-001");

        var sig1 = _provider.SignPartial(keySet.KeyShares[0].SecretShare, docketHash).Value!;
        var sig2 = _provider.SignPartial(keySet.KeyShares[1].SecretShare, docketHash).Value!;

        var aggResult = _provider.AggregateSignatures(
            [sig1, sig2],
            [keySet.KeyShares[0].ShareIndex, keySet.KeyShares[1].ShareIndex],
            2, 3);

        var verifyResult = _provider.VerifyAggregateSignature(
            aggResult.AggregateSignature!.Signature,
            keySet.GroupPublicKey,
            docketHash);

        verifyResult.IsSuccess.Should().BeTrue();
        verifyResult.Value.Should().BeTrue("aggregate signature should verify against group public key");
    }

    [Fact]
    public void AggregateSignatures_AnyTSubset_ProducesSameSignature()
    {
        var keySet = GenerateTestKeySet(2, 3);
        var docketHash = System.Text.Encoding.UTF8.GetBytes("test-docket-hash-001");

        // Sign with all three shares
        var sigs = keySet.KeyShares.Select(ks =>
            _provider.SignPartial(ks.SecretShare, docketHash).Value!).ToArray();
        var indices = keySet.KeyShares.Select(ks => ks.ShareIndex).ToArray();

        // Aggregate {1,2}
        var agg12 = _provider.AggregateSignatures(
            [sigs[0], sigs[1]], [indices[0], indices[1]], 2, 3);
        // Aggregate {1,3}
        var agg13 = _provider.AggregateSignatures(
            [sigs[0], sigs[2]], [indices[0], indices[2]], 2, 3);
        // Aggregate {2,3}
        var agg23 = _provider.AggregateSignatures(
            [sigs[1], sigs[2]], [indices[1], indices[2]], 2, 3);

        // All should produce the same aggregate signature (same point on G1)
        agg12.AggregateSignature!.Signature.Should().BeEquivalentTo(
            agg13.AggregateSignature!.Signature,
            "any t-subset should produce the same aggregate signature");
        agg13.AggregateSignature!.Signature.Should().BeEquivalentTo(
            agg23.AggregateSignature!.Signature);

        // All should verify
        _provider.VerifyAggregateSignature(agg12.AggregateSignature.Signature, keySet.GroupPublicKey, docketHash)
            .Value.Should().BeTrue();
        _provider.VerifyAggregateSignature(agg23.AggregateSignature.Signature, keySet.GroupPublicKey, docketHash)
            .Value.Should().BeTrue();
    }

    [Fact]
    public void AggregateSignatures_BelowThreshold_FailsVerification()
    {
        var keySet = GenerateTestKeySet(3, 5);
        var docketHash = System.Text.Encoding.UTF8.GetBytes("test-docket-hash-001");

        // Sign with only 2 shares (threshold is 3)
        var sig1 = _provider.SignPartial(keySet.KeyShares[0].SecretShare, docketHash).Value!;
        var sig2 = _provider.SignPartial(keySet.KeyShares[1].SecretShare, docketHash).Value!;

        // Try aggregating only 2 (below threshold of 3) - this will produce a wrong signature
        var aggResult = _provider.AggregateSignatures(
            [sig1, sig2],
            [keySet.KeyShares[0].ShareIndex, keySet.KeyShares[1].ShareIndex],
            2, 5); // Note: we pass 2 as threshold to avoid early rejection

        // Even though we got a signature, it should NOT verify against the group key
        // because we used insufficient shares for the actual 3-of-5 scheme
        var verifyResult = _provider.VerifyAggregateSignature(
            aggResult.AggregateSignature!.Signature,
            keySet.GroupPublicKey,
            docketHash);

        verifyResult.IsSuccess.Should().BeTrue();
        verifyResult.Value.Should().BeFalse(
            "aggregate from fewer than threshold shares should not verify");
    }

    [Fact]
    public void AggregateSignatures_WrongDocketHash_FailsVerification()
    {
        var keySet = GenerateTestKeySet(2, 3);
        var originalHash = System.Text.Encoding.UTF8.GetBytes("original-docket-hash");
        var tamperedHash = System.Text.Encoding.UTF8.GetBytes("tampered-docket-hash");

        var sig1 = _provider.SignPartial(keySet.KeyShares[0].SecretShare, originalHash).Value!;
        var sig2 = _provider.SignPartial(keySet.KeyShares[1].SecretShare, originalHash).Value!;

        var aggResult = _provider.AggregateSignatures(
            [sig1, sig2],
            [keySet.KeyShares[0].ShareIndex, keySet.KeyShares[1].ShareIndex],
            2, 3);

        // Verify against different hash should fail
        var verifyResult = _provider.VerifyAggregateSignature(
            aggResult.AggregateSignature!.Signature,
            keySet.GroupPublicKey,
            tamperedHash);

        verifyResult.IsSuccess.Should().BeTrue();
        verifyResult.Value.Should().BeFalse("signature should not verify against different docket hash");
    }

    [Fact]
    public void AggregateSignatures_WrongGroupKey_FailsVerification()
    {
        var keySet1 = GenerateTestKeySet(2, 3);
        var keySet2 = GenerateTestKeySet(2, 3);
        var docketHash = System.Text.Encoding.UTF8.GetBytes("test-docket-hash-001");

        var sig1 = _provider.SignPartial(keySet1.KeyShares[0].SecretShare, docketHash).Value!;
        var sig2 = _provider.SignPartial(keySet1.KeyShares[1].SecretShare, docketHash).Value!;

        var aggResult = _provider.AggregateSignatures(
            [sig1, sig2],
            [keySet1.KeyShares[0].ShareIndex, keySet1.KeyShares[1].ShareIndex],
            2, 3);

        // Verify against a different group's public key should fail
        var verifyResult = _provider.VerifyAggregateSignature(
            aggResult.AggregateSignature!.Signature,
            keySet2.GroupPublicKey,
            docketHash);

        verifyResult.IsSuccess.Should().BeTrue();
        verifyResult.Value.Should().BeFalse("signature should not verify against wrong group key");
    }

    #endregion

    #region Signer Bitfield

    [Fact]
    public void AggregateSignatures_SignerBitfield_CorrectlyEncodesParticipants()
    {
        var keySet = GenerateTestKeySet(2, 5);
        var docketHash = System.Text.Encoding.UTF8.GetBytes("test-docket-hash-001");

        // Sign with shares 2 and 4 (out of 5)
        var sig2 = _provider.SignPartial(keySet.KeyShares[1].SecretShare, docketHash).Value!;
        var sig4 = _provider.SignPartial(keySet.KeyShares[3].SecretShare, docketHash).Value!;

        var aggResult = _provider.AggregateSignatures(
            [sig2, sig4],
            [keySet.KeyShares[1].ShareIndex, keySet.KeyShares[3].ShareIndex],
            2, 5);

        aggResult.IsSuccess.Should().BeTrue();
        var bitfield = aggResult.AggregateSignature!.SignerBitfield;

        // Bitfield for indices 2 and 4 in a 5-signer scheme:
        // Bit 1 (index 2-1=1) and bit 3 (index 4-1=3) should be set
        // = 0b00001010 = 0x0A
        bitfield.Should().HaveCount(1, "5 signers fit in 1 byte");
        bitfield[0].Should().Be(0b00001010);
    }

    [Fact]
    public void AggregateSignatures_SignatureSize_IsConstant()
    {
        var docketHash = System.Text.Encoding.UTF8.GetBytes("test-docket-hash-001");

        // Test with different threshold configurations
        var keySet2of3 = GenerateTestKeySet(2, 3);
        var keySet3of5 = GenerateTestKeySet(3, 5);

        var sigs2of3 = keySet2of3.KeyShares.Take(2).Select(ks =>
            _provider.SignPartial(ks.SecretShare, docketHash).Value!).ToArray();
        var indices2of3 = keySet2of3.KeyShares.Take(2).Select(ks => ks.ShareIndex).ToArray();

        var sigs3of5 = keySet3of5.KeyShares.Take(3).Select(ks =>
            _provider.SignPartial(ks.SecretShare, docketHash).Value!).ToArray();
        var indices3of5 = keySet3of5.KeyShares.Take(3).Select(ks => ks.ShareIndex).ToArray();

        var agg1 = _provider.AggregateSignatures(sigs2of3, indices2of3, 2, 3);
        var agg2 = _provider.AggregateSignatures(sigs3of5, indices3of5, 3, 5);

        agg1.AggregateSignature!.Signature.Should().HaveCount(BLSThresholdProvider.SignatureSize);
        agg2.AggregateSignature!.Signature.Should().HaveCount(BLSThresholdProvider.SignatureSize);
        agg1.AggregateSignature.Signature.Length.Should().Be(agg2.AggregateSignature.Signature.Length,
            "aggregate signature size should be constant regardless of threshold parameters");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void VerifyAggregateSignature_NullSignature_Fails()
    {
        var result = _provider.VerifyAggregateSignature(
            null!, new byte[96], System.Text.Encoding.UTF8.GetBytes("hash"));
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void VerifyAggregateSignature_NullGroupKey_Fails()
    {
        var result = _provider.VerifyAggregateSignature(
            new byte[48], null!, System.Text.Encoding.UTF8.GetBytes("hash"));
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void AggregateSignatures_InsufficientShares_Fails()
    {
        var result = _provider.AggregateSignatures(
            [new byte[48]], [1u], 3, 5);
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region Helpers

    private BLSThresholdKeySet GenerateTestKeySet(uint threshold, uint totalSigners)
    {
        var validatorIds = Enumerable.Range(1, (int)totalSigners)
            .Select(i => $"validator-{i}").ToArray();
        var result = _provider.GenerateThresholdKeyShares(threshold, totalSigners, validatorIds);
        result.IsSuccess.Should().BeTrue($"key generation should succeed for {threshold}-of-{totalSigners}");
        return result.Value!;
    }

    #endregion
}
