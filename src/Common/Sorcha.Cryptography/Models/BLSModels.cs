// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Sorcha.Cryptography.Models;

/// <summary>
/// Threshold signature share produced by a single validator during distributed docket signing.
/// </summary>
public class BLSSigningShare
{
    /// <summary>Identifier of the signing validator.</summary>
    public required string ValidatorId { get; set; }

    /// <summary>Share index in the (t,n) scheme (1-based).</summary>
    public required uint ShareIndex { get; set; }

    /// <summary>BLS partial signature on the docket hash (G1 point, 48 bytes compressed).</summary>
    public required byte[] PartialSignature { get; set; }

    /// <summary>Hash of the docket being signed.</summary>
    public required string DocketHash { get; set; }
}

/// <summary>
/// Combined threshold signature for a docket, aggregated from t-of-n validator signing shares.
/// </summary>
public class BLSAggregateSignature
{
    /// <summary>Aggregated BLS signature (G1 point, 48 bytes compressed).</summary>
    public required byte[] Signature { get; set; }

    /// <summary>Bitfield indicating which validators contributed signing shares.</summary>
    public required byte[] SignerBitfield { get; set; }

    /// <summary>Minimum signers required (t).</summary>
    public required uint Threshold { get; set; }

    /// <summary>Total possible signers (n).</summary>
    public required uint TotalSigners { get; set; }
}

/// <summary>
/// Secret key share distributed to a single validator during threshold setup (DKG).
/// </summary>
public class BLSKeyShare : IDisposable
{
    /// <summary>Validator receiving this share.</summary>
    public required string ValidatorId { get; set; }

    /// <summary>Share index in the (t,n) scheme (1-based).</summary>
    public required uint ShareIndex { get; set; }

    /// <summary>Secret key share (Fr scalar, 32 bytes). Zeroized on Dispose.</summary>
    public required byte[] SecretShare { get; set; }

    /// <summary>Public key share (G2 point, 96 bytes compressed).</summary>
    public required byte[] PublicShare { get; set; }

    public void Dispose()
    {
        CryptographicOperations.ZeroMemory(SecretShare);
    }
}

/// <summary>
/// Result of BLS threshold key generation (DKG).
/// Contains the group public key and individual key shares for distribution.
/// </summary>
public class BLSThresholdKeySet : IDisposable
{
    /// <summary>Group public key used for aggregate signature verification (G2 point, 96 bytes).</summary>
    public required byte[] GroupPublicKey { get; set; }

    /// <summary>Individual key shares for each validator.</summary>
    public required List<BLSKeyShare> KeyShares { get; set; }

    /// <summary>Threshold (t) — minimum shares needed for valid aggregate.</summary>
    public required uint Threshold { get; set; }

    /// <summary>Total signers (n) — total number of key shares.</summary>
    public required uint TotalSigners { get; set; }

    public void Dispose()
    {
        foreach (var share in KeyShares)
            share.Dispose();
    }
}

/// <summary>
/// Result of aggregating BLS threshold signing shares.
/// </summary>
public class BLSAggregationResult
{
    /// <summary>Whether aggregation succeeded.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>The aggregated signature, if successful.</summary>
    public BLSAggregateSignature? AggregateSignature { get; init; }

    /// <summary>Error message if aggregation failed.</summary>
    public string? ErrorMessage { get; init; }

    public static BLSAggregationResult Success(BLSAggregateSignature sig) =>
        new() { IsSuccess = true, AggregateSignature = sig };

    public static BLSAggregationResult Failure(string error) =>
        new() { IsSuccess = false, ErrorMessage = error };
}
