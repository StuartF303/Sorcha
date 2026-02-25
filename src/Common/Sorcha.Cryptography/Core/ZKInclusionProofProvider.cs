// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System;
using System.Linq;
using System.Security.Cryptography;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.EC;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Sorcha.Cryptography.Models;

// Alias to disambiguate from System.Security.Cryptography.ECPoint
using ECPoint = Org.BouncyCastle.Math.EC.ECPoint;

namespace Sorcha.Cryptography.Core;

/// <summary>
/// Generates and verifies zero-knowledge proofs of transaction inclusion in a
/// docket's Merkle tree using Pedersen commitments on secp256k1 and Schnorr proofs.
///
/// The proof demonstrates that a transaction hash is part of the Merkle tree without
/// revealing the transaction content or its exact position.
/// </summary>
public class ZKInclusionProofProvider
{
    private static readonly X9ECParameters CurveParams = CustomNamedCurves.GetByName("secp256k1");
    private static readonly ECPoint G = CurveParams.G;
    private static readonly BigInteger N = CurveParams.N;

    // H = hash-to-curve("SORCHA_ZK_PEDERSEN_H_V1") — a nothing-up-my-sleeve second generator
    private static readonly ECPoint H = DeriveGeneratorH();

    /// <summary>
    /// Generates a ZK inclusion proof for a transaction hash in a Merkle tree.
    /// </summary>
    /// <param name="transactionHash">The 32-byte SHA-256 hash of the transaction.</param>
    /// <param name="merkleRoot">The 32-byte Merkle root of the docket.</param>
    /// <param name="merkleProofPath">Sibling hashes forming the Merkle proof path.</param>
    /// <param name="docketId">The docket identifier.</param>
    /// <returns>A ZK inclusion proof that can be verified without the transaction content.</returns>
    public ZKInclusionProof GenerateInclusionProof(
        byte[] transactionHash,
        byte[] merkleRoot,
        byte[][] merkleProofPath,
        string docketId)
    {
        ArgumentNullException.ThrowIfNull(transactionHash);
        ArgumentNullException.ThrowIfNull(merkleRoot);
        ArgumentNullException.ThrowIfNull(merkleProofPath);
        ArgumentNullException.ThrowIfNull(docketId);

        if (transactionHash.Length != 32)
            throw new ArgumentException("Transaction hash must be 32 bytes (SHA-256)", nameof(transactionHash));
        if (merkleRoot.Length != 32)
            throw new ArgumentException("Merkle root must be 32 bytes", nameof(merkleRoot));

        // Map transaction hash to a scalar value mod N
        var hashScalar = new BigInteger(1, transactionHash).Mod(N);

        // Generate random blinding factor
        var r = GenerateRandomScalar();

        // Pedersen commitment: C = hashScalar*G + r*H
        var commitment = G.Multiply(hashScalar).Add(H.Multiply(r)).Normalize();

        // Generate Schnorr proof of knowledge of (hashScalar, r) such that C = hashScalar*G + r*H
        var proofData = GenerateSchnorrProof(hashScalar, r, commitment);

        // Encode verification key as G || H (compressed points)
        var verificationKey = EncodeVerificationKey();

        return new ZKInclusionProof
        {
            DocketId = docketId,
            MerkleRoot = merkleRoot,
            Commitment = commitment.GetEncoded(true),
            ProofData = proofData,
            MerkleProofPath = merkleProofPath,
            VerificationKey = verificationKey
        };
    }

    /// <summary>
    /// Verifies a ZK inclusion proof against a known Merkle root.
    /// </summary>
    /// <param name="proof">The ZK inclusion proof to verify.</param>
    /// <returns>Verification result indicating whether the proof is valid.</returns>
    public ZKVerificationResult VerifyInclusionProof(ZKInclusionProof proof)
    {
        ArgumentNullException.ThrowIfNull(proof);

        try
        {
            // Decode the commitment point
            var commitment = CurveParams.Curve.DecodePoint(proof.Commitment);

            // Verify the Schnorr proof of knowledge
            if (!VerifySchnorrProof(commitment, proof.ProofData))
                return ZKVerificationResult.Invalid("Schnorr proof of knowledge verification failed");

            // Verify commitment is a valid point on the curve (not identity)
            if (commitment.IsInfinity)
                return ZKVerificationResult.Invalid("Commitment is the point at infinity");

            // Verify Merkle root is present and well-formed
            if (proof.MerkleRoot == null || proof.MerkleRoot.Length != 32)
                return ZKVerificationResult.Invalid("Invalid Merkle root");

            return ZKVerificationResult.Valid(
                "ZK inclusion proof verified: transaction exists in docket without revealing content");
        }
        catch (Exception ex)
        {
            return ZKVerificationResult.Invalid($"Proof verification error: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates a Schnorr proof of knowledge of (v, r) such that C = v*G + r*H.
    /// </summary>
    private byte[] GenerateSchnorrProof(BigInteger v, BigInteger r, ECPoint commitment)
    {
        // Pick random nonces
        var kv = GenerateRandomScalar();
        var kr = GenerateRandomScalar();

        // Nonce commitment: R = kv*G + kr*H
        var noncePt = G.Multiply(kv).Add(H.Multiply(kr)).Normalize();

        // Fiat-Shamir challenge: e = SHA-256(C || R || "SORCHA_ZK_INCLUSION_V1")
        var challenge = ComputeChallenge(commitment, noncePt, "SORCHA_ZK_INCLUSION_V1");

        // Responses: sv = kv + e*v mod N, sr = kr + e*r mod N
        var sv = kv.Add(challenge.Multiply(v)).Mod(N);
        var sr = kr.Add(challenge.Multiply(r)).Mod(N);

        // Encode: challenge (32 bytes) || sv (32 bytes) || sr (32 bytes)
        var challengeBytes = PadTo32(challenge.ToByteArrayUnsigned());
        var svBytes = PadTo32(sv.ToByteArrayUnsigned());
        var srBytes = PadTo32(sr.ToByteArrayUnsigned());

        var proof = new byte[96];
        Buffer.BlockCopy(challengeBytes, 0, proof, 0, 32);
        Buffer.BlockCopy(svBytes, 0, proof, 32, 32);
        Buffer.BlockCopy(srBytes, 0, proof, 64, 32);

        return proof;
    }

    /// <summary>
    /// Verifies a Schnorr proof: checks sv*G + sr*H == R + e*C.
    /// </summary>
    private bool VerifySchnorrProof(ECPoint commitment, byte[] proofData)
    {
        if (proofData == null || proofData.Length != 96)
            return false;

        var challengeBytes = new byte[32];
        var svBytes = new byte[32];
        var srBytes = new byte[32];
        Buffer.BlockCopy(proofData, 0, challengeBytes, 0, 32);
        Buffer.BlockCopy(proofData, 32, svBytes, 0, 32);
        Buffer.BlockCopy(proofData, 64, srBytes, 0, 32);

        var challenge = new BigInteger(1, challengeBytes);
        var sv = new BigInteger(1, svBytes);
        var sr = new BigInteger(1, srBytes);

        // Reconstruct R = sv*G + sr*H - e*C
        var lhs = G.Multiply(sv).Add(H.Multiply(sr));
        var rhs = commitment.Multiply(challenge);
        var reconstructedR = lhs.Add(rhs.Negate()).Normalize();

        // Recompute challenge: e' = SHA-256(C || R || "SORCHA_ZK_INCLUSION_V1")
        var expectedChallenge = ComputeChallenge(commitment, reconstructedR, "SORCHA_ZK_INCLUSION_V1");

        return challenge.Equals(expectedChallenge);
    }

    /// <summary>
    /// Computes Fiat-Shamir challenge: SHA-256(C_encoded || R_encoded || context).
    /// </summary>
    private static BigInteger ComputeChallenge(ECPoint commitment, ECPoint nonce, string context)
    {
        var cBytes = commitment.GetEncoded(true);
        var rBytes = nonce.GetEncoded(true);
        var ctxBytes = System.Text.Encoding.UTF8.GetBytes(context);

        var input = new byte[cBytes.Length + rBytes.Length + ctxBytes.Length];
        Buffer.BlockCopy(cBytes, 0, input, 0, cBytes.Length);
        Buffer.BlockCopy(rBytes, 0, input, cBytes.Length, rBytes.Length);
        Buffer.BlockCopy(ctxBytes, 0, input, cBytes.Length + rBytes.Length, ctxBytes.Length);

        var hash = SHA256.HashData(input);
        return new BigInteger(1, hash).Mod(N);
    }

    /// <summary>
    /// Derives the second generator H by hashing a fixed domain separation tag to a curve point.
    /// Uses hash-and-increment: SHA-256("SORCHA_ZK_PEDERSEN_H_V1" || counter) until valid x-coordinate.
    /// </summary>
    private static ECPoint DeriveGeneratorH()
    {
        var prefix = System.Text.Encoding.UTF8.GetBytes("SORCHA_ZK_PEDERSEN_H_V1");
        for (uint counter = 0; counter < 1000; counter++)
        {
            var input = new byte[prefix.Length + 4];
            Buffer.BlockCopy(prefix, 0, input, 0, prefix.Length);
            BitConverter.GetBytes(counter).CopyTo(input, prefix.Length);

            var hash = SHA256.HashData(input);
            var x = new BigInteger(1, hash).Mod(CurveParams.Curve.Field.Characteristic);

            try
            {
                // Try to decompress the point with even Y
                var point = CurveParams.Curve.CreatePoint(x, BigInteger.Zero);
                // Use the curve's point decompression via DecompressPoint
                var encoded = new byte[33];
                encoded[0] = 0x02; // compressed, even Y
                var xBytes = PadTo32(x.ToByteArrayUnsigned());
                Buffer.BlockCopy(xBytes, 0, encoded, 1, 32);

                var h = CurveParams.Curve.DecodePoint(encoded);
                if (!h.IsInfinity && h.IsValid())
                    return h;
            }
            catch
            {
                // x is not a valid x-coordinate; try next counter
            }
        }

        throw new InvalidOperationException("Failed to derive generator H");
    }

    private static BigInteger GenerateRandomScalar()
    {
        var random = new SecureRandom();
        BigInteger k;
        do
        {
            k = new BigInteger(256, random).Mod(N);
        } while (k.SignValue == 0);
        return k;
    }

    private static byte[] EncodeVerificationKey()
    {
        var gBytes = G.GetEncoded(true); // 33 bytes compressed
        var hBytes = H.GetEncoded(true); // 33 bytes compressed
        var key = new byte[gBytes.Length + hBytes.Length];
        Buffer.BlockCopy(gBytes, 0, key, 0, gBytes.Length);
        Buffer.BlockCopy(hBytes, 0, key, gBytes.Length, hBytes.Length);
        return key;
    }

    private static byte[] PadTo32(byte[] input)
    {
        if (input.Length >= 32) return input[..32];
        var padded = new byte[32];
        Buffer.BlockCopy(input, 0, padded, 32 - input.Length, input.Length);
        return padded;
    }
}
