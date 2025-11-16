// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Blueprint.Service.Models.Requests;

/// <summary>
/// Request to reject a pending action
/// </summary>
public record ActionRejectionRequest
{
    /// <summary>
    /// The hash of the transaction being rejected
    /// </summary>
    public required string TransactionHash { get; init; }

    /// <summary>
    /// The reason for rejecting the action
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// The wallet address of the rejector
    /// </summary>
    public required string SenderWallet { get; init; }

    /// <summary>
    /// The register address
    /// </summary>
    public required string RegisterAddress { get; init; }
}
