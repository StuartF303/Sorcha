// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

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
}
