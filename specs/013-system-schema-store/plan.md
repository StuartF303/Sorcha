# Implementation Plan: System Schema Store

**Branch**: `013-system-schema-store` | **Date**: 2026-01-20 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/013-system-schema-store/spec.md`

## Summary

Implement a schema store within the Blueprint service providing:
1. **Core System Schemas** - Pre-defined JSON schemas for Installation, Organisation, Participant, and Register entities
2. **Server-side Schema Store** - Categorized, versioned storage with role-based access control and multi-tenant support
3. **Client-side Caching** - WASM client caching with offline support and version-based invalidation
4. **External Integration** - SchemaStore.org lookups with graceful fallback

Technical approach: Extend Blueprint service with new schema management endpoints, add MongoDB storage for schema persistence, implement client-side IndexedDB caching in Blazor WASM.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: JsonSchema.Net 7.4.0, .NET Aspire 13.0.0, Scalar 2.10.0
**Storage**: MongoDB (schema documents), Redis (server-side caching)
**Testing**: xUnit, FluentAssertions, Moq (target >85% coverage)
**Target Platform**: Linux server (Blueprint service) + WASM (Blazor client)
**Project Type**: Web application (server API + client caching layer)
**Performance Goals**: 500ms first request, 100ms cached response, 3s external search
**Constraints**: Offline-capable (24h cached data), JWT authentication required
**Scale/Scope**: Multi-tenant (global + organization-scoped schemas), ~100 concurrent users

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | ✅ PASS | Extends existing Blueprint service; no new services created |
| II. Security First | ✅ PASS | JWT auth required (FR-010a), role-based access (FR-010b), input validation via JsonSchema.Net |
| III. API Documentation | ✅ PASS | Will use Scalar for interactive docs; OpenAPI at `/openapi/v1.json` |
| IV. Testing Requirements | ✅ PASS | Target >85% coverage; xUnit with FluentAssertions |
| V. Code Quality | ✅ PASS | .NET 10 / C# 13; nullable reference types enabled |
| VI. Blueprint Creation Standards | N/A | Feature is about schema storage, not blueprint creation |
| VII. Domain-Driven Design | ✅ PASS | Using established terminology: Schema, Category, Participant |
| VIII. Observability by Default | ✅ PASS | Will add health checks, OpenTelemetry tracing for schema operations |

**Gate Result**: PASS - Proceed to Phase 0

## Project Structure

### Documentation (this feature)

```text
specs/013-system-schema-store/
├── spec.md              # Feature specification (complete)
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (OpenAPI)
│   └── schema-api.yaml  # Schema store API contract
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── Common/
│   └── Sorcha.Blueprint.Schemas/          # EXISTING - Schema management, caching
│       ├── Models/
│       │   ├── SchemaEntry.cs             # NEW - Schema entity with metadata
│       │   ├── SchemaCategory.cs          # NEW - Category enumeration
│       │   └── SchemaSource.cs            # NEW - Source tracking
│       ├── Services/
│       │   ├── ISchemaStore.cs            # NEW - Schema store interface
│       │   ├── SchemaStore.cs             # NEW - Server-side implementation
│       │   └── ExternalSchemaProvider.cs  # NEW - SchemaStore.org integration
│       └── SystemSchemas/
│           ├── installation.json          # NEW - Core system schema
│           ├── organisation.json          # NEW - Core system schema
│           ├── participant.json           # NEW - Core system schema
│           └── register.json              # NEW - Core system schema
│
├── Services/
│   └── Sorcha.Blueprint.Service/          # EXISTING - Blueprint microservice
│       └── Endpoints/
│           └── SchemaEndpoints.cs         # NEW - Minimal API endpoints
│
└── Apps/
    └── Sorcha.UI/
        └── Sorcha.UI.Web.Client/          # EXISTING - Blazor WASM client
            └── Services/
                └── SchemaCache.cs         # NEW - IndexedDB caching

tests/
├── Sorcha.Blueprint.Schemas.Tests/        # EXISTING - Extend with schema store tests
│   ├── SchemaStoreTests.cs                # NEW - Unit tests
│   └── ExternalSchemaProviderTests.cs     # NEW - Unit tests
└── Sorcha.Blueprint.Service.IntegrationTests/  # EXISTING - Extend
    └── SchemaEndpointsTests.cs            # NEW - Integration tests
```

**Structure Decision**: Extend existing Blueprint.Schemas library (server-side) and UI.Web.Client (client caching). No new projects required - follows microservices-first principle of extending existing services.

## Complexity Tracking

> No violations requiring justification - all implementation fits within existing project structure.

| Item | Decision | Rationale |
|------|----------|-----------|
| No new service | Extend Blueprint service | Schema store is closely related to blueprint functionality |
| MongoDB storage | Reuse existing infrastructure | Blueprint service already uses MongoDB via Aspire |
| IndexedDB client cache | Standard browser storage | Best option for structured offline data in WASM |

## Constitution Check - Post Design Review

*Re-evaluation after Phase 1 design completion.*

| Principle | Status | Post-Design Notes |
|-----------|--------|-------------------|
| I. Microservices-First | ✅ PASS | Design extends existing services; no new microservices |
| II. Security First | ✅ PASS | OpenAPI contract includes JWT auth; role-based endpoints defined |
| III. API Documentation | ✅ PASS | Full OpenAPI 3.1 contract in `contracts/schema-api.yaml` |
| IV. Testing Requirements | ✅ PASS | Test files planned in project structure |
| V. Code Quality | ✅ PASS | Models and DTOs follow C# conventions |
| VI. Blueprint Creation Standards | N/A | Feature is about schema storage |
| VII. Domain-Driven Design | ✅ PASS | Consistent terminology: Schema, Category, Status |
| VIII. Observability by Default | ✅ PASS | ETag headers for caching; structured responses |

**Post-Design Gate Result**: PASS - Ready for `/speckit.tasks`

## Generated Artifacts

| Artifact | Path | Description |
|----------|------|-------------|
| Research | `research.md` | Technical decisions and patterns |
| Data Model | `data-model.md` | Entity definitions and MongoDB schema |
| API Contract | `contracts/schema-api.yaml` | OpenAPI 3.1 specification |
| Quickstart | `quickstart.md` | Developer getting started guide |
