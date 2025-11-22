# Implementation Plan: Tenant Service Authentication & Multi-Organization Identity Management

**Branch**: `001-tenant-auth` | **Date**: 2025-11-22 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-tenant-auth/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

The Tenant Service provides OAuth2/OIDC-compliant authentication and authorization for the Sorcha platform, acting as a Secure Token Service (STS) that integrates with external identity providers (Azure Entra, AWS Cognito, generic OIDC) for multi-organization support. The service issues JWT tokens containing user identity, organization context, roles, and permissions, enabling service-to-service authentication and delegated authority scenarios. Public users can authenticate via PassKey/WebAuthn without organizational affiliation.

**Technical Approach**: Build a .NET 10 minimal API service using Microsoft.AspNetCore.Authentication.OpenIdConnect for external IDP integration, System.IdentityModel.Tokens.Jwt for token issuance/validation, and Fido2NetLib for PassKey support. Use PostgreSQL with Entity Framework Core for multi-tenant data storage, Redis for token blacklisting and distributed caching, and .NET Aspire for service orchestration.

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**:
- Microsoft.AspNetCore.Authentication.OpenIdConnect (external IDP integration)
- System.IdentityModel.Tokens.Jwt (JWT token handling)
- Fido2NetLib (PassKey/WebAuthn support)
- Entity Framework Core 10 (data persistence)
- Npgsql.EntityFrameworkCore.PostgreSQL (PostgreSQL provider)
- StackExchange.Redis (distributed caching and token revocation)
- Aspire.Hosting (service orchestration)
- Scalar.AspNetCore (API documentation UI)

**Storage**: PostgreSQL (organizations, identities, IDP configurations, permissions, audit logs); Redis (token revocation lists, rate limiting counters, distributed cache)

**Testing**: xUnit, FluentAssertions, Moq, Testcontainers (PostgreSQL/Redis), NBomber (performance testing)

**Target Platform**: Linux containers (Docker), .NET Aspire orchestration, Kubernetes-ready

**Project Type**: Microservice API (ASP.NET Core Minimal APIs)

**Performance Goals**:
- Token issuance: <500ms for 1,000 concurrent requests
- Token validation: <50ms p95
- IDP callback processing: <10 seconds end-to-end
- Support 100+ organizations with <200ms query time

**Constraints**:
- Must comply with OAuth2/OIDC standards (RFC 6749, RFC 7519)
- All external IDP secrets encrypted at rest using AES-256-GCM
- TLS 1.3 required for all communication
- Clock skew tolerance: ±30 seconds for token validation
- Rate limiting: 100 token requests/minute per client, 5 failed auth attempts/minute per user

**Scale/Scope**:
- Initial: 100 organizations, 10,000 users
- Token lifetime: 1-hour access, 24-hour refresh (configurable)
- Audit log retention: 90 days minimum
- API surface: ~20 REST endpoints (auth flows, admin, token management, audit)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ Microservices-First Architecture
- **Status**: PASS
- **Evidence**: Tenant Service is independently deployable, communicates via REST APIs and .NET Aspire messaging, maintains clear boundaries with Blueprint/Wallet/Register services
- **Service Discovery**: Uses .NET Aspire service discovery for inter-service communication
- **API Boundary**: Well-defined OAuth2/OIDC endpoints, service token endpoints, admin APIs

### ✅ Security Principles - Zero Trust
- **Status**: PASS
- **Evidence**: All service-to-service communication requires JWT tokens (client credentials flow), external IDP secrets encrypted at rest, no credentials in source control
- **Secret Management**: Azure Key Vault integration planned for production, local DPAPI for development
- **Authentication**: JWT tokens for all API access, signature validation required

### ✅ Security Principles - Cryptographic Standards
- **Status**: PASS with DEPENDENCY
- **Evidence**: Uses industry-standard JWT libraries (System.IdentityModel.Tokens.Jwt), AES-256-GCM for secret encryption
- **Dependency**: Will leverage existing Sorcha.Cryptography library for encryption operations
- **Standards**: RS256/ES256 for JWT signing (asymmetric), FIDO2 for PassKey (industry standard)

### ✅ API Documentation Standards
- **Status**: PASS
- **Evidence**: Will use .NET 10 built-in OpenAPI (Microsoft.AspNetCore.OpenApi), Scalar.AspNetCore for interactive UI, XML documentation on all endpoints
- **Compliance**: No Swagger/Swashbuckle, follows constitutional mandate for .NET 10 native OpenAPI
- **Endpoints**: /openapi/v1.json for spec, /scalar for interactive docs

### ✅ Code Quality and Testing
- **Status**: PASS
- **Evidence**: xUnit for unit tests, Testcontainers for integration tests, targeting >85% coverage for new service
- **Test Strategy**: Unit (business logic), integration (API endpoints, database), contract (token validation), performance (NBomber for load testing)

### ✅ Data Storage Principles
- **Status**: PASS
- **Evidence**: PostgreSQL for relational tenant/identity data (appropriate per constitution), Redis for distributed caching (constitutional standard)
- **Migrations**: EF Core migrations for schema versioning
- **Audit**: Comprehensive audit logging for all authentication events (constitutional requirement)

### ✅ Observability
- **Status**: PASS
- **Evidence**: Structured logging with Serilog, correlation IDs for distributed tracing, integration with Seq and Zipkin (constitutional standards)
- **Monitoring**: Health checks, readiness probes, metrics for token issuance/validation performance

### ⚠️ AI-Generated Code Documentation
- **Status**: PENDING IMPLEMENTATION
- **Requirement**: All generated code must update README, docs/, .specify/ files per AI Code Documentation Policy
- **Commitment**: Will update MASTER-TASKS.md, service README, docs/architecture.md, and create Tenant Service specification document

### Phase 1 Re-Check Status
- **Pre-Research**: 7 PASS, 1 PENDING (documentation - will be resolved during implementation)
- **Post-Design**: To be verified after Phase 1 completion

## Project Structure

### Documentation (this feature)

```text
specs/001-tenant-auth/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (OAuth2/OIDC patterns, FIDO2 implementation, multi-tenancy strategies)
├── data-model.md        # Phase 1 output (EF Core entities, relationships, migrations)
├── quickstart.md        # Phase 1 output (local dev setup, testing with mock IDP)
├── contracts/           # Phase 1 output (OpenAPI specs for auth endpoints)
│   ├── auth-api.yaml   # User authentication flows
│   ├── admin-api.yaml  # Organization/IDP configuration
│   ├── token-api.yaml  # Token management (validation, revocation, refresh)
│   └── audit-api.yaml  # Audit log queries
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── Services/
│   └── Sorcha.Tenant.Service/               # NEW - Main Tenant Service project
│       ├── Program.cs                        # Minimal API setup, .NET Aspire integration
│       ├── Endpoints/                        # Endpoint groups
│       │   ├── AuthenticationEndpoints.cs   # OAuth2/OIDC flows, PassKey auth
│       │   ├── OrganizationEndpoints.cs     # Admin APIs for org/IDP config
│       │   ├── TokenEndpoints.cs            # Token validation, refresh, revocation
│       │   └── AuditEndpoints.cs            # Audit log queries
│       ├── Models/                           # Domain models
│       │   ├── Organization.cs
│       │   ├── IdentityProviderConfiguration.cs
│       │   ├── UserIdentity.cs
│       │   ├── PublicIdentity.cs
│       │   ├── OrganizationPermissionConfiguration.cs
│       │   ├── ServicePrincipal.cs
│       │   └── AuditLogEntry.cs
│       ├── Data/                             # EF Core context and repositories
│       │   ├── TenantDbContext.cs
│       │   ├── Migrations/
│       │   └── Repositories/
│       │       ├── IOrganizationRepository.cs
│       │       ├── OrganizationRepository.cs
│       │       ├── IIdentityRepository.cs
│       │       └── IdentityRepository.cs
│       ├── Services/                         # Business logic services
│       │   ├── ITokenService.cs
│       │   ├── TokenService.cs              # JWT issuance, validation, signing
│       │   ├── IExternalIdpService.cs
│       │   ├── ExternalIdpService.cs        # OIDC integration, callback handling
│       │   ├── IPassKeyService.cs
│       │   ├── PassKeyService.cs            # FIDO2/WebAuthn registration, authentication
│       │   ├── IPermissionService.cs
│       │   ├── PermissionService.cs         # Permission evaluation, token claims
│       │   ├── ITokenRevocationService.cs
│       │   └── TokenRevocationService.cs    # Redis-backed revocation list
│       ├── Extensions/
│       │   ├── ServiceCollectionExtensions.cs
│       │   └── AuthenticationExtensions.cs
│       └── README.md                         # Service documentation
│
├── Common/
│   └── Sorcha.Tenant.Models/                 # Shared DTOs for cross-service use
│       ├── TokenClaims.cs                    # Standard JWT claims structure
│       ├── OrganizationContext.cs            # Organization metadata for requests
│       └── PermissionFlags.cs                # Permissions enum (blockchain access, etc.)
│
└── Apps/
    └── Sorcha.AppHost/                       # .NET Aspire orchestration
        └── Program.cs                        # Add Tenant Service registration

tests/
├── Sorcha.Tenant.Service.Tests/              # NEW - Unit tests
│   ├── Services/
│   │   ├── TokenServiceTests.cs
│   │   ├── ExternalIdpServiceTests.cs
│   │   ├── PassKeyServiceTests.cs
│   │   └── PermissionServiceTests.cs
│   └── Endpoints/
│       └── [endpoint test files]
│
├── Sorcha.Tenant.Service.IntegrationTests/   # NEW - Integration tests
│   ├── AuthenticationFlowTests.cs            # End-to-end OAuth2 flows with Testcontainers
│   ├── TokenManagementTests.cs               # Token lifecycle tests
│   ├── PassKeyAuthenticationTests.cs
│   └── DatabaseTests/
│       └── RepositoryTests.cs
│
└── Sorcha.Tenant.Service.PerformanceTests/   # NEW - Performance tests
    └── TokenIssuanceLoadTests.cs             # NBomber load tests
```

**Structure Decision**: Microservice API architecture following existing Sorcha service patterns (Blueprint.Service, Wallet.Service, Register.Service). The Tenant Service is a standalone ASP.NET Core Minimal API project with clear separation of concerns: endpoints (HTTP layer), services (business logic), data (persistence), and models (domain). Shared DTOs in Sorcha.Tenant.Models enable other services to validate tokens and extract claims without tight coupling. Test projects follow constitutional standards with unit, integration, and performance test separation.

## Complexity Tracking

*No constitutional violations requiring justification.*

**Notes**:
- Multi-organization support is inherent to the feature requirement (not added complexity)
- Redis usage for token revocation aligns with constitutional standard for distributed caching
- PassKey/FIDO2 support is a P2 requirement (not adding unnecessary complexity)
- Service follows existing Sorcha patterns (no architectural divergence)
