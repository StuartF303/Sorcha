# Data Model: Sorcha UI Modernization

**Branch**: `025-ui-modernization` | **Date**: 2026-02-07

## Overview

This document defines the UI-side view models and DTOs needed to support the modernization features. These are client-side models in `Sorcha.UI.Core/Models/` — they map to backend API responses but are tailored for UI display concerns.

## New Models

### Admin Models (`Models/Admin/`)

#### OrganizationViewModel

Extends existing `OrganizationList` component data model with richer display properties.

| Field | Type | Purpose |
|-------|------|---------|
| Id | string | Organization identifier |
| Name | string | Display name |
| Description | string | Organization description |
| Subdomain | string | Unique subdomain |
| Status | string | active / suspended |
| MemberCount | int | Number of users in organization |
| ParticipantCount | int | Number of participants registered |
| CreatedAt | DateTimeOffset | Creation timestamp |
| UpdatedAt | DateTimeOffset | Last update timestamp |

#### CreateOrganizationRequest

| Field | Type | Purpose |
|-------|------|---------|
| Name | string | Organization name (required) |
| Description | string | Organization description |
| Subdomain | string | Unique subdomain (required) |
| AdminEmail | string | Initial admin email |

#### ValidatorStatusViewModel

| Field | Type | Purpose |
|-------|------|---------|
| TotalPendingTransactions | int | Across all registers |
| RegisterMempoolStats | List<RegisterMempoolStat> | Per-register breakdown |
| OldestPendingAge | TimeSpan? | Age of oldest pending tx |
| LastUpdated | DateTimeOffset | When status was fetched |

#### RegisterMempoolStat

| Field | Type | Purpose |
|-------|------|---------|
| RegisterId | string | Register identifier |
| RegisterName | string | Register display name |
| PendingCount | int | Pending transactions |
| OldestEntryAge | TimeSpan? | Age of oldest entry |

#### ServicePrincipalViewModel

| Field | Type | Purpose |
|-------|------|---------|
| Id | string | Service principal identifier |
| Name | string | Service name |
| Status | string | active / expired / revoked |
| LastUsedAt | DateTimeOffset? | Last authentication time |
| ExpiresAt | DateTimeOffset? | Token expiration |
| Permissions | List<string> | Assigned permissions |

---

### Dashboard Models (`Models/Dashboard/`)

#### DashboardStatsViewModel

| Field | Type | Purpose |
|-------|------|---------|
| ActiveBlueprints | int | Count of active blueprints |
| TotalWallets | int | Count of wallets |
| RecentTransactions | int | Transaction count (last 24h) |
| ConnectedPeers | int | Active peer connections |
| ActiveRegisters | int | Online registers |
| TotalOrganizations | int | Organization count |
| IsLoaded | bool | Whether data fetched successfully |
| LastUpdated | DateTimeOffset | Fetch timestamp |

---

### Workflow Models (`Models/Workflows/`)

#### WorkflowInstanceViewModel

| Field | Type | Purpose |
|-------|------|---------|
| InstanceId | string | Unique instance identifier |
| BlueprintId | string | Source blueprint identifier |
| BlueprintName | string | Blueprint display name |
| Status | string | active / completed / failed |
| CurrentActionName | string? | Name of current pending action |
| CurrentStepNumber | int | Position in workflow |
| TotalSteps | int | Total actions in blueprint |
| ParticipantCount | int | Number of participants |
| CreatedAt | DateTimeOffset | Instance creation time |
| UpdatedAt | DateTimeOffset | Last activity time |

#### PendingActionViewModel

| Field | Type | Purpose |
|-------|------|---------|
| ActionId | string | Action identifier |
| InstanceId | string | Parent workflow instance |
| BlueprintName | string | Blueprint display name |
| ActionName | string | Action display name |
| Description | string | What the user needs to do |
| Priority | string | high / normal / low |
| AssignedAt | DateTimeOffset | When action was assigned |
| DueAt | DateTimeOffset? | Optional deadline |
| DataSchema | object? | JSON Schema for input form |

#### ActionSubmissionViewModel

| Field | Type | Purpose |
|-------|------|---------|
| ActionId | string | Action being submitted |
| InstanceId | string | Workflow instance |
| Data | Dictionary<string, object> | Form data from user |

---

### Blueprint Models (`Models/Blueprints/`)

#### BlueprintListItemViewModel

Replaces LocalStorage-based blueprint listing with API-sourced data.

| Field | Type | Purpose |
|-------|------|---------|
| Id | string | Blueprint identifier |
| Title | string | Display name |
| Description | string | Blueprint description |
| Version | string | Current version |
| Status | string | draft / published |
| ActionCount | int | Number of actions |
| ParticipantCount | int | Number of participants |
| CreatedAt | DateTimeOffset | Creation time |
| UpdatedAt | DateTimeOffset | Last modified time |
| PublishedAt | DateTimeOffset? | When published (if published) |

#### BlueprintVersionViewModel

| Field | Type | Purpose |
|-------|------|---------|
| Version | string | Version number |
| PublishedAt | DateTimeOffset | Publication time |
| ActionCount | int | Actions in this version |
| ChangeDescription | string? | What changed |

#### PublishReviewViewModel

| Field | Type | Purpose |
|-------|------|---------|
| BlueprintId | string | Blueprint being published |
| Title | string | Blueprint name |
| ValidationResults | List<ValidationIssue> | Validation check results |
| IsValid | bool | Whether blueprint passes validation |
| Warnings | List<string> | Non-blocking warnings |

#### ValidationIssue

| Field | Type | Purpose |
|-------|------|---------|
| Severity | string | error / warning |
| Message | string | Human-readable description |
| Location | string? | Which action/participant affected |

---

### Template Models (`Models/Templates/`)

#### TemplateListItemViewModel

| Field | Type | Purpose |
|-------|------|---------|
| Id | string | Template identifier |
| Name | string | Display name |
| Description | string | What the template does |
| Category | string | Template category |
| UsageCount | int | How many times used |
| Parameters | List<TemplateParameter> | Configurable parameters |

#### TemplateParameter

| Field | Type | Purpose |
|-------|------|---------|
| Name | string | Parameter name |
| Description | string | What it controls |
| Type | string | string / number / boolean |
| DefaultValue | string? | Default if not provided |
| Required | bool | Whether parameter is required |

---

### Explorer Models (`Models/Explorer/`)

#### DocketViewModel

| Field | Type | Purpose |
|-------|------|---------|
| DocketId | string | Docket identifier |
| RegisterId | string | Parent register |
| Version | int | Docket sequence number |
| Hash | string | Docket hash |
| PreviousHash | string? | Previous docket hash (chain link) |
| TransactionCount | int | Number of transactions |
| TransactionIds | List<string> | Transaction IDs in this docket |
| CreatedAt | DateTimeOffset | Docket creation time |
| IsIntegrityValid | bool | Whether chain integrity verified |

#### ODataQueryModel

| Field | Type | Purpose |
|-------|------|---------|
| Filters | List<ODataFilterRow> | Filter conditions |
| OrderBy | string? | Sort field |
| OrderDirection | string | asc / desc |
| Top | int | Page size (default 20) |
| Skip | int | Offset for pagination |

#### ODataFilterRow

| Field | Type | Purpose |
|-------|------|---------|
| Field | string | Entity field name |
| Operator | string | eq / ne / gt / lt / ge / le / contains / startswith |
| Value | string | Filter value |
| LogicalOperator | string | and / or (combinator with next row) |

---

## Existing Models — Modifications

### RegisterViewModel (existing, `Models/Registers/`)

**Add**: No changes needed. Already has truncation helpers. Will adopt the new `TruncatedId` component in views instead.

### WalletDto (existing, `Models/Wallet/`)

**No changes needed.** Already has Address, Name, Algorithm, Status, Owner, CreatedAt, Metadata. The WalletList page already uses `IWalletApiService`.

### TransactionViewModel (existing, `Models/Registers/`)

**No changes needed.** Already has TxId, SenderWallet, Type, Status, Timestamp, Height. The truncation helpers will be replaced by the `TruncatedId` component.

---

## Model Relationships

```
DashboardStatsViewModel
  └── Aggregated from all services (single API call to Gateway)

OrganizationViewModel
  ├── Has many Users (UserList component, existing)
  └── Has many Participants (ParticipantListItemViewModel, existing)

WorkflowInstanceViewModel
  ├── References BlueprintId → BlueprintListItemViewModel
  ├── Has many PendingActionViewModel
  └── Has many Participants

BlueprintListItemViewModel
  ├── Has many BlueprintVersionViewModel (published versions)
  └── Can be created from TemplateListItemViewModel

DocketViewModel
  ├── Belongs to Register (RegisterViewModel)
  ├── Has many TransactionIds → TransactionViewModel
  └── Links to previous DocketViewModel (chain)
```
