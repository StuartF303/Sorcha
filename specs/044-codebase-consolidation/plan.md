# Implementation Plan: Codebase Cleanup & Consolidation

**Branch**: `044-codebase-consolidation` | **Date**: 2026-02-27 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/044-codebase-consolidation/spec.md`

## Summary

Eliminate ~1,000+ lines of duplicated code across 69 projects by consolidating shared authorization policies, middleware pipeline infrastructure, and DTOs into central locations. Fix orphaned CLI endpoints, extract shared MCP models, add missing license headers, and resolve middleware ordering bugs. All work items are independently implementable and testable.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: Sorcha.ServiceDefaults (existing shared infrastructure), Sorcha.ServiceClients (existing shared client library)
**Storage**: N/A — no database/schema changes
**Testing**: xUnit + FluentAssertions + Moq (1,100+ existing tests, zero regressions required)
**Target Platform**: .NET 10 microservices (7 services + CLI + MCP Server + UI)
**Project Type**: Microservices solution (39 source projects, 30 test projects)
**Performance Goals**: N/A — consolidation must not degrade performance
**Constraints**: Zero regressions, all tests must pass, no API contract changes
**Scale/Scope**: 7 services, 18 MCP tools, ~168 files for headers, 6 auth extension files

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | Consolidation into ServiceDefaults is shared infrastructure, not coupling |
| II. Security First | PASS | Auth policies being consolidated, not weakened; zero regressions required |
| III. API Documentation | PASS | OpenAPI/Scalar consolidation uses Scalar (correct per constitution) |
| IV. Testing Requirements | PASS | All 1,100+ tests must pass; no new untested code paths |
| V. Code Quality | PASS | Reducing duplication directly improves code quality |
| VI. Blueprint Creation | N/A | No blueprint changes |
| VII. Domain-Driven Design | N/A | No domain model changes |
| VIII. Observability | PASS | Serilog consolidation preserves existing logging |

**Post-Phase 1 Re-check**: No violations introduced. All new extension methods follow existing ServiceDefaults patterns. No new projects created — uses existing shared libraries.

## Project Structure

### Documentation (this feature)

```text
specs/044-codebase-consolidation/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0: research decisions
├── data-model.md        # Phase 1: shared structure definitions
├── quickstart.md        # Phase 1: implementation guide
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (files created/modified)

```text
src/Common/Sorcha.ServiceDefaults/
├── AuthorizationPolicyExtensions.cs   # NEW — 6 shared auth policies
├── OpenApiExtensions.cs               # NEW — shared OpenAPI/Scalar config
├── CorsExtensions.cs                  # NEW — shared CORS config
├── Extensions.cs                      # EXISTING (unchanged)
├── JwtAuthenticationExtensions.cs     # EXISTING (unchanged)
└── SerilogExtensions.cs               # EXISTING (unchanged)

src/Common/Sorcha.ServiceClients/Wallet/Models/
└── CreateWalletRequest.cs             # NEW — consolidated DTO

src/Apps/Sorcha.McpServer/Infrastructure/Models/
└── ErrorResponse.cs                   # NEW — shared error model

src/Services/Sorcha.*.Service/
├── Extensions/AuthenticationExtensions.cs  # MODIFIED — remove shared policies, keep service-specific
└── Program.cs                              # MODIFIED — use shared OpenAPI/CORS/pipeline helpers

src/Apps/Sorcha.Cli/
├── Services/IAdminServiceClient.cs    # MODIFIED — remove schema endpoints, fix alert path
├── Services/ICredentialServiceClient.cs # MODIFIED — fix paths /api/credentials → /api/v1/credentials
├── Commands/AdminCommands.cs          # MODIFIED — remove schema commands
└── Sorcha.Cli.csproj                  # MODIFIED — remove ReadLine package

~168 .cs files                         # MODIFIED — add SPDX license headers
CLAUDE.md                             # MODIFIED — document SignalR naming exception
```

**Structure Decision**: No new projects created. All shared code placed in existing `Sorcha.ServiceDefaults` and `Sorcha.ServiceClients` projects following their established extension method and models patterns.

## Implementation Tasks

### Task Group A: ServiceDefaults Extensions (P1/P2)

**A1: Authorization Policy Consolidation** (P1, FR-001 through FR-004)
- Create `AuthorizationPolicyExtensions.cs` in ServiceDefaults
- Define `AddSorchaAuthorizationPolicies()` with 6 shared policies
- Update 6 service `AuthenticationExtensions.cs` to call shared method + add service-specific policies only
- Remove duplicated policy definitions from each service
- Verify: `dotnet test` on all service test projects

**A2: OpenAPI/Scalar Consolidation** (P2, FR-005, FR-006)
- Create `OpenApiExtensions.cs` in ServiceDefaults
- Define `AddSorchaOpenApi(title, description)` with document transformer
- Define `MapSorchaOpenApiUi(title, theme)` for Scalar mapping
- Update 7 Program.cs files to use shared methods
- ApiGateway: keep existing aggregation, use shared method for base config
- Verify: all Scalar UIs render in dev mode

**A3: CORS Consolidation** (P2, FR-007)
- Create `CorsExtensions.cs` in ServiceDefaults
- Define `AddSorchaCors()` with AllowAnyOrigin policy
- Update 4 services (Blueprint, ApiGateway, Tenant, Wallet) to use shared method
- Leave Peer and Validator without CORS (intentional — internal services)

**A4: Pipeline Bug Fixes** (P2, FR-008 through FR-010)
- Tenant Service: remove duplicate `UseRateLimiter()` call
- Wallet Service: move `UseHttpsEnforcement()` before `UseInputValidation()`
- Verify middleware ordering is consistent across all services

### Task Group B: CLI Cleanup (P2)

**B1: Fix Orphaned Admin Endpoints** (FR-011, FR-012)
- Remove `/api/schemas/sectors` and `/api/schemas/providers` from `IAdminServiceClient`
- Remove `AdminSchemaSectorsCommand` and `AdminSchemaProvidersCommand`
- Fix `/api/admin/alerts` → `/api/alerts` in `IAdminServiceClient`

**B2: Fix Credential Endpoint Paths** (FR-013)
- Update all 7 paths in `ICredentialServiceClient` from `/api/credentials/` to `/api/v1/credentials/`

**B3: Remove Unused Package** (FR-014)
- Remove `ReadLine` v2.0.1 from `Sorcha.Cli.csproj`

### Task Group C: MCP & DTO Consolidation (P3)

**C1: Extract MCP ErrorResponse** (FR-015)
- Create `Infrastructure/Models/ErrorResponse.cs` as public sealed class
- Update 18 MCP tool files to remove private ErrorResponse, import shared one
- Verify: `dotnet build` on McpServer project

**C2: Merge CreateWalletRequest DTO** (FR-016)
- Create `ServiceClients/Wallet/Models/CreateWalletRequest.cs` with all fields (PQC optional)
- Add `Sorcha.ServiceClients` reference to `Wallet.Service.csproj` and `UI.Core.csproj`
- Update Wallet Service and UI Core to use shared DTO
- Remove old DTO files from both projects

### Task Group D: Compliance & Documentation (P3)

**D1: Add License Headers** (FR-017)
- Write PowerShell script to scan for missing headers and add them
- Run across all `src/**/*.cs` files
- Verify: `dotnet build` (no compilation errors from added headers)

**D2: Document SignalR Naming Exception** (FR-018)
- Add note to CLAUDE.md Development Guidelines that SignalR hub methods are exempt from `Async` suffix convention (client-callable by name)

## Parallelization Strategy

All task groups (A, B, C, D) are independent and can execute in parallel:

```
Wave 1 (parallel):  A1 + B1 + C1 + D1
Wave 2 (parallel):  A2 + B2 + C2 + D2
Wave 3 (parallel):  A3 + B3
Wave 4 (sequential): A4 (after A2/A3 so pipeline is stable)
Final: Full solution build + test
```

## Complexity Tracking

> No constitution violations. No new projects. No complexity justification needed.
