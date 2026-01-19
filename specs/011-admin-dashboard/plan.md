# Implementation Plan: Admin Dashboard and Management

**Branch**: `011-admin-dashboard` | **Date**: 2026-01-19 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/011-admin-dashboard/spec.md`

## Summary

Build an administrative dashboard for Sorcha.UI that provides system administrators with real-time service health monitoring and KPIs, tenant organization CRUD operations, and enables organization administrators to manage users within their organizations. The implementation leverages existing Tenant Service APIs and extends the current Administration.razor page with new tabs and components using MudBlazor.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: Blazor WASM, MudBlazor 8.x, Sorcha.ServiceClients
**Storage**: Existing Tenant Service APIs (PostgreSQL backend)
**Testing**: xUnit (unit), Playwright (E2E), FluentAssertions
**Target Platform**: Web (Blazor WebAssembly)
**Project Type**: Web application (monorepo - src/Apps/Sorcha.UI)
**Performance Goals**: 3s initial page load, 30s health polling interval, 500ms validation feedback
**Constraints**: Support 100+ organizations, 500 users per organization, sub-2s list loads
**Scale/Scope**: 7 service health checks, 3 admin tabs, ~10 new components

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | ✅ PASS | UI feature consuming existing Tenant Service APIs |
| II. Security First | ✅ PASS | RBAC enforced client+server, audit logging required (FR-022) |
| III. API Documentation | ✅ PASS | Tenant Service already uses Scalar; UI consumes existing APIs |
| IV. Testing Requirements | ✅ PASS | E2E tests with Playwright for UI flows |
| V. Code Quality | ✅ PASS | Following existing Blazor patterns, async/await for API calls |
| VI. Blueprint Standards | N/A | Not creating blueprints |
| VII. Domain-Driven Design | ✅ PASS | "User" appropriate for identity domain (vs "Participant" for workflow domain) |
| VIII. Observability | ✅ PASS | Consuming existing /health endpoints, structured logging |

**Gate Result**: PASS - No violations requiring justification

## Project Structure

### Documentation (this feature)

```text
specs/011-admin-dashboard/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (API contracts)
└── tasks.md             # Phase 2 output (from /speckit.tasks)
```

### Source Code (repository root)

```text
src/Apps/Sorcha.UI/
├── Sorcha.UI.Core/
│   ├── Components/
│   │   └── Admin/
│   │       ├── ServiceHealthDashboard.razor      # NEW: Health monitoring grid
│   │       ├── ServiceHealthCard.razor           # NEW: Individual service card
│   │       ├── KpiSummaryPanel.razor             # NEW: KPI metrics display
│   │       ├── OrganizationList.razor            # NEW: Paginated org table
│   │       ├── OrganizationForm.razor            # NEW: Create/Edit org dialog
│   │       ├── UserList.razor                    # NEW: Org users table
│   │       ├── UserForm.razor                    # NEW: Add/Edit user dialog
│   │       ├── BlueprintServiceAdmin.razor       # EXISTING
│   │       └── PeerServiceAdmin.razor            # EXISTING
│   └── Services/
│       ├── IHealthCheckService.cs                # NEW: Health polling service
│       ├── HealthCheckService.cs                 # NEW: Implementation
│       ├── IOrganizationAdminService.cs          # NEW: Org management facade
│       ├── OrganizationAdminService.cs           # NEW: Implementation
│       └── IAuditService.cs                      # NEW: Audit logging client
│
├── Sorcha.UI.Web.Client/
│   └── Pages/
│       └── Administration.razor                  # MODIFY: Add new tabs
│
└── Sorcha.UI.Web/
    └── Program.cs                                # MODIFY: Register new services

tests/Sorcha.UI.E2E.Tests/
├── AdminDashboardTests.cs                        # NEW: E2E tests for dashboard
├── OrganizationManagementTests.cs                # NEW: E2E tests for org CRUD
└── UserManagementTests.cs                        # NEW: E2E tests for user management
```

**Structure Decision**: Extend existing Sorcha.UI.Core with new Admin components following the established pattern (BlueprintServiceAdmin.razor, PeerServiceAdmin.razor). Services layer abstracts API calls to Tenant Service.

## Complexity Tracking

> No constitution violations requiring justification.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | - | - |
