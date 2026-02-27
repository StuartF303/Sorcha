// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors
namespace Sorcha.Wallet.Service.Models;

/// <summary>
/// Response model for gap limit status
/// </summary>
public class GapStatusResponse
{
    /// <summary>
    /// Wallet address
    /// </summary>
    public required string WalletAddress { get; set; }

    /// <summary>
    /// Gap status per account and address type
    /// </summary>
    public List<AccountGapStatus> Accounts { get; set; } = new();

    /// <summary>
    /// Overall gap limit compliance
    /// </summary>
    public bool IsCompliant => Accounts.All(a => a.IsCompliant);

    /// <summary>
    /// Warning message if approaching limit
    /// </summary>
    public string? Warning { get; set; }
}

/// <summary>
/// Gap status for a specific account and address type
/// </summary>
public class AccountGapStatus
{
    /// <summary>
    /// BIP44 account number
    /// </summary>
    public uint Account { get; set; }

    /// <summary>
    /// Address type (receive or change)
    /// </summary>
    public string AddressType { get; set; } = "receive";

    /// <summary>
    /// Number of unused addresses
    /// </summary>
    public int UnusedCount { get; set; }

    /// <summary>
    /// Maximum recommended gap (20 per BIP44)
    /// </summary>
    public int MaxRecommendedGap { get; set; } = 20;

    /// <summary>
    /// Is within recommended limit?
    /// </summary>
    public bool IsCompliant => UnusedCount < MaxRecommendedGap;

    /// <summary>
    /// Last used address index
    /// </summary>
    public int? LastUsedIndex { get; set; }

    /// <summary>
    /// Next recommended index
    /// </summary>
    public int NextRecommendedIndex => (LastUsedIndex ?? -1) + 1;
}
