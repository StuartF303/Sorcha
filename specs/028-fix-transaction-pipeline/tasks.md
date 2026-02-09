# Tasks: Fix Transaction Submission Pipeline

**Input**: Design documents from `/specs/028-fix-transaction-pipeline/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Included per constitution requirement (>85% coverage for new code).

**Organization**: Tasks grouped by user story. US1 and US2 are both P1 and tightly coupled (submitting to Validator requires monitoring to build dockets), so they share a phase.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Foundational — Service Client Layer

**Purpose**: Add the Validator Service client method and request/response models that all subsequent phases depend on.

**CRITICAL**: No user story work can begin until this phase is complete.

- [X] T001 Add `ActionTransactionSubmission`, `SignatureInfo`, and `TransactionSubmissionResult` records to `src/Common/Sorcha.ServiceClients/Validator/IValidatorServiceClient.cs` per contracts/validator-transaction-api.md
- [X] T002 Add `SubmitTransactionAsync` method to `IValidatorServiceClient` interface in `src/Common/Sorcha.ServiceClients/Validator/IValidatorServiceClient.cs`
- [X] T003 Implement `SubmitTransactionAsync` in `src/Common/Sorcha.ServiceClients/Validator/ValidatorServiceClient.cs` — HTTP POST to `/api/v1/transactions/validate`, parse 200/400/409 responses into `TransactionSubmissionResult`
- [X] T004 Add `ToActionTransactionSubmission()` mapper method on `BuiltTransaction` class in `src/Services/Sorcha.Blueprint.Service/Services/Interfaces/ITransactionBuilderService.cs` — maps TransactionData→Payload (JsonElement), TxId→TransactionId/PayloadHash, Signature→SignatureInfo, Metadata→BlueprintId/ActionId/InstanceId per data-model.md mapping table
- [X] T005 Write unit tests for `ValidatorServiceClient.SubmitTransactionAsync` in `tests/Sorcha.ServiceClients.Tests/Validator/ValidatorServiceClientTests.cs` — test success (200), validation failure (400), mempool full (409), and HTTP failure scenarios
- [X] T006 [P] Write unit tests for `BuiltTransaction.ToActionTransactionSubmission()` in `tests/Sorcha.Blueprint.Service.Tests/Services/TransactionBuilderExtensionsTests.cs` — verify all field mappings from data-model.md

**Checkpoint**: Service client layer complete. `IValidatorServiceClient` has `SubmitTransactionAsync`. `BuiltTransaction` can map to Validator format.

---

## Phase 2: User Story 1+2 — Route Action Transactions Through Validator (Priority: P1)

**Goal**: Action transactions flow from Blueprint Service → Validator Service → mempool → docket → Register Service. Registers with pending action transactions are monitored for docket building.

**Independent Test**: Run Ping-Pong walkthrough. All 10 action transactions should appear in Register DB with docket numbers.

### Validator Service Changes (US2: Register Monitoring)

- [X] T007 [US2] In `src/Services/Sorcha.Validator.Service/Endpoints/ValidationEndpoints.cs`, add `IRegisterMonitoringRegistry` as a `[FromServices]` parameter to the `ValidateTransaction` endpoint and call `monitoringRegistry.RegisterForMonitoring(request.RegisterId)` after successful mempool addition (after line 131)
- [X] T008 [US2] Write unit test in `tests/Sorcha.Validator.Service.Tests/Endpoints/ValidationEndpointsTests.cs` verifying that `RegisterForMonitoring` is called when a transaction is successfully added to the mempool

### Blueprint Service Changes (US1: Route to Validator)

- [X] T009 [US1] Add `IValidatorServiceClient` as a constructor dependency in `src/Services/Sorcha.Blueprint.Service/Services/Implementation/ActionExecutionService.cs`
- [X] T010 [US1] Register `IValidatorServiceClient` in Blueprint Service DI — find `Program.cs` or the extension method where `ActionExecutionService` is registered and ensure `IValidatorServiceClient` is available (it should already be registered via `AddServiceClients`)
- [X] T011 [US1] Replace the Register Service submission (lines 158-162) in `ActionExecutionService.ExecuteAsync` with Validator Service submission: call `transaction.ToActionTransactionSubmission()` then `_validatorClient.SubmitTransactionAsync()`. Handle error responses (throw if `!result.Success`).
- [X] T012 [US1] Add confirmation polling loop after Validator submission in `ActionExecutionService.ExecuteAsync`: poll `_registerClient.GetTransactionAsync(registerId, txId)` every 1 second, up to 30 seconds timeout. Return success only when the transaction appears in the Register with a DocketNumber. Throw `TimeoutException` if not confirmed within the timeout.
- [X] T013 [US1] Update `ActionExecutionService` unit tests in `tests/Sorcha.Blueprint.Service.Tests/Services/ActionExecutionServiceTests.cs` — mock `IValidatorServiceClient.SubmitTransactionAsync` instead of `IRegisterServiceClient.SubmitTransactionAsync`. Test: successful submission+confirmation, Validator rejection, timeout scenario.

### Register Service Changes (US1: Remove Direct Persistence)

- [X] T014 [US1] In `src/Services/Sorcha.Register.Service/Program.cs`, restrict the direct transaction submission endpoint (`POST /api/registers/{registerId}/transactions`) — either remove it or add an authorization policy that blocks external callers (only allow Validator Service docket write-backs via the existing docket endpoint)
- [X] T015 [US1] Update or remove tests in `tests/Sorcha.Register.Core.Tests/` that depend on `TransactionManager.StoreTransactionAsync` being the public submission path. Ensure tests for `GetTransactionAsync` and query methods still pass.

**Checkpoint**: Action transactions route through Validator, enter mempool, get built into dockets, and are written back to Register with docket numbers. Ping-Pong walkthrough should pass with all transactions sealed in dockets.

---

## Phase 3: User Story 3 — Transaction Status: Pending vs Confirmed (Priority: P2)

**Goal**: The "transaction:confirmed" event fires only after docket sealing, not on direct submission.

**Independent Test**: Execute an action and verify no "transaction:confirmed" event fires until the docket is written to the Register.

- [ ] T016 [US3] In `src/Services/Sorcha.Register.Service/Program.cs`, add `IEventPublisher` injection and `transaction:confirmed` event publication to the docket write-back endpoint (the `POST /api/registers/{registerId}/dockets` handler, around line 1027-1035) — publish one event per transaction in the docket after successful persistence
- [ ] T017 [US3] Remove the `transaction:confirmed` event publication from `TransactionManager.StoreTransactionAsync` in `src/Core/Sorcha.Register.Core/Managers/TransactionManager.cs` (lines 60-73) — this event should no longer fire on direct storage since action transactions won't use this path
- [ ] T018 [US3] Update `TransactionManager` tests in `tests/Sorcha.Register.Core.Tests/Managers/TransactionManagerTests.cs` to remove expectations for `transaction:confirmed` event publication on `StoreTransactionAsync`
- [ ] T019 [US3] Write test verifying the docket write endpoint in Register Service publishes `transaction:confirmed` events for each transaction in the docket

**Checkpoint**: Confirmed events only fire after docket sealing. No premature "confirmed" signals.

---

## Phase 4: User Story 4 — Peer Gossip for Public Registers (Priority: P3)

**Goal**: Pending transactions on public registers are gossiped to remote validators.

**Independent Test**: Submit a transaction for a public register and verify Peer Service receives gossip.

**Note**: Per research decision R7, this is deferred to a follow-up feature. The tasks below are the minimal stubs needed if implementing now. Skip this phase for MVP.

- [ ] T020 [US4] Add `GossipTransactionAsync(string registerId, byte[] transactionData, CancellationToken)` method to `IPeerServiceClient` in `src/Common/Sorcha.ServiceClients/Peer/IPeerServiceClient.cs`
- [ ] T021 [US4] Implement `GossipTransactionAsync` in the Peer Service client (stub returning `Task.CompletedTask` for now)
- [ ] T022 [US4] In `src/Services/Sorcha.Validator.Service/Endpoints/ValidationEndpoints.cs`, after mempool addition, check if register is public (query Register Service) and call `_peerClient.GossipTransactionAsync()` if so
- [ ] T023 [US4] Write test verifying gossip is called for public registers and NOT called for private registers

**Checkpoint**: Public register transactions are gossiped. Private register transactions stay local.

---

## Phase 5: Polish & Verification

**Purpose**: End-to-end validation and documentation

- [X] T024 Build solution and verify zero new compiler errors: `dotnet build`
- [X] T025 Run full test suite and verify all existing tests pass: `dotnet test`
- [ ] T026 Run Ping-Pong walkthrough and verify all 10 action transactions have docket numbers: `pwsh walkthroughs/PingPong/test-ping-pong-workflow.ps1`
- [ ] T027 Run Organization Ping-Pong walkthrough and verify 10/10 steps, 40/40 actions: `pwsh walkthroughs/OrganizationPingPong/test-org-ping-pong.ps1`
- [ ] T028 Update `src/Services/Sorcha.Register.Service/README.md` transaction flow documentation to reflect the new pipeline
- [ ] T029 Update `.specify/MASTER-TASKS.md` with task status for 028

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Foundational)**: No dependencies — start immediately
- **Phase 2 (US1+US2)**: Depends on Phase 1 completion — BLOCKS walkthrough verification
- **Phase 3 (US3)**: Depends on Phase 2 — can run independently of Phase 4
- **Phase 4 (US4)**: Depends on Phase 1 — can run independently of Phase 2/3 (deferred for MVP)
- **Phase 5 (Polish)**: Depends on Phase 2+3 completion (Phase 4 optional)

### Within Phase 1

- T001, T002 must complete before T003 (interface before implementation)
- T004 is independent of T001-T003 (different file)
- T005, T006 are parallel (different test files)

### Within Phase 2

- T007 and T009-T010 can run in parallel (different services)
- T011 depends on T009 (needs `_validatorClient` injected first)
- T012 depends on T011 (polling follows submission)
- T013 depends on T011-T012 (tests the new flow)
- T014 can run in parallel with T007-T012 (different service)
- T015 depends on T014

### Parallel Opportunities

```
Phase 1 parallel:
  T001+T002 (interface) || T004 (mapper) — different files
  T005 || T006 — different test files

Phase 2 parallel:
  T007+T008 (Validator) || T009+T010 (Blueprint DI) || T014 (Register) — different services
```

---

## Implementation Strategy

### MVP (Phases 1+2)

1. Complete Phase 1: Service client layer + mapper
2. Complete Phase 2: Route action transactions through Validator + monitoring
3. **VALIDATE**: Run Ping-Pong walkthrough — all 10 transactions should have docket numbers
4. This alone fixes the core pipeline defect (SC-001, SC-002)

### Full Feature (Phases 1+2+3)

5. Complete Phase 3: Correct event timing
6. **VALIDATE**: Confirm events fire only after docket sealing
7. Skip Phase 4 (peer gossip) per research decision R7

### Verification

8. Complete Phase 5: Build, test, walkthroughs, docs

---

## Notes

- US1 and US2 are combined in Phase 2 because submitting to the Validator (US1) requires monitoring (US2) to actually build dockets — they are not independently useful
- US4 (peer gossip) is deferred per research R7 — single-validator mode doesn't need it
- The confirmation polling loop (T012) is the key sequential execution mechanism per user's decision (Option C)
- Existing `TransactionManager.StoreTransactionAsync` still works for the docket write-back path — it's only the direct submission that's being removed/restricted
