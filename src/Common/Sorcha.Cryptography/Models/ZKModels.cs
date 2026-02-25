// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System;
using System.Collections.Generic;

namespace Sorcha.Cryptography.Models;

/// <summary>
/// A zero-knowledge proof of transaction inclusion in a docket's Merkle tree.
/// Proves that a transaction exists in the tree without revealing the transaction
/// content or its exact position.
/// </summary>
public class ZKInclusionProof
{
    /// <summary>Docket ID containing the transaction.</summary>
    public required string DocketId { get; set; }

    /// <summary>Merkle root of the docket's transaction tree.</summary>
    public required byte[] MerkleRoot { get; set; }

    /// <summary>Pedersen commitment to the transaction hash: C = hash*G + r*H.</summary>
    public required byte[] Commitment { get; set; }

    /// <summary>
    /// Schnorr proof of knowledge of the commitment opening.
    /// Contains (challenge, responseValue, responseBlinding) concatenated.
    /// </summary>
    public required byte[] ProofData { get; set; }

    /// <summary>
    /// Blinded Merkle proof path — sibling hashes needed to reconstruct root.
    /// The prover commits to each sibling hash to avoid leaking tree structure.
    /// </summary>
    public required byte[][] MerkleProofPath { get; set; }

    /// <summary>
    /// Verification key — the public parameters (generator points) for proof verification.
    /// Encoded as G || H (two compressed EC points).
    /// </summary>
    public required byte[] VerificationKey { get; set; }
}

/// <summary>
/// A Bulletproof-style range proof demonstrating that a committed numeric value
/// lies within [0, 2^BitLength) without revealing the actual value.
/// Uses Pedersen commitments on secp256k1 with bit decomposition.
/// </summary>
public class RangeProof
{
    /// <summary>Pedersen commitment to the secret value: C = v*G + r*H.</summary>
    public required byte[] Commitment { get; set; }

    /// <summary>Number of bits in the range (value must be in [0, 2^BitLength)).</summary>
    public required int BitLength { get; set; }

    /// <summary>
    /// Per-bit commitments: C_i = b_i*G + r_i*H where b_i is the i-th bit.
    /// </summary>
    public required byte[][] BitCommitments { get; set; }

    /// <summary>
    /// Per-bit OR proofs: each proves C_i commits to 0 or 1 without revealing which.
    /// Each element contains (e0, s0, e1, s1) — the simulated and real challenge/response pairs.
    /// </summary>
    public required byte[][] BitProofs { get; set; }

    /// <summary>
    /// Aggregation proof: proves sum of bit commitments equals the value commitment.
    /// Contains (challenge, response) for the aggregation Schnorr proof.
    /// </summary>
    public required byte[] AggregationProof { get; set; }

    /// <summary>
    /// Verification key — the public parameters (generator points) for proof verification.
    /// Encoded as G || H (two compressed EC points).
    /// </summary>
    public required byte[] VerificationKey { get; set; }
}

/// <summary>
/// Result of a ZK proof verification operation.
/// </summary>
public class ZKVerificationResult
{
    /// <summary>Whether the proof verified successfully.</summary>
    public required bool IsValid { get; init; }

    /// <summary>Human-readable verification status message.</summary>
    public required string Message { get; init; }

    /// <summary>Creates a successful verification result.</summary>
    public static ZKVerificationResult Valid(string message = "Proof verified successfully") =>
        new() { IsValid = true, Message = message };

    /// <summary>Creates a failed verification result.</summary>
    public static ZKVerificationResult Invalid(string reason) =>
        new() { IsValid = false, Message = reason };
}
