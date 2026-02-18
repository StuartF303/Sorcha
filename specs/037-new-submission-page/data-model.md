# Data Model: New Submission Page (037)

**Date**: 2026-02-18
**Branch**: `037-new-submission-page`

## Entities

### New Entities

#### DefaultWalletPreference (client-side only)
Stored in browser local storage via `ILocalStorageService`.

| Field | Type | Description |
|-------|------|-------------|
| WalletAddress | string | The user's preferred wallet address for signing |

**Storage key**: `sorcha:preferences:defaultWallet`
**Lifecycle**: Persists across browser sessions. Cleared if the wallet is no longer linked.

---

### Existing Entities (Referenced)

#### RegisterViewModel (existing)
From `Sorcha.UI.Core/Models/Registers/RegisterViewModel.cs`.

| Field | Type | Used By |
|-------|------|---------|
| Id | string | Register identification |
| Name | string | Display in register grouping header |
| Description | string? | Display in register grouping header |
| Status | RegisterStatus | Filter: only show Online registers |
| TenantId | string | Org-scoping |

#### BlueprintInfo (existing, from AvailableBlueprintsResponse)
From `Sorcha.Blueprint.Service/Models/Responses/AvailableBlueprintsResponse.cs`.

| Field | Type | Used By |
|-------|------|---------|
| BlueprintId | string | Identify blueprint for start flow |
| Title | string | Display on blueprint card |
| Description | string? | Display on blueprint card |
| Version | int | Display version info |
| AvailableActions | List&lt;ActionInfo&gt; | Determine startable actions |

#### ActionInfo (existing, from AvailableBlueprintsResponse)
| Field | Type | Used By |
|-------|------|---------|
| ActionId | string | Identify Action 0 for start flow |
| Title | string | Display action name |
| Description | string? | Display action description |
| IsAvailable | bool | Filter available actions |
| DataSchema | string? | Schema $id only — need full blueprint fetch for rendering |

#### Blueprint (existing domain model)
From `Sorcha.Blueprint.Models/Blueprint.cs`. Fetched via `GET /api/blueprints/{id}` for full action schemas.

| Field | Type | Used By |
|-------|------|---------|
| Id | string | Blueprint identification |
| Title | string | Display |
| Actions | List&lt;Action&gt; | Get Action 0 with full DataSchemas for form rendering |

#### Action (existing domain model)
From `Sorcha.Blueprint.Models/Action.cs`.

| Field | Type | Used By |
|-------|------|---------|
| Id | int | Action identification |
| Title | string | Form dialog title |
| Description | string? | Form dialog subtitle |
| DataSchemas | IEnumerable&lt;JsonDocument&gt; | Form rendering via SorchaFormRenderer |
| IsStartingAction | bool | Identify Action 0 |

#### Instance (existing)
From `Sorcha.Blueprint.Service/Models/Instance.cs`.

| Field | Type | Used By |
|-------|------|---------|
| Id | string | Instance reference for confirmation |
| BlueprintId | string | Link to blueprint |
| RegisterId | string | Link to register |
| State | InstanceState | Track workflow state |
| CurrentActionIds | List&lt;int&gt; | Current step |
| CreatedAt | DateTimeOffset | Display |

#### WalletDto (existing)
From `Sorcha.UI.Core/Models/Wallet/WalletDto.cs`.

| Field | Type | Used By |
|-------|------|---------|
| Address | string | Wallet identification and signing |
| Name | string | Display in wallet selector |
| Algorithm | string | Display (ED25519, NISTP256, etc.) |
| Status | string | Filter active wallets only |

#### FormSubmission (existing)
From `Sorcha.UI.Core/Models/Forms/FormSubmission.cs`. Output of `SorchaFormRenderer.OnSubmit`.

| Field | Type | Used By |
|-------|------|---------|
| Data | Dictionary&lt;string, object?&gt; | Form field values |
| SigningWalletAddress | string | Wallet that signed |
| Signature | string | Cryptographic signature |
| Hash | string | Data hash |
| CanonicalJson | string | Canonical JSON serialisation |

#### ActionSubmissionRequest (existing, Blueprint Service)
From `Sorcha.Blueprint.Service/Models/Requests/ActionSubmissionRequest.cs`. Required body for execute endpoint.

| Field | Type | Used By |
|-------|------|---------|
| BlueprintId | string (required) | Identify blueprint |
| ActionId | string (required) | Identify action |
| InstanceId | string? | Link to instance |
| SenderWallet | string (required) | Signing wallet address |
| RegisterAddress | string (required) | Target register |
| PayloadData | Dictionary&lt;string, object&gt; (required) | Form data |

---

## New View Models (UI Layer)

### RegisterBlueprintGroup
Groups blueprints by register for display on the New Submission page.

| Field | Type | Description |
|-------|------|-------------|
| Register | RegisterViewModel | The register (authority/jurisdiction) |
| Blueprints | List&lt;StartableBlueprintViewModel&gt; | Available blueprints in this register |

### StartableBlueprintViewModel
A blueprint the user can start within a specific register.

| Field | Type | Description |
|-------|------|-------------|
| BlueprintId | string | Blueprint identifier |
| Title | string | Display name |
| Description | string? | Brief description of the service |
| Version | int | Published version |
| RegisterId | string | Register this blueprint is published on |
| StartingActionTitle | string | Name of Action 0 |
| StartingActionDescription | string? | Description of Action 0 |

---

## Data Flow

### Browse Flow
```
User navigates to /my-workflows
  → GetWalletsAsync() → user's wallets
  → GetRegistersAsync() → user's accessible registers (online only)
  → For each register × wallet:
      GetAvailableBlueprintsAsync(wallet, registerId)
        → BlueprintInfo[] per register
  → Group by register → RegisterBlueprintGroup[]
  → Display as service directory
```

### Submit Flow
```
User clicks "Start" on a blueprint
  → GetBlueprintDetailAsync(blueprintId) → full Blueprint with Action.DataSchemas
  → Find Action 0 (IsStartingAction or first action)
  → Open NewSubmissionDialog with:
      - Action 0 model (for SorchaFormRenderer)
      - Smart-defaulted wallet address
      - Register context
  → User fills form, clicks Submit
  → FormSigningService signs data with wallet
  → CreateInstanceAsync(blueprintId, registerId) → Instance with id
  → SubmitActionAsync(instanceId, actionId, submission) → confirmation
  → Show success notification with instance reference
```
