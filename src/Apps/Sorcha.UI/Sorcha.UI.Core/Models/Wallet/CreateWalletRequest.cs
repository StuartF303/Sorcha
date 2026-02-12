// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.ComponentModel.DataAnnotations;

namespace Sorcha.UI.Core.Models.Wallet;

/// <summary>
/// Request model for creating a new wallet
/// </summary>
public class CreateWalletRequest
{
    /// <summary>
    /// Friendly name for the wallet
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; set; }

    /// <summary>
    /// Cryptographic algorithm (ED25519, NISTP256, RSA4096)
    /// </summary>
    [Required]
    public required string Algorithm { get; set; }

    /// <summary>
    /// Number of words in mnemonic (12, 15, 18, 21, or 24)
    /// </summary>
    [Range(12, 24)]
    public int WordCount { get; set; } = 12;

    /// <summary>
    /// Optional passphrase for additional security
    /// </summary>
    public string? Passphrase { get; set; }

    /// <summary>
    /// Optional metadata tags
    /// </summary>
    public Dictionary<string, string>? Tags { get; set; }
}
