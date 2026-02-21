// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.ComponentModel.DataAnnotations;

namespace Sorcha.Register.Models;

/// <summary>
/// Encrypted payload within a transaction
/// </summary>
public class PayloadModel
{
    /// <summary>
    /// Wallet addresses authorized to decrypt this payload
    /// </summary>
    public string[] WalletAccess { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Size of encrypted payload in bytes
    /// </summary>
    public ulong PayloadSize { get; set; }

    /// <summary>
    /// SHA-256 hash of payload for integrity
    /// </summary>
    [Required]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted data (Base64 encoded)
    /// </summary>
    [Required]
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Encryption metadata flags
    /// </summary>
    public string? PayloadFlags { get; set; }

    /// <summary>
    /// Initialization vector for encryption
    /// </summary>
    public Challenge? IV { get; set; }

    /// <summary>
    /// Per-wallet encryption challenges
    /// </summary>
    public Challenge[]? Challenges { get; set; }

    /// <summary>
    /// MIME type describing the plaintext data format (e.g., "application/json", "application/pdf").
    /// When absent (legacy payloads), inferred as "application/octet-stream".
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Encoding scheme for the Data field. Supported values: "identity" (native JSON),
    /// "base64url" (RFC 4648 §5), "base64" (legacy read-only), "br+base64url", "gzip+base64url".
    /// When absent (legacy payloads), inferred as "base64".
    /// </summary>
    public string? ContentEncoding { get; set; }
}
