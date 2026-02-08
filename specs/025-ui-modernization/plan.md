# Implementation Plan: Sorcha UI Modernization

**Branch**: `025-ui-modernization` | **Date**: 2026-02-07 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/025-ui-modernization/spec.md`

## Summary

Comprehensive overhaul of the Sorcha.UI Blazor WASM application to close the gap between UI capabilities and backend service APIs. The work spans 12 user stories across 3 existing projects (`Sorcha.UI.Core`, `Sorcha.UI.Web.Client`, `Sorcha.UI.E2E.Tests`): creating a reusable identifier truncation component, restructuring navigation from a tabbed admin page to individual pages with direct links, adding organization management and validator admin pages, wiring dashboard stat cards to live APIs, replacing 3 placeholder pages (My Wallet, My Transactions, My Workflows) with real backend integration, migrating blueprint persistence from LocalStorage to the Blueprint Service API, connecting the template library to the backend template API, and adding docket chain inspection and OData query builder to the Explorer. All backend APIs already exist — this is purely a frontend integration effort.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (Blazor WASM)
**Primary Dependencies**: MudBlazor (Material Design components), Blazor.Diagrams (Designer), Blazored.LocalStorage, System.Net.Http.Json, Microsoft.AspNetCore.Components.WebAssembly
**Storage**: Browser-side only (HttpClient → API Gateway → backend services). No direct database access. Blueprint persistence migrates from LocalStorage to API.
**Testing**: NUnit + Microsoft.Playwright.NUnit for E2E tests against Docker; bUnit + xUnit for component unit tests
**Target Platform**: Blazor WebAssembly (browser)
**Project Type**: Web application (Blazor WASM client + backend services via YARP gateway)
**Performance Goals**: Pages load within 3 seconds, API calls complete within 2 seconds, real-time updates via SignalR
**Constraints**: All data access through API Gateway (no direct service calls), JWT authentication required, MudBlazor component library for all UI elements
**Scale/Scope**: ~24 existing pages, adding ~8 new pages, modifying ~6 existing pages, ~7 new services, ~15 new view models

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| I. Microservices-First | PASS | UI is a standalone WASM client consuming services via gateway. No coupling introduced. |
| II. Security First | PASS | All API calls use JWT authentication via existing `AuthenticatedHttpMessageHandler`. Mnemonics shown once only. No secrets in client. |
| III. API Documentation | N/A | No new backend APIs created. UI consumes existing documented endpoints. |
| IV. Testing Requirements | PASS | E2E tests planned for all new pages using existing Playwright + NUnit infrastructure. Component unit tests for new services. |
| V. Code Quality | PASS | Using async/await, DI, nullable reference types. Following existing patterns in codebase. |
| VI. Blueprint Creation Standards | PASS | Designer continues to work with JSON blueprints. Cloud persistence preserves same format. |
| VII. Domain-Driven Design | PASS | Using Sorcha domain language: Blueprint, Action, Participant, Disclosure, Publish. |
| VIII. Observability by Default | PASS | Health check service already in place. New pages follow existing error handling patterns. |

**Post-Phase 1 Re-check**: All gates remain PASS. No new patterns or architectural deviations introduced.

## Project Structure

### Documentation (this feature)

```text
specs/025-ui-modernization/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 research decisions
├── data-model.md        # View model definitions
├── quickstart.md        # Manual verification guide
├── contracts/           # Service contracts
│   └── ui-service-contracts.md
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
src/Apps/Sorcha.UI/
├── Sorcha.UI.Core/                      # Shared components and services
│   ├── Components/
│   │   ├── Admin/                       # MODIFY: Extract existing admin components
│   │   │   ├── ServiceHealthDashboard.razor  # Existing — promoted to standalone page
│   │   │   ├── PeerServiceAdmin.razor        # Existing — promoted to standalone page
│   │   │   ├── OrganizationList.razor        # Existing — enhance with CRUD
│   │   │   ├── OrganizationForm.razor        # Existing — enhance for edit/deactivate
│   │   │   └── UserList.razor                # Existing — no changes
│   │   ├── Admin/Validator/             # NEW
│   │   │   └── ValidatorPanel.razor          # Mempool status, validation activity
│   │   ├── Admin/ServicePrincipals/     # NEW
│   │   │   └── ServicePrincipalList.razor    # Credential list with status
│   │   ├── Explorer/                    # NEW
│   │   │   ├── DocketChain.razor             # Docket chain visualization
│   │   │   ├── DocketDetail.razor            # Individual docket inspector
│   │   │   └── ODataQueryBuilder.razor       # Visual query builder
│   │   ├── Shared/                      # MODIFY
│   │   │   ├── TruncatedId.razor             # NEW: Reusable truncation component
│   │   │   ├── EmptyState.razor              # NEW: Reusable empty state
│   │   │   ├── ServiceUnavailable.razor      # NEW: Reusable error state
│   │   │   ├── ConfirmDialog.razor           # Existing — no changes
│   │   │   ├── JsonTreeView.razor            # Existing — no changes
│   │   │   └── JsonTreeNode.razor            # Existing — no changes
│   │   ├── Workflows/                   # NEW
│   │   │   ├── WorkflowList.razor            # Workflow instance list
│   │   │   ├── WorkflowDetail.razor          # Workflow detail with action history
│   │   │   ├── ActionList.razor              # Pending action list
│   │   │   └── ActionForm.razor              # Dynamic form from data schema
│   │   ├── Blueprints/                  # NEW
│   │   │   ├── BlueprintList.razor           # API-backed blueprint list
│   │   │   ├── PublishReview.razor            # Publishing validation review
│   │   │   └── VersionHistory.razor          # Blueprint version list
│   │   └── Templates/                   # NEW
│   │       ├── TemplateList.razor            # Backend-driven template list
│   │       └── TemplateEvaluator.razor       # Parameter form for template evaluation
│   ├── Models/
│   │   ├── Admin/                       # MODIFY: Add new admin models
│   │   │   ├── HealthResponse.cs             # Existing
│   │   │   ├── OrganizationViewModel.cs      # NEW: Rich org view model
│   │   │   ├── ValidatorStatusViewModel.cs   # NEW: Mempool/consensus status
│   │   │   └── ServicePrincipalViewModel.cs  # NEW: Credential view model
│   │   ├── Dashboard/                   # NEW
│   │   │   └── DashboardStatsViewModel.cs
│   │   ├── Workflows/                   # NEW
│   │   │   ├── WorkflowInstanceViewModel.cs
│   │   │   ├── PendingActionViewModel.cs
│   │   │   └── ActionSubmissionViewModel.cs
│   │   ├── Blueprints/                  # NEW
│   │   │   ├── BlueprintListItemViewModel.cs
│   │   │   ├── BlueprintVersionViewModel.cs
│   │   │   └── PublishReviewViewModel.cs
│   │   ├── Templates/                   # NEW
│   │   │   └── TemplateListItemViewModel.cs
│   │   └── Explorer/                    # NEW
│   │       ├── DocketViewModel.cs
│   │       └── ODataQueryModel.cs
│   └── Services/
│       ├── IDashboardService.cs              # NEW
│       ├── DashboardService.cs               # NEW
│       ├── IOrganizationAdminService.cs      # MODIFY: Add CRUD methods
│       ├── OrganizationAdminService.cs       # MODIFY: Implement CRUD
│       ├── IValidatorAdminService.cs         # NEW
│       ├── ValidatorAdminService.cs          # NEW
│       ├── IWorkflowService.cs               # NEW
│       ├── WorkflowService.cs                # NEW
│       ├── IBlueprintApiService.cs           # NEW
│       ├── BlueprintApiService.cs            # NEW
│       ├── ITemplateApiService.cs            # NEW
│       ├── TemplateApiService.cs             # NEW
│       ├── IDocketService.cs                 # NEW
│       ├── DocketService.cs                  # NEW
│       ├── IODataQueryService.cs             # NEW
│       ├── ODataQueryService.cs              # NEW
│       ├── ITransactionService.cs            # MODIFY: Add GetMyTransactionsAsync
│       └── TransactionService.cs             # MODIFY: Implement wallet query
│
├── Sorcha.UI.Web.Client/                # Blazor WASM pages
│   ├── Pages/
│   │   ├── Home.razor                        # MODIFY: Wire to DashboardService
│   │   ├── MyWorkflows.razor                 # MODIFY: Replace placeholder
│   │   ├── MyActions.razor                   # Already real — minor enhancement
│   │   ├── MyTransactions.razor              # MODIFY: Replace placeholder
│   │   ├── MyWallet.razor                    # MODIFY: Replace placeholder
│   │   ├── Blueprints.razor                  # MODIFY: API instead of LocalStorage
│   │   ├── Templates.razor                   # MODIFY: API instead of hardcoded
│   │   ├── Administration.razor              # MODIFY: Redirect to new pages
│   │   ├── Admin/                            # NEW directory
│   │   │   ├── SystemHealth.razor            # NEW: Standalone health page
│   │   │   ├── PeerNetwork.razor             # NEW: Standalone peer admin page
│   │   │   ├── Organizations.razor           # NEW: Organization CRUD page
│   │   │   ├── Validator.razor               # NEW: Validator admin page
│   │   │   └── ServicePrincipals.razor       # NEW: Service principal page
│   │   └── Registers/
│   │       └── Detail.razor                  # MODIFY: Add docket chain tab
│   └── Components/Layout/
│       └── MainLayout.razor                  # MODIFY: Restructure navigation
│
└── Sorcha.UI.Web/                       # Server host — no changes expected

tests/Sorcha.UI.E2E.Tests/
├── Docker/
│   ├── AdminHealthTests.cs                   # NEW: System Health page tests
│   ├── AdminOrganizationsTests.cs            # NEW: Organization management tests
│   ├── AdminValidatorTests.cs                # NEW: Validator panel tests
│   ├── DashboardLiveTests.cs                 # NEW: Live stat card tests
│   ├── WorkflowTests.cs                      # NEW: My Workflows/Actions tests
│   ├── BlueprintCloudTests.cs                # NEW: Cloud persistence tests
│   ├── WalletManagementTests.cs              # NEW: My Wallet tests
│   ├── TransactionHistoryTests.cs            # NEW: My Transactions tests
│   ├── TemplateLibraryTests.cs               # NEW: Template API tests
│   └── ExplorerEnhancementTests.cs           # NEW: Docket chain + query builder tests
├── PageObjects/
│   ├── AdminPages/                           # NEW
│   │   ├── SystemHealthPage.cs
│   │   ├── OrganizationsPage.cs
│   │   ├── ValidatorPage.cs
│   │   └── ServicePrincipalsPage.cs
│   ├── WorkflowPages/                        # NEW
│   │   ├── MyWorkflowsPage.cs
│   │   └── MyActionsPage.cs
│   └── ExplorerPages/                        # NEW
│       └── QueryBuilderPage.cs
└── Infrastructure/
    └── TestConstants.cs                      # MODIFY: Add new routes
```

**Structure Decision**: This feature modifies the existing Blazor WASM project structure (`Sorcha.UI.Core` for shared logic, `Sorcha.UI.Web.Client` for pages). No new projects are created — all work fits within the existing 3 projects (Core, Web.Client, E2E.Tests). New pages go in a `Pages/Admin/` subdirectory to organize the flattened admin pages.

## Complexity Tracking

No constitution violations. All work fits within existing project structure and patterns.

## Implementation Phases

### Phase 1: Cross-Cutting Foundation (US1 + US2)

**Goal**: Build the reusable TruncatedId component and restructure navigation.

**Files**:
- NEW: `Sorcha.UI.Core/Components/Shared/TruncatedId.razor` — truncation component
- NEW: `Sorcha.UI.Core/Components/Shared/EmptyState.razor` — empty state component
- NEW: `Sorcha.UI.Core/Components/Shared/ServiceUnavailable.razor` — error state component
- MODIFY: `Sorcha.UI.Web.Client/Components/Layout/MainLayout.razor` — restructure nav
- NEW: `Sorcha.UI.Web.Client/Pages/Admin/SystemHealth.razor` — extracted from Administration
- NEW: `Sorcha.UI.Web.Client/Pages/Admin/PeerNetwork.razor` — extracted from Administration
- MODIFY: `Sorcha.UI.Web.Client/Pages/Administration.razor` — redirect to new pages
- MODIFY: Existing pages using ad-hoc truncation → adopt TruncatedId component

**Risk**: Low. New components, existing patterns.
**Est. Tests**: ~15 E2E + ~10 unit

### Phase 2: Admin Pages (US3 + US4 + US5)

**Goal**: Add Organization Management, Validator Admin, and Service Principal pages.

**Files**:
- NEW: `Sorcha.UI.Web.Client/Pages/Admin/Organizations.razor`
- NEW: `Sorcha.UI.Web.Client/Pages/Admin/Validator.razor`
- NEW: `Sorcha.UI.Web.Client/Pages/Admin/ServicePrincipals.razor`
- NEW: `Sorcha.UI.Core/Components/Admin/Validator/ValidatorPanel.razor`
- NEW: `Sorcha.UI.Core/Components/Admin/ServicePrincipals/ServicePrincipalList.razor`
- MODIFY: `Sorcha.UI.Core/Services/IOrganizationAdminService.cs` — add CRUD
- MODIFY: `Sorcha.UI.Core/Services/OrganizationAdminService.cs` — implement CRUD
- NEW: `Sorcha.UI.Core/Services/IValidatorAdminService.cs`
- NEW: `Sorcha.UI.Core/Services/ValidatorAdminService.cs`
- NEW: `Sorcha.UI.Core/Models/Admin/OrganizationViewModel.cs`
- NEW: `Sorcha.UI.Core/Models/Admin/ValidatorStatusViewModel.cs`
- NEW: `Sorcha.UI.Core/Models/Admin/ServicePrincipalViewModel.cs`

**Risk**: Medium. Organization CRUD depends on Tenant Service API availability.
**Est. Tests**: ~20 E2E + ~15 unit

### Phase 3: Dashboard & Stats (US6)

**Goal**: Wire dashboard stat cards to live gateway endpoint.

**Files**:
- MODIFY: `Sorcha.UI.Web.Client/Pages/Home.razor` — replace hardcoded stats
- NEW: `Sorcha.UI.Core/Services/IDashboardService.cs`
- NEW: `Sorcha.UI.Core/Services/DashboardService.cs`
- NEW: `Sorcha.UI.Core/Models/Dashboard/DashboardStatsViewModel.cs`
- MODIFY: `Sorcha.UI.Core/Extensions/ServiceCollectionExtensions.cs` — register DashboardService

**Risk**: Low. Single API call, simple mapping.
**Est. Tests**: ~8 E2E + ~5 unit

### Phase 4: Workflow Management (US7)

**Goal**: Replace My Workflows and My Actions placeholders with real API integration.

**Files**:
- MODIFY: `Sorcha.UI.Web.Client/Pages/MyWorkflows.razor` — replace placeholder
- MODIFY: `Sorcha.UI.Web.Client/Pages/MyActions.razor` — enhance with detail view
- NEW: `Sorcha.UI.Core/Components/Workflows/WorkflowList.razor`
- NEW: `Sorcha.UI.Core/Components/Workflows/WorkflowDetail.razor`
- NEW: `Sorcha.UI.Core/Components/Workflows/ActionList.razor`
- NEW: `Sorcha.UI.Core/Components/Workflows/ActionForm.razor`
- NEW: `Sorcha.UI.Core/Services/IWorkflowService.cs`
- NEW: `Sorcha.UI.Core/Services/WorkflowService.cs`
- NEW: `Sorcha.UI.Core/Models/Workflows/WorkflowInstanceViewModel.cs`
- NEW: `Sorcha.UI.Core/Models/Workflows/PendingActionViewModel.cs`
- NEW: `Sorcha.UI.Core/Models/Workflows/ActionSubmissionViewModel.cs`

**Risk**: High. Action form generation from JSON Schema is complex. Delegation tokens required for execution.
**Est. Tests**: ~20 E2E + ~15 unit

### Phase 5: Blueprint Cloud Persistence (US8)

**Goal**: Migrate blueprint storage from LocalStorage to Blueprint Service API.

**Files**:
- NEW: `Sorcha.UI.Core/Services/IBlueprintApiService.cs`
- NEW: `Sorcha.UI.Core/Services/BlueprintApiService.cs`
- MODIFY: `Sorcha.UI.Web.Client/Pages/Blueprints.razor` — use API instead of LocalStorage
- MODIFY: `Sorcha.UI.Core/Components/Designer/LoadBlueprintDialog.razor` — load from API
- NEW: `Sorcha.UI.Core/Components/Blueprints/PublishReview.razor`
- NEW: `Sorcha.UI.Core/Components/Blueprints/VersionHistory.razor`
- NEW: `Sorcha.UI.Core/Models/Blueprints/BlueprintListItemViewModel.cs`
- NEW: `Sorcha.UI.Core/Models/Blueprints/BlueprintVersionViewModel.cs`
- NEW: `Sorcha.UI.Core/Models/Blueprints/PublishReviewViewModel.cs`
- MODIFY: `Sorcha.UI.Core/Extensions/ServiceCollectionExtensions.cs` — swap DI registration

**Risk**: Medium. Designer integration needs careful handling to preserve existing functionality.
**Est. Tests**: ~15 E2E + ~10 unit

### Phase 6: Wallet Management (US9)

**Goal**: Replace My Wallet placeholder with real Wallet Service integration.

**Files**:
- MODIFY: `Sorcha.UI.Web.Client/Pages/MyWallet.razor` — replace placeholder
- Uses existing `IWalletApiService` (already has full CRUD + signing)
- Existing `Wallets/WalletList.razor` already works — link from My Wallet or reuse component
- Existing wallet models already defined in `Models/Wallet/`

**Risk**: Low. Wallet API service already exists and is tested. Main work is UI composition.
**Est. Tests**: ~10 E2E + ~5 unit

### Phase 7: Transaction History (US10)

**Goal**: Replace My Transactions placeholder with real Register Service queries.

**Files**:
- MODIFY: `Sorcha.UI.Web.Client/Pages/MyTransactions.razor` — replace placeholder
- MODIFY: `Sorcha.UI.Core/Services/ITransactionService.cs` — add wallet-scoped query
- MODIFY: `Sorcha.UI.Core/Services/TransactionService.cs` — implement wallet query
- Uses existing `TransactionViewModel`, `TransactionList.razor`, `TransactionDetail.razor`

**Risk**: Low. Transaction service and components already exist. Adding wallet-scoped query method.
**Est. Tests**: ~10 E2E + ~5 unit

### Phase 8: Template Library (US11)

**Goal**: Connect template page to backend template API.

**Files**:
- MODIFY: `Sorcha.UI.Web.Client/Pages/Templates.razor` — use API
- NEW: `Sorcha.UI.Core/Services/ITemplateApiService.cs`
- NEW: `Sorcha.UI.Core/Services/TemplateApiService.cs`
- NEW: `Sorcha.UI.Core/Components/Templates/TemplateList.razor`
- NEW: `Sorcha.UI.Core/Components/Templates/TemplateEvaluator.razor`
- NEW: `Sorcha.UI.Core/Models/Templates/TemplateListItemViewModel.cs`

**Risk**: Low. Template API is straightforward CRUD + evaluate.
**Est. Tests**: ~8 E2E + ~5 unit

### Phase 9: Explorer Enhancements (US12)

**Goal**: Add docket chain inspection and OData query builder.

**Files**:
- MODIFY: `Sorcha.UI.Web.Client/Pages/Registers/Detail.razor` — add docket tab
- NEW: `Sorcha.UI.Core/Components/Explorer/DocketChain.razor`
- NEW: `Sorcha.UI.Core/Components/Explorer/DocketDetail.razor`
- NEW: `Sorcha.UI.Core/Components/Explorer/ODataQueryBuilder.razor`
- NEW: `Sorcha.UI.Core/Services/IDocketService.cs`
- NEW: `Sorcha.UI.Core/Services/DocketService.cs`
- NEW: `Sorcha.UI.Core/Services/IODataQueryService.cs`
- NEW: `Sorcha.UI.Core/Services/ODataQueryService.cs`
- NEW: `Sorcha.UI.Core/Models/Explorer/DocketViewModel.cs`
- NEW: `Sorcha.UI.Core/Models/Explorer/ODataQueryModel.cs`
- MODIFY: `Sorcha.UI.Web.Client/Pages/Registers/Query.razor` — integrate query builder

**Risk**: Medium. OData query builder has complex UI logic. Docket chain display is straightforward.
**Est. Tests**: ~15 E2E + ~10 unit

### Phase 10: Polish & Retroactive Truncation (Cross-cutting)

**Goal**: Apply TruncatedId component to all existing pages, ensure empty states and error states are consistent, update E2E test constants and page objects.

**Files**:
- MODIFY: All pages with ad-hoc truncation → adopt TruncatedId
- MODIFY: `WalletList.razor` — replace `TruncateAddress` method
- MODIFY: `RegisterCard.razor` — replace `Register.Id[..8]...`
- MODIFY: `TransactionViewModel` — remove inline truncation properties
- MODIFY: `MyActions.razor` — replace `InstanceId[..8]...`
- MODIFY: `Sorcha.UI.E2E.Tests/Infrastructure/TestConstants.cs` — add new routes
- NEW: All Page Object classes for new pages
- NEW: All E2E test classes for new pages

**Risk**: Low. Mechanical replacement work.
**Est. Tests**: ~10 E2E (navigation + cross-cutting)

---

## Phase Summary

| Phase | User Stories | New Files | Modified Files | Est. Tests |
|-------|-------------|-----------|----------------|------------|
| 1 | US1, US2 | ~5 | ~8 | ~25 |
| 2 | US3, US4, US5 | ~10 | ~3 | ~35 |
| 3 | US6 | ~3 | ~2 | ~13 |
| 4 | US7 | ~8 | ~2 | ~35 |
| 5 | US8 | ~6 | ~4 | ~25 |
| 6 | US9 | ~1 | ~1 | ~15 |
| 7 | US10 | ~0 | ~3 | ~15 |
| 8 | US11 | ~4 | ~1 | ~13 |
| 9 | US12 | ~7 | ~2 | ~25 |
| 10 | Cross-cutting | ~12 | ~8 | ~10 |
| **Total** | **12 stories** | **~56** | **~34** | **~211** |

## Key Risks & Mitigations

1. **Action form generation from JSON Schema** (Phase 4): The Blueprint Service uses JSON Schema for action data validation. Generating dynamic MudBlazor forms from arbitrary JSON Schema is complex. Mitigation: Start with simple field types (string, number, boolean) and iterate. Use existing `JsonSchema.Net` for validation.

2. **Blueprint Designer + API persistence** (Phase 5): The Designer currently saves/loads from LocalStorage via `IBlueprintStorageService`. Swapping to API must preserve the Designer's auto-save behavior. Mitigation: Keep the same interface, only change the implementation. Add debouncing to avoid excessive API calls on every keystroke.

3. **Delegation tokens for workflow execution** (Phase 4): Executing actions requires `X-Delegation-Token` header. The UI needs to obtain this from the Wallet Service. Mitigation: Use existing `IWalletApiService` delegation endpoints.

4. **OData query builder complexity** (Phase 9): Building a visual query builder that generates valid OData is non-trivial. Mitigation: Start with simple filters (field eq value), add operators incrementally. Show the raw OData string so power users can verify.

5. **Service Principal endpoints** (Phase 2): No dedicated CRUD endpoint found in Tenant Service. Mitigation: Display token introspection data and last-used metrics from auth endpoints. Note: may need a future Tenant Service update for full management.
