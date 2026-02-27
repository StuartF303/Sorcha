# Research: 044 Codebase Consolidation

**Date**: 2026-02-27
**Branch**: `044-codebase-consolidation`

## Decision 1: Where to Place Shared Authorization Policies

**Decision**: Add a new `AuthorizationPolicyExtensions.cs` file in `Sorcha.ServiceDefaults`

**Rationale**:
- ServiceDefaults already contains `JwtAuthenticationExtensions.cs` — auth policies are the natural companion
- All 7 services already reference ServiceDefaults
- The `Microsoft.Extensions.Hosting` namespace convention means services need zero new `using` statements
- `Microsoft.AspNetCore.Authentication.JwtBearer` is already a dependency of ServiceDefaults
- Pattern: `AddSorchaAuthorizationPolicies()` extension method with generic `TBuilder : IHostApplicationBuilder` constraint
- Each service calls the shared method then adds its own policies via `AddAuthorization(options => { ... })`

**Alternatives Considered**:
- New `Sorcha.AuthorizationDefaults` project: Rejected — unnecessary project proliferation, ServiceDefaults already serves this role
- Merge into `Extensions.cs`: Rejected — file is already 495 lines, separation of concerns is better

## Decision 2: Shared vs. Service-Specific Authorization Policies

**Decision**: Extract these 5 policies as shared, leave all others service-specific:

| Shared Policy | Services Using |
|--------------|---------------|
| RequireAuthenticated | All 6 |
| RequireService | All 6 |
| RequireOrganizationMember | All 6 |
| RequireDelegatedAuthority | 5 of 6 (not Peer) |
| RequireAdministrator | 3 of 6 (Blueprint, Validator, Tenant) |

Additionally, `CanWriteDockets` is identical in Register and Validator — include as shared.

**Rationale**: These 6 policies are identical across services. The remaining ~15 policies are genuinely service-specific (CanManageBlueprints, CanSubmitTransactions, CanManageWallets, etc.) and should stay in each service's `AuthenticationExtensions.cs`.

**Alternatives Considered**:
- Extract ALL policies to shared: Rejected — service-specific policies reference service-specific claim names and shouldn't be centralized
- Extract only the 3 universal policies: Rejected — RequireDelegatedAuthority and RequireAdministrator are complex enough to warrant single-source-of-truth

## Decision 3: Pipeline Consolidation Approach

**Decision**: Create two new extension methods in ServiceDefaults:
1. `AddSorchaOpenApi(string title, string description)` — registers OpenAPI with document transformer
2. `MapSorchaOpenApiUi(string title, ScalarTheme theme = Purple)` — maps Scalar UI in development

Do NOT create an all-in-one pipeline method — services have legitimate ordering differences.

**Rationale**:
- Blueprint needs `UseOutputCache()` and `UseJsonLdContentNegotiation()` between security headers and auth
- Peer and Validator have gRPC service mappings at specific points
- Tenant has custom rate limiting different from the standard
- An all-in-one method would need so many opt-out flags it defeats the purpose
- Instead: provide building blocks that services compose in the correct order

**Alternatives Considered**:
- `UseSorchaStandardPipeline()` all-in-one: Rejected — too many service-specific exceptions
- Only extract CORS: Rejected — OpenAPI is the higher-value extraction (more boilerplate per service)

## Decision 4: CORS Consolidation

**Decision**: Add `AddSorchaCors()` extension to ServiceDefaults using the existing `AllowAnyOrigin` pattern. Services opt in by calling it.

**Rationale**: 4 of 7 services use identical CORS. Peer and Validator intentionally skip CORS (internal services). The AllowAnyOrigin policy is acceptable for current development stage.

## Decision 5: CreateWalletRequest DTO Placement

**Decision**: Place in `src/Common/Sorcha.ServiceClients/Wallet/Models/CreateWalletRequest.cs`

**Rationale**:
- ServiceClients already has `Register/Models/` subfolder with shared DTOs (GovernanceClientModels, PublishedParticipantModels)
- ServiceClients has zero upstream dependencies on App or Service layers — no circular dependency risk
- Aligns with existing CONSOLIDATION-PLAN.md which calls for `ServiceClients/Wallet/Models/`
- Both UI Core and Wallet Service can safely add a reference to ServiceClients

**Project Reference Updates Required**:
- `Wallet.Service.csproj` → add `Sorcha.ServiceClients` reference
- `UI.Core.csproj` → add `Sorcha.ServiceClients` reference (doesn't reference it yet)
- CLI already has its own Refit-based DTOs (different serialization attributes) — leave as-is for now

**Alternatives Considered**:
- New `Sorcha.Wallet.Models` project: Rejected — unnecessary project proliferation
- `Sorcha.Wallet.Core`: Rejected — mixes domain models with HTTP DTOs
- Place in ServiceClients root: Rejected — Register already uses subfolder pattern

## Decision 6: MCP Server ErrorResponse Location

**Decision**: Place in `src/Apps/Sorcha.McpServer/Infrastructure/Models/ErrorResponse.cs`

**Rationale**:
- `Infrastructure/` already contains shared classes (McpErrorHandler, GracefulDegradation, etc.)
- Adding a `Models/` subfolder follows convention and keeps Infrastructure organized
- All 18 tools already import `Sorcha.McpServer.Infrastructure` namespace
- The class becomes `public sealed class` (not `private sealed`) so all tools can reference it

**Alternatives Considered**:
- Place directly in Infrastructure root: Rejected — already has 4 files, Models subfolder is cleaner
- New top-level Models/ folder: Rejected — Infrastructure is the established shared location
- Leave as-is: Rejected — 18 identical private classes is clearly a DRY violation

## Decision 7: SignalR Hub Async Naming

**Decision**: Document as convention exception in CLAUDE.md rather than rename

**Rationale**:
- SignalR client-callable hub methods are invoked by name from JavaScript — `connection.invoke("Subscribe")`
- Adding `Async` suffix would require updating all client-side JavaScript in Blazor components
- The .NET SignalR documentation examples don't use `Async` suffix for hub methods
- This is an industry-standard convention, not a deviation

## Decision 8: CLI Credential Path Fix

**Decision**: Update `ICredentialServiceClient` paths from `/api/credentials/` to `/api/v1/credentials/` to match YARP routes

**Rationale**: YARP already routes `/api/v1/credentials/{**catch-all}` to `blueprint-cluster`. The CLI just has the wrong path prefix. One-line fix per endpoint definition.

## Decision 9: License Header Automation

**Decision**: Write a PowerShell script to scan and add missing headers, then run it once

**Rationale**:
- ~168 files need the header — manual editing is error-prone
- A script can check for existing header presence (idempotent)
- Script can be reused for future files (or added as a pre-commit hook later)
