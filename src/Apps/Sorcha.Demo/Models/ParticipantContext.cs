// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Demo.Models;

/// <summary>
/// Stores information about a participant and their wallet
/// </summary>
public class ParticipantContext
{
    /// <summary>
    /// Participant identifier (e.g., "Employee", "Manager")
    /// </summary>
    public string ParticipantId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Wallet address
    /// </summary>
    public string WalletAddress { get; set; } = string.Empty;

    /// <summary>
    /// Wallet algorithm (ED25519, NIST_P256, RSA4096)
    /// </summary>
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>
    /// Mnemonic phrase (INSECURE - demo only!)
    /// Stored only for demo purposes to allow wallet recreation
    /// </summary>
    public string? Mnemonic { get; set; }

    /// <summary>
    /// When the wallet was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Number of actions this participant has executed
    /// </summary>
    public int ActionsExecuted { get; set; }
}
