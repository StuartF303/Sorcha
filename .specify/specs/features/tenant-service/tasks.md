# Tasks: Tenant Service

**Feature Branch**: `tenant-service`
**Created**: 2025-12-03
**Updated**: 2025-12-07
**Status**: MVP Phase 1 Complete - Authentication API Implemented

## Task Summary

| Status | Count |
|--------|-------|
| Complete | 12 |
| In Progress | 0 |
| Pending | 9 |
| **Total** | **21** |

---

## MVD Phase (Stub)

### TENANT-001: Define ITenantProvider Interface
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 1 hour
- **Assignee**: AI Assistant
- **Dependencies**: None

**Description**: Define tenant context resolution interface.

**Acceptance Criteria**:
- [x] ITenantProvider interface
- [x] GetCurrentTenant method
- [x] GetTenantConfigurationAsync method
- [x] ValidateTenantAsync method

---

### TENANT-002: Define TenantConfiguration Model
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 1 hour
- **Assignee**: AI Assistant
- **Dependencies**: TENANT-001

**Description**: Define tenant configuration record.

**Acceptance Criteria**:
- [x] TenantConfiguration record
- [x] TenantId property
- [x] Name property
- [x] IsActive property
- [x] Settings dictionary

---

### TENANT-003: Implement SimpleTenantProvider
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 2 hours
- **Assignee**: AI Assistant
- **Dependencies**: TENANT-001

**Description**: Implement development stub.

**Acceptance Criteria**:
- [x] X-Tenant-Id header resolution
- [x] JWT claim resolution
- [x] Default tenant fallback
- [x] Logging

---

### TENANT-004: Service Integration
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: TENANT-003

**Description**: Integrate into all services.

**Acceptance Criteria**:
- [x] Wallet Service integration
- [x] Register Service integration
- [x] Blueprint Service integration
- [x] DI registration

---

## Post-MVD Phase (Full Implementation)

### TENANT-005: Full Tenant Service Project
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 8 hours
- **Assignee**: AI Assistant
- **Completed**: 2025-12-07
- **Dependencies**: None

**Description**: Create full Tenant Service with foundational infrastructure.

**Acceptance Criteria**:
- [x] Project created (Sorcha.Tenant.Service)
- [x] Service defaults integrated (.NET Aspire)
- [x] Domain models (Organization, UserIdentity, PublicIdentity, ServicePrincipal, AuditLogEntry)
- [x] Database context (TenantDbContext with EF Core)
- [x] JWT authentication configured
- [x] Authorization policies defined
- [x] Redis connection with circuit breaker
- [x] Health checks (PostgreSQL, Redis)

---

### TENANT-005a: Token Revocation Service
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Completed**: 2025-12-07
- **Dependencies**: TENANT-005

**Description**: Implement Redis-backed token revocation and rate limiting.

**Acceptance Criteria**:
- [x] ITokenRevocationService interface
- [x] Redis implementation with TTL-based expiry
- [x] Token tracking for user/organization bulk revocation
- [x] Failed authentication attempt tracking
- [x] Rate limiting with configurable thresholds
- [x] Fail-open pattern for Redis unavailability

---

### TENANT-005b: Tenant Provider Service
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 3 hours
- **Assignee**: AI Assistant
- **Completed**: 2025-12-07
- **Dependencies**: TENANT-005

**Description**: Implement multi-tenant context resolution.

**Acceptance Criteria**:
- [x] ITenantProvider interface (extended)
- [x] JWT claim resolution (org_id)
- [x] HTTP header fallback (X-Organization-Id)
- [x] Schema name generation (org_{id} pattern)
- [x] AsyncLocal request-scoped context
- [x] PostgreSQL schema limit compliance (63 chars)

---

### TENANT-005c: DI and Authentication Extensions
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 3 hours
- **Assignee**: AI Assistant
- **Completed**: 2025-12-07
- **Dependencies**: TENANT-005

**Description**: Create extension methods for service registration.

**Acceptance Criteria**:
- [x] AddTenantServices() - main registration
- [x] AddTenantDatabase() - EF Core PostgreSQL/InMemory
- [x] AddTenantRepositories() - repository implementations
- [x] AddTenantRedis() - Redis with Polly circuit breaker
- [x] AddTenantAuthentication() - JWT Bearer
- [x] AddTenantAuthorization() - 9 authorization policies
- [x] AddTenantHealthChecks() - PostgreSQL and Redis

---

### TENANT-006: Organization Repository
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Completed**: 2025-12-07
- **Dependencies**: TENANT-005

**Description**: Implement organization persistence.

**Acceptance Criteria**:
- [x] IOrganizationRepository interface
- [x] OrganizationRepository implementation
- [x] CRUD operations
- [x] Multi-tenant schema support

---

### TENANT-006a: Identity Repository
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Completed**: 2025-12-07
- **Dependencies**: TENANT-006

**Description**: Implement identity persistence.

**Acceptance Criteria**:
- [x] IIdentityRepository interface
- [x] IdentityRepository implementation
- [x] User identity operations
- [x] Public identity operations
- [x] Service principal operations

---

### TENANT-007: Organization API Endpoints
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 6 hours
- **Assignee**: AI Assistant
- **Completed**: 2025-12-07
- **Dependencies**: TENANT-006

**Description**: Implement Organization REST API endpoints.

**Acceptance Criteria**:
- [x] POST /api/organizations - Create organization
- [x] GET /api/organizations - List organizations
- [x] GET /api/organizations/{id} - Get organization details
- [x] GET /api/organizations/by-subdomain/{subdomain} - Get by subdomain
- [x] PUT /api/organizations/{id} - Update organization
- [x] DELETE /api/organizations/{id} - Deactivate organization
- [x] GET /api/organizations/validate-subdomain/{subdomain} - Validate subdomain
- [x] POST /api/organizations/{id}/users - Add user to organization
- [x] GET /api/organizations/{id}/users - List organization users
- [x] GET /api/organizations/{id}/users/{userId} - Get specific user
- [x] PUT /api/organizations/{id}/users/{userId} - Update user
- [x] DELETE /api/organizations/{id}/users/{userId} - Remove user
- [x] OpenAPI documentation via .NET 10 built-in
- [x] Authorization policies applied (RequireAdministrator, RequireOrganizationMember)

---

### TENANT-007a: Authentication API Endpoints
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Completed**: 2025-12-07
- **Dependencies**: TENANT-005

**Description**: Implement authentication API endpoints for token management.

**Acceptance Criteria**:
- [x] POST /api/auth/token/refresh - Refresh access token
- [x] POST /api/auth/token/revoke - Revoke token
- [x] POST /api/auth/token/introspect - Token introspection (service-to-service)
- [x] POST /api/auth/token/revoke-user - Revoke all user tokens (admin)
- [x] POST /api/auth/token/revoke-organization - Revoke all org tokens (admin)
- [x] GET /api/auth/me - Get current user info
- [x] POST /api/auth/logout - Logout and revoke current token
- [x] TokenService implementation
- [x] JWT token generation (user, public, service)
- [x] Token validation and introspection

---

### TENANT-007b: Service-to-Service Authentication
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 6 hours
- **Assignee**: AI Assistant
- **Completed**: 2025-12-07
- **Dependencies**: TENANT-007a

**Description**: Implement OAuth2 client credentials flow for service-to-service authentication.

**Acceptance Criteria**:
- [x] POST /api/service-auth/token - Client credentials token
- [x] POST /api/service-auth/token/delegated - Delegated authority token
- [x] POST /api/service-auth/rotate-secret - Rotate client secret
- [x] POST /api/service-principals - Register service principal
- [x] GET /api/service-principals - List service principals
- [x] GET /api/service-principals/{id} - Get service principal
- [x] GET /api/service-principals/by-client/{clientId} - Get by client ID
- [x] PUT /api/service-principals/{id}/scopes - Update scopes
- [x] POST /api/service-principals/{id}/suspend - Suspend principal
- [x] POST /api/service-principals/{id}/reactivate - Reactivate principal
- [x] DELETE /api/service-principals/{id} - Revoke principal
- [x] ServiceAuthService implementation
- [x] Client secret hashing and verification
- [x] Scope filtering for requested scopes

---

### TENANT-008: Azure AD Integration
- **Status**: Pending
- **Priority**: P3
- **Estimate**: 12 hours
- **Assignee**: TBD
- **Dependencies**: TENANT-005

**Description**: Implement Azure AD SSO.

**Acceptance Criteria**:
- [ ] Azure AD configuration
- [ ] Token validation
- [ ] Claim mapping

---

### TENANT-009: Azure B2C Integration
- **Status**: Pending
- **Priority**: P3
- **Estimate**: 12 hours
- **Assignee**: TBD
- **Dependencies**: TENANT-005

**Description**: Implement Azure B2C integration.

**Acceptance Criteria**:
- [ ] B2C configuration
- [ ] Custom flows
- [ ] User registration

---

### TENANT-010: Tenant Policies
- **Status**: Pending
- **Priority**: P3
- **Estimate**: 8 hours
- **Assignee**: TBD
- **Dependencies**: TENANT-006

**Description**: Implement tenant-specific policies.

**Acceptance Criteria**:
- [ ] Policy model
- [ ] Quota enforcement
- [ ] Feature flags

---

### TENANT-011: Unit Tests
- **Status**: Pending
- **Priority**: P2
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: TENANT-007

**Description**: Unit tests for tenant service.

**Acceptance Criteria**:
- [ ] Repository tests
- [ ] Service tests
- [ ] Provider tests

---

### TENANT-012: Integration Tests
- **Status**: Pending
- **Priority**: P2
- **Estimate**: 8 hours
- **Assignee**: TBD
- **Dependencies**: TENANT-007

**Description**: Integration tests.

**Acceptance Criteria**:
- [ ] API endpoint tests
- [ ] Cross-service tests
- [ ] IdP integration tests

---

### TENANT-013: Admin UI
- **Status**: Pending
- **Priority**: P3
- **Estimate**: 16 hours
- **Assignee**: TBD
- **Dependencies**: TENANT-007

**Description**: Create tenant admin UI.

**Acceptance Criteria**:
- [ ] Tenant list view
- [ ] Tenant detail view
- [ ] Configuration editing

---

### TENANT-014: Documentation
- **Status**: Pending
- **Priority**: P2
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: TENANT-007

**Description**: Complete documentation.

**Acceptance Criteria**:
- [ ] README
- [ ] API documentation
- [ ] Integration guide

---

## Notes

- SimpleTenantProvider is sufficient for MVD
- Full implementation is post-MVD priority
- Identity provider integration depends on customer requirements
- Multi-region tenant support is future consideration
