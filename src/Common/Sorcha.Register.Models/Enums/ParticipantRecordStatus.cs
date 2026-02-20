// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Register.Models.Enums;

/// <summary>
/// Represents the lifecycle status of a published participant record
/// </summary>
public enum ParticipantRecordStatus
{
    /// <summary>
    /// Normal operating state — participant is active and discoverable
    /// </summary>
    Active,

    /// <summary>
    /// Transitioning — participant is still usable but flagged for replacement
    /// </summary>
    Deprecated,

    /// <summary>
    /// Permanently retired — excluded from default queries
    /// </summary>
    Revoked
}
