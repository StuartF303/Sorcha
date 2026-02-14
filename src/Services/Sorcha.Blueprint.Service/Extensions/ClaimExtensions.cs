// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Blueprint.Service.Extensions;

/// <summary>
/// Extension methods for extracting claims from HttpContext.
/// Provides consistent org_id extraction across all endpoints.
/// </summary>
internal static class ClaimExtensions
{
    /// <summary>
    /// Gets the organization ID from the current user's claims.
    /// Checks "org_id" claim (standard across Sorcha services).
    /// </summary>
    public static string? GetOrganizationId(this HttpContext context)
        => context.User.FindFirst("org_id")?.Value;

    /// <summary>
    /// Checks if the current token is a service-to-service token.
    /// Service tokens bypass org-scoped restrictions.
    /// </summary>
    public static bool IsServiceToken(this HttpContext context)
        => context.User.Claims.Any(c => c.Type == "token_type" && c.Value == "service");
}
