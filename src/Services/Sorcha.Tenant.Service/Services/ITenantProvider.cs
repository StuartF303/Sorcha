// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Tenant.Service.Services;

/// <summary>
/// Provides tenant context resolution for multi-tenant operations.
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// Gets the current tenant/organization identifier from the request context.
    /// </summary>
    /// <returns>The organization ID or null if not in tenant context.</returns>
    string? GetCurrentTenantId();

    /// <summary>
    /// Gets the database schema name for the current tenant.
    /// </summary>
    /// <returns>The schema name (e.g., "org_acme") or "public" for shared tables.</returns>
    string GetTenantSchema();

    /// <summary>
    /// Gets the database schema name for a specific organization.
    /// </summary>
    /// <param name="organizationId">The organization identifier.</param>
    /// <returns>The schema name for the organization.</returns>
    string GetSchemaForOrganization(string organizationId);

    /// <summary>
    /// Sets the current tenant context (typically from JWT claims or request headers).
    /// </summary>
    /// <param name="organizationId">The organization identifier.</param>
    void SetCurrentTenant(string? organizationId);

    /// <summary>
    /// Checks if the current context is within a tenant scope.
    /// </summary>
    bool IsInTenantScope { get; }
}
