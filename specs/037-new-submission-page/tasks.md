# Tasks: New Submission Page

**Input**: Design documents from `/specs/037-new-submission-page/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Included — spec requires unit tests for WalletPreferenceService (SC-007) and submission flow coverage.

**Organization**: Tasks grouped by user story (5 stories from spec.md). US1+US2+US5 are P1 and tightly coupled (browse + submit + nav). US3+US4 are P2 (wallet preference + pending actions fix). All depend on foundational Phase 2.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1–US5)
- All file paths relative to repository root

---

## Phase 1: Setup

**Purpose**: Verify project structure and ensure all referenced projects build

- [x] T001 Verify existing UI projects build cleanly — run `dotnet build src/Apps/Sorcha.UI/Sorcha.UI.Core/` and `dotnet build src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/` and confirm zero errors
- [x] T002 Verify existing UI Core test project builds — run `dotnet build tests/Sorcha.UI.Core.Tests/` and confirm zero errors

---

## Phase 2: Foundational (Shared Models & Services)

**Purpose**: Create view models and services shared across multiple user stories. MUST complete before any user story work.

### Models

- [x] T003 [P] Create `AvailableBlueprintsViewModel` and `BlueprintInfoViewModel` and `ActionInfoViewModel` in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Workflows/AvailableBlueprintsViewModel.cs` — maps to `AvailableBlueprintsResponse` from Blueprint Service (WalletAddress, RegisterAddress, Blueprints list)
- [x] T004 [P] Create `StartableBlueprintViewModel` in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Workflows/StartableBlueprintViewModel.cs` — display model (BlueprintId, Title, Description, Version, RegisterId, StartingActionTitle, StartingActionDescription)
- [x] T005 [P] Create `RegisterBlueprintGroup` in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Workflows/RegisterBlueprintGroup.cs` — groups blueprints by register (Register: RegisterViewModel, Blueprints: List<StartableBlueprintViewModel>)
- [x] T006 [P] Create `ActionSubmissionResultViewModel` in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Workflows/ActionSubmissionResultViewModel.cs` — result from action execution (TransactionId, InstanceId, IsComplete, NextActions, Warnings)

### Services

- [x] T007 [P] Create `IWalletPreferenceService` interface in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/IWalletPreferenceService.cs` — methods: GetDefaultWalletAsync, SetDefaultWalletAsync, ClearDefaultWalletAsync, GetSmartDefaultAsync(List<WalletDto>)
- [x] T008 Implement `WalletPreferenceService` in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/WalletPreferenceService.cs` — uses `ILocalStorageService` with key `sorcha:preferences:defaultWallet`, implements smart default logic (1 wallet: auto-select, multi: check stored default, fallback to first)
- [x] T009 Add `GetAvailableBlueprintsAsync(string walletAddress, string registerId, CancellationToken ct)` method to `IBlueprintApiService` in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/IBlueprintApiService.cs` and implement in `BlueprintApiService.cs` — calls `GET /api/actions/{wallet}/{register}/blueprints`, deserialises to `AvailableBlueprintsViewModel`
- [x] T010 Add `CreateInstanceAsync(string blueprintId, string registerId, CancellationToken ct)` method to `IWorkflowService` in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/IWorkflowService.cs` and implement in `WorkflowService.cs` — calls `POST /api/instances` with `{ blueprintId, registerId }`, returns `WorkflowInstanceViewModel`
- [x] T011 Rewrite `SubmitActionAsync` in `IWorkflowService` and `WorkflowService.cs` — accept full request model (blueprintId, actionId, instanceId, senderWallet, registerAddress, payloadData), send complete `ActionSubmissionRequest` body, add `X-Delegation-Token` header (user's JWT), return `ActionSubmissionResultViewModel`
- [x] T012 Register `IWalletPreferenceService` as scoped in DI — add to `ServiceCollectionExtensions.cs` in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Extensions/ServiceCollectionExtensions.cs`

### Tests

- [x] T013 Write unit tests for `WalletPreferenceService` in `tests/Sorcha.UI.Core.Tests/Services/WalletPreferenceServiceTests.cs` — test scenarios: single wallet auto-select, multi wallet stored default, multi wallet no default (first wallet), stored default no longer in list (fallback + clear), set default persists, clear default works

**Checkpoint**: All shared models, services, and tests are complete. User story implementation can begin.

---

## Phase 3: User Story 5 — Navigation Order Update (Priority: P1)

**Goal**: Swap "New Submission" before "Pending Actions" in the sidebar navigation.

**Independent Test**: Open the application and verify sidebar order.

**Dependencies**: None — can start immediately after Phase 2.

- [x] T014 [US5] Swap nav link order in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Components/Layout/MainLayout.razor` — move the "New Submission" `MudNavLink` (href="my-workflows") above the "Pending Actions" `MudNavLink` (href="my-actions") within the "MY ACTIVITY" section

**Checkpoint**: Navigation order reflects natural workflow lifecycle.

---

## Phase 4: User Story 1 — Browse Available Services (Priority: P1)

**Goal**: Redesign the "New Submission" page into a service directory showing blueprints grouped by register.

**Independent Test**: Navigate to `/my-workflows`, verify registers displayed with blueprints grouped beneath each.

**Dependencies**: Phase 2 (shared models and services).

- [x] T015 [US1] Rewrite `MyWorkflows.razor` in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/MyWorkflows.razor` — replace workflow instance list with service directory layout: on load, fetch user wallets via `IWalletApiService.GetWalletsAsync()`, fetch online registers via `IRegisterService.GetRegistersAsync()`, for each register+wallet call `IBlueprintApiService.GetAvailableBlueprintsAsync()`, group results into `RegisterBlueprintGroup[]`, display register sections with blueprint cards showing title, description, version, and a "Start" button
- [x] T016 [US1] Implement empty state handling in `MyWorkflows.razor` — show guidance message when: (a) no linked wallets (direct to My Wallet page), (b) no accessible registers, (c) no startable blueprints found across any register. Hide register sections with zero blueprints.
- [x] T017 [US1] Add loading state to `MyWorkflows.razor` — show `MudProgressLinear` or skeleton while registers and blueprints are being fetched. Handle per-register loading failures gracefully (show error for failed register, continue loading others).

**Checkpoint**: Users can browse available services grouped by register. No submission capability yet.

---

## Phase 5: User Story 2 — Start a New Submission (Priority: P1)

**Goal**: Users can click "Start" on a blueprint and submit a form that creates a workflow instance and executes Action 0.

**Independent Test**: Click "Start", fill form, submit, verify instance created and action executed.

**Dependencies**: Phase 2 (services) + Phase 4 (browse page provides the "Start" button).

### Components

- [x] T018 [P] [US2] Create `WalletSelector.razor` component in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Workflows/WalletSelector.razor` — inline wallet selector: hidden if 1 wallet (auto-selected), `MudSelect` dropdown if 2+ wallets (showing name + truncated address + algorithm), warning with link to My Wallet if 0 wallets. Parameters: Wallets (List<WalletDto>), SelectedAddress (string, two-way bindable), ShowSetDefault (bool), OnSetDefault (EventCallback<string>).
- [x] T019 [US2] Create `NewSubmissionDialog.razor` in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Workflows/NewSubmissionDialog.razor` — MudDialog that: (1) receives BlueprintId, RegisterId, WalletAddress as parameters, (2) fetches full blueprint via `IBlueprintApiService.GetBlueprintDetailAsync()`, (3) finds Action 0 (IsStartingAction or first action), (4) renders `WalletSelector` at top (if multi-wallet), (5) renders `SorchaFormRenderer` with Action 0 model + wallet address, (6) on submit: calls `CreateInstanceAsync()` then `SubmitActionAsync()` in sequence, (7) closes with success result containing instance reference, (8) shows loading overlay during submission, (9) handles errors (instance creation failure, action execution failure) with appropriate messages

### Page Integration

- [x] T020 [US2] Wire "Start" button in `MyWorkflows.razor` — on click, get user wallets, get smart default wallet, open `NewSubmissionDialog` with blueprintId, registerId, and wallet address. On dialog success result, show `MudSnackbar` with instance reference and offer navigation link.
- [x] T021 [US2] Handle edge case in `NewSubmissionDialog` — if blueprint is no longer available (unpublished between browse and start), show error and close dialog. If instance creation succeeds but action execution fails, show partial success message explaining the action will appear in Pending Actions.

**Checkpoint**: Full browse-and-submit flow works end-to-end. Users can discover services and start submissions.

---

## Phase 6: User Story 3 — Wallet Selection with Default Preference (Priority: P2)

**Goal**: Users with multiple wallets can set a default, which persists across sessions.

**Independent Test**: With two wallets, set a default, start a new submission, verify default is pre-selected.

**Dependencies**: Phase 2 (WalletPreferenceService) + Phase 5 (NewSubmissionDialog uses wallet selector).

- [x] T022 [US3] Wire default wallet persistence into `WalletSelector.razor` — add "Set as default" checkbox or button next to the dropdown. When toggled, call `IWalletPreferenceService.SetDefaultWalletAsync()`. Show subtle indicator on the currently-defaulted wallet.
- [x] T023 [US3] Wire smart default into `NewSubmissionDialog.razor` — on init, call `IWalletPreferenceService.GetSmartDefaultAsync()` to determine the pre-selected wallet. If the stored default is no longer in the wallet list, clear it and fall back to first wallet.
- [x] T024 [US3] Wire smart default into `MyWorkflows.razor` "Start" button handler — when opening NewSubmissionDialog, resolve the smart default wallet address and pass it as the WalletAddress parameter.

**Checkpoint**: Multi-wallet users have a seamless default preference experience.

---

## Phase 7: User Story 4 — Fix Pending Actions Submission Flow (Priority: P2)

**Goal**: Wire the wallet and actual submission call into the Pending Actions page.

**Independent Test**: Open a pending action, fill form, submit, verify backend receives the request and action disappears from list.

**Dependencies**: Phase 2 (rewritten SubmitActionAsync + WalletPreferenceService).

- [x] T025 [US4] Fix `ActionForm.razor` in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Workflows/ActionForm.razor` — add `WalletAddress` parameter (string), set `_signingWallet = WalletAddress` and `_participantAddress = WalletAddress` in `OnParametersSet`, so `SorchaFormRenderer` receives populated signing wallet and participant address
- [x] T026 [US4] Fix `MyActions.razor` in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/MyActions.razor` — in `HandleTakeAction`: (1) fetch user wallets via `IWalletApiService.GetWalletsAsync()`, (2) get smart default via `IWalletPreferenceService.GetSmartDefaultAsync()`, (3) pass wallet address to `ActionForm` dialog as parameter, (4) on dialog result Ok with `ActionSubmissionViewModel`: build full `ActionSubmissionRequest` (blueprintId from action context, actionId, instanceId, senderWallet, registerAddress, payloadData from form data), call `IWorkflowService.SubmitActionAsync()`, (5) on success: remove action from list + show success snackbar, (6) on failure: show error snackbar, keep action in list
- [x] T027 [US4] Inject `IWalletApiService` and `IWalletPreferenceService` into `MyActions.razor` — add `@inject` directives and update the component to use these services in the take-action flow

**Checkpoint**: Pending Actions submission flow works end-to-end. Actions are actually submitted to the backend.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, validation, and final verification.

- [x] T028 [P] Update `.specify/MASTER-TASKS.md` with task status for 037-new-submission-page
- [x] T029 [P] Update `docs/development-status.md` to reflect new submission page completion
- [x] T030 Run build validation — `dotnet build` the full solution and confirm zero warnings, zero errors
- [x] T031 Run test validation — execute `dotnet test tests/Sorcha.UI.Core.Tests/` and verify all pass (baseline + new WalletPreferenceService tests)

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup) ──► Phase 2 (Foundational) ──┬──► Phase 3 (US5 - Nav) ──────────────────► Phase 8 (Polish)
                                               ├──► Phase 4 (US1 - Browse) ──► Phase 5 (US2 - Submit) ──┤
                                               ├──► Phase 6 (US3 - Wallet Default) ◄── Phase 5 ──────────┤
                                               └──► Phase 7 (US4 - Fix Pending) ─────────────────────────┘
```

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1
- **Phase 3 (US5)**: Depends on Phase 2 — **can run in parallel with Phase 4**
- **Phase 4 (US1)**: Depends on Phase 2
- **Phase 5 (US2)**: Depends on Phase 4 (browse page provides "Start" button)
- **Phase 6 (US3)**: Depends on Phase 5 (wallet selector in NewSubmissionDialog)
- **Phase 7 (US4)**: Depends on Phase 2 — **can run in parallel with Phase 4/5**
- **Phase 8 (Polish)**: Depends on all user stories complete

### User Story Dependencies

| Story | Depends On | Blocks |
|-------|-----------|--------|
| US5 (Nav Order) | Foundational only | — |
| US1 (Browse) | Foundational only | US2 |
| US2 (Submit) | US1 | US3 |
| US3 (Wallet Default) | US2 | — |
| US4 (Fix Pending) | Foundational only | — |

### Within Each User Story

1. Models/view models before components
2. Components before page integration
3. Service methods before UI consumption
4. Core functionality before edge case handling

### Parallel Opportunities

- **T003 + T004 + T005 + T006**: Four model files, all independent
- **T007 + T009 + T010**: Interface additions in different files
- **Phase 3 + Phase 4**: US5 (nav swap) and US1 (browse page) have zero file overlap
- **Phase 4 + Phase 7**: US1 (browse) and US4 (fix pending) modify different pages
- **T028 + T029**: Documentation tasks

---

## Implementation Strategy

### MVP First (US1 + US5 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational — models, services, tests
3. Complete Phase 3: US5 — nav order swap (trivial)
4. Complete Phase 4: US1 — browse page redesign
5. **STOP and VALIDATE**: Navigate to /my-workflows, verify service directory loads with registers and blueprints

### Incremental Delivery

1. Setup + Foundational → **Infrastructure ready**
2. US5 (Nav Order) → **Navigation corrected**
3. US1 (Browse) → **Service directory visible — can demonstrate to stakeholders**
4. US2 (Submit) → **Full submission flow — core value delivered**
5. US3 (Wallet Default) → **Multi-wallet polish**
6. US4 (Fix Pending) → **Pending actions pipeline complete**
7. Polish → **Documentation current, all tests green**

### Risk Mitigation

- **Pre-existing test failures**: UI Core has 4 pre-existing YAML failures (ExportImportTests) — baseline is ~517 pass. Only count NEW failures as regressions.
- **X-Delegation-Token**: If the Blueprint Service requires a specific delegation token format beyond the user's JWT, the SubmitActionAsync rewrite may need adjustment. Research R2 indicates the JWT should work.
- **GetAvailableBlueprintsAsync returns all blueprints**: The endpoint currently has no participant filtering (research R2). All published blueprints are shown regardless of wallet. This is acceptable for MVP but should be noted.
- **ActionInfo.DataSchema is just $id**: Must fetch full blueprint via GetBlueprintDetailAsync for form rendering (research R1). This adds one extra API call per submission start.

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- Total tasks: 31 (2 setup + 11 foundational + 1 US5 + 3 US1 + 4 US2 + 3 US3 + 3 US4 + 4 polish)
- Pre-existing test baselines documented in memory — do not count as regressions
- No backend changes — all work is in Blazor WASM UI layer
- Existing form renderer pipeline (SorchaFormRenderer, ControlDispatcher, FormSchemaService, FormSigningService) is reused without modification
