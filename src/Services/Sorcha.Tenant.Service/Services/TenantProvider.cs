// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Security.Claims;

namespace Sorcha.Tenant.Service.Services;

/// <summary>
/// Default implementation of ITenantProvider using AsyncLocal for request-scoped tenant context.
/// </summary>
public class TenantProvider : ITenantProvider
{
    private static readonly AsyncLocal<string?> _currentTenantId = new();
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Schema prefix for tenant-specific tables.
    /// </summary>
    private const string TenantSchemaPrefix = "org_";

    /// <summary>
    /// Public schema name for shared tables.
    /// </summary>
    private const string PublicSchema = "public";

    /// <summary>
    /// Claim type for organization ID in JWT tokens.
    /// </summary>
    private const string OrgIdClaimType = "org_id";

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc/>
    public string? GetCurrentTenantId()
    {
        // First check AsyncLocal (explicitly set)
        if (!string.IsNullOrEmpty(_currentTenantId.Value))
        {
            return _currentTenantId.Value;
        }

        // Then check HTTP context for JWT claim
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var orgIdClaim = httpContext.User.FindFirst(OrgIdClaimType);
            if (orgIdClaim is not null && !string.IsNullOrEmpty(orgIdClaim.Value))
            {
                return orgIdClaim.Value;
            }
        }

        // Check for X-Organization-Id header (for admin operations)
        var orgHeader = httpContext?.Request.Headers["X-Organization-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(orgHeader))
        {
            return orgHeader;
        }

        return null;
    }

    /// <inheritdoc/>
    public string GetTenantSchema()
    {
        var tenantId = GetCurrentTenantId();
        return string.IsNullOrEmpty(tenantId) ? PublicSchema : GetSchemaForOrganization(tenantId);
    }

    /// <inheritdoc/>
    public string GetSchemaForOrganization(string organizationId)
    {
        if (string.IsNullOrWhiteSpace(organizationId))
        {
            return PublicSchema;
        }

        // Sanitize organization ID for schema name (alphanumeric and underscores only)
        var sanitized = new string(organizationId
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '_')
            .ToArray());

        // Ensure schema name doesn't exceed PostgreSQL limit (63 chars)
        if (sanitized.Length > 58) // Reserve space for prefix
        {
            sanitized = sanitized[..58];
        }

        return $"{TenantSchemaPrefix}{sanitized}";
    }

    /// <inheritdoc/>
    public void SetCurrentTenant(string? organizationId)
    {
        _currentTenantId.Value = organizationId;
    }

    /// <inheritdoc/>
    public bool IsInTenantScope => !string.IsNullOrEmpty(GetCurrentTenantId());
}
