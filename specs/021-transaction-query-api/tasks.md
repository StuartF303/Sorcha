# Tasks: Transaction Query API

**Input**: Design documents from `/specs/021-transaction-query-api/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/query-api.md

**Tests**: Included — constitution requires >80% coverage for core libraries and >85% for new code.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Verify baseline and establish branch readiness

- [x] T001 Verify all existing tests pass on branch — run `dotnet test` for Register Core, Register Service, Validator Service, and ServiceClients projects to establish baseline
- [x] T002 Verify PrevTxId field exists on TransactionModel in `src/Common/Sorcha.Register.Models/TransactionModel.cs` — confirm it is `string PrevTxId` with `[StringLength(64, MinimumLength = 64)]` attribute

**Checkpoint**: Baseline established, PrevTxId field confirmed present.

---

## Phase 2: Foundational (Storage Layer — Blocks All User Stories)

**Purpose**: Add PrevTxId index and repository method across all storage implementations. These MUST be complete before any user story can be implemented.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T003 Add ascending index on `PrevTxId` to `CreateTransactionIndexesAsync` in `src/Core/Sorcha.Register.Storage.MongoDB/MongoRegisterRepository.cs` — add `new(Builders<TransactionModel>.IndexKeys.Ascending(t => t.PrevTxId))` to the existing index list
- [x] T004 Add `GetTransactionsByPrevTxIdAsync(string registerId, string prevTxId, CancellationToken ct)` returning `Task<IEnumerable<TransactionModel>>` to `IRegisterRepository` interface in `src/Core/Sorcha.Register.Core/Storage/IRegisterRepository.cs` — follow pattern of `GetAllTransactionsBySenderAddressAsync`
- [x] T005 Implement `GetTransactionsByPrevTxIdAsync` in `src/Core/Sorcha.Register.Storage.MongoDB/MongoRegisterRepository.cs` — use `Builders<TransactionModel>.Filter.Eq(t => t.PrevTxId, prevTxId)`, `Find`, `SortByDescending(t => t.TimeStamp)`, `ToListAsync`; return empty if prevTxId is null/empty
- [x] T006 [P] Implement `GetTransactionsByPrevTxIdAsync` in `src/Core/Sorcha.Register.Storage.InMemory/InMemoryRegisterRepository.cs` — filter with `.Where(t => t.PrevTxId == prevTxId)`, `.OrderByDescending(t => t.TimeStamp)`, `.ToList()`; return `Enumerable.Empty<TransactionModel>()` if register not found or prevTxId is null/empty
- [x] T007 Add `GetTransactionsByPrevTxIdPaginatedAsync(string registerId, string prevTxId, int page, int pageSize, CancellationToken ct)` returning `Task<PaginatedResult<TransactionModel>>` to `QueryManager` in `src/Core/Sorcha.Register.Core/Managers/QueryManager.cs` — follow pagination pattern from `GetTransactionsByWalletPaginatedAsync`: validate args, call repository, OrderByDescending(TimeStamp), Skip/Take, return PaginatedResult
- [x] T008 Build all modified projects — run `dotnet build` for Sorcha.Register.Core, Sorcha.Register.Storage.MongoDB, Sorcha.Register.Storage.InMemory to verify compilation

### Tests for Foundational Phase

- [x] T009 [P] Add QueryManager PrevTxId tests in `tests/Sorcha.Register.Core.Tests/Managers/QueryManagerTests.cs` — test: single match returns paginated result with 1 item; multiple matches (fork) returns all; no matches returns empty; null/empty prevTxId returns empty; pagination with page/pageSize; invalid page clamped to 1; pageSize clamped to 1-100 range
- [x] T010 [P] Add InMemoryRegisterRepository PrevTxId tests in `tests/Sorcha.Register.Core.Tests/` (or appropriate test file) — test: returns matching transactions; returns empty for non-existent register; returns empty for null/empty prevTxId; results sorted by TimeStamp descending
- [x] T011 Run foundational tests — `dotnet test tests/Sorcha.Register.Core.Tests` — verify all new and existing tests pass

**Checkpoint**: Storage layer complete — repository, index, QueryManager, and InMemory implementation all working with tests.

---

## Phase 3: User Story 1 — Fork Detection During Validation (Priority: P1) 🎯 MVP

**Goal**: The validation engine can query transactions by PrevTxId through the full stack (endpoint → service client → QueryManager → repository) and detect forks when multiple transactions reference the same predecessor.

**Independent Test**: Submit two transactions with the same PrevTxId to a register, then query — result contains both, indicating a fork. ValidationEngine flags the fork with a VAL_CHAIN_FORK error code.

### Implementation for User Story 1

- [x] T012 [US1] Add `GET /api/query/previous/{prevTxId}/transactions` endpoint to Register Service in `src/Services/Sorcha.Register.Service/Program.cs` — add to `/api/query` group with `CanReadTransactions` authorization, parameters: prevTxId (path), registerId (query, required), page (query, default 1), pageSize (query, default 20); delegate to `QueryManager.GetTransactionsByPrevTxIdPaginatedAsync`; return `Results.Ok(result)` or `Results.BadRequest` if registerId missing; add `.WithName("GetTransactionsByPrevTxId").WithSummary("Query transactions by previous transaction ID")` for OpenAPI
- [x] T013 [US1] Add `GetTransactionsByPrevTxIdAsync` method to `IRegisterServiceClient` interface in `src/Common/Sorcha.ServiceClients/Register/IRegisterServiceClient.cs` — signature: `Task<TransactionPage> GetTransactionsByPrevTxIdAsync(string registerId, string prevTxId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)`; add XML doc comment
- [x] T014 [US1] Implement `GetTransactionsByPrevTxIdAsync` in `RegisterServiceClient` in `src/Common/Sorcha.ServiceClients/Register/RegisterServiceClient.cs` — follow pattern of `GetTransactionsByWalletAsync`: set auth header, build URL `/api/query/previous/{prevTxId}/transactions?registerId={registerId}&$skip={(page-1)*pageSize}&$top={pageSize}&$count=true`, GET request, deserialize to TransactionPage, handle 404/error cases
- [x] T015 [US1] Add fork detection to `ValidateChainAsync` in `src/Services/Sorcha.Validator.Service/Services/ValidationEngine.cs` — when PreviousTransactionId is specified: call `_registerClient.GetTransactionsByPrevTxIdAsync(registerId, previousTransactionId)`, if `result.Total > 0` then existing transactions already claim this predecessor = fork detected, add validation violation with code `VAL_CHAIN_FORK` and message indicating fork; wrap in try/catch for HttpRequestException (transient)
- [x] T016 [US1] Build all US1 projects — run `dotnet build` for Register Service, ServiceClients, Validator Service to verify compilation

### Tests for User Story 1

- [x] T017 [P] [US1] Add RegisterServiceClient PrevTxId tests in `tests/Sorcha.ServiceClients.Tests/` — test: successful query returns TransactionPage; empty result for no matches; handles 404; handles server error
- [x] T018 [P] [US1] Add ValidationEngine fork detection tests in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs` — test: no fork (0 existing successors) passes; fork detected (1+ existing successors) returns VAL_CHAIN_FORK violation; transient error on service unavailable returns non-fatal VAL_CHAIN_TRANSIENT; fork detection skipped when PreviousTransactionId is null/empty; fork detection skipped when chain validation disabled
- [x] T019 [US1] Run US1 tests — `dotnet test tests/Sorcha.ServiceClients.Tests` and `dotnet test tests/Sorcha.Validator.Service.Tests` — verify all new and existing tests pass

**Checkpoint**: Full stack working — ValidationEngine can detect forks via service client → endpoint → QueryManager → repository. US1 independently testable.

---

## Phase 4: User Story 2 — Chain Integrity Auditing (Priority: P2)

**Goal**: The predecessor query endpoint supports chain traversal — auditors can walk a chain forward by successive queries to verify completeness and detect gaps or forks.

**Independent Test**: Create a register with a known chain of transactions, walk forward from genesis using PrevTxId queries, verify all transactions are reachable in order.

**Note**: US2 uses the same query infrastructure built in Phase 2 and Phase 3. This phase adds validation of chain traversal behavior and edge cases.

### Tests for User Story 2

- [x] T020 [P] [US2] Add chain traversal tests in `tests/Sorcha.Register.Core.Tests/Managers/QueryManagerTests.cs` — test: walk a 10-transaction chain forward from genesis (each query returns next successor); detect gap when a transaction is missing; detect fork when two transactions share a predecessor; handle chain tip (query returns empty = end of chain)
- [x] T021 [US2] Run US2 tests — `dotnet test tests/Sorcha.Register.Core.Tests` — verify all new and existing tests pass

**Checkpoint**: Chain auditing capability verified through tests. US2 independently testable.

---

## Phase 5: User Story 3 — Efficient Query Performance (Priority: P2)

**Goal**: The PrevTxId query uses the MongoDB index for efficient lookups, not collection scans. Performance remains under 500ms for large registers.

**Independent Test**: Query a register with 10,000+ transactions and verify response time is under 500ms.

**Note**: The MongoDB index was already added in T003 (Phase 2). This phase validates that the index is actually used and performance meets the target.

### Tests for User Story 3

- [x] T022 [US3] Add MongoDB index verification test in `tests/Sorcha.Register.Storage.MongoDB.Tests/MongoRegisterRepositoryIntegrationTests.cs` — using Testcontainers with MongoDB 7.0: create a register, insert transactions with known PrevTxId values, call `GetTransactionsByPrevTxIdAsync`, verify correct results returned; verify PrevTxId index exists on the collection (list indexes and check for PrevTxId_1)
- [x] T023 [US3] Run US3 tests — `dotnet test tests/Sorcha.Register.Storage.MongoDB.Tests` — verify all new and existing integration tests pass

**Checkpoint**: Performance verified via index existence and integration tests. US3 independently testable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, documentation, and cleanup

- [x] T024 Run full test suite — `dotnet test` across all affected test projects (Register Core, Register Storage MongoDB, Validator Service, ServiceClients) — verify zero regressions against baseline from T001
- [x] T025 [P] Update `specs/021-transaction-query-api/tasks.md` — mark all completed tasks
- [x] T026 [P] Update `.specify/MASTER-TASKS.md` — update task status for 021-transaction-query-api

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup (T001-T002) — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational (Phase 2) — the MVP
- **User Story 2 (Phase 4)**: Depends on Foundational (Phase 2); builds on US1 infrastructure
- **User Story 3 (Phase 5)**: Depends on Foundational (Phase 2); validates index from T003
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (P1)**: Requires Foundational phase. Builds the REST endpoint and service client that US2 and US3 also use.
- **US2 (P2)**: Requires Foundational phase. Tests chain traversal using the same QueryManager from Phase 2.
- **US3 (P2)**: Requires Foundational phase. Validates the MongoDB index from T003 via integration tests.

### Within Each Phase

- Repository interface (T004) before implementations (T005, T006)
- Implementations before QueryManager (T007)
- Endpoint (T012) before service client (T013, T014)
- Service client before ValidationEngine integration (T015)
- Build verification before tests

### Parallel Opportunities

- T005 (MongoDB impl) and T006 (InMemory impl) can run in parallel
- T009 (QueryManager tests) and T010 (InMemory tests) can run in parallel
- T017 (ServiceClient tests) and T018 (ValidationEngine tests) can run in parallel
- T020 (chain traversal tests) can run in parallel with T022 (MongoDB integration tests)
- T025 and T026 (documentation) can run in parallel

---

## Parallel Example: Foundational Phase

```
# These can run in parallel (different files):
Task T005: MongoDB GetTransactionsByPrevTxIdAsync implementation
Task T006: InMemory GetTransactionsByPrevTxIdAsync implementation

# These tests can run in parallel (different files):
Task T009: QueryManager PrevTxId tests
Task T010: InMemory repository PrevTxId tests
```

## Parallel Example: User Story 1

```
# These tests can run in parallel (different files):
Task T017: RegisterServiceClient PrevTxId tests
Task T018: ValidationEngine fork detection tests
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T002)
2. Complete Phase 2: Foundational (T003-T011) — storage layer with index, repository, QueryManager
3. Complete Phase 3: User Story 1 (T012-T019) — full stack endpoint + service client + fork detection
4. **STOP and VALIDATE**: Run all tests, verify fork detection works end-to-end
5. This alone delivers the primary value: ValidationEngine can detect forks

### Incremental Delivery

1. Setup + Foundational → Storage layer ready
2. Add User Story 1 → Fork detection working → **Deploy/Demo (MVP!)**
3. Add User Story 2 → Chain traversal verified → Deploy/Demo
4. Add User Story 3 → Performance validated → Deploy/Demo
5. Polish → Documentation updated → Ready for PR

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story
- The data model (TransactionModel.PrevTxId) already exists — no model changes needed
- PaginatedResult<T> and TransactionPage already exist — no new pagination models needed
- Follow existing patterns: GetTransactionsByWallet (endpoint/client), GetAllTransactionsBySenderAddress (repository)
- MongoDB per-register database means register scoping is implicit in the collection
- Query layer does NOT log or emit metrics for fork detection — callers handle observability
