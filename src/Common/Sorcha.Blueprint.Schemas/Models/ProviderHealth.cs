// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Blueprint.Schemas.Models;

/// <summary>
/// Health status of a schema provider.
/// </summary>
public enum ProviderHealth
{
    /// <summary>
    /// Provider is operating normally.
    /// </summary>
    Healthy,

    /// <summary>
    /// Provider is partially available or experiencing issues.
    /// </summary>
    Degraded,

    /// <summary>
    /// Provider is unreachable.
    /// </summary>
    Unavailable,

    /// <summary>
    /// Provider status has not been determined yet.
    /// </summary>
    Unknown
}
