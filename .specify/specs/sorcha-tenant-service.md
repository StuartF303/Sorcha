# Sorcha.Tenant.Service Specification (Boilerplate)

**Version:** 0.1
**Date:** 2025-11-13
**Status:** To Be Specified
**Related Constitution:** [constitution.md](../constitution.md)

## Executive Summary

This specification will define the Sorcha.Tenant.Service - the multi-tenant management and identity federation service for the Sorcha platform. This service is responsible for:

- Tenant provisioning and management
- Integration with identity providers (Azure AD, Azure B2C)
- Tenant-specific configuration and policies
- API authentication and authorization

## Current Status

⚠️ **This service specification is a placeholder for future development**

The Tenant Service is referenced in the platform architecture but is not yet fully specified. Multi-tenant isolation will be implemented at the application level until this service is available.

## Minimal Boilerplate Interface

Until fully specified and implemented, tenant management should be handled through:

### ITenantProvider (Placeholder)

```csharp
namespace Sorcha.Tenant.Abstractions;

/// <summary>
/// Provides tenant context for the current operation
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// Gets the current tenant identifier
    /// </summary>
    string GetCurrentTenant();

    /// <summary>
    /// Gets tenant configuration
    /// </summary>
    Task<TenantConfiguration?> GetTenantConfigurationAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a tenant exists and is active
    /// </summary>
    Task<bool> ValidateTenantAsync(
        string tenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Tenant configuration
/// </summary>
public record TenantConfiguration
{
    public string TenantId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public Dictionary<string, string> Settings { get; init; } = new();
}
```

### Stub Implementation

For development purposes, provide a simple implementation:

```csharp
namespace Sorcha.Tenant.Abstractions;

public class SimpleTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SimpleTenantProvider> _logger;
    private const string DefaultTenantId = "default-tenant";

    public SimpleTenantProvider(
        IHttpContextAccessor httpContextAccessor,
        ILogger<SimpleTenantProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string GetCurrentTenant()
    {
        // Try to get from header, claim, or subdomain
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            // Check X-Tenant-Id header
            if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId))
            {
                return tenantId.ToString();
            }

            // Check claims
            var tenantClaim = httpContext.User.FindFirst("tenant_id");
            if (tenantClaim != null)
            {
                return tenantClaim.Value;
            }
        }

        _logger.LogWarning("No tenant context found, using default tenant");
        return DefaultTenantId;
    }

    public Task<TenantConfiguration?> GetTenantConfigurationAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        // Return a simple default configuration
        var config = new TenantConfiguration
        {
            TenantId = tenantId,
            Name = $"Tenant {tenantId}",
            IsActive = true,
            Settings = new Dictionary<string, string>()
        };

        return Task.FromResult<TenantConfiguration?>(config);
    }

    public Task<bool> ValidateTenantAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        // For now, accept any non-empty tenant ID
        return Task.FromResult(!string.IsNullOrWhiteSpace(tenantId));
    }
}
```

## Integration Points

### Wallet Service Integration

The Wallet Service will:
- Accept `ITenantProvider` as a dependency
- Use tenant ID for data isolation in repositories
- Validate tenant before wallet operations
- Include tenant in audit logs

### Future Development

When fully specified, the Tenant Service will provide:
- Complete tenant lifecycle management
- Identity provider integration (Azure AD, B2C)
- Tenant-specific policies and quotas
- Multi-region tenant support
- Tenant billing and metering
- Administrative UI

## Dependencies

To be defined when specification is complete.

## Timeline

To be determined based on priority and roadmap.

---

**Document Status:** Placeholder - Awaiting Full Specification
**Priority:** Medium (Basic tenant isolation can be implemented at app level)
**Assigned To:** To Be Determined
