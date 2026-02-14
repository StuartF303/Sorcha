// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Tenant.Service.Models;

/// <summary>
/// Stores platform-level configuration flags (e.g., bootstrap completion).
/// </summary>
public class SystemConfiguration
{
    /// <summary>
    /// Configuration key (primary key).
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Configuration value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// When this configuration entry was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
