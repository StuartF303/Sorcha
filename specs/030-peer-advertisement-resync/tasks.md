# Tasks: Register-to-Peer Advertisement Resync

**Input**: Design documents from `/specs/030-peer-advertisement-resync/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/bulk-advertise.yaml
**Total Tasks**: 29
**Branch**: `030-peer-advertisement-resync`

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Exact file paths included in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add Redis dependency to Peer Service and create shared DTOs

- [x] T001 Add `Aspire.StackExchange.Redis` package reference to `src/Services/Sorcha.Peer.Service/Sorcha.Peer.Service.csproj`
- [x] T002 Add `builder.AddRedisClient("redis")` to `src/Services/Sorcha.Peer.Service/Program.cs` DI registration section
- [x] T003 [P] Create `BulkAdvertiseRequest`, `AdvertisementItem`, and `BulkAdvertiseResponse` DTOs in `src/Services/Sorcha.Peer.Service/Models/PeerManagementDtos.cs` (add to existing file)
- [x] T004 [P] Create `IRedisAdvertisementStore` interface in `src/Services/Sorcha.Peer.Service/Replication/IRedisAdvertisementStore.cs` with methods: `SetLocalAsync`, `SetRemoteAsync`, `GetAllLocalAsync`, `GetAllRemoteAsync`, `RemoveLocalAsync`, `RemoveLocalExceptAsync`, `RemoveRemoteByPeerAsync`

**Checkpoint**: Redis configured, shared types defined — user story implementation can begin

---

## Phase 2: User Story 1 — Advertisements Survive Service Restart (Priority: P1) MVP

**Goal**: After the Peer Service restarts, previously advertised registers are loaded from Redis and immediately available via `/api/registers/available` within 5 seconds (SC-002).

**Independent Test**: Advertise 5 registers, restart Peer Service, verify all 5 appear in Available Registers within 5 seconds.

**Acceptance**: FR-001, FR-002, FR-007, FR-008, FR-010

### Implementation

- [x] T005 [US1] Implement `RedisAdvertisementStore` in `src/Services/Sorcha.Peer.Service/Replication/RedisAdvertisementStore.cs`: constructor takes `IConnectionMultiplexer` + `ILogger`; key pattern `peer:advert:local:{registerId}` and `peer:advert:remote:{peerId}:{registerId}`; 300s TTL on all writes; JSON serialization; SCAN-based `GetAllLocalAsync`/`GetAllRemoteAsync`; `RemoveLocalExceptAsync` accepts a HashSet of register IDs to keep and deletes all other `peer:advert:local:*` keys
- [x] T006 [US1] Write unit tests for `RedisAdvertisementStore` in `tests/Sorcha.Peer.Service.Tests/Replication/RedisAdvertisementStoreTests.cs`: mock `IConnectionMultiplexer`/`IDatabase`; test SetLocal serialization + TTL, GetAllLocal deserialization, RemoveLocal key deletion, RemoveLocalExcept keeps correct keys, SetRemote with composite key, graceful handling when Redis unavailable (FR-010)
- [x] T007 [US1] Refactor `RegisterAdvertisementService` in `src/Services/Sorcha.Peer.Service/Replication/RegisterAdvertisementService.cs`: add `IRedisAdvertisementStore` constructor parameter; register as DI dependency in Program.cs (change from `AddSingleton<RegisterAdvertisementService>()` to register with factory or interface)
- [x] T008 [US1] Add write-through to Redis in `RegisterAdvertisementService.AdvertiseRegister()`: after updating `_localAdvertisements` dictionary, call `_store.SetLocalAsync()` fire-and-forget with try/catch (FR-010 fallback); same pattern in `RemoveAdvertisement()` → call `_store.RemoveLocalAsync()`
- [x] T009 [US1] Add Redis startup load in `RegisterAdvertisementService`: create `LoadFromRedisAsync()` method called from a new `IHostedService` or directly during startup; load all `peer:advert:local:*` entries → populate `_localAdvertisements`; load all `peer:advert:remote:*` entries → populate peer advertisement state; log count of loaded entries
- [x] T010 [US1] Update existing single-register `POST /api/registers/{registerId}/advertise` endpoint in `src/Services/Sorcha.Peer.Service/Program.cs` (line ~441): no code changes needed if AdvertiseRegister/RemoveAdvertisement already writes through — verify the endpoint still works correctly with the refactored service
- [x] T011 [US1] Update existing `RegisterAdvertisementServiceTests` in `tests/Sorcha.Peer.Service.Tests/Replication/RegisterAdvertisementServiceTests.cs`: add `Mock<IRedisAdvertisementStore>` to constructor; update all 18 existing test constructors; verify all existing tests still pass with the new dependency
- [x] T012 [US1] Run Peer Service test suite: `dotnet test tests/Sorcha.Peer.Service.Tests` — baseline 504 pass; verify no regressions from Redis refactor

**Checkpoint**: Peer Service persists ads to Redis, loads on startup. US1 acceptance scenarios independently verifiable.

---

## Phase 3: User Story 2 — Register Service Startup Re-Advertisement (Priority: P1)

**Goal**: When the Register Service starts, it pushes all `advertise: true` registers to the Peer Service via a bulk endpoint. Retries with backoff if Peer Service is unreachable (FR-003, FR-004).

**Independent Test**: Stop Peer Service, create a register, start Peer Service, verify new register appears within 60 seconds.

**Acceptance**: FR-003, FR-004, FR-005

### Implementation

- [x] T013 [US2] Add `POST /api/registers/bulk-advertise` endpoint to `src/Services/Sorcha.Peer.Service/Program.cs`: accept `BulkAdvertiseRequest` body; iterate advertisements and call `AdvertiseRegister()`/`RemoveAdvertisement()` for each; return `BulkAdvertiseResponse` with processed/added/updated counts; add OpenAPI docs `.WithName("BulkAdvertiseRegisters").WithSummary("Bulk advertise or sync register advertisements").WithTags("Registers")`
- [x] T014 [US2] Add `BulkAdvertiseAsync` method to `IPeerServiceClient` interface in `src/Common/Sorcha.ServiceClients/Peer/IPeerServiceClient.cs`: signature `Task<BulkAdvertiseResponse?> BulkAdvertiseAsync(BulkAdvertiseRequest request, CancellationToken cancellationToken = default)`
- [x] T015 [US2] Implement `BulkAdvertiseAsync` in `PeerServiceClient` in `src/Common/Sorcha.ServiceClients/Peer/PeerServiceClient.cs`: HTTP POST to `/api/registers/bulk-advertise`; follow same patterns as existing `AdvertiseRegisterAsync` (null check on `_httpClient`, debug logging, exception handling); deserialize `BulkAdvertiseResponse`
- [x] T016 [US2] Create `AdvertisementResyncService : BackgroundService` in `src/Services/Sorcha.Register.Service/Services/AdvertisementResyncService.cs`: constructor takes `IPeerServiceClient`, `IRegisterRepository`, `ILogger`; `ExecuteAsync`: query all registers with `Advertise == true` via `_repository.QueryRegistersAsync(r => r.Advertise)`, build `BulkAdvertiseRequest` with `FullSync = true`, call `_peerClient.BulkAdvertiseAsync()`; on `HttpRequestException`: retry with exponential backoff (1s, 2s, 4s, 8s, 16s, max 60s) per FR-004; structured logging for success/failure
- [x] T017 [US2] Register `AdvertisementResyncService` in `src/Services/Sorcha.Register.Service/Program.cs`: add `builder.Services.AddHostedService<AdvertisementResyncService>()`
- [x] T018 [US2] Write tests for `AdvertisementResyncService` in `tests/Sorcha.Register.Service.Tests/Services/AdvertisementResyncServiceTests.cs`: mock `IPeerServiceClient` and `IRegisterRepository`; test startup push queries all advertise=true registers; test BulkAdvertiseRequest is built with FullSync=true; test retry on HttpRequestException; test cancellation token respected; test empty register list sends empty advertisements array
- [x] T019 [US2] Run Register Service test suite: `dotnet test tests/Sorcha.Register.Service.Tests` — establish baseline; verify no regressions

**Checkpoint**: Register Service pushes all public registers to Peer Service on startup with retry. US2 acceptance scenarios independently verifiable.

---

## Phase 4: User Story 3 — Periodic Reconciliation (Priority: P2)

**Goal**: The Register Service periodically (every 5 minutes) reconciles advertisement state, self-healing any drift. The full-sync mode removes stale local advertisements (FR-006, FR-009, FR-011).

**Independent Test**: Delete a register's Redis entry manually. Within 5 minutes, verify it is restored.

**Acceptance**: FR-006, FR-009, FR-011

### Implementation

- [x] T020 [US3] Add periodic loop to `AdvertisementResyncService` in `src/Services/Sorcha.Register.Service/Services/AdvertisementResyncService.cs`: after initial startup push (US2), loop with `await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken)`; each iteration performs the same full-sync push; make interval configurable via `IOptions<AdvertisementResyncOptions>` with default 5 minutes
- [x] T021 [US3] Implement full-sync mode in bulk endpoint: in `POST /api/registers/bulk-advertise` handler in `src/Services/Sorcha.Peer.Service/Program.cs`, when `request.FullSync == true`, call `_store.RemoveLocalExceptAsync(advertisedIds)` to remove stale local ads; add removed count to response
- [x] T022 [US3] Add idempotency validation: in `RegisterAdvertisementService.AdvertiseRegister()`, skip Redis write if the advertisement data hasn't changed (same syncState, version, docketVersion, isPublic); log at Debug level when skipped; ensures FR-009 (no unnecessary side effects)
- [x] T023 [US3] Write reconciliation-specific tests in `tests/Sorcha.Register.Service.Tests/Services/AdvertisementResyncServiceTests.cs`: test periodic loop fires after delay; test full-sync removes stale entries; test idempotent behavior (no changes → no writes); test configurable interval via options

**Checkpoint**: System self-heals within 5 minutes. US3 acceptance scenarios independently verifiable.

---

## Phase 5: User Story 4 — Remote Peer Visibility (Priority: P3)

**Goal**: Remote peer advertisements received via gossip are persisted to Redis, surviving restarts and feeding into the unified Available Registers view (FR-001, FR-002).

**Independent Test**: With two nodes connected via gossip, create a register on Node A. Verify it appears in Node B's Available Registers.

**Acceptance**: FR-001 (remote ads), FR-002 (reverification)

### Implementation

- [x] T024 [US4] Add Redis write-through in `ProcessRemoteAdvertisementsAsync()` in `src/Services/Sorcha.Peer.Service/Replication/RegisterAdvertisementService.cs`: after updating `peer.AdvertisedRegisters`, also call `_store.SetRemoteAsync(peerId, registerInfo)` for each advertisement; fire-and-forget with try/catch for Redis failures (FR-010)
- [x] T025 [US4] Refactor `GetNetworkAdvertisedRegisters()` in `src/Services/Sorcha.Peer.Service/Replication/RegisterAdvertisementService.cs`: in addition to scanning `PeerNode.AdvertisedRegisters`, also include local advertisements from `_localAdvertisements` in the aggregation (currently only scans remote peers); this ensures the Available Registers endpoint shows both local and remote registers in a unified view
- [x] T026 [US4] Add remote advertisement startup load: in the `LoadFromRedisAsync()` method (created in T009), load `peer:advert:remote:*` entries and populate peer advertisement state via `ProcessRemoteAdvertisementsAsync()`; these loaded entries serve as a warm cache until the next gossip exchange refreshes them (or they expire via 5-min TTL)
- [x] T027 [US4] Write tests for remote advertisement persistence in `tests/Sorcha.Peer.Service.Tests/Replication/RegisterAdvertisementServiceTests.cs`: test ProcessRemoteAdvertisementsAsync calls SetRemoteAsync on store; test GetNetworkAdvertisedRegisters includes local ads; test startup load restores remote ads from Redis

**Checkpoint**: All user stories complete. Remote peer advertisements persist across restarts.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, verification, edge cases

- [x] T028 Update `src/Services/Sorcha.Peer.Service/Program.cs` to handle Redis unavailability gracefully in the bulk endpoint (FR-010): wrap Redis operations in try/catch, log warning, continue with in-memory-only operation
- [x] T029 Run full test suites and build verification: `dotnet build src/Services/Sorcha.Peer.Service && dotnet build src/Services/Sorcha.Register.Service && dotnet build src/Common/Sorcha.ServiceClients && dotnet test tests/Sorcha.Peer.Service.Tests && dotnet test tests/Sorcha.Register.Service.Tests` — verify Peer Service >= 504 pass baseline, no build warnings

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **US1 (Phase 2)**: Depends on Setup (Phase 1) — BLOCKS US2, US3, US4
- **US2 (Phase 3)**: Depends on US1 (needs bulk endpoint + Redis store)
- **US3 (Phase 4)**: Depends on US2 (extends AdvertisementResyncService with periodic loop)
- **US4 (Phase 5)**: Depends on US1 (needs Redis store for remote ads); can run parallel with US2/US3
- **Polish (Phase 6)**: Depends on all stories complete

### User Story Dependencies

```
Phase 1: Setup ──────────────┐
                              ▼
Phase 2: US1 (P1) ──────────┬───────────────┐
         Redis persistence   │               │
                              ▼               ▼
Phase 3: US2 (P1)      Phase 5: US4 (P3)
         Startup push          Remote persistence
                │               │
                ▼               │
Phase 4: US3 (P2)             │
         Periodic reconcil.    │
                │               │
                ▼               ▼
Phase 6: Polish ◄─────────────┘
```

### Within Each User Story

- Models/DTOs before services
- Services before endpoints
- Core implementation before tests
- Story complete before moving to next priority

### Parallel Opportunities

- **Phase 1**: T003 and T004 can run in parallel (different files)
- **Phase 2 (US1)**: T005 and T006 can potentially overlap (store impl + store tests)
- **Phase 3 (US2)**: T014 and T015 can overlap (interface + implementation, different files)
- **US2 and US4**: Can run in parallel after US1 completes (different services/files)

---

## Parallel Example: User Story 1

```bash
# After Setup complete, launch store implementation + tests in parallel:
Task: "T005 [US1] Implement RedisAdvertisementStore in .../RedisAdvertisementStore.cs"
Task: "T006 [US1] Write unit tests for RedisAdvertisementStore in .../RedisAdvertisementStoreTests.cs"

# Then sequentially: refactor existing service, add write-through, startup load
```

## Parallel Example: User Story 2

```bash
# Interface and implementation can overlap:
Task: "T014 [US2] Add BulkAdvertiseAsync to IPeerServiceClient"
Task: "T015 [US2] Implement BulkAdvertiseAsync in PeerServiceClient"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T004)
2. Complete Phase 2: US1 — Redis Persistence (T005-T012)
3. **STOP and VALIDATE**: Restart Peer Service, verify ads reload from Redis in <5s
4. This alone fixes the core bug for single-node deployments

### Incremental Delivery

1. Setup + US1 → Peer Service restart fix (MVP)
2. Add US2 → Register Service startup push (full system restart fix)
3. Add US3 → Periodic reconciliation (self-healing)
4. Add US4 → Remote peer visibility (multi-node fix)
5. Polish → Edge cases, docs, verification

### Task Counts by Story

| Phase | Story | Tasks | Priority |
|-------|-------|-------|----------|
| Phase 1 | Setup | 4 | - |
| Phase 2 | US1 | 8 | P1 |
| Phase 3 | US2 | 7 | P1 |
| Phase 4 | US3 | 4 | P2 |
| Phase 5 | US4 | 4 | P3 |
| Phase 6 | Polish | 2 | - |
| **Total** | | **29** | |

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Baseline test counts: Peer Service 504 pass, Register Service TBD
- Redis key pattern: `peer:advert:local:{registerId}` / `peer:advert:remote:{peerId}:{registerId}`
- TTL: 300 seconds (5 minutes) on all advertisement keys
- All Redis operations are fire-and-forget with try/catch for FR-010 compliance
