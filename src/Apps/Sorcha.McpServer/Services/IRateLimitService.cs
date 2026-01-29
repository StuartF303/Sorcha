// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.McpServer.Services;

/// <summary>
/// Handles rate limiting for MCP tool invocations.
/// </summary>
public interface IRateLimitService
{
    /// <summary>
    /// Checks if the request should be rate limited.
    /// </summary>
    /// <param name="userId">The user making the request.</param>
    /// <param name="tenantId">The tenant the user belongs to.</param>
    /// <param name="toolCategory">The category of tool being invoked.</param>
    /// <returns>A rate limit result indicating if the request is allowed.</returns>
    RateLimitResult CheckRateLimit(string userId, string tenantId, string toolCategory);
}

/// <summary>
/// Result of a rate limit check.
/// </summary>
public sealed record RateLimitResult
{
    /// <summary>
    /// Whether the request is allowed.
    /// </summary>
    public required bool IsAllowed { get; init; }

    /// <summary>
    /// Time until rate limit resets, if rate limited.
    /// </summary>
    public TimeSpan? RetryAfter { get; init; }

    /// <summary>
    /// The reason for rate limiting, if applicable.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Creates an allowed result.
    /// </summary>
    public static RateLimitResult Allowed() => new() { IsAllowed = true };

    /// <summary>
    /// Creates a rate limited result.
    /// </summary>
    public static RateLimitResult Limited(TimeSpan retryAfter, string reason) =>
        new() { IsAllowed = false, RetryAfter = retryAfter, Reason = reason };
}
