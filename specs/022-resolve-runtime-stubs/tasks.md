# Tasks: Resolve Runtime Stubs and Production-Critical TODOs

**Input**: Design documents from `/specs/022-resolve-runtime-stubs/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-changes.md, quickstart.md

**Tests**: Tests are included per constitution requirement (>85% coverage on new code).

**Organization**: Tasks are grouped by user story (7 stories mapped to 7 implementation groups A-G).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Branch preparation and baseline verification

- [ ] T001 Verify all existing tests pass on branch (run `dotnet test` across Validator Service, Register Core, TransactionHandler, Wallet, Cryptography, Peer Service)
- [ ] T002 Run static analysis scan for all current `NotImplementedException` and production-critical TODO locations to establish baseline count

**Checkpoint**: Branch is clean, baseline established

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Repository and shared infrastructure changes needed by multiple stories

**CRITICAL**: No user story work can begin until this phase is complete

- [ ] T003 Add `GetAccessByIdAsync(Guid accessId, CancellationToken)` method to `IWalletRepository` interface in `src/Common/Sorcha.Wallet.Core/Repositories/Interfaces/IWalletRepository.cs`
- [ ] T004 [P] Implement `GetAccessByIdAsync` in `InMemoryWalletRepository` in `src/Common/Sorcha.Wallet.Core/Repositories/Implementation/InMemoryWalletRepository.cs`
- [ ] T005 [P] Implement `GetAccessByIdAsync` in `EfCoreWalletRepository` in `src/Common/Sorcha.Wallet.Core/Repositories/EfCoreWalletRepository.cs`
- [ ] T006 Write tests for `GetAccessByIdAsync` in both repository implementations in `tests/Sorcha.Wallet.Core.Tests/`

**Checkpoint**: Foundation ready — user story implementation can begin

---

## Phase 3: User Story 1 — Eliminate Runtime Exceptions from Stubs (Priority: P1) MVP

**Goal**: Zero `NotImplementedException` across all reachable code paths

**Independent Test**: Invoke each previously-stubbed operation and verify no unhandled exception occurs

### Implementation

- [ ] T007 [P] [US1] Replace `NotImplementedException` in `WalletManager.GenerateNewAddressAsync` with structured 400 error response explaining client-side derivation in `src/Common/Sorcha.Wallet.Core/Services/Implementation/WalletManager.cs` (line ~409)
- [ ] T008 [P] [US1] Implement `DelegationService.UpdateAccessAsync` using `GetAccessByIdAsync` repository method — retrieve access record, apply updates, persist changes in `src/Common/Sorcha.Wallet.Core/Services/Implementation/DelegationService.cs` (line ~210)
- [ ] T009 [P] [US1] Replace `NotImplementedException` with `NotSupportedException("Binary serialization is not supported. Use JSON format.")` in `JsonTransactionSerializer.SerializeBinary` and `DeserializeBinary` in `src/Common/Sorcha.TransactionHandler/Serialization/JsonTransactionSerializer.cs` (lines ~110, ~117)
- [ ] T010 [P] [US1] Replace `NotImplementedException` with `NotSupportedException` in `Transaction.SerializeBinary` in `src/Common/Sorcha.TransactionHandler/Core/Transaction.cs` (line ~161)

### Tests

- [ ] T011 [P] [US1] Write tests for `WalletManager.GenerateNewAddressAsync` — verify structured error returned (no exception) in `tests/Sorcha.Wallet.Core.Tests/`
- [ ] T012 [P] [US1] Write tests for `DelegationService.UpdateAccessAsync` — verify access record updated and persisted in `tests/Sorcha.Wallet.Core.Tests/`
- [ ] T013 [P] [US1] Write tests for binary serialization methods — verify `NotSupportedException` thrown (not `NotImplementedException`) in `tests/Sorcha.TransactionHandler.Tests/`

**Checkpoint**: `grep -r "NotImplementedException" src/` returns zero results in modified files

---

## Phase 4: User Story 2 — Secure Wallet and Delegation Access (Priority: P1)

**Goal**: Wallet endpoints enforce ownership/delegation authorization; JWT claims extracted correctly; bootstrap returns token

**Independent Test**: Attempt wallet operations as different users — unauthorized access returns 403

### Implementation

- [ ] T014 [US2] Update `GetCurrentUser` in `WalletEndpoints` to return 401 Unauthorized when no JWT identity present (replace "anonymous" fallback) in `src/Services/Sorcha.Wallet.Service/Endpoints/WalletEndpoints.cs` (lines ~1038-1048)
- [ ] T015 [US2] Update `GetCurrentUser` in `DelegationEndpoints` to return 401 Unauthorized when no JWT identity (replace "anonymous" fallback) in `src/Services/Sorcha.Wallet.Service/Endpoints/DelegationEndpoints.cs` (lines ~243-247)
- [ ] T016 [US2] Add ownership/delegation authorization check to `GetWallet` endpoint — verify caller is owner or has delegated access before returning wallet data in `src/Services/Sorcha.Wallet.Service/Endpoints/WalletEndpoints.cs` (line ~264)
- [ ] T017 [US2] Implement bootstrap token generation — call `ITokenService.GenerateTokenAsync` after admin user creation instead of deferring to login in `src/Services/Sorcha.Tenant.Service/Endpoints/BootstrapEndpoints.cs` (line ~123)

### Tests

- [ ] T018 [P] [US2] Write endpoint tests for wallet authorization — owner access allowed, non-owner returns 403, delegated access allowed in `tests/Sorcha.Wallet.Service.Tests/`
- [ ] T019 [P] [US2] Write endpoint tests for JWT claim extraction — missing JWT returns 401 in `tests/Sorcha.Wallet.Service.Tests/`
- [ ] T020 [P] [US2] Write test for bootstrap token generation — verify response includes `accessToken` field in `tests/Sorcha.Tenant.Service.Tests/`

**Checkpoint**: Wallet endpoints reject unauthorized access; bootstrap returns usable token

---

## Phase 5: User Story 3 — Validator Network Integration with Peer Service (Priority: P2)

**Goal**: Validators communicate via real gRPC calls for consensus, heartbeats, and registration

**Independent Test**: Deploy 2+ validators and verify consensus round completes with real inter-validator communication

### Implementation

- [ ] T021 [US3] Wire `SignatureCollector.RequestSignatureFromValidatorAsync` to create gRPC channel to peer validator endpoint and call `ValidatorService.RequestVote` — replace simulated response in `src/Services/Sorcha.Validator.Service/Services/SignatureCollector.cs` (line ~232)
- [ ] T022 [US3] Wire `RotatingLeaderElectionService` heartbeat broadcasting — use `IPeerServiceClient` to announce leader status to peer validators in `src/Services/Sorcha.Validator.Service/Services/RotatingLeaderElectionService.cs` (line ~165)
- [ ] T023 [US3] Implement on-chain validator registration in `ValidatorRegistry` — create registration transactions via `IRegisterServiceClient` for register/approve/deregister operations in `src/Services/Sorcha.Validator.Service/Services/ValidatorRegistry.cs` (lines ~285, ~316, ~436)
- [ ] T024 [US3] Wire `ValidationEngineService` register discovery — call `IRegisterMonitoringRegistry.GetMonitoredRegistersAsync()` instead of empty list in `src/Services/Sorcha.Validator.Service/Services/ValidationEngineService.cs` (line ~73)
- [ ] T025 [US3] Implement `ConsensusFailureHandler` persistence — store `ConsensusFailureRecord` in Redis with 30-day TTL in `src/Services/Sorcha.Validator.Service/Services/ConsensusFailureHandler.cs` (line ~182)
- [ ] T026 [US3] Wire `DocketBuildTriggerService` to initiate consensus process after building a docket in `src/Services/Sorcha.Validator.Service/Services/DocketBuildTriggerService.cs` (line ~113)

### Tests

- [ ] T027 [P] [US3] Write tests for `SignatureCollector` gRPC integration — mock gRPC channel, verify `RequestVote` called with correct docket data in `tests/Sorcha.Validator.Service.Tests/`
- [ ] T028 [P] [US3] Write tests for `RotatingLeaderElectionService` heartbeat — verify `IPeerServiceClient` called on heartbeat interval in `tests/Sorcha.Validator.Service.Tests/`
- [ ] T029 [P] [US3] Write tests for `ValidatorRegistry` on-chain registration — verify `IRegisterServiceClient` called with registration transaction in `tests/Sorcha.Validator.Service.Tests/`
- [ ] T030 [P] [US3] Write tests for `ValidationEngineService` register discovery — verify monitored registers returned (not empty) in `tests/Sorcha.Validator.Service.Tests/`
- [ ] T031 [P] [US3] Write tests for `ConsensusFailureHandler` persistence — verify Redis write and TTL in `tests/Sorcha.Validator.Service.Tests/`
- [ ] T032 [P] [US3] Write tests for `DocketBuildTriggerService` consensus triggering — verify consensus engine called after docket build in `tests/Sorcha.Validator.Service.Tests/`

**Checkpoint**: Validator consensus uses real gRPC; failures persisted; registers discovered dynamically

---

## Phase 6: User Story 4 — Accurate Peer Node Status Reporting (Priority: P2)

**Goal**: Peer node metrics reflect actual system state (no hardcoded zeros)

**Independent Test**: Query peer node status and verify non-zero values matching known system state

### Implementation

- [ ] T033 [P] [US4] Add `Stopwatch`-based uptime tracking to Peer Service startup — inject uptime provider into `HubNodeConnectionService` and `HeartbeatService` in `src/Services/Sorcha.Peer.Service/Services/HubNodeConnectionService.cs` (lines ~116-218) and `src/Services/Sorcha.Peer.Service/Services/HeartbeatService.cs` (lines ~74, ~163, ~228)
- [ ] T034 [P] [US4] Inject `SystemRegisterCache` (or repository) into `HubNodeConnectionService` — replace hardcoded `CurrentSystemRegisterVersion = 0`, `TotalBlueprints = 0`, `LastBlueprintPublishedAt = 0` with real values in `src/Services/Sorcha.Peer.Service/Services/HubNodeConnectionService.cs`
- [ ] T035 [P] [US4] Inject `SystemRegisterCache` into `HeartbeatService` — replace hardcoded system register version with real value in `src/Services/Sorcha.Peer.Service/Services/HeartbeatService.cs`
- [ ] T036 [P] [US4] Wire real session IDs from connection manager into `PeriodicSyncService` and `HeartbeatMonitorService` — replace `SessionId = string.Empty` in `src/Services/Sorcha.Peer.Service/Replication/PeriodicSyncService.cs` (line ~149) and `src/Services/Sorcha.Peer.Service/Monitoring/HeartbeatMonitorService.cs` (line ~127)
- [ ] T037 [US4] Update `ValidatorGrpcService.GetHealthStatus` — replace `ActiveRegisters = 0` with actual count from `IRegisterMonitoringRegistry` in `src/Services/Sorcha.Validator.Service/GrpcServices/ValidatorGrpcService.cs` (line ~179)

### Tests

- [ ] T038 [P] [US4] Write tests for `HubNodeConnectionService` — verify status reports contain non-zero system register version and blueprint count when data exists in `tests/Sorcha.Peer.Service.Tests/`
- [ ] T039 [P] [US4] Write tests for `HeartbeatService` — verify heartbeat messages contain real system register version in `tests/Sorcha.Peer.Service.Tests/`
- [ ] T040 [P] [US4] Write tests for uptime tracking — verify uptime increases over time (not always zero) in `tests/Sorcha.Peer.Service.Tests/`

**Checkpoint**: All peer node metrics reflect actual system state

---

## Phase 7: User Story 5 — Production-Ready Data Persistence (Priority: P2)

**Goal**: Pending registrations and memory pool survive service restarts; shared across instances

**Independent Test**: Store data, restart service, verify data survives

### Implementation

- [ ] T041 [US5] Rewrite `PendingRegistrationStore` — replace `ConcurrentDictionary` with Redis Hash operations following MemPoolManager pattern (key: `register:pending:{registerId}`, JSON serialization) in `src/Services/Sorcha.Register.Service/Services/PendingRegistrationStore.cs`
- [ ] T042 [US5] Update DI registration for `PendingRegistrationStore` — inject `IConnectionMultiplexer` in `src/Services/Sorcha.Register.Service/Program.cs` (line ~363)
- [ ] T043 [US5] Add memory pool persistence option to `ValidatorOrchestrator` — ensure pooled transactions can be recovered after restart by leveraging existing Redis-backed MemPoolManager in `src/Services/Sorcha.Validator.Service/Services/ValidatorOrchestrator.cs` (line ~96)

### Tests

- [ ] T044 [P] [US5] Write tests for Redis-backed `PendingRegistrationStore` — verify Add, TryRemove, Exists, CleanupExpired using mock `IDatabase` in `tests/Sorcha.Register.Core.Tests/`
- [ ] T045 [P] [US5] Write tests for `ValidatorOrchestrator` persistence — verify transactions recovered from pool after simulated restart in `tests/Sorcha.Validator.Service.Tests/`

**Checkpoint**: Pending registrations and pool data persist across restarts

---

## Phase 8: User Story 6 — Cryptographic Key Recovery and Keychain Portability (Priority: P3)

**Goal**: Users can recover keys from backup data and export/import keychains with password encryption

**Independent Test**: Create wallet, export keychain, import on fresh instance, verify keys match

### Implementation

- [ ] T046 [US6] Implement `CryptoModule.RecoverKeySetAsync` — use NBitcoin to reconstruct key set from mnemonic/key data with optional password in `src/Common/Sorcha.Cryptography/Core/CryptoModule.cs` (line ~56)
- [ ] T047 [US6] Implement `KeyChain.ExportAsync` — serialize keychain to JSON, derive encryption key from password via PBKDF2, encrypt with AES-256-GCM, include salt/IV/checksum in `src/Common/Sorcha.Cryptography/Models/KeyChain.cs` (line ~112)
- [ ] T048 [US6] Implement `KeyChain.ImportAsync` — derive key from password, decrypt AES-256-GCM, verify checksum, restore keychain state in `src/Common/Sorcha.Cryptography/Models/KeyChain.cs` (line ~130)

### Tests

- [ ] T049 [P] [US6] Write tests for `RecoverKeySetAsync` — valid recovery, corrupted data returns error, wrong password returns error in `tests/Sorcha.Cryptography.Tests/`
- [ ] T050 [P] [US6] Write tests for keychain round-trip — export with password, import with same password, verify keys match; import with wrong password fails in `tests/Sorcha.Cryptography.Tests/`

**Checkpoint**: Key recovery and keychain export/import functional

---

## Phase 9: User Story 7 — Transaction Version Backward Compatibility (Priority: P3)

**Goal**: System can read V1/V2/V3 format transactions; binary stubs replaced with clear errors

**Independent Test**: Create transactions in each version format, verify deserialization succeeds

### Implementation

- [ ] T051 [US7] Implement V3 transaction adapter in `TransactionFactory` — map V3 fields to current V4 model with minimal transformation in `src/Common/Sorcha.TransactionHandler/Versioning/TransactionFactory.cs` (line ~91)
- [ ] T052 [US7] Implement V2 transaction adapter — map V2 fields to V4 model, add defaults for missing fields in `src/Common/Sorcha.TransactionHandler/Versioning/TransactionFactory.cs` (line ~99)
- [ ] T053 [US7] Implement V1 transaction adapter — map V1 fields to V4 model, add defaults for all missing fields in `src/Common/Sorcha.TransactionHandler/Versioning/TransactionFactory.cs` (line ~107)
- [ ] T054 [US7] Implement version detection and adapter dispatch — detect transaction version from JSON, route to correct adapter in `src/Common/Sorcha.TransactionHandler/Versioning/TransactionFactory.cs` (line ~116)

### Tests

- [ ] T055 [P] [US7] Write tests for V3 adapter — deserialize V3 JSON, verify all fields accessible in `tests/Sorcha.TransactionHandler.Tests/`
- [ ] T056 [P] [US7] Write tests for V2 adapter — deserialize V2 JSON, verify defaults applied for missing fields in `tests/Sorcha.TransactionHandler.Tests/`
- [ ] T057 [P] [US7] Write tests for V1 adapter — deserialize V1 JSON, verify all defaults and field mapping in `tests/Sorcha.TransactionHandler.Tests/`
- [ ] T058 [P] [US7] Write tests for version detection — verify correct adapter selected for each version in `tests/Sorcha.TransactionHandler.Tests/`

**Checkpoint**: All transaction versions V1-V4 readable by current system

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Final verification and documentation

- [ ] T059 Run full static analysis scan — verify zero `NotImplementedException` in `src/` and zero production-critical TODOs resolved
- [ ] T060 Run full test suite across all projects — verify no regressions from baseline (595 Validator, 148 Register Core, 88 Fluent, 323 Engine)
- [ ] T061 [P] Update MASTER-TASKS.md with completed stub resolutions in `.specify/MASTER-TASKS.md`
- [ ] T062 [P] Update XML documentation on all modified public API endpoints

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS User Stories 1 and 2
- **US1 (Phase 3)**: Depends on Foundational (T003-T006)
- **US2 (Phase 4)**: Depends on Foundational (T003-T006) — can run in parallel with US1
- **US3 (Phase 5)**: Can start after Setup (no foundational dependency)
- **US4 (Phase 6)**: Can start after Setup (no foundational dependency)
- **US5 (Phase 7)**: Can start after Setup (no foundational dependency)
- **US6 (Phase 8)**: Can start after Setup (no foundational dependency)
- **US7 (Phase 9)**: Can start after US1 T009/T010 (binary stubs done first)
- **Polish (Phase 10)**: Depends on all user stories complete

### User Story Dependencies

- **US1 (P1)**: Depends on Foundational only — no other story dependencies
- **US2 (P1)**: Depends on Foundational only — no other story dependencies
- **US3 (P2)**: Independent — no foundational or story dependencies
- **US4 (P2)**: Independent — no foundational or story dependencies
- **US5 (P2)**: Independent — no foundational or story dependencies
- **US6 (P3)**: Independent — no foundational or story dependencies
- **US7 (P3)**: Depends on US1 T009/T010 (binary stubs must be replaced before version adapters built)

### Parallel Opportunities

Within each phase, tasks marked [P] can execute concurrently:
- Phase 2: T004 and T005 in parallel
- Phase 3: T007, T008, T009, T010 all in parallel; T011, T012, T013 all in parallel
- Phase 4: T018, T019, T020 in parallel
- Phase 5: T027-T032 all in parallel
- Phase 6: T033-T036 all in parallel; T038-T040 all in parallel
- Phase 7: T044, T045 in parallel
- Phase 8: T049, T050 in parallel
- Phase 9: T055-T058 all in parallel

Across phases (different services, no file conflicts):
- US3 + US4 can run simultaneously (Validator + Peer services)
- US5 + US6 can run simultaneously (Register + Cryptography)

---

## Implementation Strategy

### MVP First (US1 + US2 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (repository method)
3. Complete Phase 3: US1 — Zero NotImplementedExceptions
4. Complete Phase 4: US2 — Wallet authorization secured
5. **STOP and VALIDATE**: All runtime stubs eliminated, wallet access secured
6. This alone delivers significant production readiness improvement

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. US1 → Zero runtime crashes (MVP!)
3. US2 → Wallet security (MVP+)
4. US3 → Validator network integration
5. US4 → Peer metrics accuracy
6. US5 → Data persistence
7. US6 → Crypto completeness
8. US7 → Version compatibility
9. Polish → Final verification

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable
- Commit after each phase completion
- Test baseline: 595 Validator, 148 Register Core — must not regress
- Binary serialization: `NotSupportedException` (not full implementation) per user decision
