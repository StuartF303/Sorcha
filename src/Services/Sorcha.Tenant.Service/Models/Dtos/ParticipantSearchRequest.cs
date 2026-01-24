// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;
using Sorcha.Tenant.Models;

namespace Sorcha.Tenant.Service.Models.Dtos;

/// <summary>
/// Request model for searching participants.
/// </summary>
public record ParticipantSearchRequest
{
    /// <summary>
    /// Search query term (matches display name, email).
    /// </summary>
    [StringLength(256)]
    public string? Query { get; init; }

    /// <summary>
    /// Filter by specific organization ID.
    /// If not specified, searches across accessible organizations.
    /// </summary>
    public Guid? OrganizationId { get; init; }

    /// <summary>
    /// Filter by participant status.
    /// </summary>
    public ParticipantIdentityStatus? Status { get; init; }

    /// <summary>
    /// Filter to only participants with linked wallets.
    /// </summary>
    public bool? HasLinkedWallet { get; init; }

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of results per page.
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
