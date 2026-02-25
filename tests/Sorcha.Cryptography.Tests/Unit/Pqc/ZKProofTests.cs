// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Security.Cryptography;
using FluentAssertions;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Models;
using Xunit;

namespace Sorcha.Cryptography.Tests.Unit.Pqc;

/// <summary>
/// Tests for zero-knowledge proof providers: ZK inclusion proofs and range proofs.
/// Validates proof generation, verification, soundness (invalid proofs rejected),
/// and proof sizes.
/// </summary>
public class ZKInclusionProofTests
{
    private readonly ZKInclusionProofProvider _provider = new();

    [Fact]
    public void GenerateInclusionProof_ValidInputs_ProducesValidProof()
    {
        // Arrange
        var txHash = SHA256.HashData("test-transaction-data"u8.ToArray());
        var merkleRoot = SHA256.HashData("merkle-root-data"u8.ToArray());
        var merkleProofPath = new[]
        {
            SHA256.HashData("sibling-1"u8.ToArray()),
            SHA256.HashData("sibling-2"u8.ToArray())
        };

        // Act
        var proof = _provider.GenerateInclusionProof(txHash, merkleRoot, merkleProofPath, "docket-001");

        // Assert
        proof.Should().NotBeNull();
        proof.DocketId.Should().Be("docket-001");
        proof.MerkleRoot.Should().BeEquivalentTo(merkleRoot);
        proof.Commitment.Should().NotBeNullOrEmpty();
        proof.ProofData.Should().HaveCount(96); // challenge + sv + sr
        proof.MerkleProofPath.Should().HaveCount(2);
        proof.VerificationKey.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void VerifyInclusionProof_ValidProof_ReturnsValid()
    {
        // Arrange
        var txHash = SHA256.HashData("transaction-payload"u8.ToArray());
        var merkleRoot = SHA256.HashData("root"u8.ToArray());
        var proof = _provider.GenerateInclusionProof(txHash, merkleRoot, [], "docket-002");

        // Act
        var result = _provider.VerifyInclusionProof(proof);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Message.Should().Contain("verified");
    }

    [Fact]
    public void VerifyInclusionProof_TamperedProofData_ReturnsInvalid()
    {
        // Arrange
        var txHash = SHA256.HashData("transaction"u8.ToArray());
        var merkleRoot = SHA256.HashData("root"u8.ToArray());
        var proof = _provider.GenerateInclusionProof(txHash, merkleRoot, [], "docket-003");

        // Tamper with the Schnorr proof
        proof.ProofData[50] ^= 0xFF;

        // Act
        var result = _provider.VerifyInclusionProof(proof);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("failed");
    }

    [Fact]
    public void GenerateInclusionProof_DifferentTransactions_ProduceDifferentCommitments()
    {
        // Arrange
        var txHash1 = SHA256.HashData("transaction-1"u8.ToArray());
        var txHash2 = SHA256.HashData("transaction-2"u8.ToArray());
        var root = SHA256.HashData("root"u8.ToArray());

        // Act
        var proof1 = _provider.GenerateInclusionProof(txHash1, root, [], "d1");
        var proof2 = _provider.GenerateInclusionProof(txHash2, root, [], "d2");

        // Assert — commitments differ (different values + different random blinding)
        proof1.Commitment.Should().NotBeEquivalentTo(proof2.Commitment);
    }

    [Fact]
    public void GenerateInclusionProof_SameTransaction_ProduceDifferentCommitments()
    {
        // Arrange — same transaction, but blinding factor is random
        var txHash = SHA256.HashData("same-tx"u8.ToArray());
        var root = SHA256.HashData("root"u8.ToArray());

        // Act
        var proof1 = _provider.GenerateInclusionProof(txHash, root, [], "d1");
        var proof2 = _provider.GenerateInclusionProof(txHash, root, [], "d2");

        // Assert — commitments differ due to random blinding (hiding property)
        proof1.Commitment.Should().NotBeEquivalentTo(proof2.Commitment);
    }

    [Fact]
    public void GenerateInclusionProof_NullTransactionHash_ThrowsArgumentNullException()
    {
        var root = SHA256.HashData("root"u8.ToArray());
        var act = () => _provider.GenerateInclusionProof(null!, root, [], "d1");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerateInclusionProof_WrongHashSize_ThrowsArgumentException()
    {
        var badHash = new byte[16]; // Not 32 bytes
        var root = SHA256.HashData("root"u8.ToArray());
        var act = () => _provider.GenerateInclusionProof(badHash, root, [], "d1");
        act.Should().Throw<ArgumentException>().WithMessage("*32 bytes*");
    }

    [Fact]
    public void VerifyInclusionProof_InvalidMerkleRoot_ReturnsInvalid()
    {
        // Arrange
        var txHash = SHA256.HashData("tx"u8.ToArray());
        var root = SHA256.HashData("root"u8.ToArray());
        var proof = _provider.GenerateInclusionProof(txHash, root, [], "d1");

        // Corrupt the Merkle root
        proof.MerkleRoot = new byte[16]; // Wrong size

        // Act
        var result = _provider.VerifyInclusionProof(proof);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void VerifyInclusionProof_CommitmentSize_Is33Bytes()
    {
        // Compressed secp256k1 point = 33 bytes
        var txHash = SHA256.HashData("tx"u8.ToArray());
        var root = SHA256.HashData("root"u8.ToArray());
        var proof = _provider.GenerateInclusionProof(txHash, root, [], "d1");

        proof.Commitment.Should().HaveCount(33);
    }

    [Fact]
    public void VerifyInclusionProof_VerificationKeySize_Is66Bytes()
    {
        // G (33 bytes compressed) + H (33 bytes compressed) = 66 bytes
        var txHash = SHA256.HashData("tx"u8.ToArray());
        var root = SHA256.HashData("root"u8.ToArray());
        var proof = _provider.GenerateInclusionProof(txHash, root, [], "d1");

        proof.VerificationKey.Should().HaveCount(66);
    }
}

public class RangeProofTests
{
    private readonly RangeProofProvider _provider = new();

    [Fact]
    public void GenerateRangeProof_ValidValue_ProducesValidProof()
    {
        // Arrange
        long value = 42;
        int bitLength = 8;

        // Act
        var proof = _provider.GenerateRangeProof(value, bitLength);

        // Assert
        proof.Should().NotBeNull();
        proof.BitLength.Should().Be(8);
        proof.Commitment.Should().NotBeNullOrEmpty();
        proof.BitCommitments.Should().HaveCount(8);
        proof.BitProofs.Should().HaveCount(8);
        proof.AggregationProof.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void VerifyRangeProof_ValidProof_ReturnsValid()
    {
        // Arrange
        var proof = _provider.GenerateRangeProof(100, 8);

        // Act
        var result = _provider.VerifyRangeProof(proof);

        // Assert
        result.IsValid.Should().BeTrue(result.Message);
        result.Message.Should().Contain("verified");
    }

    [Fact]
    public void VerifyRangeProof_ZeroValue_Succeeds()
    {
        // Edge case: minimum value in range
        var proof = _provider.GenerateRangeProof(0, 8);
        var result = _provider.VerifyRangeProof(proof);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyRangeProof_MaxValue_Succeeds()
    {
        // Edge case: maximum value in 8-bit range (255)
        var proof = _provider.GenerateRangeProof(255, 8);
        var result = _provider.VerifyRangeProof(proof);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GenerateRangeProof_ValueOutOfRange_ThrowsArgumentOutOfRange()
    {
        // 256 >= 2^8
        var act = () => _provider.GenerateRangeProof(256, 8);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GenerateRangeProof_NegativeValue_ThrowsArgumentOutOfRange()
    {
        var act = () => _provider.GenerateRangeProof(-1, 8);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GenerateRangeProof_ZeroBitLength_ThrowsArgumentOutOfRange()
    {
        var act = () => _provider.GenerateRangeProof(0, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GenerateRangeProof_ExcessiveBitLength_ThrowsArgumentOutOfRange()
    {
        var act = () => _provider.GenerateRangeProof(0, 65);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void VerifyRangeProof_TamperedBitProof_ReturnsInvalid()
    {
        // Arrange
        var proof = _provider.GenerateRangeProof(42, 8);

        // Tamper with a bit proof
        proof.BitProofs[3][10] ^= 0xFF;

        // Act
        var result = _provider.VerifyRangeProof(proof);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void VerifyRangeProof_TamperedAggregationProof_ReturnsInvalid()
    {
        // Arrange
        var proof = _provider.GenerateRangeProof(42, 8);

        // Tamper with aggregation proof
        proof.AggregationProof[10] ^= 0xFF;

        // Act
        var result = _provider.VerifyRangeProof(proof);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void VerifyRangeProof_TamperedCommitment_ReturnsInvalid()
    {
        // Arrange
        var proof = _provider.GenerateRangeProof(42, 8);

        // Replace commitment with a different one (generate for value=99)
        var otherProof = _provider.GenerateRangeProof(99, 8);
        proof.Commitment = otherProof.Commitment;

        // Act
        var result = _provider.VerifyRangeProof(proof);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GenerateRangeProof_SameValue_ProducesDifferentCommitments()
    {
        // Hiding property: same value, different random blinding
        var proof1 = _provider.GenerateRangeProof(42, 8);
        var proof2 = _provider.GenerateRangeProof(42, 8);

        proof1.Commitment.Should().NotBeEquivalentTo(proof2.Commitment);
    }

    [Fact]
    public void GenerateRangeProof_BitCommitmentSize_Is33BytesEach()
    {
        // Each bit commitment is a compressed secp256k1 point
        var proof = _provider.GenerateRangeProof(42, 8);

        foreach (var bc in proof.BitCommitments)
            bc.Should().HaveCount(33);
    }

    [Fact]
    public void GenerateRangeProof_BitProofSize_Is128BytesEach()
    {
        // Each OR proof: e0(32) + s0(32) + e1(32) + s1(32) = 128 bytes
        var proof = _provider.GenerateRangeProof(42, 8);

        foreach (var bp in proof.BitProofs)
            bp.Should().HaveCount(128);
    }

    [Fact]
    public void VerifyRangeProof_16BitRange_Succeeds()
    {
        // Larger range: 16-bit (0 to 65535)
        var proof = _provider.GenerateRangeProof(50000, 16);
        var result = _provider.VerifyRangeProof(proof);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyRangeProof_32BitRange_Succeeds()
    {
        // 32-bit range
        var proof = _provider.GenerateRangeProof(1_000_000_000, 32);
        var result = _provider.VerifyRangeProof(proof);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyRangeProof_NullProof_ThrowsArgumentNullException()
    {
        var act = () => _provider.VerifyRangeProof(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VerifyRangeProof_MismatchedBitCount_ReturnsInvalid()
    {
        // Arrange — tamper with bit length to mismatch commitments
        var proof = _provider.GenerateRangeProof(42, 8);
        proof.BitLength = 4; // Says 4 bits but has 8 bit commitments

        // Act
        var result = _provider.VerifyRangeProof(proof);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void VerifyRangeProof_VerificationCompletesUnder1Second()
    {
        // SC-008: Zero-knowledge proofs verify in under 1 second
        var proof = _provider.GenerateRangeProof(42, 16);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = _provider.VerifyRangeProof(proof);
        sw.Stop();

        result.IsValid.Should().BeTrue();
        sw.ElapsedMilliseconds.Should().BeLessThan(1000, "SC-008 requires verification under 1 second");
    }
}
