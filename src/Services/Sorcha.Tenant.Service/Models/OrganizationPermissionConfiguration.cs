// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Tenant.Service.Models;

/// <summary>
/// Organization-level permissions for blockchain access and operations.
/// Stored in per-organization schema (org_{organization_id}).
/// Defines what resources and operations organization members can access.
/// </summary>
public class OrganizationPermissionConfiguration
{
    /// <summary>
    /// Unique configuration identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Associated organization ID (one configuration per organization).
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// List of blockchain IDs that members can access.
    /// Empty array means no blockchain access.
    /// </summary>
    public Guid[] ApprovedBlockchains { get; set; } = Array.Empty<Guid>();

    /// <summary>
    /// Whether members can create new blockchains.
    /// Secure by default (false).
    /// </summary>
    public bool CanCreateBlockchain { get; set; } = false;

    /// <summary>
    /// Whether members can publish blueprints to the network.
    /// Secure by default (false).
    /// </summary>
    public bool CanPublishBlueprint { get; set; } = false;

    /// <summary>
    /// Last update timestamp (UTC).
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
