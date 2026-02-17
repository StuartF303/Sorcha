// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Blueprint.Service.Models;

/// <summary>
/// Per-organisation configuration for schema visibility filtering.
/// </summary>
public sealed class OrganisationSchemaPreferences
{
    /// <summary>
    /// MongoDB document ID.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Organisation identifier.
    /// </summary>
    public required string OrganizationId { get; set; }

    /// <summary>
    /// Sector IDs visible to designers. Null means all sectors enabled.
    /// Empty array means nothing visible.
    /// </summary>
    public string[]? EnabledSectors { get; set; }

    /// <summary>
    /// When preferences were last changed.
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; set; }

    /// <summary>
    /// User who made the last change.
    /// </summary>
    public string? ModifiedByUserId { get; set; }
}
