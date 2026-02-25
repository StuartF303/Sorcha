// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Security.Cryptography;
using FluentAssertions;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Models;
using Sorcha.Cryptography.Utilities;
using Xunit;

namespace Sorcha.Register.Service.Tests.Unit;

/// <summary>
/// Integration tests for ZK proof verification flow.
/// Simulates the full auditor scenario: transactions exist in a docket's Merkle tree,
/// an auditor receives a ZK proof of inclusion, and verifies it without seeing payload data.
/// </summary>
public class ZKProofIntegrationTests
{
    private readonly ZKInclusionProofProvider _zkProvider = new();
    private readonly RangeProofProvider _rangeProvider = new();
    private readonly Sorcha.Cryptography.Core.HashProvider _hashProvider = new();

    [Fact]
    public void FullFlow_CreateDocketTransactions_GenerateAndVerifyInclusionProof()
    {
        // Arrange — simulate 5 transactions in a docket
        var transactionPayloads = new[]
        {
            "{ \"action\": \"submit-claim\", \"amount\": 1500.00 }",
            "{ \"action\": \"approve-claim\", \"claimId\": \"CLM-001\" }",
            "{ \"action\": \"submit-evidence\", \"docHash\": \"abc123\" }",
            "{ \"action\": \"review-complete\", \"status\": \"approved\" }",
            "{ \"action\": \"payout\", \"amount\": 1500.00, \"recipient\": \"ws1q...\" }"
        };

        // Generate deterministic TxIds (SHA-256 of payload, as hex)
        var txIds = transactionPayloads
            .Select(p => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(p))).ToLowerInvariant())
            .ToList();

        // Build Merkle tree
        var merkleTree = new MerkleTree(_hashProvider);
        var merkleRoot = merkleTree.ComputeMerkleRoot(txIds.AsReadOnly());

        // Target: prove transaction #2 (approve-claim) is in the docket
        var targetTxId = txIds[1];
        var targetTxHash = Convert.FromHexString(targetTxId);
        var merkleRootBytes = Convert.FromHexString(merkleRoot);

        // Build proof path (sibling hashes)
        var proofPath = BuildMerkleProofPath(txIds, targetTxId);

        // Act — generate ZK inclusion proof
        var proof = _zkProvider.GenerateInclusionProof(
            targetTxHash, merkleRootBytes,
            proofPath.Select(Convert.FromHexString).ToArray(),
            "docket-42");

        // Simulate auditor: receives only the proof (NOT the transaction payload)
        // Verify the proof
        var result = _zkProvider.VerifyInclusionProof(proof);

        // Assert — proof is valid
        result.IsValid.Should().BeTrue(result.Message);
        result.Message.Should().Contain("verified");
        proof.DocketId.Should().Be("docket-42");
        proof.MerkleRoot.Should().BeEquivalentTo(merkleRootBytes);
    }

    [Fact]
    public void FullFlow_VerifyInclusionProof_WithMerkleVerification()
    {
        // Arrange — 3 transactions
        var txIds = Enumerable.Range(0, 3)
            .Select(i => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"tx-payload-{i}"))).ToLowerInvariant())
            .ToList();

        var merkleTree = new MerkleTree(_hashProvider);
        var merkleRoot = merkleTree.ComputeMerkleRoot(txIds.AsReadOnly());

        var targetTxId = txIds[2]; // Last transaction
        var proofPath = BuildMerkleProofPath(txIds, targetTxId);

        // Generate ZK proof
        var zkProof = _zkProvider.GenerateInclusionProof(
            Convert.FromHexString(targetTxId),
            Convert.FromHexString(merkleRoot),
            proofPath.Select(Convert.FromHexString).ToArray(),
            "docket-99");

        // Act — auditor verifies both the ZK proof AND the Merkle proof
        var zkResult = _zkProvider.VerifyInclusionProof(zkProof);
        var merkleValid = merkleTree.VerifyMerkleProof(targetTxId, merkleRoot, proofPath.AsReadOnly());

        // Assert — both verifications succeed
        zkResult.IsValid.Should().BeTrue("ZK proof should verify");
        merkleValid.Should().BeTrue("Merkle proof should verify");
    }

    [Fact]
    public void FullFlow_DifferentTransactions_ProduceIndependentProofs()
    {
        // Arrange — same docket, two different transactions
        var txIds = Enumerable.Range(0, 4)
            .Select(i => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"payload-{i}"))).ToLowerInvariant())
            .ToList();

        var merkleTree = new MerkleTree(_hashProvider);
        var merkleRoot = merkleTree.ComputeMerkleRoot(txIds.AsReadOnly());
        var rootBytes = Convert.FromHexString(merkleRoot);

        // Generate proofs for tx0 and tx3
        var proof0 = _zkProvider.GenerateInclusionProof(
            Convert.FromHexString(txIds[0]), rootBytes,
            BuildMerkleProofPath(txIds, txIds[0]).Select(Convert.FromHexString).ToArray(),
            "docket-1");

        var proof3 = _zkProvider.GenerateInclusionProof(
            Convert.FromHexString(txIds[3]), rootBytes,
            BuildMerkleProofPath(txIds, txIds[3]).Select(Convert.FromHexString).ToArray(),
            "docket-1");

        // Act — verify both independently
        var result0 = _zkProvider.VerifyInclusionProof(proof0);
        var result3 = _zkProvider.VerifyInclusionProof(proof3);

        // Assert — both valid, commitments differ (zero-knowledge property)
        result0.IsValid.Should().BeTrue();
        result3.IsValid.Should().BeTrue();
        proof0.Commitment.Should().NotBeEquivalentTo(proof3.Commitment,
            "different transactions should produce different commitments");
    }

    [Fact]
    public void FullFlow_TamperedProof_RejectedByAuditor()
    {
        // Arrange — generate a valid proof
        var txIds = new[] { "tx-alpha", "tx-beta", "tx-gamma" }
            .Select(p => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(p))).ToLowerInvariant())
            .ToList();

        var merkleTree = new MerkleTree(_hashProvider);
        var merkleRoot = merkleTree.ComputeMerkleRoot(txIds.AsReadOnly());

        var proof = _zkProvider.GenerateInclusionProof(
            Convert.FromHexString(txIds[0]),
            Convert.FromHexString(merkleRoot),
            BuildMerkleProofPath(txIds, txIds[0]).Select(Convert.FromHexString).ToArray(),
            "docket-tamper-test");

        // Valid proof first
        _zkProvider.VerifyInclusionProof(proof).IsValid.Should().BeTrue();

        // Act — tamper with the Schnorr proof data
        proof.ProofData[40] ^= 0xFF;

        // Assert — tampered proof is rejected
        var result = _zkProvider.VerifyInclusionProof(proof);
        result.IsValid.Should().BeFalse("tampered proof should be rejected");
    }

    [Fact]
    public void FullFlow_RangeProof_AuditorVerifiesAmountInRange()
    {
        // Scenario: auditor needs to verify a transaction amount is within policy limits
        // (e.g., payout between 0 and 65535) without seeing the actual amount
        long secretAmount = 1500;
        int bitLength = 16; // Range: [0, 65535]

        // Act — generate range proof for the secret amount
        var proof = _rangeProvider.GenerateRangeProof(secretAmount, bitLength);

        // Auditor receives the proof and verifies range compliance
        var result = _rangeProvider.VerifyRangeProof(proof);

        // Assert — valid proof, auditor knows amount is in range without seeing it
        result.IsValid.Should().BeTrue(result.Message);
        proof.BitLength.Should().Be(16);
        proof.Commitment.Should().HaveCount(33, "compressed EC point");
    }

    [Fact]
    public void FullFlow_CombinedProofs_InclusionAndRange()
    {
        // Full auditor scenario: verify both transaction inclusion AND amount compliance

        // Step 1: Set up docket with transactions
        var txIds = Enumerable.Range(0, 5)
            .Select(i => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"tx-{i}"))).ToLowerInvariant())
            .ToList();

        var merkleTree = new MerkleTree(_hashProvider);
        var merkleRoot = merkleTree.ComputeMerkleRoot(txIds.AsReadOnly());

        // Step 2: Generate inclusion proof for target transaction
        var targetIdx = 2;
        var inclusionProof = _zkProvider.GenerateInclusionProof(
            Convert.FromHexString(txIds[targetIdx]),
            Convert.FromHexString(merkleRoot),
            BuildMerkleProofPath(txIds, txIds[targetIdx]).Select(Convert.FromHexString).ToArray(),
            "docket-combined");

        // Step 3: Generate range proof for the transaction amount
        long txAmount = 2500;
        var rangeProof = _rangeProvider.GenerateRangeProof(txAmount, 16);

        // Step 4: Auditor verifies both proofs
        var inclusionResult = _zkProvider.VerifyInclusionProof(inclusionProof);
        var rangeResult = _rangeProvider.VerifyRangeProof(rangeProof);

        // Assert — both proofs verify successfully
        inclusionResult.IsValid.Should().BeTrue("transaction is in the docket");
        rangeResult.IsValid.Should().BeTrue("amount is within policy range");
    }

    [Fact]
    public void FullFlow_SingleTransactionDocket_InclusionProofWorks()
    {
        // Edge case: docket with a single transaction
        var txId = Convert.ToHexString(SHA256.HashData("solo-transaction"u8.ToArray())).ToLowerInvariant();
        var txIds = new List<string> { txId };

        var merkleTree = new MerkleTree(_hashProvider);
        var merkleRoot = merkleTree.ComputeMerkleRoot(txIds.AsReadOnly());

        // For single tx, merkle root == tx hash, proof path is empty
        var proof = _zkProvider.GenerateInclusionProof(
            Convert.FromHexString(txId),
            Convert.FromHexString(merkleRoot),
            Array.Empty<byte[]>(),
            "docket-single");

        var result = _zkProvider.VerifyInclusionProof(proof);
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Builds a Merkle proof path (sibling hashes) for a target transaction.
    /// Mirrors the MerkleTree.ComputeMerkleRoot algorithm.
    /// </summary>
    private List<string> BuildMerkleProofPath(List<string> txIds, string targetTxId)
    {
        if (txIds.Count <= 1)
            return [];

        var proofPath = new List<string>();
        var currentLevel = txIds.Select(h => h.ToLowerInvariant()).ToList();
        int targetIdx = currentLevel.FindIndex(h => string.Equals(h, targetTxId, StringComparison.OrdinalIgnoreCase));
        if (targetIdx < 0)
            return [];

        while (currentLevel.Count > 1)
        {
            var nextLevel = new List<string>();
            int nextTargetIdx = targetIdx / 2;

            for (int i = 0; i < currentLevel.Count; i += 2)
            {
                string left = currentLevel[i];
                string right = (i + 1 < currentLevel.Count) ? currentLevel[i + 1] : left;

                if (i == targetIdx || i + 1 == targetIdx)
                {
                    proofPath.Add(i == targetIdx ? right : left);
                }

                // Matches MerkleTree.CombineAndHash
                string combined = left + right;
                byte[] combinedBytes = System.Text.Encoding.UTF8.GetBytes(combined);
                byte[] hash = _hashProvider.ComputeHash(combinedBytes, Sorcha.Cryptography.Enums.HashType.SHA256);
                nextLevel.Add(Convert.ToHexString(hash).ToLowerInvariant());
            }

            currentLevel = nextLevel;
            targetIdx = nextTargetIdx;
        }

        return proofPath;
    }
}
