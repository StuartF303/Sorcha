// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Tenant.Models;

/// <summary>
/// Search criteria for participant discovery.
/// </summary>
public record ParticipantSearchCriteria
{
    /// <summary>
    /// Search query term (matches display name, email, or wallet address).
    /// </summary>
    public string? Query { get; init; }

    /// <summary>
    /// Filter by specific organization ID.
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
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of results per page.
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Organization IDs the requesting user has access to (for visibility filtering).
    /// </summary>
    public IReadOnlyList<Guid>? AccessibleOrganizations { get; init; }

    /// <summary>
    /// Whether the requesting user has system admin privileges (bypasses org filtering).
    /// </summary>
    public bool IsSystemAdmin { get; init; }
}
