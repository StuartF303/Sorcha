# UI Service Contracts: New Submission Page (037)

**Date**: 2026-02-18

## New Methods on Existing Services

### IBlueprintApiService — New Method

#### GetAvailableBlueprintsAsync

Wraps existing `GET /api/actions/{wallet}/{register}/blueprints`.

```
Method: GetAvailableBlueprintsAsync(string walletAddress, string registerId, CancellationToken ct)
Returns: AvailableBlueprintsViewModel?

HTTP: GET /api/actions/{walletAddress}/{registerId}/blueprints
Auth: Bearer JWT (via AuthenticatedHttpMessageHandler)
```

**Response model** (new ViewModel):
```
AvailableBlueprintsViewModel
  WalletAddress: string
  RegisterAddress: string
  Blueprints: List<BlueprintInfoViewModel>

BlueprintInfoViewModel
  BlueprintId: string
  Title: string
  Description: string?
  Version: int
  AvailableActions: List<ActionInfoViewModel>

ActionInfoViewModel
  ActionId: string
  Title: string
  Description: string?
  IsAvailable: bool
```

---

### IWorkflowService — New Method

#### CreateInstanceAsync

Wraps `POST /api/instances`.

```
Method: CreateInstanceAsync(string blueprintId, string registerId, CancellationToken ct)
Returns: WorkflowInstanceViewModel?

HTTP: POST /api/instances
Auth: Bearer JWT
Body: { "blueprintId": "...", "registerId": "..." }
```

---

### IWorkflowService — Updated Method

#### SubmitActionAsync (rewrite)

Fix the existing method to pass the full request body and delegation token.

```
Method: SubmitActionAsync(ActionSubmissionRequest submission, CancellationToken ct)
Returns: ActionSubmissionResultViewModel?

HTTP: POST /api/instances/{instanceId}/actions/{actionId}/execute
Auth: Bearer JWT
Headers: X-Delegation-Token: {JWT}
Body: {
  "blueprintId": "...",
  "actionId": "...",
  "instanceId": "...",
  "senderWallet": "...",
  "registerAddress": "...",
  "payloadData": { ... }
}
```

**New request model** (replaces `ActionSubmissionViewModel`):
```
ActionSubmissionRequest (UI-side)
  BlueprintId: string (required)
  ActionId: string (required)
  InstanceId: string (required)
  SenderWallet: string (required)
  RegisterAddress: string (required)
  PayloadData: Dictionary<string, object> (required)
```

**New response model**:
```
ActionSubmissionResultViewModel
  TransactionId: string
  InstanceId: string
  IsComplete: bool
  NextActions: List<NextActionInfo>?
  Warnings: List<string>?
```

---

## New Service

### IWalletPreferenceService

Manages the user's default wallet preference in browser local storage.

```
Interface: IWalletPreferenceService

Methods:
  GetDefaultWalletAsync() → string?
    Reads from localStorage key "sorcha:preferences:defaultWallet"

  SetDefaultWalletAsync(string walletAddress) → Task
    Writes to localStorage key "sorcha:preferences:defaultWallet"

  ClearDefaultWalletAsync() → Task
    Removes the localStorage key

  GetSmartDefaultAsync(List<WalletDto> wallets) → string?
    If 1 wallet: returns its address
    If multiple: returns stored default if still in wallet list, otherwise first wallet
    If 0 wallets: returns null
```

**DI Registration**: Scoped, registered in `AddBlueprintStorageServices()` or similar extension.
**Dependency**: `ILocalStorageService` (Blazored.LocalStorage).

---

## New Components

### NewSubmissionDialog.razor

MudDialog for starting a new blueprint submission. Opened when user clicks "Start" on a blueprint card.

```
Parameters:
  BlueprintId: string (required) — which blueprint to start
  RegisterId: string (required) — which register to submit to
  WalletAddress: string (required) — pre-selected signing wallet

Internal flow:
  1. OnInitializedAsync: Fetch full blueprint via IBlueprintApiService.GetBlueprintDetailAsync()
  2. Find Action 0 (IsStartingAction or Id == 0)
  3. Render SorchaFormRenderer with Action 0's DataSchemas
  4. On submit callback:
     a. CreateInstanceAsync(blueprintId, registerId) → get instanceId
     b. Build ActionSubmissionRequest from FormSubmission + instance context
     c. SubmitActionAsync(request) → get confirmation
     d. Close dialog with DialogResult.Ok(confirmation)
  5. On cancel: MudDialog.Cancel()
```

### WalletSelector.razor

Inline wallet selector component (not a dialog — embedded at top of form).

```
Parameters:
  Wallets: List<WalletDto> (required) — available wallets
  SelectedAddress: string — current selection (two-way bindable)
  SelectedAddressChanged: EventCallback<string>
  ShowSetDefault: bool = true — show "Set as default" option
  OnSetDefault: EventCallback<string> — called when user sets default

Rendering:
  If 1 wallet: hidden (auto-selected, no UI)
  If 2+ wallets: MudSelect dropdown with wallet name + address + algorithm
  If 0 wallets: warning message with link to My Wallet page
```

---

## Updated Components

### ActionForm.razor — Fix

```
New Parameters:
  WalletAddress: string — pre-populated signing wallet address

Changes:
  - Set _signingWallet = WalletAddress in OnParametersSet
  - Set _participantAddress = WalletAddress (wallet IS the participant address)
  - SorchaFormRenderer receives populated SigningWalletAddress and ParticipantAddress
```

### MyActions.razor — Fix

```
Changes in HandleTakeAction method:
  1. Get user's wallets via IWalletApiService.GetWalletsAsync()
  2. Get smart default wallet via IWalletPreferenceService.GetSmartDefaultAsync()
  3. Pass wallet address to ActionForm dialog as parameter
  4. On dialog result (Ok with ActionSubmissionViewModel):
     a. Build full ActionSubmissionRequest from dialog result + action context
     b. Call IWorkflowService.SubmitActionAsync(request)
     c. On success: remove action from list, show success snackbar
     d. On failure: show error snackbar
```
