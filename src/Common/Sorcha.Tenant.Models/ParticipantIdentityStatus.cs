// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Tenant.Models;

/// <summary>
/// Status of a participant identity within an organization.
/// </summary>
public enum ParticipantIdentityStatus
{
    /// <summary>
    /// Participant is active and can participate in workflows.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Participant has been soft-deleted but preserved for audit trail.
    /// </summary>
    Inactive = 1,

    /// <summary>
    /// Participant has been temporarily disabled by an administrator.
    /// </summary>
    Suspended = 2
}
