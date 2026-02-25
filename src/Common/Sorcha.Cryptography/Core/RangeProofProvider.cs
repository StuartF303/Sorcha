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
/// Generates and verifies Bulletproof-style range proofs using Pedersen commitments
/// on secp256k1. Proves that a committed value lies within [0, 2^bitLength) without
/// revealing the actual value.
///
/// Uses bit decomposition with per-bit OR proofs (Sigma protocol) to prove each bit
/// is 0 or 1, then aggregates to prove the committed value equals the bit sum.
/// </summary>
public class RangeProofProvider
{
    private static readonly X9ECParameters CurveParams = CustomNamedCurves.GetByName("secp256k1");
    private static readonly ECPoint G = CurveParams.G;
    private static readonly BigInteger N = CurveParams.N;

    // Same H generator as ZKInclusionProofProvider — deterministic derivation
    private static readonly ECPoint H = DeriveGeneratorH();

    /// <summary>Maximum supported bit length for range proofs.</summary>
    public const int MaxBitLength = 64;

    /// <summary>
    /// Generates a range proof that a value lies within [0, 2^bitLength).
    /// </summary>
    /// <param name="value">The secret value to prove is in range.</param>
    /// <param name="bitLength">Number of bits defining the range [0, 2^bitLength).</param>
    /// <returns>A range proof that can be verified without knowledge of the value.</returns>
    public RangeProof GenerateRangeProof(long value, int bitLength)
    {
        if (bitLength <= 0 || bitLength > MaxBitLength)
            throw new ArgumentOutOfRangeException(nameof(bitLength), $"Bit length must be between 1 and {MaxBitLength}");
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be non-negative");
        if (value >= (1L << bitLength))
            throw new ArgumentOutOfRangeException(nameof(value), $"Value must be less than 2^{bitLength}");

        // Generate blinding factor for the value commitment
        var r = GenerateRandomScalar();
        var vBig = BigInteger.ValueOf(value);

        // Value commitment: C = v*G + r*H
        var commitment = G.Multiply(vBig).Add(H.Multiply(r)).Normalize();

        // Bit decomposition
        var bits = new int[bitLength];
        for (int i = 0; i < bitLength; i++)
            bits[i] = (int)((value >> i) & 1);

        // Per-bit blinding factors (must sum to r for aggregation)
        var bitBlindings = new BigInteger[bitLength];
        var blindingSum = BigInteger.Zero;
        for (int i = 0; i < bitLength - 1; i++)
        {
            bitBlindings[i] = GenerateRandomScalar();
            blindingSum = blindingSum.Add(bitBlindings[i]).Mod(N);
        }
        // Last blinding factor: ensures sum of (2^i * r_i) == r
        // We need: sum(2^i * r_i) = r mod N
        // Compute: r_last = (r - sum(2^i * r_i for i < last)) * (2^last)^-1 mod N
        var weightedSum = BigInteger.Zero;
        for (int i = 0; i < bitLength - 1; i++)
        {
            var weight = BigInteger.One.ShiftLeft(i);
            weightedSum = weightedSum.Add(weight.Multiply(bitBlindings[i])).Mod(N);
        }
        var lastWeight = BigInteger.One.ShiftLeft(bitLength - 1);
        var lastWeightInv = lastWeight.ModInverse(N);
        bitBlindings[bitLength - 1] = r.Subtract(weightedSum).Multiply(lastWeightInv).Mod(N);

        // Generate per-bit commitments: C_i = b_i*G + r_i*H
        var bitCommitments = new byte[bitLength][];
        var bitPoints = new ECPoint[bitLength];
        for (int i = 0; i < bitLength; i++)
        {
            var bScalar = BigInteger.ValueOf(bits[i]);
            var point = G.Multiply(bScalar).Add(H.Multiply(bitBlindings[i])).Normalize();
            bitPoints[i] = point;
            bitCommitments[i] = point.GetEncoded(true);
        }

        // Generate OR proofs for each bit (proves C_i commits to 0 or 1)
        var bitProofs = new byte[bitLength][];
        for (int i = 0; i < bitLength; i++)
        {
            bitProofs[i] = GenerateBitORProof(bitPoints[i], bits[i], bitBlindings[i]);
        }

        // Generate aggregation proof: sum(2^i * C_i) == C
        var aggregationProof = GenerateAggregationProof(
            commitment, bitPoints, bitBlindings, bits, r, bitLength);

        return new RangeProof
        {
            Commitment = commitment.GetEncoded(true),
            BitLength = bitLength,
            BitCommitments = bitCommitments,
            BitProofs = bitProofs,
            AggregationProof = aggregationProof,
            VerificationKey = EncodeVerificationKey()
        };
    }

    /// <summary>
    /// Verifies a range proof.
    /// </summary>
    /// <param name="proof">The range proof to verify.</param>
    /// <returns>Verification result.</returns>
    public ZKVerificationResult VerifyRangeProof(RangeProof proof)
    {
        ArgumentNullException.ThrowIfNull(proof);

        try
        {
            if (proof.BitLength <= 0 || proof.BitLength > MaxBitLength)
                return ZKVerificationResult.Invalid($"Invalid bit length: {proof.BitLength}");

            if (proof.BitCommitments.Length != proof.BitLength)
                return ZKVerificationResult.Invalid("Bit commitments count doesn't match bit length");

            if (proof.BitProofs.Length != proof.BitLength)
                return ZKVerificationResult.Invalid("Bit proofs count doesn't match bit length");

            // Decode commitment
            var commitment = CurveParams.Curve.DecodePoint(proof.Commitment);
            if (commitment.IsInfinity)
                return ZKVerificationResult.Invalid("Commitment is point at infinity");

            // Decode and verify each bit commitment + OR proof
            var bitPoints = new ECPoint[proof.BitLength];
            for (int i = 0; i < proof.BitLength; i++)
            {
                bitPoints[i] = CurveParams.Curve.DecodePoint(proof.BitCommitments[i]);
                if (bitPoints[i].IsInfinity)
                    return ZKVerificationResult.Invalid($"Bit commitment {i} is point at infinity");

                if (!VerifyBitORProof(bitPoints[i], proof.BitProofs[i]))
                    return ZKVerificationResult.Invalid($"Bit OR proof {i} verification failed");
            }

            // Verify aggregation: sum(2^i * C_i) == C
            var (aggSuccess, aggDetail) = VerifyAggregationProofDetailed(
                commitment, bitPoints, proof.AggregationProof, proof.BitLength);
            if (!aggSuccess)
                return ZKVerificationResult.Invalid($"Aggregation proof verification failed: {aggDetail}");

            return ZKVerificationResult.Valid(
                $"Range proof verified: committed value is in [0, 2^{proof.BitLength})");
        }
        catch (Exception ex)
        {
            return ZKVerificationResult.Invalid($"Range proof verification error: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates an OR proof that C_i commits to 0 or 1.
    /// Uses Sigma protocol with simulation for the false branch.
    ///
    /// If bit=0: C_i = 0*G + r_i*H, so C_i - 0*G = r_i*H (real) and C_i - 1*G = r_i*H - G (simulated)
    /// If bit=1: C_i = 1*G + r_i*H, so C_i - 1*G = r_i*H (real) and C_i - 0*G = r_i*H + G (simulated)
    /// </summary>
    private byte[] GenerateBitORProof(ECPoint commitmentPoint, int bit, BigInteger blinding)
    {
        // For the case where bit=b (b is 0 or 1):
        // Real case: C - b*G = blinding*H → prove knowledge of blinding
        // Simulated case: C - (1-b)*G = blinding*H + (2b-1)*G → simulate

        // Simulated branch (1-bit)
        var simChallenge = GenerateRandomScalar();
        var simResponse = GenerateRandomScalar();

        // Real branch nonce
        var k = GenerateRandomScalar();

        ECPoint realStatement, simStatement;
        ECPoint realNonce, simReconstructed;

        if (bit == 0)
        {
            // Real: C - 0*G = blinding*H → prove knowledge of blinding in base H
            realStatement = commitmentPoint; // C - 0*G = C
            // Simulated: C - 1*G → simulate knowledge of opening
            simStatement = commitmentPoint.Add(G.Negate()).Normalize();
        }
        else
        {
            // Real: C - 1*G = blinding*H → prove knowledge of blinding in base H
            realStatement = commitmentPoint.Add(G.Negate()).Normalize();
            // Simulated: C - 0*G = C → simulate knowledge of opening
            simStatement = commitmentPoint;
        }

        // Real nonce commitment: R_real = k*H
        realNonce = H.Multiply(k).Normalize();

        // Simulated reconstruction: R_sim = simResponse*H - simChallenge*simStatement
        simReconstructed = H.Multiply(simResponse).Add(simStatement.Multiply(simChallenge).Negate()).Normalize();

        // Compute overall challenge: e = SHA-256(C || R0 || R1 || "SORCHA_ZK_RANGE_BIT_V1")
        ECPoint r0, r1;
        if (bit == 0)
        {
            r0 = realNonce;     // real branch = bit 0
            r1 = simReconstructed; // simulated branch = bit 1
        }
        else
        {
            r0 = simReconstructed; // simulated branch = bit 0
            r1 = realNonce;     // real branch = bit 1
        }

        var overallChallenge = ComputeBitChallenge(commitmentPoint, r0, r1);

        // Real challenge: e_real = overall - e_sim mod N
        var realChallenge = overallChallenge.Subtract(simChallenge).Mod(N);

        // Real response: s_real = k + e_real * blinding mod N
        var realResponse = k.Add(realChallenge.Multiply(blinding)).Mod(N);

        // Output: e0(32) || s0(32) || e1(32) || s1(32) = 128 bytes
        BigInteger e0, s0, e1, s1;
        if (bit == 0)
        {
            e0 = realChallenge; s0 = realResponse;
            e1 = simChallenge; s1 = simResponse;
        }
        else
        {
            e0 = simChallenge; s0 = simResponse;
            e1 = realChallenge; s1 = realResponse;
        }

        var proof = new byte[128];
        Buffer.BlockCopy(PadTo32(e0.ToByteArrayUnsigned()), 0, proof, 0, 32);
        Buffer.BlockCopy(PadTo32(s0.ToByteArrayUnsigned()), 0, proof, 32, 32);
        Buffer.BlockCopy(PadTo32(e1.ToByteArrayUnsigned()), 0, proof, 64, 32);
        Buffer.BlockCopy(PadTo32(s1.ToByteArrayUnsigned()), 0, proof, 96, 32);

        return proof;
    }

    /// <summary>
    /// Verifies an OR proof that a commitment is to 0 or 1.
    /// </summary>
    private bool VerifyBitORProof(ECPoint commitmentPoint, byte[] proofData)
    {
        if (proofData == null || proofData.Length != 128) return false;

        var e0 = new BigInteger(1, proofData[..32]);
        var s0 = new BigInteger(1, proofData[32..64]);
        var e1 = new BigInteger(1, proofData[64..96]);
        var s1 = new BigInteger(1, proofData[96..128]);

        // Statement 0: C - 0*G = C (prove knowledge of blinding in base H)
        var stmt0 = commitmentPoint;
        // Statement 1: C - 1*G (prove knowledge of blinding in base H)
        var stmt1 = commitmentPoint.Add(G.Negate()).Normalize();

        // Reconstruct nonces: R_i = s_i*H - e_i*stmt_i
        var r0 = H.Multiply(s0).Add(stmt0.Multiply(e0).Negate()).Normalize();
        var r1 = H.Multiply(s1).Add(stmt1.Multiply(e1).Negate()).Normalize();

        // Verify overall challenge: e0 + e1 == SHA-256(C || R0 || R1)
        var expectedChallenge = ComputeBitChallenge(commitmentPoint, r0, r1);
        var actualSum = e0.Add(e1).Mod(N);

        return actualSum.Equals(expectedChallenge);
    }

    /// <summary>
    /// Generates the aggregation proof: proves sum(2^i * C_i) == C.
    /// The proof is a Fiat-Shamir binding tag that commits to the relationship between
    /// the value commitment and all bit commitments. Combined with the per-bit OR proofs,
    /// this constitutes a complete range proof:
    /// - OR proofs prove each C_i commits to 0 or 1
    /// - Aggregation proves C = sum(2^i * C_i), binding the value to the bits
    /// </summary>
    private byte[] GenerateAggregationProof(
        ECPoint commitment,
        ECPoint[] bitPoints,
        BigInteger[] bitBlindings,
        int[] bits,
        BigInteger totalBlinding,
        int bitLength)
    {
        // Compute the weighted sum point: S = sum(2^i * C_i)
        var weightedSum = CurveParams.Curve.Infinity;
        for (int i = 0; i < bitLength; i++)
        {
            var weight = BigInteger.One.ShiftLeft(i);
            weightedSum = weightedSum.Add(bitPoints[i].Multiply(weight));
        }
        weightedSum = weightedSum.Normalize();

        // Compute binding tag: SHA-256(C || S || bitLength || "SORCHA_ZK_RANGE_AGG_V1")
        // This binds the commitment to the specific bit decomposition
        var cBytes = commitment.GetEncoded(true);
        var sBytes = weightedSum.GetEncoded(true);
        var ctx = System.Text.Encoding.UTF8.GetBytes("SORCHA_ZK_RANGE_AGG_BINDING_V1");
        var blBytes = BitConverter.GetBytes(bitLength);

        var input = new byte[cBytes.Length + sBytes.Length + blBytes.Length + ctx.Length];
        int offset = 0;
        Buffer.BlockCopy(cBytes, 0, input, offset, cBytes.Length); offset += cBytes.Length;
        Buffer.BlockCopy(sBytes, 0, input, offset, sBytes.Length); offset += sBytes.Length;
        Buffer.BlockCopy(blBytes, 0, input, offset, blBytes.Length); offset += blBytes.Length;
        Buffer.BlockCopy(ctx, 0, input, offset, ctx.Length);

        // The 64-byte proof: binding hash (32) || weighted sum encoding (32 from compressed point)
        var bindingHash = SHA256.HashData(input);
        var proof = new byte[64];
        Buffer.BlockCopy(bindingHash, 0, proof, 0, 32);
        // Include first 32 bytes of S encoding as additional binding
        Buffer.BlockCopy(sBytes, 0, proof, 32, Math.Min(sBytes.Length, 32));

        return proof;
    }

    /// <summary>
    /// Verifies the aggregation proof: checks sum(2^i * C_i) == C and validates binding tag.
    /// </summary>
    private (bool Success, string Detail) VerifyAggregationProofDetailed(
        ECPoint commitment,
        ECPoint[] bitPoints,
        byte[] proofData,
        int bitLength)
    {
        if (proofData == null || proofData.Length != 64)
            return (false, "Invalid proof data length");

        // Compute weighted sum: S = sum(2^i * C_i)
        var weightedSum = CurveParams.Curve.Infinity;
        for (int i = 0; i < bitLength; i++)
        {
            var weight = BigInteger.One.ShiftLeft(i);
            weightedSum = weightedSum.Add(bitPoints[i].Multiply(weight));
        }
        weightedSum = weightedSum.Normalize();

        // Check S == C (the commitment should equal the weighted sum)
        var cEnc = commitment.GetEncoded(true);
        var sEnc = weightedSum.GetEncoded(true);
        if (!cEnc.SequenceEqual(sEnc))
            return (false, $"Point mismatch C={Convert.ToHexString(cEnc[..8])}... S={Convert.ToHexString(sEnc[..8])}...");

        // Verify binding tag
        var ctx = System.Text.Encoding.UTF8.GetBytes("SORCHA_ZK_RANGE_AGG_BINDING_V1");
        var blBytes = BitConverter.GetBytes(bitLength);
        var input = new byte[cEnc.Length + sEnc.Length + blBytes.Length + ctx.Length];
        int offset = 0;
        Buffer.BlockCopy(cEnc, 0, input, offset, cEnc.Length); offset += cEnc.Length;
        Buffer.BlockCopy(sEnc, 0, input, offset, sEnc.Length); offset += sEnc.Length;
        Buffer.BlockCopy(blBytes, 0, input, offset, blBytes.Length); offset += blBytes.Length;
        Buffer.BlockCopy(ctx, 0, input, offset, ctx.Length);

        var expectedBinding = SHA256.HashData(input);
        if (!proofData[..32].SequenceEqual(expectedBinding))
            return (false, "Binding tag mismatch");

        return (true, "OK");
    }

    private static BigInteger ComputeBitChallenge(ECPoint commitment, ECPoint r0, ECPoint r1)
    {
        var cBytes = commitment.GetEncoded(true);
        var r0Bytes = r0.GetEncoded(true);
        var r1Bytes = r1.GetEncoded(true);
        var ctx = System.Text.Encoding.UTF8.GetBytes("SORCHA_ZK_RANGE_BIT_V1");

        var input = new byte[cBytes.Length + r0Bytes.Length + r1Bytes.Length + ctx.Length];
        int offset = 0;
        Buffer.BlockCopy(cBytes, 0, input, offset, cBytes.Length); offset += cBytes.Length;
        Buffer.BlockCopy(r0Bytes, 0, input, offset, r0Bytes.Length); offset += r0Bytes.Length;
        Buffer.BlockCopy(r1Bytes, 0, input, offset, r1Bytes.Length); offset += r1Bytes.Length;
        Buffer.BlockCopy(ctx, 0, input, offset, ctx.Length);

        return new BigInteger(1, SHA256.HashData(input)).Mod(N);
    }

    /// <summary>
    /// Derives the second generator H — same as ZKInclusionProofProvider for consistency.
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
                var encoded = new byte[33];
                encoded[0] = 0x02;
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
        var gBytes = G.GetEncoded(true);
        var hBytes = H.GetEncoded(true);
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
