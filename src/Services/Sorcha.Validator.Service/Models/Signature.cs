// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Validator.Service.Models;

/// <summary>
/// Represents a cryptographic signature produced by the validator wallet.
/// </summary>
/// <remarks>
/// <para>
/// This class encapsulates a digital signature created using the validator's wallet.
/// Signatures are attached to domain objects (Docket, ConsensusVote) and persisted
/// to the Register Service as part of the blockchain ledger.
/// </para>
///
/// <para><b>Related Requirements:</b></para>
/// <list type="bullet">
///   <item>FR-003: Validator MUST sign all dockets before broadcasting</item>
///   <item>FR-004: Validator MUST sign all consensus votes</item>
///   <item>FR-005: Validator MUST verify peer vote signatures</item>
///   <item>SC-002: 100% of dockets contain valid signatures</item>
/// </list>
/// </remarks>
public class Signature
{
    /// <summary>
    /// Signer's public key bytes.
    /// </summary>
    /// <remarks>
    /// This public key can be used to verify the signature. Key length depends on algorithm:
    /// <list type="bullet">
    ///   <item>ED25519: 32 bytes</item>
    ///   <item>NISTP256: 32 bytes</item>
    ///   <item>RSA4096: 512 bytes</item>
    /// </list>
    /// </remarks>
    public required byte[] PublicKey { get; init; }

    /// <summary>
    /// The cryptographic signature bytes.
    /// </summary>
    /// <remarks>
    /// Raw signature bytes produced by signing the data hash. Signature length varies by algorithm.
    /// </remarks>
    public required byte[] SignatureValue { get; init; }

    /// <summary>
    /// Signature algorithm used (e.g., "ED25519", "NISTP256", "RSA4096").
    /// </summary>
    /// <remarks>
    /// This string value should match one of the WalletAlgorithm enum names.
    /// Used to select the appropriate verification method.
    /// </remarks>
    public required string Algorithm { get; init; }

    /// <summary>
    /// UTC timestamp when the signature was created.
    /// </summary>
    /// <remarks>
    /// Used for audit logging and to track when signing operations occurred.
    /// </remarks>
    public required DateTimeOffset SignedAt { get; init; }

    /// <summary>
    /// Bech32-encoded wallet address of the signer (optional).
    /// </summary>
    /// <remarks>
    /// The wallet address that created this signature. Optional field that can be
    /// used to identify the signing validator without needing to derive the address
    /// from the public key.
    /// </remarks>
    /// <example>ws11qr4f5ulrxg450l2zunexd7mscapvcx9mzefwq8lp5ntnuj2e9lwkkczg0t6</example>
    public string? SignedBy { get; init; }
}
