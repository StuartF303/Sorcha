# Research: New Submission Page (037)

**Date**: 2026-02-18
**Branch**: `037-new-submission-page`

## R1: Blueprint Discovery Endpoint

**Decision**: Use existing `GET /api/actions/{wallet}/{register}/blueprints` endpoint, iterated per-register on the client.

**Rationale**: The endpoint already exists and returns `AvailableBlueprintsResponse` with `BlueprintInfo` and `ActionInfo` per blueprint. The client iterates over the user's accessible registers (from `IRegisterService.GetRegistersAsync()`) and calls this endpoint per register per wallet.

**Findings**:
- Endpoint location: `src/Services/Sorcha.Blueprint.Service/Program.cs` lines 620-673
- Response model: `AvailableBlueprintsResponse` with `BlueprintInfo[]` (id, title, description, version, available actions)
- **Gap**: `ActionInfo.DataSchema` only returns the `$id` URI string, not the full JSON schema. Full schemas needed for form rendering must come from `GET /api/blueprints/{id}` (full Blueprint model with `Action.DataSchemas`).
- **Gap**: No participant-level filtering yet (comment says "For MVP, all actions are available"). All published blueprints are returned regardless of wallet/participant matching.
- **Gap**: No UI service client method wraps this endpoint.

**Alternatives considered**:
- New "startable blueprints" endpoint: Rejected — adds backend work when client-side iteration is sufficient for MVP.
- Fetch all blueprints via `GET /api/blueprints`: Rejected — doesn't associate blueprints with registers.

---

## R2: Instance Creation + Action Execution Flow

**Decision**: Client-side two-step orchestration — `POST /api/instances` then `POST /api/instances/{id}/actions/{actionId}/execute`.

**Rationale**: Both endpoints exist. The form renders before instance creation (from blueprint schema). Instance is created only on submit, then Action 0 is immediately executed. No orphan instances if user abandons form.

**Findings**:
- Instance creation: `POST /api/instances` takes `CreateInstanceRequest` (blueprintId, registerId, optional tenantId + metadata). Returns full `Instance` object with id.
- Action execution: `POST /api/instances/{id}/actions/{actionId}/execute` takes `ActionSubmissionRequest` (blueprintId, actionId, instanceId, senderWallet, registerAddress, payloadData). Requires `X-Delegation-Token` header.
- **Gap**: `IWorkflowService` has no `CreateInstanceAsync()` method — must be added.
- **Gap**: `SubmitActionAsync()` sends incomplete body (only `{ data }` instead of full `ActionSubmissionRequest`) and doesn't pass `X-Delegation-Token` header.

**X-Delegation-Token**:
- Required by the execute endpoint middleware.
- In the Blazor WASM context, the user's JWT bearer token can be passed as the delegation token — the Blueprint Service uses it to authenticate wallet signing operations on behalf of the user.
- The `AuthenticatedHttpMessageHandler` already attaches the JWT to outgoing HTTP calls. The delegation token should be the same JWT, passed as an additional header.

---

## R3: Wallet Selection and Default Preference

**Decision**: Smart default using `ILocalStorageService` (Blazored.LocalStorage, already a dependency).

**Rationale**: `Blazored.LocalStorage` v4.5.0 is already in `Sorcha.UI.Core.csproj` and registered in DI. Existing usage patterns in `OfflineSyncService`, `BlueprintStorageService`, and `BrowserTokenCache` establish the convention.

**Findings**:
- `IWalletApiService.GetWalletsAsync()` returns `List<WalletDto>` — user's wallets with Address, Name, Algorithm.
- `ILocalStorageService` available via `Blazored.LocalStorage`. Key convention: `sorcha:{scope}:{item}`.
- Proposed key: `sorcha:preferences:defaultWallet` storing the wallet address string.
- Smart default logic: if 1 wallet → auto-select; if multiple → check local storage for default → fall back to first wallet.

---

## R4: Form Rendering Pipeline

**Decision**: Reuse existing `SorchaFormRenderer` component pipeline from branch 032.

**Rationale**: Complete form rendering infrastructure exists: `FormSchemaService` for schema-to-form generation, `ControlDispatcher` for field rendering, `FormContext` for state management, `FormSigningService` for wallet signing.

**Findings**:
- `SorchaFormRenderer` parameters: `Action` (Blueprint.Models.Action), `ParticipantAddress`, `SigningWalletAddress`, `OnSubmit`, `OnReject`, `OnCancel`, `IsReadOnly`, `PreviousData`.
- The `Action` model must be a full `Sorcha.Blueprint.Models.Action` with `DataSchemas` as `IEnumerable<JsonDocument>`.
- `ActionInfo` from the discovery endpoint only has schema `$id` — need full blueprint fetch via `GET /api/blueprints/{id}` to get `Action.DataSchemas`.
- `FormSigningService.SignWithWallet()` calls `IWalletApiService.SignDataAsync()` with `IsPreHashed = true`.
- `FormSubmission` result contains: `Data` (Dictionary<string, object?>), `SigningWalletAddress`, `Signature`, `Hash`, `CanonicalJson`, file attachments, credential presentations.

---

## R5: ActionForm.razor Gaps

**Decision**: Fix two gaps in the existing ActionForm component and create a new NewSubmissionDialog for the submission flow.

**Rationale**: ActionForm is designed for existing pending actions. The new submission flow has different requirements (no existing instance, needs instance creation, different context). Two separate components is cleaner than overloading one.

**Findings**:
- `ActionForm.razor` gaps:
  1. `_signingWallet` is `string.Empty` — never populated from user's wallets
  2. `_participantAddress` is `string.Empty` — never populated
- `MyActions.razor` gap: After `ActionForm` dialog closes with `DialogResult.Ok(submissionVm)`, only shows snackbar — never calls `SubmitActionAsync()`.
- Fix: Pass wallet address into ActionForm as a parameter, wire up the actual submission call in MyActions.razor.

---

## R6: Register Access Model

**Decision**: Use `IRegisterService.GetRegistersAsync()` to get all registers, then filter client-side.

**Rationale**: The endpoint returns all registers for the authenticated user (JWT-scoped). No per-user access filtering exists server-side currently, but org-scoping via JWT provides adequate filtering.

**Findings**:
- `GET /api/registers` with optional `?tenantId=` query parameter.
- Returns all registers for the authenticated user's tenant context.
- `RegisterViewModel` has: Id, Name, Description, Status, TenantId, Height.
- Only show registers with `Status == Online` (active and accessible).

---

## Summary of Implementation Gaps

| # | Gap | Resolution |
|---|-----|-----------|
| 1 | No `GetAvailableBlueprintsAsync()` in UI services | Add to `IBlueprintApiService` — calls `GET /api/actions/{wallet}/{register}/blueprints` |
| 2 | No `CreateInstanceAsync()` in UI services | Add to `IWorkflowService` — calls `POST /api/instances` |
| 3 | `SubmitActionAsync()` sends incomplete body | Rewrite to pass full `ActionSubmissionRequest` + `X-Delegation-Token` header |
| 4 | `ActionInfo.DataSchema` is just `$id` URI | Fetch full blueprint via `GET /api/blueprints/{id}` for form rendering |
| 5 | `ActionForm.razor` wallet not wired | Pass wallet address as parameter, populate from smart default |
| 6 | `MyActions.razor` doesn't call submit | Wire `SubmitActionAsync()` after dialog closes with submission data |
| 7 | No new submission dialog component | Create `NewSubmissionDialog.razor` for the submission flow |
| 8 | No default wallet preference storage | Use `ILocalStorageService` with key `sorcha:preferences:defaultWallet` |
