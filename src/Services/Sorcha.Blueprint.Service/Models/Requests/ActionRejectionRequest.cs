// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.ComponentModel.DataAnnotations;

namespace Sorcha.Blueprint.Service.Models.Requests;

/// <summary>
/// Request to reject an action in a workflow.
/// Rejection routes the workflow according to the action's rejection configuration.
/// </summary>
public record ActionRejectionRequest
{
    /// <summary>
    /// The reason for rejecting the action (required when rejectionConfig.requireReason is true)
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public required string Reason { get; init; }

    /// <summary>
    /// The hash of the transaction being rejected (legacy support)
    /// </summary>
    public string? TransactionHash { get; init; }

    /// <summary>
    /// The wallet address of the rejector (legacy support)
    /// </summary>
    public string? SenderWallet { get; init; }

    /// <summary>
    /// The register address (legacy support)
    /// </summary>
    public string? RegisterAddress { get; init; }

    /// <summary>
    /// Specific field errors to communicate to the target participant
    /// </summary>
    public Dictionary<string, string>? FieldErrors { get; init; }

    /// <summary>
    /// Branch ID for parallel workflows.
    /// Required when rejecting an action in a specific branch.
    /// </summary>
    public string? BranchId { get; init; }

    /// <summary>
    /// Additional rejection metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
