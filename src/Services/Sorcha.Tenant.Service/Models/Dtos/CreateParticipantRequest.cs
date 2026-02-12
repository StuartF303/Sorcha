// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.ComponentModel.DataAnnotations;

namespace Sorcha.Tenant.Service.Models.Dtos;

/// <summary>
/// Request model for creating a new participant identity.
/// Used by administrators to register users as participants.
/// </summary>
public record CreateParticipantRequest
{
    /// <summary>
    /// User ID from Tenant Service to register as participant.
    /// Must be an existing user in the organization.
    /// </summary>
    [Required]
    public Guid UserId { get; init; }

    /// <summary>
    /// Display name for the participant.
    /// If not provided, will be copied from the user's display name.
    /// </summary>
    [StringLength(256, MinimumLength = 1)]
    public string? DisplayName { get; init; }
}
