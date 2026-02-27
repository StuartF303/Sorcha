// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors
using System.ComponentModel.DataAnnotations;

namespace Sorcha.Wallet.Service.Models;

/// <summary>
/// Request model for updating wallet metadata
/// </summary>
public class UpdateWalletRequest
{
    /// <summary>
    /// New wallet name
    /// </summary>
    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; set; }

    /// <summary>
    /// Metadata tags to add/update
    /// </summary>
    public Dictionary<string, string>? Tags { get; set; }
}
