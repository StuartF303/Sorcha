// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;
using Sorcha.Tenant.Models;

namespace Sorcha.Tenant.Service.Models.Dtos;

/// <summary>
/// Request model for updating an existing participant identity.
/// </summary>
public record UpdateParticipantRequest
{
    /// <summary>
    /// Updated display name for the participant.
    /// </summary>
    [StringLength(256, MinimumLength = 1)]
    public string? DisplayName { get; init; }

    /// <summary>
    /// Updated status for the participant.
    /// </summary>
    public ParticipantIdentityStatus? Status { get; init; }
}
