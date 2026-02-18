# Tasks: Unified Transaction Submission & System Wallet Signing Service

**Input**: Design documents from `/specs/036-unified-transaction-submission/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Included — plan.md explicitly requires unit tests for signing service and integration tests for submission flow (SC-008).

**Organization**: Tasks grouped by user story (5 stories from spec.md). US1 and US2 are P1 and can run in parallel. US3/US4 (P2) depend on US1. US5 (P3) depends on US3+US4.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1–US5)
- All file paths relative to repository root

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Verify project structure and create folder for new signing service

- [x] T001 Create SystemWallet directory at `src/Common/Sorcha.ServiceClients/SystemWallet/` and verify existing test project `tests/Sorcha.ServiceClients.Tests/` has correct references

---

## Phase 2: User Story 1 — System Wallet Signing Service (Priority: P1) MVP

**Goal**: Create a centralised, secure system wallet signing service with whitelist, rate limiting, and audit logging. Available via explicit opt-in DI registration.

**Independent Test**: Inject the signing service in a unit test, call SignAsync with a mocked IWalletServiceClient, verify signature is returned with correct audit log entries and security controls enforced.

**Dependencies**: None — can start immediately after Phase 1

### Models

- [x] T002 [P] [US1] Create `SystemSignResult` record in `src/Common/Sorcha.ServiceClients/SystemWallet/SystemSignResult.cs` per contract (Signature, PublicKey, Algorithm, WalletAddress — all required init)
- [x] T003 [P] [US1] Create `SystemWalletSigningOptions` configuration class in `src/Common/Sorcha.ServiceClients/SystemWallet/SystemWalletSigningOptions.cs` per contract (ValidatorId, AllowedDerivationPaths, MaxSignsPerRegisterPerMinute)
- [x] T004 [P] [US1] Create `ISystemWalletSigningService` interface in `src/Common/Sorcha.ServiceClients/SystemWallet/ISystemWalletSigningService.cs` per contract (SignAsync with 6 parameters)

### Implementation

- [x] T005 [US1] Implement `SystemWalletSigningService` in `src/Common/Sorcha.ServiceClients/SystemWallet/SystemWalletSigningService.cs` — wallet address caching (thread-safe SemaphoreSlim), whitelist validation, sliding-window rate limiting (ConcurrentDictionary), wallet lifecycle (create if missing, recreate on 401/not-found), signing via `IWalletServiceClient` using `{TxId}:{PayloadHash}` format with `isPreHashed: true`, structured audit logging for every operation
- [x] T006 [US1] Create `AddSystemWalletSigning` DI extension method in `src/Common/Sorcha.ServiceClients/SystemWallet/SystemWalletSigningExtensions.cs` — registers singleton `ISystemWalletSigningService`, binds `SystemWalletSigningOptions` from config section `"SystemWalletSigning"`, requires `IWalletServiceClient` already registered

### Tests

- [x] T007 [US1] Write unit tests for `SystemWalletSigningService` in `tests/Sorcha.ServiceClients.Tests/SystemWallet/SystemWalletSigningServiceTests.cs` — test scenarios: successful signing returns result, whitelist rejection for unknown derivation path, rate limit enforcement after max signs, wallet auto-creation on first call, wallet recreation on unavailability (401), audit log emitted on success, audit log emitted on rejection, concurrent signing thread safety

**Checkpoint**: System Wallet Signing Service is complete and independently testable. Can sign transactions with proper security controls.

---

## Phase 3: User Story 2 — Unified Transaction Submission (Priority: P1)

**Goal**: Make the validation pipeline process all transaction types uniformly — remove the signature verification skip for genesis/control so all transactions must provide valid signatures.

**Independent Test**: Submit a pre-signed genesis transaction to the generic endpoint, verify it passes signature verification (which was previously skipped) and enters the unverified pool.

**Dependencies**: None — can run in parallel with US1

### Implementation

- [x] T008 [US2] Remove signature verification skip for genesis/control transactions in `src/Services/Sorcha.Validator.Service/Services/ValidationEngine.cs` — locate the `IsGenesisOrControlTransaction()` check that skips signature verification (research R1: ~line 490-498), remove only the signature skip while preserving schema validation skip and blueprint conformance skip

### Tests

- [x] T009 [US2] Update validation pipeline tests in `tests/Sorcha.Validator.Service.Tests/` to provide valid signatures for genesis/control transaction test cases — any test that previously relied on the signature skip for genesis/control must now construct properly signed transactions with `{TxId}:{PayloadHash}` format

**Checkpoint**: Validator now verifies signatures for ALL transaction types. Schema and blueprint conformance skips for genesis/control remain correct.

---

## Phase 4: User Story 3 — Register Creation Uses Unified Submission (Priority: P2)

**Goal**: Migrate the register creation orchestrator to sign genesis transactions locally via `ISystemWalletSigningService` and submit through the generic validation endpoint instead of the legacy genesis endpoint.

**Independent Test**: Create a new register via the orchestrator, verify the genesis transaction is signed by the system wallet and submitted to `POST /api/v1/transactions/validate`, not `POST /api/validator/genesis`.

**Dependencies**: Depends on US1 (signing service must exist). Benefits from US2 (signatures now verified) but not strictly blocked.

### Implementation

- [x] T010 [US3] Register `ISystemWalletSigningService` in Register Service DI — add `builder.Services.AddSystemWalletSigning(builder.Configuration)` call in `src/Services/Sorcha.Register.Service/Program.cs` and add `SystemWalletSigning` configuration section
- [x] T011 [US3] Modify `RegisterCreationOrchestrator` in `src/Services/Sorcha.Register.Service/Services/RegisterCreationOrchestrator.cs` — inject `ISystemWalletSigningService`, replace `IValidatorServiceClient.SubmitGenesisTransactionAsync()` with: (1) build genesis transaction payload, (2) compute payload hash, (3) call `ISystemWalletSigningService.SignAsync()` with derivation path `"sorcha:register-control"`, (4) construct `ActionTransactionSubmission` with signature, (5) call `IValidatorServiceClient.SubmitTransactionAsync()` (generic endpoint), (6) set metadata `Type=Genesis`

### Tests

- [x] T012 [US3] Update `RegisterCreationOrchestrator` tests in `tests/Sorcha.Register.Core.Tests/` — mock `ISystemWalletSigningService.SignAsync()` to return a valid `SystemSignResult`, verify `SubmitTransactionAsync` (not `SubmitGenesisTransactionAsync`) is called with correct payload, signatures, and metadata

**Checkpoint**: Register creation uses the unified submission path. Genesis transactions are system-wallet-signed and validated through the generic endpoint.

---

## Phase 5: User Story 4 — Blueprint Publish Uses Unified Submission (Priority: P2)

**Goal**: Migrate the blueprint publish flow in Register Service to sign control transactions locally via `ISystemWalletSigningService` and submit through the generic validation endpoint.

**Independent Test**: Publish a blueprint to a register, verify the control transaction is signed by the system wallet and submitted to the generic endpoint, not the legacy genesis endpoint.

**Dependencies**: Depends on US1 (signing service) and T010 (DI already registered in Register Service by US3). Can run in parallel with US3 if T010 is completed first.

### Implementation

- [x] T013 [US4] Modify blueprint publish endpoint in `src/Services/Sorcha.Register.Service/Program.cs` (~line 1185 per research R3) — replace `IValidatorServiceClient.SubmitGenesisTransactionAsync()` with: (1) compute payload hash of blueprint control record, (2) call `ISystemWalletSigningService.SignAsync()` with derivation path `"sorcha:register-control"`, (3) construct `ActionTransactionSubmission` with signature, (4) call `IValidatorServiceClient.SubmitTransactionAsync()`, (5) set metadata `Type=Control` and `transactionType=BlueprintPublish`

### Tests

- [x] T014 [US4] Write or update blueprint publish tests in `tests/Sorcha.Register.Core.Tests/` — mock `ISystemWalletSigningService.SignAsync()`, verify `SubmitTransactionAsync` (not `SubmitGenesisTransactionAsync`) is called with control transaction payload, correct signatures, and Blueprint-specific metadata

**Checkpoint**: Blueprint publish uses the unified submission path. Control transactions are system-wallet-signed and validated through the generic endpoint.

---

## Phase 6: User Story 5 — Legacy Genesis Endpoint Deprecation (Priority: P3)

**Goal**: Remove the legacy genesis endpoint, its associated request models, and client methods. Only one submission method should remain on the validator service client.

**Independent Test**: Verify no code references `SubmitGenesisTransactionAsync`, the genesis endpoint route returns 404, and all system flows (register creation, blueprint publish) still work via the generic endpoint.

**Dependencies**: Depends on US3 and US4 (all callers migrated away from genesis endpoint)

### Implementation

- [x] T015 [P] [US5] Remove genesis endpoint handler from `src/Services/Sorcha.Validator.Service/Endpoints/ValidationEndpoints.cs` — delete the `POST /api/validator/genesis` route and its handler method (includes inline wallet acquisition, signing, and submission logic that is now replaced by the signing service)
- [x] T016 [P] [US5] Remove `SubmitGenesisTransactionAsync` method from `src/Common/Sorcha.ServiceClients/Validator/IValidatorServiceClient.cs` interface
- [x] T017 [P] [US5] Remove `SubmitGenesisTransactionAsync` implementation from `src/Common/Sorcha.ServiceClients/Validator/ValidatorServiceClient.cs`
- [x] T018 [US5] Remove genesis-related request/response models — locate and remove `GenesisTransactionSubmission`, `GenesisSignature`, `GenesisTransactionRequest` from `src/Common/Sorcha.ServiceClients/Validator/` (or wherever they reside in ServiceClients)
- [x] T019 [US5] Rename `ActionTransactionSubmission` to `TransactionSubmission` in `src/Common/Sorcha.ServiceClients/Validator/` — update class name, file name, and all references across the codebase (Register Service, Validator Service, Blueprint Service, test projects). Per contract doc: this is cosmetic but improves clarity now that all types use this model
- [x] T020 [US5] Update all references to renamed types and removed methods across test projects — fix compilation errors in `tests/Sorcha.Validator.Service.Tests/`, `tests/Sorcha.Register.Core.Tests/`, `tests/Sorcha.ServiceClients.Tests/`

### Tests

- [x] T021 [US5] Remove or update genesis-specific test cases in `tests/Sorcha.Validator.Service.Tests/Endpoints/` — delete tests for the removed genesis endpoint, ensure no test references `SubmitGenesisTransactionAsync`

**Checkpoint**: Legacy genesis endpoint and all associated models are fully removed. Single submission path remains.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, validation, and final verification

- [x] T022 [P] Update `.specify/MASTER-TASKS.md` with task status for 036-unified-transaction-submission
- [x] T023 [P] Update `docs/development-status.md` to reflect unified transaction submission completion
- [x] T024 [P] Add audit trail documentation for direct-write paths identified in research R3 (Blueprint Service Program.cs ~line 880 and ~line 1031 that bypass validator — document as known technical debt for future remediation)
- [x] T025 Run quickstart.md validation — execute build, test, and Docker rebuild commands from `specs/036-unified-transaction-submission/quickstart.md` and verify all pass
- [x] T026 Verify all affected test suites pass: `dotnet test tests/Sorcha.ServiceClients.Tests/`, `dotnet test tests/Sorcha.Validator.Service.Tests/`, `dotnet test tests/Sorcha.Register.Core.Tests/`

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup) ──► Phase 2 (US1) ──┬──► Phase 4 (US3) ──┬──► Phase 6 (US5) ──► Phase 7 (Polish)
                 ──► Phase 3 (US2) ──┘──► Phase 5 (US4) ──┘
```

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (US1)**: Depends on Phase 1
- **Phase 3 (US2)**: Depends on Phase 1 — **can run in parallel with Phase 2**
- **Phase 4 (US3)**: Depends on Phase 2 (US1 — signing service)
- **Phase 5 (US4)**: Depends on Phase 2 (US1) and T010 from US3 (DI registration)
- **Phase 6 (US5)**: Depends on Phase 4 (US3) and Phase 5 (US4) — all callers migrated
- **Phase 7 (Polish)**: Depends on all user stories complete

### User Story Dependencies

| Story | Depends On | Blocks |
|-------|-----------|--------|
| US1 (Signing Service) | Setup only | US3, US4 |
| US2 (Unified Submission) | Setup only | — (US3/US4 benefit but not blocked) |
| US3 (Register Creation) | US1 | US5 |
| US4 (Blueprint Publish) | US1, T010 from US3 | US5 |
| US5 (Legacy Cleanup) | US3, US4 | — |

### Within Each User Story

1. Models/interfaces before implementation
2. Implementation before DI registration
3. DI registration before integration/caller changes
4. All implementation before tests (tests verify the completed work)

### Parallel Opportunities

- **Phase 2 + Phase 3**: US1 and US2 have zero overlap — different projects, different files
- **T002 + T003 + T004**: Three model/interface files in US1, all independent
- **T015 + T016 + T017**: Three removal tasks in US5, all in different files
- **T022 + T023 + T024**: Documentation tasks in Polish phase

---

## Parallel Example: US1 + US2 Concurrent

```bash
# Agent A: US1 — System Wallet Signing Service
Task: T002 "Create SystemSignResult record"
Task: T003 "Create SystemWalletSigningOptions"
Task: T004 "Create ISystemWalletSigningService interface"
# then sequentially:
Task: T005 "Implement SystemWalletSigningService"
Task: T006 "Create AddSystemWalletSigning DI extension"
Task: T007 "Write signing service unit tests"

# Agent B: US2 — Unified Submission (in parallel with Agent A)
Task: T008 "Remove signature verification skip"
Task: T009 "Update validation pipeline tests"
```

## Parallel Example: US5 Removals

```bash
# All three removals in parallel (different files):
Task: T015 "Remove genesis endpoint handler"
Task: T016 "Remove genesis method from interface"
Task: T017 "Remove genesis method implementation"
# then sequentially:
Task: T018 "Remove genesis request models"
Task: T019 "Rename ActionTransactionSubmission"
Task: T020 "Update all references"
Task: T021 "Update test cases"
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: US1 — System Wallet Signing Service
3. **STOP and VALIDATE**: Run signing service tests, verify whitelist/rate-limit/audit
4. The signing service is immediately usable by any caller

### Incremental Delivery

1. Setup → **Foundation ready**
2. US1 (Signing Service) + US2 (Pipeline Fix) → **Core infrastructure complete**
3. US3 (Register Creation) → **First caller migrated — test end-to-end register flow**
4. US4 (Blueprint Publish) → **Second caller migrated — test blueprint flow**
5. US5 (Legacy Cleanup) → **Dead code removed, single submission path**
6. Polish → **Documentation current, all tests green**

### Risk Mitigation

- **Pre-existing test failures**: Validator Service has ~56 pre-existing failures (memory notes). Only count NEW failures as regressions.
- **Register Core pre-existing**: ~9 pre-existing failures. Baseline is ~139 pass.
- **Signing data format**: Already confirmed as `{TxId}:{PayloadHash}` — no format change needed (research R1).
- **Blueprint Service direct writes** (research R3 paths 6-7): Out of scope — documented as tech debt in T024.

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- Total tasks: 26 (1 setup + 6 US1 + 2 US2 + 3 US3 + 2 US4 + 7 US5 + 5 polish)
- Pre-existing test baselines documented in memory — do not count as regressions
- `SenderWallet = "system"` is NOT used for signed transactions — the signing service provides a real wallet address
