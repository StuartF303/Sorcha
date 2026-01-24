# Implementation Plan: Participant Identity Registry

**Branch**: `001-participant-identity` | **Date**: 2026-01-24 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-participant-identity/spec.md`

## Summary

Implement a Participant Identity Registry that bridges Tenant Service users with Blueprint workflow participants and Wallet signing keys. The system will manage participant registration (admin and self-service), wallet address linking with cryptographic verification, participant search/discovery, and role assignments for blueprint workflows. Uses PostgreSQL for relational storage, integrates with existing Tenant, Wallet, and Blueprint services via established service client patterns.

## Technical Context

**Language/Version**: C# 13 / .NET 10.0
**Primary Dependencies**: Entity Framework Core 10, FluentValidation 11.10, Sorcha.ServiceClients, Sorcha.Storage.Abstractions
**Storage**: PostgreSQL (via Aspire integration, multi-tenant schema pattern from Tenant Service)
**Testing**: xUnit v3 + FluentAssertions + Moq (target >85% coverage)
**Target Platform**: Linux containers (Docker/Aspire orchestration)
**Project Type**: Microservice + UI components (backend API + Blazor WASM components)
**Performance Goals**: <2s search for 10,000 participants, <200ms CRUD operations
**Constraints**: Platform-wide wallet address uniqueness, 5-minute challenge expiration, soft-delete for audit
**Scale/Scope**: 10,000 participants per organization, 10 wallet addresses per participant

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Justification |
|-----------|--------|---------------|
| I. Microservices-First | ✅ PASS | New endpoints added to Tenant Service (participant identity is a tenant concept); no new service required |
| II. Security First | ✅ PASS | Signature verification for wallet linking, org-scoped access control, audit logging |
| III. API Documentation | ✅ PASS | Scalar OpenAPI with XML docs on all endpoints |
| IV. Testing Requirements | ✅ PASS | Target >85% coverage, xUnit + FluentAssertions + Moq |
| V. Code Quality | ✅ PASS | C# 13, async/await, DI, nullable enabled |
| VI. Blueprint Creation | ✅ N/A | Not creating blueprints, only participant role assignments |
| VII. Domain-Driven Design | ✅ PASS | Uses "Participant" terminology per constitution |
| VIII. Observability | ✅ PASS | Structured logging, health checks, OpenTelemetry integration |

**Gate Status**: PASSED - No violations requiring justification.

## Project Structure

### Documentation (this feature)

```text
specs/001-participant-identity/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (OpenAPI specs)
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
# Extend existing Tenant Service (participant identity is tenant-scoped)
src/Services/Sorcha.Tenant.Service/
├── Endpoints/
│   └── ParticipantEndpoints.cs          # NEW: REST endpoints for participants
├── Data/
│   ├── TenantDbContext.cs               # MODIFY: Add participant entities
│   └── Repositories/
│       ├── IParticipantRepository.cs    # NEW: Repository interface
│       └── ParticipantRepository.cs     # NEW: Repository implementation
├── Models/
│   ├── ParticipantIdentity.cs           # NEW: Domain entity
│   ├── LinkedWalletAddress.cs           # NEW: Domain entity
│   ├── WalletLinkChallenge.cs           # NEW: Domain entity
│   └── Dtos/
│       ├── CreateParticipantRequest.cs  # NEW
│       ├── ParticipantResponse.cs       # NEW
│       ├── LinkWalletRequest.cs         # NEW
│       └── ParticipantSearchRequest.cs  # NEW
├── Services/
│   ├── IParticipantService.cs           # NEW: Business logic interface
│   ├── ParticipantService.cs            # NEW: Business logic implementation
│   └── IWalletVerificationService.cs    # NEW: Signature verification
└── Migrations/
    └── YYYYMMDD_AddParticipantIdentity.cs # NEW: EF migration

# Extend common models
src/Common/Sorcha.Tenant.Models/
├── ParticipantIdentityStatus.cs         # NEW: Status enum
├── WalletLinkStatus.cs                  # NEW: Link status enum
└── ParticipantSearchCriteria.cs         # NEW: Search filter model

# Extend service clients
src/Common/Sorcha.ServiceClients/
├── Interfaces/
│   └── IParticipantServiceClient.cs     # NEW: Client interface
└── Clients/
    └── ParticipantServiceClient.cs      # NEW: HTTP client implementation

# UI Components (Blazor WASM)
src/Apps/Sorcha.UI/Sorcha.UI.Core/
├── Components/
│   └── Participants/
│       ├── ParticipantList.razor        # NEW: Directory view
│       ├── ParticipantForm.razor        # NEW: Create/edit form
│       ├── WalletLinkDialog.razor       # NEW: Wallet linking flow
│       └── ParticipantSearch.razor      # NEW: Search component
├── Services/
│   └── ParticipantApiService.cs         # NEW: UI service layer
└── Models/
    └── ParticipantViewModels.cs         # NEW: UI-specific models

src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/
└── Pages/
    └── Participants/
        ├── Index.razor                  # NEW: /participants route
        ├── Create.razor                 # NEW: /participants/create
        ├── Details.razor                # NEW: /participants/{id}
        └── MyProfile.razor              # NEW: /my-participant-profile

# Tests
tests/Sorcha.Tenant.Service.Tests/
├── Endpoints/
│   └── ParticipantEndpointsTests.cs     # NEW
├── Services/
│   └── ParticipantServiceTests.cs       # NEW
└── Repositories/
    └── ParticipantRepositoryTests.cs    # NEW

tests/Sorcha.UI.Core.Tests/
└── Components/
    └── ParticipantListTests.cs          # NEW
```

**Structure Decision**: Extend existing Tenant Service rather than create new microservice. Rationale:
1. Participant identity is fundamentally a tenant/organization concept
2. Shares authentication context and organization membership data
3. Avoids service proliferation (constitution principle I)
4. Reuses existing multi-tenant schema pattern
5. Service clients provide clean integration boundary for other services

## Complexity Tracking

> No constitution violations requiring justification. Structure uses existing patterns.

| Decision | Rationale |
|----------|-----------|
| Extend Tenant Service | Participant identity is org-scoped; reuses existing auth and multi-tenant infrastructure |
| Separate UI components | MudBlazor components in Sorcha.UI.Core for reuse across Admin and main UI |
| Repository pattern | Follows existing TenantDbContext pattern with IRepository abstraction |

## Constitution Re-Check (Post Phase 1 Design)

*Verified after completing data model and API contracts*

| Principle | Status | Design Validation |
|-----------|--------|-------------------|
| I. Microservices-First | ✅ PASS | Extends Tenant Service; IParticipantServiceClient provides clean boundary for Blueprint/Register integration |
| II. Security First | ✅ PASS | Challenge-response wallet verification; org-scoped data isolation; audit logging in ParticipantAuditEntry |
| III. API Documentation | ✅ PASS | OpenAPI 3.1 contract in contracts/participant-api.yaml; all endpoints documented |
| IV. Testing Requirements | ✅ PASS | Test structure defined; service/repository/endpoint test files specified |
| V. Code Quality | ✅ PASS | Async patterns throughout; nullable types in data model; DI via service extensions |
| VI. Blueprint Creation | ✅ N/A | No blueprint creation; ParticipantRoleAssignment links participants to existing blueprints |
| VII. Domain-Driven Design | ✅ PASS | ParticipantIdentity, LinkedWalletAddress entities follow DDD; "Participant" terminology used |
| VIII. Observability | ✅ PASS | Structured audit logging; health checks inherit from Tenant Service |

**Post-Design Gate Status**: PASSED - Design artifacts conform to all constitution principles.

## Generated Artifacts

| Artifact | Path | Description |
|----------|------|-------------|
| Research | [research.md](./research.md) | Technical decisions and best practices |
| Data Model | [data-model.md](./data-model.md) | Entity definitions, relationships, migrations |
| API Contract | [contracts/participant-api.yaml](./contracts/participant-api.yaml) | OpenAPI 3.1 specification |
| Quickstart | [quickstart.md](./quickstart.md) | Developer implementation guide |

## Next Steps

Run `/speckit.tasks` to generate the implementation task list.
