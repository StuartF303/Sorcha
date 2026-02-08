// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.UI.Core.Models.Admin;

/// <summary>
/// View model for service principal (service-to-service credentials) display.
/// </summary>
public record ServicePrincipalViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = "active";
    public DateTimeOffset? LastUsedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public List<string> Permissions { get; init; } = [];

    /// <summary>
    /// True if credential expires within 7 days.
    /// </summary>
    public bool IsNearExpiration =>
        ExpiresAt.HasValue && ExpiresAt.Value - DateTimeOffset.UtcNow < TimeSpan.FromDays(7);
}
