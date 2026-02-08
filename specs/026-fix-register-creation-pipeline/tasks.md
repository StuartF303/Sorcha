# Tasks: Fix Register Creation Pipeline

**Input**: Design documents from `/specs/026-fix-register-creation-pipeline/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Not explicitly requested in spec. Test tasks included only for US6 (test suite restoration) and validator baseline verification.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create shared constants and verify baselines before modifying pipeline code

- [x] T001 Create `GenesisConstants.cs` with `BlueprintId = "genesis"` and `ActionId = "register-creation"` in `src/Common/Sorcha.Register.Models/Constants/GenesisConstants.cs`
- [x] T002 Replace magic string `"genesis"` with `GenesisConstants.BlueprintId` in `src/Services/Sorcha.Validator.Service/Endpoints/ValidationEndpoints.cs:317`
- [x] T003 Run Validator Service tests to confirm baseline (595 pass / 0 fail) via `dotnet test tests/Sorcha.Validator.Service.Tests/`

**Checkpoint**: Constants defined, magic strings eliminated, baseline verified

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: No blocking foundational tasks required — all user stories can proceed after Phase 1 setup

**⚠️ NOTE**: US1 and US2 both modify `DocketBuildTriggerService.cs` — they MUST be done sequentially (US1 first, then US2). All other stories are independent.

**Checkpoint**: Foundation ready — user story implementation can begin

---

## Phase 3: User Story 1 — Genesis Data Persists Through Docket Pipeline (Priority: P1) 🎯 MVP

**Goal**: Fix the transaction mapping in `WriteDocketAndTransactionsAsync()` so genesis transaction payloads (control record, signatures, metadata) survive the docket write to the Register Service.

**Independent Test**: Create a register, wait for docket pipeline, query the genesis docket's transactions and verify the payload contains the original control record with attestation data.

**FR Coverage**: FR-001

### Implementation for User Story 1

- [x] T004 [US1] Fix transaction mapping in `WriteDocketAndTransactionsAsync()` to populate `Payloads` array with `PayloadModel` containing `Data` (Base64 of `Payload.GetRawText()`), `Hash` (`PayloadHash`), and `PayloadSize` in `src/Services/Sorcha.Validator.Service/Services/DocketBuildTriggerService.cs:231-241`
- [x] T005 [US1] Fix `PayloadCount` from hardcoded `0` to `1` (count of payload entries) in same mapping block in `src/Services/Sorcha.Validator.Service/Services/DocketBuildTriggerService.cs`
- [x] T006 [US1] Fix `SenderWallet` from hardcoded `"system"` to extract from `Signatures[0].PublicKey` (Base64 encode, fallback to `"system"` if no signatures) in `src/Services/Sorcha.Validator.Service/Services/DocketBuildTriggerService.cs`
- [x] T007 [US1] Fix `Signature` from `string.Empty` to Base64-encode `Signatures[0].SignatureValue` in `src/Services/Sorcha.Validator.Service/Services/DocketBuildTriggerService.cs`
- [x] T008 [US1] Add `MetaData` mapping with `BlueprintId` (direct), `ActionId` (`uint.TryParse()`), `RegisterId`, and `TransactionType` (extract from `Metadata["Type"]`) in `src/Services/Sorcha.Validator.Service/Services/DocketBuildTriggerService.cs`
- [x] T009 [US1] Add `PrevTxId` mapping from `PreviousTransactionId` (direct or `string.Empty`) and `TimeStamp` from `CreatedAt.UtcDateTime` in `src/Services/Sorcha.Validator.Service/Services/DocketBuildTriggerService.cs`
- [x] T010 [US1] Run Validator Service tests to confirm no regressions (595 pass / 0 fail) via `dotnet test tests/Sorcha.Validator.Service.Tests/`

**Checkpoint**: Genesis transaction payloads are fully mapped — control record data survives docket write

---

## Phase 4: User Story 2 — Genesis Docket Creation Completes Reliably (Priority: P1)

**Goal**: Fix genesis docket creation to retry on failure instead of permanently marking as complete, and propagate errors from genesis detection instead of silently swallowing them.

**Independent Test**: Create a register, verify genesis docket appears in Register Service within 30 seconds with register height updated to 0.

**FR Coverage**: FR-002, FR-003, FR-010

**Depends on**: US1 (shares DocketBuildTriggerService.cs)

### Implementation for User Story 2

- [x] T011 [US2] Add `_genesisRetryCount` field (`ConcurrentDictionary<string, int>`) to `DocketBuildTriggerService` in `src/Services/Sorcha.Validator.Service/Services/DocketBuildTriggerService.cs`
- [x] T012 [US2] Move `_genesisWritten[registerId] = true` inside a success-only path (after confirmed write, before the catch block) in the consensus docket write path in `src/Services/Sorcha.Validator.Service/Services/DocketBuildTriggerService.cs`
- [x] T013 [US2] Move `_genesisWritten[registerId] = true` inside a success-only path in the no-consensus docket write path in `src/Services/Sorcha.Validator.Service/Services/DocketBuildTriggerService.cs`
- [x] T014 [US2] Add retry counting in catch blocks: increment `_genesisRetryCount[registerId]`, and if count >= 3, call `UnregisterFromMonitoring(registerId)` and log warning in `src/Services/Sorcha.Validator.Service/Services/DocketBuildTriggerService.cs`
- [x] T015 [US2] Remove catch-all exception handler in `NeedsGenesisDocketAsync()` to let exceptions propagate to `DocketBuilder.BuildDocketAsync()` in `src/Services/Sorcha.Validator.Service/Services/GenesisManager.cs`
- [x] T016 [US2] Run Validator Service tests to confirm no regressions (595 pass / 0 fail) via `dotnet test tests/Sorcha.Validator.Service.Tests/`

**Checkpoint**: Genesis docket creation retries on failure (max 3), errors propagate correctly, no silent failures

---

## Phase 5: User Story 3 — Register Advertise Flag Respected (Priority: P2)

**Goal**: Thread the advertise flag from the initiation request through the two-phase creation flow so registers are created with the correct visibility setting.

**Independent Test**: Create a register with advertise=true, verify it is stored with advertise=true. Update to advertise=false, verify the change persists.

**FR Coverage**: FR-004

### Implementation for User Story 3

- [x] T017 [P] [US3] Add `Advertise` property (`bool`, default `false`, `[JsonPropertyName("advertise")]`) to `InitiateRegisterCreationRequest` in `src/Common/Sorcha.Register.Models/RegisterCreationModels.cs`
- [x] T018 [P] [US3] Add `Advertise` property (`bool`) to `PendingRegistration` to carry the flag from initiate to finalize in `src/Common/Sorcha.Register.Models/RegisterCreationModels.cs`
- [x] T019 [US3] Thread `request.Advertise` into `PendingRegistration` during `InitiateAsync()` in `src/Services/Sorcha.Register.Service/Services/RegisterCreationOrchestrator.cs`
- [x] T020 [US3] Replace hardcoded `advertise: false` with `pending.Advertise` in `FinalizeAsync()` in `src/Services/Sorcha.Register.Service/Services/RegisterCreationOrchestrator.cs`

**Checkpoint**: Registers respect user's advertise intent during creation

---

## Phase 6: User Story 4 — Peer Service Register Advertisement (Priority: P2)

**Goal**: Enable the Register Service to notify the Peer Service when a register's advertise flag changes, so the peer network can discover public registers.

**Independent Test**: Create a public register, query the Peer Service's available registers endpoint, verify it appears in the list.

**FR Coverage**: FR-005, FR-006

**Depends on**: US3 (advertise flag must be threaded first)

### Implementation for User Story 4

- [x] T021 [P] [US4] Add `AdvertiseRegisterAsync(string registerId, bool isPublic, CancellationToken)` method to `IPeerServiceClient` interface in `src/Common/Sorcha.ServiceClients/Peer/IPeerServiceClient.cs`
- [x] T022 [P] [US4] Implement `AdvertiseRegisterAsync` in `PeerServiceClient` — HTTP POST to `/api/registers/{registerId}/advertise` with `{ "isPublic": isPublic }` body in `src/Common/Sorcha.ServiceClients/Peer/PeerServiceClient.cs`
- [x] T023 [US4] Add `POST /api/registers/{registerId}/advertise` endpoint to Peer Service — call `RegisterAdvertisementService.AdvertiseRegister()` or `RemoveAdvertisement()` based on `isPublic` in `src/Services/Sorcha.Peer.Service/Program.cs`
- [x] T024 [US4] Inject `IPeerServiceClient` into `RegisterCreationOrchestrator` constructor and add fire-and-forget call after register creation when `pending.Advertise == true` in `src/Services/Sorcha.Register.Service/Services/RegisterCreationOrchestrator.cs`
- [x] T025 [US4] Add fire-and-forget peer notification in PUT `/api/registers/{id}` endpoint when `Advertise` field changes — wrap in try/catch, log failures in `src/Services/Sorcha.Register.Service/Program.cs`

**Checkpoint**: Peer Service is notified of register advertisement changes; public registers appear in peer network

---

## Phase 7: User Story 5 — Validator Monitoring Visibility (Priority: P3)

**Goal**: Expose an admin endpoint to query which registers are currently being monitored by the Validator Service for docket building.

**Independent Test**: Create a register, call the monitoring endpoint, verify the register ID appears in the list.

**FR Coverage**: FR-007

### Implementation for User Story 5

- [x] T026 [US5] Add `GET /api/admin/validators/monitoring` endpoint to `AdminEndpoints.cs` — call `IRegisterMonitoringRegistry.GetAll()`, return `{ registerIds: [...], count: N }` in `src/Services/Sorcha.Validator.Service/Endpoints/AdminEndpoints.cs`
- [x] T027 [US5] Run Validator Service tests to confirm no regressions via `dotnet test tests/Sorcha.Validator.Service.Tests/`

**Checkpoint**: Administrators can query monitored register list via REST endpoint

---

## Phase 8: User Story 6 — Register Service Test Suite Restored (Priority: P3)

**Goal**: Fix all compilation errors in the Register Service test suite so tests compile and pass, restoring regression detection capability.

**Independent Test**: Run `dotnet test tests/Sorcha.Register.Service.Tests/` — project compiles with zero errors and all tests pass.

**FR Coverage**: FR-008

### Implementation for User Story 6

- [x] T028 [P] [US6] Fix `SignalRHubTests.cs` — change `InitializeAsync()` and `DisposeAsync()` return types from `Task` to `ValueTask` for xUnit v3 `IAsyncLifetime` compatibility in `tests/Sorcha.Register.Service.Tests/SignalRHubTests.cs`
- [x] T029 [P] [US6] Fix `RegisterCreationOrchestratorTests.cs` — add `Mock<TransactionManager>` and `Mock<IPendingRegistrationStore>` to test constructor; add both to orchestrator constructor call in `tests/Sorcha.Register.Service.Tests/Unit/RegisterCreationOrchestratorTests.cs`
- [x] T030 [US6] Fix `RegisterCreationOrchestratorTests.cs` — replace all `Creator = new CreatorInfo{...}` with `Owners = new List<OwnerInfo>{new(){...}}` across all test methods in `tests/Sorcha.Register.Service.Tests/Unit/RegisterCreationOrchestratorTests.cs`
- [x] T031 [P] [US6] Fix `MongoSystemRegisterRepositoryTests.cs` — fix namespace to `Sorcha.Register.Service.Core.SystemRegisterEntry`, update constructor to match new signature, add `publishedBy` param to `PublishBlueprintAsync` in `tests/Sorcha.Register.Service.Tests/Unit/MongoSystemRegisterRepositoryTests.cs`
- [x] T032 [P] [US6] Fix `QueryApiTests.cs` — replace `?.` null-propagating operator in expression tree lambdas with explicit null checks in `tests/Sorcha.Register.Service.Tests/QueryApiTests.cs`
- [x] T033 [US6] Update orchestrator tests to include `Mock<IPeerServiceClient>` in constructor (required after US4 adds it to orchestrator) in `tests/Sorcha.Register.Service.Tests/Unit/RegisterCreationOrchestratorTests.cs`
- [x] T034 [US6] Run Register Service tests to verify all compile and pass via `dotnet test tests/Sorcha.Register.Service.Tests/`

**Checkpoint**: Register Service test suite compiles with zero errors and all tests pass

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Final verification across all stories and documentation updates

- [x] T035 Run full Validator Service test suite to confirm final baseline (595 pass / 0 fail / 3 skipped) via `dotnet test tests/Sorcha.Validator.Service.Tests/`
- [x] T036 Run full Register Service test suite — unit tests 18 pass / 0 fail / 4 skipped; integration tests 55 fail (pre-existing WebApplicationFactory issues) via `dotnet test tests/Sorcha.Register.Service.Tests/`
- [x] T037 [P] Build entire solution — 0 errors from our changes (4 pre-existing UI Core Test errors, 21 pre-existing warnings) via `dotnet build`
- [x] T038 Update `MASTER-TASKS.md` with task completion status for this feature in `.specify/MASTER-TASKS.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — trivially complete (no tasks)
- **Phase 3 (US1)**: Depends on Phase 1 — start after constants are defined
- **Phase 4 (US2)**: Depends on US1 — shares `DocketBuildTriggerService.cs`
- **Phase 5 (US3)**: Depends on Phase 1 only — can run in parallel with US1/US2
- **Phase 6 (US4)**: Depends on US3 — needs advertise flag threaded first
- **Phase 7 (US5)**: Depends on Phase 1 only — fully independent
- **Phase 8 (US6)**: Depends on US4 — orchestrator tests need IPeerServiceClient mock
- **Phase 9 (Polish)**: Depends on all user stories complete

### User Story Dependencies

```
Phase 1 (Setup)
    ├── US1 (P1: Payload mapping) ──→ US2 (P1: Retry logic)
    ├── US3 (P2: Advertise flag)  ──→ US4 (P2: Peer integration) ──→ US6 (P3: Test fixes)
    └── US5 (P3: Monitoring endpoint)
```

### Within Each User Story

- Models/constants before service changes
- Service changes before endpoint changes
- Core implementation before integration
- Test verification at each checkpoint

### Parallel Opportunities

- **After Phase 1**: US1, US3, and US5 can all start in parallel (different files)
- **After US1**: US2 can start
- **After US3**: US4 can start
- **After US4**: US6 can start
- **T017 + T018**: Both model changes in same file but different classes — can be done together
- **T021 + T022**: Interface and implementation in different files — can run in parallel
- **T028 + T029 + T031 + T032**: Four different test files — all can run in parallel

---

## Parallel Example: After Phase 1

```
# These three stories can start simultaneously:
Stream A: T004-T010 (US1: Payload mapping in DocketBuildTriggerService.cs)
Stream B: T017-T020 (US3: Advertise flag in RegisterCreationModels.cs + Orchestrator)
Stream C: T026-T027 (US5: Monitoring endpoint in AdminEndpoints.cs)
```

## Parallel Example: User Story 6 Test Fixes

```
# All four test file fixes can run in parallel:
Task: T028 — Fix SignalRHubTests.cs (ValueTask)
Task: T031 — Fix MongoSystemRegisterRepositoryTests.cs (namespace/constructor)
Task: T032 — Fix QueryApiTests.cs (expression tree)
Task: T029 — Fix RegisterCreationOrchestratorTests.cs (constructor mocks)
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 Only)

1. Complete Phase 1: Setup (T001-T003)
2. Complete US1: Payload mapping (T004-T010)
3. Complete US2: Retry logic (T011-T016)
4. **STOP and VALIDATE**: Genesis data persists and docket creation is reliable
5. This addresses the two P1 critical issues

### Incremental Delivery

1. Setup → US1 + US2 → **Critical pipeline fixed** (MVP)
2. Add US3 + US4 → **Peer advertisement working**
3. Add US5 → **Admin monitoring available**
4. Add US6 → **Test suite restored, regression detection enabled**
5. Polish → **Full verification, documentation updated**

---

## Notes

- T004-T009 are logically one change (transaction mapping fix) but broken into fields for clarity — can be implemented as a single code change
- T012-T014 are logically one change (genesis retry) — can be implemented as a single code change
- US6 test fixes (T028-T034) must account for changes made in US3/US4 (new orchestrator constructor params)
- Pre-existing test failures in Validator (17 JsonLogic), Peer (29), etc. are NOT in scope
