// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Wallet.Service.Models;

/// <summary>
/// Request to create or retrieve a system wallet for a validator
/// </summary>
public class SystemWalletRequest
{
    /// <summary>
    /// Validator ID requesting the system wallet
    /// </summary>
    public string? ValidatorId { get; set; }
}

/// <summary>
/// Response containing the system wallet address
/// </summary>
public class SystemWalletResponse
{
    /// <summary>
    /// Address of the system wallet
    /// </summary>
    public required string Address { get; set; }
}
