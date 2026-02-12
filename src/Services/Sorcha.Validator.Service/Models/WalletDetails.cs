// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Validator.Service.Models;

/// <summary>
/// Represents cached information about the validator's wallet retrieved from the Wallet Service.
/// </summary>
/// <remarks>
/// <para>
/// This class stores wallet metadata that is cached in memory for the lifetime of the
/// Validator Service. The cache is invalidated and refreshed when wallet rotation is
/// detected (version change).
/// </para>
///
/// <para><b>Lifecycle:</b></para>
/// <list type="bullet">
///   <item>Created once during Validator Service initialization</item>
///   <item>Cached in memory for service lifetime</item>
///   <item>Invalidated on wallet rotation detection</item>
///   <item>Never persisted to disk/logs/database (security requirement SC-006)</item>
/// </list>
///
/// <para><b>Related Requirements:</b></para>
/// <list type="bullet">
///   <item>FR-002: Validator MUST cache wallet details for service lifetime</item>
///   <item>FR-015: Validator MUST detect wallet key rotation and refresh cache</item>
///   <item>SC-001: Wallet initialization completes in under 5 seconds</item>
/// </list>
/// </remarks>
public class WalletDetails
{
    /// <summary>
    /// Unique identifier for the wallet.
    /// </summary>
    /// <remarks>
    /// This is typically a GUID matching the wallet ID in the Wallet Service.
    /// </remarks>
    public required string WalletId { get; init; }

    /// <summary>
    /// Bech32-encoded wallet address.
    /// </summary>
    /// <remarks>
    /// Format: ws11q... (Sorcha wallet prefix "ws1" with Bech32m encoding).
    /// This address identifies the validator in the blockchain network and is
    /// included in the ProposerValidatorId field of dockets.
    /// </remarks>
    /// <example>ws11qr4f5ulrxg450l2zunexd7mscapvcx9mzefwq8lp5ntnuj2e9lwkkczg0t6</example>
    public required string Address { get; init; }

    /// <summary>
    /// Public key bytes for signature verification.
    /// </summary>
    /// <remarks>
    /// The public key is used by peer validators to verify signatures on dockets
    /// and consensus votes. Key length depends on the algorithm:
    /// <list type="bullet">
    ///   <item>ED25519: 32 bytes</item>
    ///   <item>NISTP256: 32 bytes</item>
    ///   <item>RSA4096: 512 bytes</item>
    /// </list>
    /// </remarks>
    public required byte[] PublicKey { get; init; }

    /// <summary>
    /// Cryptographic algorithm used by this wallet.
    /// </summary>
    /// <remarks>
    /// Determines which signing and verification methods are used from the
    /// Sorcha.Cryptography library.
    /// </remarks>
    public required WalletAlgorithm Algorithm { get; init; }

    /// <summary>
    /// Wallet version for rotation detection.
    /// </summary>
    /// <remarks>
    /// This version number increments when the wallet's cryptographic keys are
    /// rotated. The Validator Service compares this version before and after
    /// signing operations to detect rotation and invalidate the cache.
    /// </remarks>
    public required int Version { get; init; }

    /// <summary>
    /// BIP44 derivation path for this wallet, if applicable.
    /// </summary>
    /// <remarks>
    /// Format: m/44'/coin_type'/account'/change/address_index
    /// Optional field - may be null for non-HD wallets or when not using derivation.
    /// </remarks>
    /// <example>m/44'/0'/0'/0/0</example>
    public string? DerivationPath { get; init; }

    /// <summary>
    /// UTC timestamp when wallet details were cached.
    /// </summary>
    /// <remarks>
    /// Used for logging and diagnostics to track when the wallet was last retrieved
    /// from the Wallet Service.
    /// </remarks>
    public required DateTimeOffset CachedAt { get; init; }
}
