# Tasks: Sorcha UI Modernization

**Input**: Design documents from `/specs/025-ui-modernization/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ui-service-contracts.md

**Tests**: E2E tests (Playwright + NUnit) included per the spec requirement (SC-010: "All new pages have accompanying Playwright E2E tests").

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Paths relative to `src/Apps/Sorcha.UI/` unless otherwise noted

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create reusable shared components and model directories that multiple stories depend on

- [x] T001 [P] Create `Sorcha.UI.Core/Components/Shared/TruncatedId.razor` — reusable identifier truncation component with MudTooltip for hover-to-reveal, click-to-copy via JS interop (`navigator.clipboard.writeText()`), MudSnackbar confirmation, auto-detect "0x" prefix, show first 6 + "..." + last 6 chars, skip truncation for values <= 12 chars. Parameters: `Value` (string), `MaxLength` (int, default 12)
- [x] T002 [P] Create `Sorcha.UI.Core/Components/Shared/EmptyState.razor` — reusable empty state component with icon, title, description, and optional action button (e.g., "Create your first wallet"). Parameters: `Icon` (string), `Title` (string), `Description` (string), `ActionText` (string?), `OnAction` (EventCallback)
- [x] T003 [P] Create `Sorcha.UI.Core/Components/Shared/ServiceUnavailable.razor` — reusable error state component for when backend APIs are unreachable. Shows error icon, service name, message, and retry button. Parameters: `ServiceName` (string), `OnRetry` (EventCallback)
- [x] T004 Create model directory structure: `Sorcha.UI.Core/Models/Dashboard/`, `Sorcha.UI.Core/Models/Workflows/`, `Sorcha.UI.Core/Models/Blueprints/`, `Sorcha.UI.Core/Models/Templates/`, `Sorcha.UI.Core/Models/Explorer/` — create directories and add `_Imports.razor` updates for new component namespaces in `Sorcha.UI.Core/Components/_Imports.razor`

**Checkpoint**: Shared components ready. All subsequent phases can use TruncatedId, EmptyState, and ServiceUnavailable.

---

## Phase 2: Foundational — Navigation Restructure (US2)

**Purpose**: Flatten navigation and extract admin pages to individual routes. MUST complete before admin pages (US3-US5) can be built.

**Goal**: Replace the tabbed Administration page with individual admin pages accessible via direct navigation links.

**Independent Test**: Log in as admin, verify sidebar shows ADMIN group with direct links, each page loads correctly.

- [x] T005 [US2] Create `Sorcha.UI.Web.Client/Pages/Admin/SystemHealth.razor` — standalone page at `@page "/admin/health"` wrapping the existing `ServiceHealthDashboard` component from `Sorcha.UI.Core/Components/Admin/`. Include page title, breadcrumb, and `@attribute [Authorize(Roles = "Administrator")]`
- [x] T006 [P] [US2] Create `Sorcha.UI.Web.Client/Pages/Admin/PeerNetwork.razor` — standalone page at `@page "/admin/peers"` wrapping the existing `PeerServiceAdmin` component. Include page title, breadcrumb, and admin authorization
- [x] T007 [US2] Modify `Sorcha.UI.Web.Client/Components/Layout/MainLayout.razor` — restructure navigation: replace the single "Administration" link under MANAGEMENT with a collapsible ADMIN MudNavGroup containing: System Health (`/admin/health`), Peer Network (`/admin/peers`), Organizations (`/admin/organizations`), Validator (`/admin/validator`), Service Principals (`/admin/principals`). Keep MANAGEMENT section with just Wallets (group) and Registers
- [x] T008 [US2] Modify `Sorcha.UI.Web.Client/Pages/Administration.razor` — change `@page "/admin"` to redirect to `/admin/health` using `NavigationManager.NavigateTo("/admin/health", replace: true)` in `OnInitialized`. Keep the route for backwards compatibility but the page now just redirects
- [x] T009 [US2] Update `tests/Sorcha.UI.E2E.Tests/Infrastructure/TestConstants.cs` — add new admin route constants: `AdminHealth = "/admin/health"`, `AdminPeers = "/admin/peers"`, `AdminOrganizations = "/admin/organizations"`, `AdminValidator = "/admin/validator"`, `AdminPrincipals = "/admin/principals"`

**Checkpoint**: Navigation restructured. Admin group visible in sidebar with direct links. Old `/admin` redirects to `/admin/health`.

---

## Phase 3: Organization Management (US3)

**Goal**: Full CRUD for organizations through a dedicated admin page via Tenant Service API.

**Independent Test**: Navigate to `/admin/organizations`, create an org, edit it, deactivate it.

### Models & Services

- [x] T010 [P] [US3] Create `Sorcha.UI.Core/Models/Admin/OrganizationViewModel.cs` — view model with fields: Id, Name, Description, Subdomain, Status (active/suspended), MemberCount, ParticipantCount, CreatedAt, UpdatedAt. Also include `CreateOrganizationRequest` record with Name, Description, Subdomain, AdminEmail and `UpdateOrganizationRequest` record with Name, Description, Status
- [x] T011 [US3] Modify `Sorcha.UI.Core/Services/IOrganizationAdminService.cs` — add methods: `GetOrganizationAsync(string id)`, `UpdateOrganizationAsync(string id, UpdateOrganizationRequest request)`, `DeactivateOrganizationAsync(string id)`, `ValidateSubdomainAsync(string subdomain)`. Keep existing methods unchanged
- [x] T012 [US3] Modify `Sorcha.UI.Core/Services/OrganizationAdminService.cs` — implement the 4 new methods: GET `/api/organizations/{id}`, PUT `/api/organizations/{id}`, DELETE `/api/organizations/{id}`, GET `/api/organizations/validate-subdomain/{subdomain}`. Use existing HttpClient and error handling patterns

### Components & Pages

- [x] T013 [US3] Modify `Sorcha.UI.Core/Components/Admin/OrganizationList.razor` — enhance to support full CRUD: add Edit and Deactivate buttons per row, use `TruncatedId` for organization IDs, add "Create Organization" button in header, emit `OnEdit` and `OnDeactivate` EventCallbacks. Show member count and status chip per organization
- [x] T014 [US3] Modify `Sorcha.UI.Core/Components/Admin/OrganizationForm.razor` — enhance for edit mode: accept optional `OrganizationViewModel` for pre-fill, add Subdomain field with async validation (calls `ValidateSubdomainAsync`), add Status dropdown for edit mode (active/suspended), submit creates or updates based on mode
- [x] T015 [US3] Create `Sorcha.UI.Web.Client/Pages/Admin/Organizations.razor` — page at `@page "/admin/organizations"` with admin authorization. Composes `OrganizationList` and `OrganizationForm` (in MudDialog). Handles create/edit/deactivate flows with confirmation dialogs (using existing `ConfirmDialog`). Shows `ServiceUnavailable` when API unreachable, `EmptyState` when no organizations exist

### E2E Tests

- [x] T016 [P] [US3] Create `tests/Sorcha.UI.E2E.Tests/PageObjects/AdminPages/OrganizationsPage.cs` — page object with locators: CreateButton, OrgTable, EditButton(id), DeactivateButton(id), OrgFormDialog, NameInput, SubdomainInput, SaveButton. Methods: `ClickCreateAsync()`, `FillFormAsync(name, subdomain)`, `SubmitFormAsync()`, `GetOrgRowAsync(name)`
- [x] T017 [US3] Create `tests/Sorcha.UI.E2E.Tests/Docker/AdminOrganizationsTests.cs` — E2E tests extending `AuthenticatedDockerTestBase` with `[Category("Docker")]` `[Category("Admin")]`. Tests: page loads with org list, create new org, edit org, deactivate org with confirmation, org IDs are truncated, empty state shown when no orgs

**Checkpoint**: Organization CRUD fully functional. Admin can create, edit, deactivate orgs via `/admin/organizations`.

---

## Phase 4: Validator Admin Panel (US4)

**Goal**: Display validator mempool status and validation activity on a dedicated admin page.

**Independent Test**: Navigate to `/admin/validator`, verify mempool stats displayed.

- [x] T018 [P] [US4] Create `Sorcha.UI.Core/Models/Admin/ValidatorStatusViewModel.cs` — view model with fields: TotalPendingTransactions (int), RegisterMempoolStats (List<RegisterMempoolStat>), OldestPendingAge (TimeSpan?), LastUpdated (DateTimeOffset). Include `RegisterMempoolStat` record with RegisterId, RegisterName, PendingCount, OldestEntryAge
- [x] T019 [P] [US4] Create `Sorcha.UI.Core/Services/IValidatorAdminService.cs` — interface with `GetMempoolStatusAsync()` returning `ValidatorStatusViewModel` and `GetRegisterMempoolAsync(string registerId)` returning `RegisterMempoolStat`
- [x] T020 [US4] Create `Sorcha.UI.Core/Services/ValidatorAdminService.cs` — implementation calling GET `/api/admin/mempool` and GET `/api/v1/transactions/mempool/{registerId}`. Map responses to view models. Handle errors by returning empty status with IsLoaded=false pattern
- [x] T021 [US4] Create `Sorcha.UI.Core/Components/Admin/Validator/ValidatorPanel.razor` — component displaying: summary cards (total pending, oldest age), MudTable of per-register mempool stats (RegisterName, PendingCount, OldestEntryAge), auto-refresh timer (30s interval). Use `TruncatedId` for register IDs
- [x] T022 [US4] Create `Sorcha.UI.Web.Client/Pages/Admin/Validator.razor` — page at `@page "/admin/validator"` with admin authorization. Injects `IValidatorAdminService`, renders `ValidatorPanel`, handles loading/error states with `ServiceUnavailable`
- [x] T023 [US4] Register `IValidatorAdminService`/`ValidatorAdminService` in `Sorcha.UI.Core/Extensions/ServiceCollectionExtensions.cs` as scoped service with HttpClient

**Checkpoint**: Validator admin page shows live mempool data at `/admin/validator`.

---

## Phase 5: Service Principal Management (US5)

**Goal**: Display service-to-service credentials with status and expiration warnings.

**Independent Test**: Navigate to `/admin/principals`, verify credential list displayed.

- [x] T024 [P] [US5] Create `Sorcha.UI.Core/Models/Admin/ServicePrincipalViewModel.cs` — view model with Id, Name, Status (active/expired/revoked), LastUsedAt (DateTimeOffset?), ExpiresAt (DateTimeOffset?), Permissions (List<string>). Include computed `IsNearExpiration` property (true if ExpiresAt within 7 days)
- [x] T025 [US5] Create `Sorcha.UI.Core/Components/Admin/ServicePrincipals/ServicePrincipalList.razor` — MudTable displaying service principals with columns: Name, Status (MudChip with color based on status), Last Used (relative time), Expires (with warning icon if near expiration), Permissions count. Include detail expansion panel per row
- [x] T026 [US5] Create `Sorcha.UI.Web.Client/Pages/Admin/ServicePrincipals.razor` — page at `@page "/admin/principals"` with admin authorization. Note: Since no dedicated backend CRUD exists, display data from auth token introspection endpoints. Show `ServiceUnavailable` when API unreachable. Include informational banner noting full management will be available in a future update

**Checkpoint**: Service principal list displayed at `/admin/principals`.

---

## Phase 6: Dashboard Live Statistics (US6)

**Goal**: Wire dashboard stat cards to live gateway `/api/dashboard` endpoint.

**Independent Test**: Load dashboard, verify stat cards show live values, verify fallback when gateway down.

- [x] T027 [P] [US6] Create `Sorcha.UI.Core/Models/Dashboard/DashboardStatsViewModel.cs` — view model with ActiveBlueprints (int), TotalWallets (int), RecentTransactions (int), ConnectedPeers (int), ActiveRegisters (int), TotalOrganizations (int), IsLoaded (bool), LastUpdated (DateTimeOffset)
- [x] T028 [P] [US6] Create `Sorcha.UI.Core/Services/IDashboardService.cs` — interface with `GetDashboardStatsAsync()` returning `DashboardStatsViewModel`
- [x] T029 [US6] Create `Sorcha.UI.Core/Services/DashboardService.cs` — implementation calling GET `/api/dashboard` on API Gateway. Map response to `DashboardStatsViewModel`. On error, return model with `IsLoaded = false`
- [x] T030 [US6] Register `IDashboardService`/`DashboardService` in `Sorcha.UI.Core/Extensions/ServiceCollectionExtensions.cs` as scoped service with HttpClient
- [x] T031 [US6] Modify `Sorcha.UI.Web.Client/Pages/Home.razor` — replace hardcoded stat cards (currently showing 0) with live data from `IDashboardService`. Show MudSkeleton during loading. Show "Data unavailable" indicator when `IsLoaded == false`. Add stat cards for: Active Blueprints, Wallets, Recent Transactions, Connected Peers, Active Registers. Use MudCards with icons matching existing design

### E2E Tests

- [x] T032 [US6] Create `tests/Sorcha.UI.E2E.Tests/Docker/DashboardLiveTests.cs` — E2E tests extending `AuthenticatedDockerTestBase` with `[Category("Docker")]` `[Category("Dashboard")]`. Tests: stat cards render with values, stat cards show non-negative numbers, page handles API timeout gracefully

**Checkpoint**: Dashboard shows live statistics from backend.

---

## Phase 7: Workflow Instance Management (US7)

**Goal**: Replace My Workflows and My Actions placeholders with real Blueprint Service API integration.

**Independent Test**: Create workflow via API, verify it appears in My Workflows, pending actions appear in My Actions.

### Models & Services

- [x] T033 [P] [US7] Create `Sorcha.UI.Core/Models/Workflows/WorkflowInstanceViewModel.cs` — view model with InstanceId, BlueprintId, BlueprintName, Status (active/completed/failed), CurrentActionName, CurrentStepNumber, TotalSteps, ParticipantCount, CreatedAt, UpdatedAt
- [x] T034 [P] [US7] Create `Sorcha.UI.Core/Models/Workflows/PendingActionViewModel.cs` — view model with ActionId, InstanceId, BlueprintName, ActionName, Description, Priority (high/normal/low), AssignedAt, DueAt, DataSchema (JsonElement?)
- [x] T035 [P] [US7] Create `Sorcha.UI.Core/Models/Workflows/ActionSubmissionViewModel.cs` — view model with ActionId, InstanceId, Data (Dictionary<string, object>)
- [x] T036 [US7] Create `Sorcha.UI.Core/Services/IWorkflowService.cs` — interface with: `GetMyWorkflowsAsync(int page, int pageSize)`, `GetWorkflowAsync(string instanceId)`, `GetPendingActionsAsync()`, `SubmitActionAsync(ActionSubmissionViewModel submission)`, `RejectActionAsync(string instanceId, string actionId, string reason)`
- [x] T037 [US7] Create `Sorcha.UI.Core/Services/WorkflowService.cs` — implementation calling Blueprint Service orchestration API: GET `/api/instances/` (with user filter), GET `/api/instances/{id}`, GET `/api/instances/{id}/next-actions`, POST `/api/instances/{id}/actions/{aid}/execute` (with X-Delegation-Token header), POST `/api/instances/{id}/actions/{aid}/reject`

### Components

- [x] T038 [P] [US7] Create `Sorcha.UI.Core/Components/Workflows/WorkflowList.razor` — MudTable displaying workflow instances: BlueprintName, Status (MudChip), CurrentStep/TotalSteps progress, CreatedAt (relative time). Click row to navigate to detail. Use `TruncatedId` for InstanceId. Parameters: `Workflows` (list), `OnSelect` (EventCallback)
- [x] T039 [P] [US7] Create `Sorcha.UI.Core/Components/Workflows/ActionList.razor` — MudTable displaying pending actions: ActionName, BlueprintName, Priority (color-coded chip), AssignedAt (relative time), DueAt (with urgency indicator). Click row to open ActionForm. Use `TruncatedId` for IDs
- [x] T040 [US7] Create `Sorcha.UI.Core/Components/Workflows/WorkflowDetail.razor` — detail panel showing: workflow header (blueprint name, status, progress bar), participant list, action history timeline (MudTimeline), current pending action highlight, data payload viewer (using JsonTreeView). Parameters: `InstanceId` (string)
- [x] T041 [US7] Create `Sorcha.UI.Core/Components/Workflows/ActionForm.razor` — dynamic form generated from `PendingActionViewModel.DataSchema` (JSON Schema). For each schema property, generate appropriate MudBlazor input: string → MudTextField, number → MudNumericField, boolean → MudSwitch, enum → MudSelect. Submit and Reject buttons. Emit `OnSubmit` and `OnReject` EventCallbacks

### Pages

- [x] T042 [US7] Modify `Sorcha.UI.Web.Client/Pages/MyWorkflows.razor` — replace placeholder content with: inject `IWorkflowService`, call `GetMyWorkflowsAsync` on load, render `WorkflowList` component with pagination, show `EmptyState` when no workflows, show `ServiceUnavailable` on API error. Click workflow navigates to detail view (inline or separate route)
- [x] T043 [US7] Modify `Sorcha.UI.Web.Client/Pages/MyActions.razor` — enhance existing page: ensure it uses `IWorkflowService.GetPendingActionsAsync()` for action list, add `ActionForm` dialog for data submission, show success/error snackbar after submission. Keep existing SignalR real-time notification integration
- [x] T044 [US7] Register `IWorkflowService`/`WorkflowService` in `Sorcha.UI.Core/Extensions/ServiceCollectionExtensions.cs`

### E2E Tests

- [x] T045 [P] [US7] Create `tests/Sorcha.UI.E2E.Tests/PageObjects/WorkflowPages/MyWorkflowsPage.cs` — page object with locators: WorkflowTable, WorkflowRow(id), StatusChip, DetailPanel
- [x] T046 [US7] Create `tests/Sorcha.UI.E2E.Tests/Docker/WorkflowTests.cs` — E2E tests with `[Category("Docker")]` `[Category("Workflows")]`. Tests: My Workflows page loads, empty state shown when no workflows, My Actions page loads, action list renders, workflow IDs use truncation

**Checkpoint**: My Workflows shows real data, My Actions integrates with workflow API.

---

## Phase 8: Blueprint Cloud Persistence & Publishing (US8)

**Goal**: Migrate blueprint storage from LocalStorage to Blueprint Service API with publishing flow.

**Independent Test**: Create blueprint in Designer, clear LocalStorage, refresh — blueprint still available from API.

### Models & Services

- [x] T047 [P] [US8] Create `Sorcha.UI.Core/Models/Blueprints/BlueprintListItemViewModel.cs` — view model with Id, Title, Description, Version, Status (draft/published), ActionCount, ParticipantCount, CreatedAt, UpdatedAt, PublishedAt
- [x] T048 [P] [US8] Create `Sorcha.UI.Core/Models/Blueprints/BlueprintVersionViewModel.cs` — view model with Version, PublishedAt, ActionCount, ChangeDescription
- [x] T049 [P] [US8] Create `Sorcha.UI.Core/Models/Blueprints/PublishReviewViewModel.cs` — view model with BlueprintId, Title, ValidationResults (List<ValidationIssue>), IsValid, Warnings (List<string>). Include `ValidationIssue` record with Severity, Message, Location
- [x] T050 [US8] Create `Sorcha.UI.Core/Services/IBlueprintApiService.cs` — interface with: `GetBlueprintsAsync(int page, int pageSize, string? search, string? status)`, `GetBlueprintAsync(string id)`, `SaveBlueprintAsync(object blueprint)`, `DeleteBlueprintAsync(string id)`, `PublishBlueprintAsync(string id)`, `GetVersionsAsync(string id)`, `GetVersionAsync(string id, string version)`
- [x] T051 [US8] Create `Sorcha.UI.Core/Services/BlueprintApiService.cs` — implementation calling Blueprint Service: GET/POST/PUT/DELETE `/api/blueprints/`, POST `/api/blueprints/{id}/publish`, GET `/api/blueprints/{id}/versions`. SaveBlueprintAsync uses POST for new (no Id) and PUT for existing

### Components & Pages

- [x] T052 [P] [US8] Create `Sorcha.UI.Core/Components/Blueprints/PublishReview.razor` — dialog showing validation results before publishing: list of ValidationIssues with severity icons (error=red, warning=yellow), IsValid indicator, Publish/Cancel buttons. Publish button disabled when IsValid=false
- [x] T053 [P] [US8] Create `Sorcha.UI.Core/Components/Blueprints/VersionHistory.razor` — MudTimeline showing version history: version number, publish date, action count, change description. Click version to view that snapshot
- [x] T054 [US8] Modify `Sorcha.UI.Web.Client/Pages/Blueprints.razor` — replace LocalStorage-based loading with `IBlueprintApiService.GetBlueprintsAsync()`. Add search bar, status filter (draft/published/all), pagination. Show Publish button for draft blueprints. Show version history for published blueprints. Use `TruncatedId` for blueprint IDs
- [x] T055 [US8] Modify `Sorcha.UI.Core/Components/Designer/LoadBlueprintDialog.razor` — change from loading LocalStorage keys to calling `IBlueprintApiService.GetBlueprintsAsync()`. Show blueprint list with name, status, last modified. Select blueprint loads from API
- [x] T056 [US8] Modify `Sorcha.UI.Core/Extensions/ServiceCollectionExtensions.cs` — register `IBlueprintApiService`/`BlueprintApiService`. Update Designer save flow: change DI registration so `IBlueprintStorageService` resolves to a new `ApiBlueprintStorageService` wrapper that delegates to `IBlueprintApiService` (preserving the existing save interface the Designer uses)

### E2E Tests

- [x] T057 [US8] Create `tests/Sorcha.UI.E2E.Tests/Docker/BlueprintCloudTests.cs` — E2E tests with `[Category("Docker")]` `[Category("Blueprints")]`. Tests: Blueprints page loads from API, blueprint list shows name/status/version, search filters blueprints, publish flow shows validation review

**Checkpoint**: Blueprints persist to API. Designer saves to cloud. Publishing flow works.

---

## Phase 9: Wallet Management (US9)

**Goal**: Replace My Wallet placeholder with real Wallet Service integration.

**Independent Test**: Create wallet, verify it appears in list, view addresses, perform signing.

- [x] T058 [US9] Modify `Sorcha.UI.Web.Client/Pages/MyWallet.razor` — replace placeholder content with real wallet management: inject existing `IWalletApiService`, call `GetWalletsAsync()` on load, display wallet list using existing `WalletDto` model. Show Create Wallet button → navigate to `/wallets/create`. Show wallet cards with: name, primary address (using `TruncatedId`), algorithm chip, creation date. Click wallet → navigate to `/wallets/{address}`. Show `EmptyState` when no wallets, `ServiceUnavailable` on API error. Add "Manage All Wallets" link to `/wallets` for full management

### E2E Tests

- [x] T059 [US9] Create `tests/Sorcha.UI.E2E.Tests/Docker/WalletManagementTests.cs` — E2E tests with `[Category("Docker")]` `[Category("Wallets")]`. Tests: My Wallet page loads, wallet list renders (if wallets exist), create wallet button navigates to create page, wallet addresses use truncation, empty state shown when no wallets

**Checkpoint**: My Wallet page shows real wallet data from Wallet Service.

---

## Phase 10: Transaction History (US10)

**Goal**: Replace My Transactions placeholder with real Register Service queries.

**Independent Test**: View My Transactions, verify paginated list with filtering.

- [x] T060 [US10] Modify `Sorcha.UI.Core/Services/ITransactionService.cs` — add method `GetMyTransactionsAsync(string walletAddress, int page, int pageSize)` returning `PaginatedList<TransactionViewModel>`
- [x] T061 [US10] Modify `Sorcha.UI.Core/Services/TransactionService.cs` — implement `GetMyTransactionsAsync` calling GET `/api/query/wallets/{address}/transactions?page={page}&pageSize={pageSize}`. Map response to existing `TransactionViewModel` list
- [x] T062 [US10] Modify `Sorcha.UI.Web.Client/Pages/MyTransactions.razor` — replace placeholder/sample data with real API calls: inject `ITransactionService` and `IWalletApiService`, get user's wallet address, call `GetMyTransactionsAsync()`. Render existing `TransactionList` component (reuse from Registers). Add filter controls: register dropdown, date range picker, type selector, status filter. Use `TruncatedId` for all IDs. Show `EmptyState` when no transactions, `ServiceUnavailable` on error

### E2E Tests

- [x] T063 [US10] Create `tests/Sorcha.UI.E2E.Tests/Docker/TransactionHistoryTests.cs` — E2E tests with `[Category("Docker")]` `[Category("Transactions")]`. Tests: My Transactions page loads, transaction list renders (if data exists), filter controls visible, transaction IDs use truncation, empty state shown when no transactions

**Checkpoint**: My Transactions shows real transaction data with filtering.

---

## Phase 11: Template Library (US11)

**Goal**: Connect Templates page to backend template API.

**Independent Test**: Load Templates page, verify templates fetched from API, use template to create blueprint.

- [x] T064 [P] [US11] Create `Sorcha.UI.Core/Models/Templates/TemplateListItemViewModel.cs` — view model with Id, Name, Description, Category, UsageCount, Parameters (List<TemplateParameter>). Include `TemplateParameter` record with Name, Description, Type (string/number/boolean), DefaultValue, Required
- [x] T065 [US11] Create `Sorcha.UI.Core/Services/ITemplateApiService.cs` — interface with: `GetTemplatesAsync(string? category)`, `GetTemplateAsync(string id)`, `EvaluateTemplateAsync(string id, Dictionary<string, object> parameters)`, `ValidateParametersAsync(string id, Dictionary<string, object> parameters)`
- [x] T066 [US11] Create `Sorcha.UI.Core/Services/TemplateApiService.cs` — implementation calling Blueprint Service: GET `/api/templates/`, GET `/api/templates/{id}`, POST `/api/templates/evaluate`, POST `/api/templates/{id}/validate`. Register in ServiceCollectionExtensions.cs
- [x] T067 [P] [US11] Create `Sorcha.UI.Core/Components/Templates/TemplateList.razor` — MudGrid of template cards: Name, Description, Category chip, Usage count badge. Category filter dropdown at top. Click card to select template. Parameters: `Templates` (list), `OnSelect` (EventCallback)
- [x] T068 [US11] Create `Sorcha.UI.Core/Components/Templates/TemplateEvaluator.razor` — dialog for template parameter input: for each `TemplateParameter`, generate input field based on Type. Validate required params. Preview button calls `ValidateParametersAsync`. Use button calls `EvaluateTemplateAsync` and navigates to Designer with result
- [x] T069 [US11] Modify `Sorcha.UI.Web.Client/Pages/Templates.razor` — replace hardcoded template data with `ITemplateApiService.GetTemplatesAsync()`. Render `TemplateList` component. Click "Use Template" opens `TemplateEvaluator` dialog. Show `EmptyState` when no templates, `ServiceUnavailable` on error

### E2E Tests

- [x] T070 [US11] Create `tests/Sorcha.UI.E2E.Tests/Docker/TemplateLibraryTests.cs` — E2E tests with `[Category("Docker")]` `[Category("Templates")]`. Tests: Templates page loads, template cards render, category filter works, Use Template button visible

**Checkpoint**: Templates loaded from backend API. Users can evaluate and use templates.

---

## Phase 12: Explorer Enhancements (US12)

**Goal**: Add docket chain inspection and OData query builder.

**Independent Test**: View register docket chain, build and execute OData query.

### Models & Services

- [x] T071 [P] [US12] Create `Sorcha.UI.Core/Models/Explorer/DocketViewModel.cs` — view model with DocketId, RegisterId, Version (int), Hash, PreviousHash, TransactionCount, TransactionIds (List<string>), CreatedAt, IsIntegrityValid
- [x] T072 [P] [US12] Create `Sorcha.UI.Core/Models/Explorer/ODataQueryModel.cs` — model with Filters (List<ODataFilterRow>), OrderBy, OrderDirection (asc/desc), Top (default 20), Skip. Include `ODataFilterRow` record with Field, Operator (eq/ne/gt/lt/ge/le/contains/startswith), Value, LogicalOperator (and/or)
- [x] T073 [US12] Create `Sorcha.UI.Core/Services/IDocketService.cs` — interface with: `GetDocketsAsync(string registerId)`, `GetDocketAsync(string registerId, string docketId)`, `GetDocketTransactionsAsync(string registerId, string docketId)`, `GetLatestDocketAsync(string registerId)`
- [x] T074 [US12] Create `Sorcha.UI.Core/Services/DocketService.cs` — implementation calling Register Service: GET `/api/registers/{id}/dockets/`, GET `/api/registers/{id}/dockets/{docketId}`, GET `/api/registers/{id}/dockets/{docketId}/transactions`, GET `/api/registers/{id}/dockets/latest`. Register in ServiceCollectionExtensions.cs
- [x] T075 [US12] Create `Sorcha.UI.Core/Services/IODataQueryService.cs` — interface with: `ExecuteTransactionQueryAsync(ODataQueryModel query)`, `ExecuteRegisterQueryAsync(ODataQueryModel query)`, `BuildFilterString(ODataQueryModel model)` (pure function)
- [x] T076 [US12] Create `Sorcha.UI.Core/Services/ODataQueryService.cs` — implementation: `BuildFilterString` converts ODataFilterRow list to OData $filter string (e.g., "Type eq 'Transfer' and Status eq 'Confirmed'"). `ExecuteTransactionQueryAsync` calls GET `/odata/Transactions?$filter={filter}&$orderby={orderby}&$top={top}&$skip={skip}&$count=true`. Register in ServiceCollectionExtensions.cs

### Components

- [x] T077 [P] [US12] Create `Sorcha.UI.Core/Components/Explorer/DocketChain.razor` — MudTimeline showing docket chain: each node shows Version, Hash (using `TruncatedId`), TransactionCount, CreatedAt. Chain links visualized with connecting lines. Click node expands `DocketDetail`. Parameters: `RegisterId` (string)
- [x] T078 [P] [US12] Create `Sorcha.UI.Core/Components/Explorer/DocketDetail.razor` — detail panel for single docket: Hash (full, copyable), PreviousHash (`TruncatedId`), TransactionIds list (each using `TruncatedId`), integrity status badge (valid/invalid). Parameters: `Docket` (DocketViewModel)
- [x] T079 [US12] Create `Sorcha.UI.Core/Components/Explorer/ODataQueryBuilder.razor` — visual query builder: row-based filter conditions (add/remove rows), each row has: Field dropdown (populated from schema), Operator dropdown (eq/ne/gt/lt/contains/startswith), Value text input, logical combinator (and/or). OrderBy dropdown + direction toggle. Preview panel showing raw OData query string. Execute button runs query. Results displayed in MudTable with pagination. Parameters: `OnExecute` (EventCallback<ODataQueryModel>)

### Pages

- [x] T080 [US12] Modify `Sorcha.UI.Web.Client/Pages/Registers/Detail.razor` — add new "Docket Chain" MudTab to the register detail page. Tab content renders `DocketChain` component with the current register ID. Only show tab when register has dockets
- [x] T081 [US12] Modify `Sorcha.UI.Web.Client/Pages/Registers/Query.razor` — integrate `ODataQueryBuilder` component. Replace or enhance existing query form with the visual builder. Show results in existing transaction table format. Display raw OData query for reference

### E2E Tests

- [x] T082 [US12] Create `tests/Sorcha.UI.E2E.Tests/Docker/ExplorerEnhancementTests.cs` — E2E tests with `[Category("Docker")]` `[Category("Explorer")]`. Tests: register detail page has Docket Chain tab, docket chain renders (if dockets exist), docket hashes use truncation, query builder renders with filter rows, add/remove filter row works, execute query returns results

**Checkpoint**: Docket chain visible in register details. OData query builder functional.

---

## Phase 13: Polish & Cross-Cutting Concerns

**Purpose**: Apply TruncatedId to existing pages, ensure consistency, final cleanup

### Retroactive Truncation

- [x] T083 [P] Modify `Sorcha.UI.Core/Components/Registers/RegisterCard.razor` — replace `Register.Id[..8]...` with `<TruncatedId Value="@Register.Id" />` component
- [x] T084 [P] Modify `Sorcha.UI.Web.Client/Pages/Wallets/WalletList.razor` — replace `TruncateAddress()` private method with `<TruncatedId Value="@wallet.Address" />` component. Remove the `TruncateAddress` static method
- [x] T085 [P] Modify `Sorcha.UI.Web.Client/Pages/MyActions.razor` — replace `action.InstanceId[..Math.Min(8, action.InstanceId.Length)]...` with `<TruncatedId Value="@action.InstanceId" />`
- [x] T086 [P] Modify `Sorcha.UI.Core/Components/Registers/TransactionRow.razor` — replace any inline truncation with `<TruncatedId>` for TxId, SenderWallet, and other identifiers displayed in the row
- [x] T087 Modify `Sorcha.UI.Core/Models/Registers/TransactionViewModel.cs` — remove `TxIdTruncated` and `SenderTruncated` computed properties (no longer needed, truncation handled by component). Update any references in views to use `<TruncatedId>` instead

### Admin Navigation E2E Tests

- [x] T088 [P] Create `tests/Sorcha.UI.E2E.Tests/PageObjects/AdminPages/SystemHealthPage.cs` — page object for system health page with locators for KPI cards, service health cards, refresh button
- [x] T089 [P] Create `tests/Sorcha.UI.E2E.Tests/PageObjects/AdminPages/ValidatorPage.cs` — page object for validator page with locators for mempool stats, register table
- [x] T090 [P] Create `tests/Sorcha.UI.E2E.Tests/PageObjects/AdminPages/ServicePrincipalsPage.cs` — page object for service principals page with locators for credential table
- [x] T091 Create `tests/Sorcha.UI.E2E.Tests/Docker/AdminHealthTests.cs` — E2E tests extending `AuthenticatedDockerTestBase` with `[Category("Docker")]` `[Category("Admin")]`. Tests: System Health page loads at `/admin/health`, health cards render, Peer Network page loads at `/admin/peers`, all admin nav links work, old `/admin` redirects to `/admin/health`
- [x] T092 Run `dotnet build` on the full solution to verify no compilation errors across all modified files
- [ ] T093 Run `dotnet test tests/Sorcha.UI.E2E.Tests/ --filter "Category=Docker"` to verify all E2E tests pass against Docker deployment
- [ ] T094 Run quickstart.md manual verification for all 12 user stories against Docker deployment

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Nav Restructure — US2)**: Depends on Phase 1 (TruncatedId needed for admin pages)
- **Phase 3 (Org Mgmt — US3)**: Depends on Phase 2 (Organizations page needs nav link)
- **Phase 4 (Validator — US4)**: Depends on Phase 2 (Validator page needs nav link)
- **Phase 5 (Svc Principals — US5)**: Depends on Phase 2 (Principals page needs nav link)
- **Phase 6 (Dashboard — US6)**: Depends on Phase 1 only (no admin nav dependency)
- **Phase 7 (Workflows — US7)**: Depends on Phase 1 only
- **Phase 8 (Blueprints — US8)**: Depends on Phase 1 only
- **Phase 9 (Wallet — US9)**: Depends on Phase 1 only
- **Phase 10 (Transactions — US10)**: Depends on Phase 1 only
- **Phase 11 (Templates — US11)**: Depends on Phase 1 only
- **Phase 12 (Explorer — US12)**: Depends on Phase 1 only
- **Phase 13 (Polish)**: Depends on all prior phases

### User Story Dependencies

- **US1 (Truncation)**: Phase 1 — no dependencies, enables all others
- **US2 (Nav Restructure)**: Phase 2 — depends on US1, blocks US3/US4/US5
- **US3 (Org Mgmt)**: Phase 3 — depends on US2
- **US4 (Validator)**: Phase 4 — depends on US2, can run parallel with US3
- **US5 (Svc Principals)**: Phase 5 — depends on US2, can run parallel with US3/US4
- **US6-US12**: Depend only on US1 (Phase 1) — can run in parallel with each other

### Within Each User Story

- Models before services (no UI without data shapes)
- Services before components (no components without data)
- Components before pages (no pages without components)
- Pages before E2E tests (no tests without pages)

### Parallel Opportunities

After Phase 1 + Phase 2 complete:
- US3, US4, US5 can run in parallel (different admin pages)
- US6, US7, US8, US9, US10, US11, US12 can all run in parallel (independent pages/services)

---

## Parallel Example: Phase 1

```
Launch in parallel (different files, no dependencies):
  T001: TruncatedId.razor
  T002: EmptyState.razor
  T003: ServiceUnavailable.razor

Then: T004 (directory structure, depends on nothing but runs after for clean ordering)
```

## Parallel Example: After Phase 2

```
Launch admin pages in parallel:
  Phase 3 (T010-T017): Organizations
  Phase 4 (T018-T023): Validator
  Phase 5 (T024-T026): Service Principals

Launch user pages in parallel:
  Phase 6 (T027-T032): Dashboard
  Phase 7 (T033-T046): Workflows
  Phase 8 (T047-T057): Blueprints
  Phase 9 (T058-T059): Wallet
  Phase 10 (T060-T063): Transactions
  Phase 11 (T064-T070): Templates
  Phase 12 (T071-T082): Explorer
```

---

## Implementation Strategy

### MVP First (Phases 1-3)

1. Complete Phase 1: Shared components (TruncatedId, EmptyState, ServiceUnavailable)
2. Complete Phase 2: Navigation restructure + admin page extraction
3. Complete Phase 3: Organization Management
4. **STOP and VALIDATE**: Admin can create/manage orgs, nav is flattened, truncation works
5. Deploy/demo if ready

### Incremental Delivery

1. Phases 1-2 → Foundation ready (nav + shared components)
2. Phase 3 → Org Management MVP
3. Phases 4-5 → Full admin suite
4. Phase 6 → Live dashboard
5. Phases 7-8 → Core workflows + blueprints
6. Phases 9-10 → Wallet + transactions (replace placeholders)
7. Phases 11-12 → Templates + explorer (polish)
8. Phase 13 → Final polish + retroactive truncation

### Parallel Team Strategy

With multiple developers after Phase 2:
- Developer A: Admin pages (Phases 3, 4, 5)
- Developer B: Core user pages (Phases 6, 7)
- Developer C: Persistence pages (Phases 8, 9, 10)
- Developer D: Library + Explorer (Phases 11, 12)

---

## Notes

- All new services must be registered in `Sorcha.UI.Core/Extensions/ServiceCollectionExtensions.cs`
- All new pages need `// SPDX-License-Identifier: MIT` and `// Copyright (c) 2026 Sorcha Contributors` header
- Use existing `AuthenticatedHttpMessageHandler` for JWT tokens — no manual token handling
- MudBlazor components throughout: MudTable, MudCard, MudDialog, MudChip, MudTooltip, MudSnackbar, MudTimeline, MudTextField, MudSelect, MudNumericField, MudSwitch
- Existing components to reuse: `ConfirmDialog`, `JsonTreeView`, `TransactionList`, `TransactionDetail`, `RegisterCard`
- `TruncatedId` component is the single source of truth for identifier display — all ad-hoc truncation must be replaced
