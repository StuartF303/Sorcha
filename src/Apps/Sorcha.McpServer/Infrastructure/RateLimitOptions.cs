// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.McpServer.Infrastructure;

/// <summary>
/// Configuration options for rate limiting.
/// </summary>
public sealed class RateLimitOptions
{
    /// <summary>
    /// Maximum requests per minute per user.
    /// </summary>
    public int PerUserRequestsPerMinute { get; set; } = 100;

    /// <summary>
    /// Maximum requests per minute per tenant.
    /// </summary>
    public int PerTenantRequestsPerMinute { get; set; } = 1000;

    /// <summary>
    /// Maximum admin tool requests per minute per user.
    /// </summary>
    public int AdminToolsRequestsPerMinute { get; set; } = 50;
}
