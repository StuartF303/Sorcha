// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Register.Models;

/// <summary>
/// Represents encryption challenge data for wallet-based decryption
/// </summary>
public class Challenge
{
    /// <summary>
    /// Challenge data (encrypted key material)
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// Wallet address this challenge is for
    /// </summary>
    public string? Address { get; set; }
}
