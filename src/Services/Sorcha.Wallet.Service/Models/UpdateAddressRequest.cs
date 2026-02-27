// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors
namespace Sorcha.Wallet.Service.Models;

/// <summary>
/// Request model for updating wallet address metadata
/// </summary>
public class UpdateAddressRequest
{
    /// <summary>
    /// Updated label (null to leave unchanged)
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Updated notes (null to leave unchanged)
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Updated tags (null to leave unchanged)
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Additional metadata to merge (null to leave unchanged)
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
