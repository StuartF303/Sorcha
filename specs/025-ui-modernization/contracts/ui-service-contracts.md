# UI Service Contracts: Sorcha UI Modernization

**Branch**: `025-ui-modernization` | **Date**: 2026-02-07

## Overview

These contracts define the UI service layer — TypeScript-style interface descriptions for `Sorcha.UI.Core/Services/` that consume backend APIs. Each service maps UI view models to backend API calls.

---

## 1. IDashboardService (NEW)

Fetches aggregated dashboard statistics from the API Gateway.

```
Interface: IDashboardService
Location: Sorcha.UI.Core/Services/IDashboardService.cs

GetDashboardStatsAsync() → DashboardStatsViewModel
  HTTP: GET /api/dashboard
  Maps: Gateway aggregated response → DashboardStatsViewModel
  Error: Returns DashboardStatsViewModel with IsLoaded = false
```

---

## 2. IOrganizationAdminService (MODIFY — already exists)

Extend existing service with full CRUD operations.

```
Interface: IOrganizationAdminService
Location: Sorcha.UI.Core/Services/IOrganizationAdminService.cs

EXISTING:
  GetOrganizationsAsync() → List<OrganizationViewModel>
  CreateOrganizationAsync(request) → OrganizationViewModel
  GetPlatformStatsAsync() → PlatformKpis

ADD:
  GetOrganizationAsync(id) → OrganizationViewModel
    HTTP: GET /api/organizations/{id}

  UpdateOrganizationAsync(id, request) → OrganizationViewModel
    HTTP: PUT /api/organizations/{id}

  DeactivateOrganizationAsync(id) → bool
    HTTP: DELETE /api/organizations/{id}

  ValidateSubdomainAsync(subdomain) → bool
    HTTP: GET /api/organizations/validate-subdomain/{subdomain}
```

---

## 3. IValidatorAdminService (NEW)

Fetches validator/mempool status for the admin panel.

```
Interface: IValidatorAdminService
Location: Sorcha.UI.Core/Services/IValidatorAdminService.cs

GetMempoolStatusAsync() → ValidatorStatusViewModel
  HTTP: GET /api/admin/mempool
  Maps: Admin mempool response → ValidatorStatusViewModel

GetRegisterMempoolAsync(registerId) → RegisterMempoolStat
  HTTP: GET /api/v1/transactions/mempool/{registerId}
  Maps: Mempool stats response → RegisterMempoolStat
```

---

## 4. IWorkflowService (NEW)

Manages workflow instances and actions for the current user.

```
Interface: IWorkflowService
Location: Sorcha.UI.Core/Services/IWorkflowService.cs

GetMyWorkflowsAsync(page, pageSize) → PaginatedList<WorkflowInstanceViewModel>
  HTTP: GET /api/instances/ (filtered by current user)
  Maps: Instance list response → WorkflowInstanceViewModel list

GetWorkflowAsync(instanceId) → WorkflowInstanceViewModel
  HTTP: GET /api/instances/{instanceId}
  Maps: Instance detail → WorkflowInstanceViewModel

GetPendingActionsAsync() → List<PendingActionViewModel>
  HTTP: GET /api/instances/ (filtered to active) → for each, GET next-actions
  Maps: Next actions aggregated → PendingActionViewModel list

SubmitActionAsync(submission) → bool
  HTTP: POST /api/instances/{instanceId}/actions/{actionId}/execute
  Body: ActionSubmissionViewModel data
  Headers: X-Delegation-Token (from wallet service delegation)

RejectActionAsync(instanceId, actionId, reason) → bool
  HTTP: POST /api/instances/{instanceId}/actions/{actionId}/reject
  Body: { reason: string }
```

---

## 5. IBlueprintApiService (NEW — replaces IBlueprintStorageService for cloud persistence)

Cloud-backed blueprint storage replacing LocalStorage.

```
Interface: IBlueprintApiService
Location: Sorcha.UI.Core/Services/IBlueprintApiService.cs

GetBlueprintsAsync(page, pageSize, search?, status?) → PaginatedList<BlueprintListItemViewModel>
  HTTP: GET /api/blueprints/?page={page}&pageSize={pageSize}&search={search}&status={status}
  Maps: Blueprint list response → BlueprintListItemViewModel list

GetBlueprintAsync(id) → BlueprintDetailViewModel
  HTTP: GET /api/blueprints/{id}
  Maps: Blueprint detail → full blueprint model for Designer

SaveBlueprintAsync(blueprint) → BlueprintListItemViewModel
  HTTP: POST /api/blueprints/ (create) or PUT /api/blueprints/{id} (update)
  Body: Blueprint JSON

DeleteBlueprintAsync(id) → bool
  HTTP: DELETE /api/blueprints/{id}

PublishBlueprintAsync(id) → PublishReviewViewModel
  HTTP: POST /api/blueprints/{id}/publish
  Maps: Publish response with validation results

GetVersionsAsync(id) → List<BlueprintVersionViewModel>
  HTTP: GET /api/blueprints/{id}/versions
  Maps: Version list

GetVersionAsync(id, version) → BlueprintDetailViewModel
  HTTP: GET /api/blueprints/{id}/versions/{version}
```

---

## 6. ITemplateApiService (NEW)

Backend-driven template library.

```
Interface: ITemplateApiService
Location: Sorcha.UI.Core/Services/ITemplateApiService.cs

GetTemplatesAsync(category?) → List<TemplateListItemViewModel>
  HTTP: GET /api/templates/?category={category}
  Maps: Template list response → TemplateListItemViewModel list

GetTemplateAsync(id) → TemplateListItemViewModel
  HTTP: GET /api/templates/{id}

EvaluateTemplateAsync(id, parameters) → BlueprintDetailViewModel
  HTTP: POST /api/templates/evaluate
  Body: { templateId: id, parameters: {} }
  Maps: Evaluated blueprint result

ValidateParametersAsync(id, parameters) → ValidationResult
  HTTP: POST /api/templates/{id}/validate
  Body: { parameters: {} }
```

---

## 7. IDocketService (NEW)

Docket chain inspection for register detail pages.

```
Interface: IDocketService
Location: Sorcha.UI.Core/Services/IDocketService.cs

GetDocketsAsync(registerId) → List<DocketViewModel>
  HTTP: GET /api/registers/{registerId}/dockets/
  Maps: Docket list → DocketViewModel list

GetDocketAsync(registerId, docketId) → DocketViewModel
  HTTP: GET /api/registers/{registerId}/dockets/{docketId}
  Maps: Docket detail with transaction IDs

GetDocketTransactionsAsync(registerId, docketId) → List<TransactionViewModel>
  HTTP: GET /api/registers/{registerId}/dockets/{docketId}/transactions
  Maps: Transaction list within docket

GetLatestDocketAsync(registerId) → DocketViewModel
  HTTP: GET /api/registers/{registerId}/dockets/latest
```

---

## 8. IODataQueryService (NEW)

Executes OData queries against the Register Service.

```
Interface: IODataQueryService
Location: Sorcha.UI.Core/Services/IODataQueryService.cs

ExecuteTransactionQueryAsync(query) → PaginatedList<TransactionViewModel>
  HTTP: GET /odata/Transactions?$filter={filter}&$orderby={orderby}&$top={top}&$skip={skip}&$count=true
  Maps: OData response → TransactionViewModel list with total count

ExecuteRegisterQueryAsync(query) → PaginatedList<RegisterViewModel>
  HTTP: GET /odata/Registers?$filter={filter}&$orderby={orderby}&$top={top}&$skip={skip}&$count=true

BuildFilterString(model) → string
  Pure function: ODataQueryModel → OData $filter string
  Example: "Type eq 'Transfer' and Status eq 'Confirmed'" from filter rows
```

---

## 9. ITransactionService (MODIFY — already exists)

Extend existing service with user-scoped queries.

```
Interface: ITransactionService
Location: Sorcha.UI.Core/Services/ITransactionService.cs

EXISTING:
  GetTransactionsByRegisterAsync(registerId, page, pageSize) → TransactionListResponse

ADD:
  GetMyTransactionsAsync(walletAddress, page, pageSize) → PaginatedList<TransactionViewModel>
    HTTP: GET /api/query/wallets/{address}/transactions?page={page}&pageSize={pageSize}
    Maps: Wallet transaction response → TransactionViewModel list
```

---

## Backend API Route Mapping (via API Gateway YARP)

All UI HTTP calls go through the API Gateway at `/api/*`:

| UI Service | Gateway Route | Backend Service |
|-----------|--------------|----------------|
| IDashboardService | `/api/dashboard` | Gateway (local) |
| IOrganizationAdminService | `/api/organizations/*` | Tenant Service (5110) |
| IValidatorAdminService | `/api/admin/mempool`, `/api/v1/transactions/mempool/*` | Validator Service (5004) |
| IWorkflowService | `/api/instances/*` | Blueprint Service (5000) |
| IBlueprintApiService | `/api/blueprints/*` | Blueprint Service (5000) |
| ITemplateApiService | `/api/templates/*` | Blueprint Service (5000) |
| IDocketService | `/api/registers/{id}/dockets/*` | Register Service (5290) |
| IODataQueryService | `/odata/*` | Register Service (5290) |
| ITransactionService | `/api/query/*` | Register Service (5290) |
| IWalletApiService | `/api/v1/wallets/*` | Wallet Service (5001) |
