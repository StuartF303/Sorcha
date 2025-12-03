# Implementation Plan: Tenant Service

**Feature Branch**: `tenant-service`
**Created**: 2025-12-03
**Status**: Stub Only (Awaiting Full Specification)

## Summary

The Tenant Service provides multi-tenant management and identity federation for the Sorcha platform. Currently implemented as a stub with SimpleTenantProvider for development purposes. Full implementation is planned for post-MVD.

## Design Decisions

### Decision 1: Interface-First Approach

**Approach**: Define ITenantProvider interface, implement stub for development.

**Rationale**:
- Services can integrate immediately
- Full implementation can follow
- Consistent API across development and production

### Decision 2: Header and Claim Resolution

**Approach**: Support both X-Tenant-Id header and JWT tenant_id claim.

**Rationale**:
- Header for service-to-service calls
- Claim for user-authenticated requests
- Flexibility for different integration patterns

## Technical Approach

### Architecture (Stub)

```
┌─────────────────────────────────────────────────────────┐
│              Sorcha.Tenant.Abstractions                  │
│                    (.NET Library)                        │
├─────────────────────────────────────────────────────────┤
│  Interfaces/                                             │
│  └── ITenantProvider.cs                                 │
├─────────────────────────────────────────────────────────┤
│  Models/                                                 │
│  └── TenantConfiguration.cs                             │
├─────────────────────────────────────────────────────────┤
│  Implementations/                                        │
│  └── SimpleTenantProvider.cs (Development stub)         │
└─────────────────────────────────────────────────────────┘
```

### Component Status

| Component | Status | Notes |
|-----------|--------|-------|
| ITenantProvider | 100% | Interface defined |
| TenantConfiguration | 100% | Record defined |
| SimpleTenantProvider | 100% | Development stub |
| Full Tenant Service | 0% | Post-MVD |
| Identity Provider Integration | 0% | Post-MVD |

## Dependencies

### Production Dependencies

None (abstractions only)

### Service Dependencies

- All services depend on Sorcha.Tenant.Abstractions

## Migration/Integration Notes

### Usage in Services

```csharp
// Register in DI
services.AddSingleton<ITenantProvider, SimpleTenantProvider>();

// Use in service
public class WalletService
{
    private readonly ITenantProvider _tenantProvider;

    public async Task<Wallet?> GetWalletAsync(string address)
    {
        var tenantId = _tenantProvider.GetCurrentTenant();
        return await _repository.GetByAddressAndTenantAsync(address, tenantId);
    }
}
```

## Open Questions

1. Which identity providers to support beyond Azure AD/B2C?
2. How to handle tenant data migration?
3. Should tenants have separate databases or use shared with row-level isolation?
