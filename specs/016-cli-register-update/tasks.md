# Tasks: CLI Register Commands Update

**Input**: Design documents from `/specs/016-cli-register-update/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓

**Tests**: Included (per plan.md Phase F requirement: >85% coverage for new code)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Add shared model references and prepare project structure

- [x] T001 Add ProjectReference to Sorcha.Register.Models in `src/Apps/Sorcha.Cli/Sorcha.Cli.csproj`
- [x] T002 Add ProjectReference to Sorcha.Blueprint.Models in `src/Apps/Sorcha.Cli/Sorcha.Cli.csproj`
- [x] T003 Verify project compiles with new references: `dotnet build src/Apps/Sorcha.Cli` (namespace conflict expected until Phase 2 completes)

---

## Phase 2: Foundational - Shared Model Migration (Blocking Prerequisites)

**Purpose**: Migrate CLI to use shared models. MUST complete before any user story can proceed.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete - all commands depend on shared model types.

### Refit Client Updates

- [x] T004 Update `IRegisterServiceClient.cs`: Change `GetRegistersAsync` return type from `Sorcha.Cli.Models.Register` to `Sorcha.Register.Models.Register` in `src/Apps/Sorcha.Cli/Services/IRegisterServiceClient.cs`
- [x] T005 Update `IRegisterServiceClient.cs`: Change `GetRegisterAsync` return type to `Sorcha.Register.Models.Register` in `src/Apps/Sorcha.Cli/Services/IRegisterServiceClient.cs`
- [x] T006 Update `IRegisterServiceClient.cs`: Change `GetTransactionsAsync` return type from `Transaction` to `TransactionModel` in `src/Apps/Sorcha.Cli/Services/IRegisterServiceClient.cs`
- [x] T007 Update `IRegisterServiceClient.cs`: Change `GetTransactionAsync` return type to `TransactionModel` in `src/Apps/Sorcha.Cli/Services/IRegisterServiceClient.cs`

### Wallet Model Updates

- [x] T008 Add `IsPreHashed` bool property to `SignTransactionRequest` in `src/Apps/Sorcha.Cli/Models/Wallet.cs`
- [x] T009 Add `DerivationPath` string? property to `SignTransactionRequest` in `src/Apps/Sorcha.Cli/Models/Wallet.cs`

### Command Display Logic Updates

- [x] T010 Update `RegisterListCommand` display logic for new Register model fields (Height, Status, TenantId, Advertise, CreatedAt, UpdatedAt) in `src/Apps/Sorcha.Cli/Commands/RegisterCommands.cs`
- [x] T011 Update `RegisterGetCommand` display logic for all Register model fields (Height, Status, TenantId, Advertise, IsFullReplica, Votes, CreatedAt, UpdatedAt) in `src/Apps/Sorcha.Cli/Commands/RegisterCommands.cs`
- [x] T012 Update `TxListCommand` display logic for TransactionModel (TxId instead of Id, Payloads array instead of flat Payload) in `src/Apps/Sorcha.Cli/Commands/TransactionCommands.cs`
- [x] T013 Update `TxGetCommand` display logic for TransactionModel fields in `src/Apps/Sorcha.Cli/Commands/TransactionCommands.cs`
- [x] T014 Replace `--skip`/`--take` options with `--page`/`--page-size` in `TxListCommand` in `src/Apps/Sorcha.Cli/Commands/TransactionCommands.cs`

### Cleanup

- [x] T015 Delete duplicate model file `src/Apps/Sorcha.Cli/Models/Register.cs` (Register, CreateRegisterRequest, Transaction, SubmitTransactionRequest, SubmitTransactionResponse)
- [x] T016 Verify build succeeds: `dotnet build src/Apps/Sorcha.Cli`
- [ ] T017 Verify existing `register list` command works with shared models
- [ ] T018 Verify existing `register get` command works with shared models
- [ ] T019 Verify existing `tx list` command works with shared models

**Checkpoint**: Foundation ready - shared models in place, existing commands working. User story implementation can now begin.

---

## Phase 3: User Story 2 - Shared Models Display (Priority: P1) 🎯 MVP

**Goal**: CLI displays all fields from shared Register/Transaction models (Height, Status, Advertise, IsFullReplica, Votes, TenantId, CreatedAt, UpdatedAt)

**Independent Test**: Run `sorcha register list` and `sorcha register get --id <id>` and verify all shared model fields are displayed.

**Note**: Most implementation is in Phase 2 (Foundational). This phase validates the story is complete.

### Tests for User Story 2

- [ ] T020 [P] [US2] Add unit test verifying Register model fields display correctly in `tests/Sorcha.Cli.Tests/Commands/RegisterCommandsTests.cs`
- [ ] T021 [P] [US2] Add unit test verifying TransactionModel fields display correctly in `tests/Sorcha.Cli.Tests/Commands/TransactionCommandsTests.cs`

### Validation for User Story 2

- [ ] T022 [US2] Manual validation: Run `sorcha register list` and verify Height, Status, TenantId, Advertise columns
- [ ] T023 [US2] Manual validation: Run `sorcha register get --id <id>` and verify all fields displayed
- [ ] T024 [US2] Manual validation: Run `sorcha tx list --register-id <id>` with new pagination options

**Checkpoint**: User Story 2 complete - CLI displays all shared model fields

---

## Phase 4: User Story 1 - Two-Phase Register Creation (Priority: P1) 🎯 MVP

**Goal**: Create registers using the two-phase cryptographic attestation flow (initiate → sign → finalize)

**Independent Test**: Run `sorcha register create --name "Test" --tenant-id <id> --owner-wallet <addr>` and verify register is created with genesis transaction and docket.

### Refit Endpoints for User Story 1

- [x] T025 [P] [US1] Add `InitiateRegisterCreationAsync` method (POST /api/registers/initiate) to `src/Apps/Sorcha.Cli/Services/IRegisterServiceClient.cs`
- [x] T026 [P] [US1] Add `FinalizeRegisterCreationAsync` method (POST /api/registers/finalize) to `src/Apps/Sorcha.Cli/Services/IRegisterServiceClient.cs`

### Implementation for User Story 1

- [x] T027 [US1] Rewrite `RegisterCreateCommand`: Replace `--org-id` with `--tenant-id` and add `--owner-wallet` required option in `src/Apps/Sorcha.Cli/Commands/RegisterCommands.cs`
- [x] T028 [US1] Implement initiate phase: Build `InitiateRegisterCreationRequest` with name, tenantId, single owner in `src/Apps/Sorcha.Cli/Commands/RegisterCommands.cs`
- [x] T029 [US1] Implement signing phase: Call wallet service sign endpoint with `IsPreHashed=true` for attestation hash in `src/Apps/Sorcha.Cli/Commands/RegisterCommands.cs`
- [x] T030 [US1] Implement finalize phase: Build `FinalizeRegisterCreationRequest` with signed attestations and call finalize in `src/Apps/Sorcha.Cli/Commands/RegisterCommands.cs`
- [x] T031 [US1] Add success output displaying Register ID, Genesis TX ID, Genesis Docket ID in `src/Apps/Sorcha.Cli/Commands/RegisterCommands.cs`
- [x] T032 [US1] Add error handling for wallet unreachable scenario in `src/Apps/Sorcha.Cli/Commands/RegisterCommands.cs`
- [x] T033 [US1] Add error handling for attestation expired (5-min timeout) scenario in `src/Apps/Sorcha.Cli/Commands/RegisterCommands.cs`
- [x] T034 [US1] Add error handling for invalid signature/finalization failure in `src/Apps/Sorcha.Cli/Commands/RegisterCommands.cs`
- [x] T035 [US1] Support `--output json` for creation response in `src/Apps/Sorcha.Cli/Commands/RegisterCommands.cs`

### Tests for User Story 1

- [ ] T036 [P] [US1] Add unit test for two-phase creation happy path (mock wallet + register service) in `tests/Sorcha.Cli.Tests/Commands/RegisterCreateCommandTests.cs`
- [ ] T037 [P] [US1] Add unit test for attestation expiration error handling in `tests/Sorcha.Cli.Tests/Commands/RegisterCreateCommandTests.cs`
- [ ] T038 [P] [US1] Add unit test for wallet unreachable error handling in `tests/Sorcha.Cli.Tests/Commands/RegisterCreateCommandTests.cs`
- [ ] T039 [P] [US1] Add unit test for signing failure error handling in `tests/Sorcha.Cli.Tests/Commands/RegisterCreateCommandTests.cs`

**Checkpoint**: User Story 1 complete - registers created with proper cryptographic attestations

---

## Phase 5: User Story 3 - Docket Inspection (Priority: P2)

**Goal**: Browse dockets (sealed blocks) in a register for auditing

**Independent Test**: Run `sorcha docket list --register-id <id>` against a register with transactions and verify docket details.

### Refit Endpoints for User Story 3

- [x] T040 [P] [US3] Add `ListDocketsAsync` method (GET /api/registers/{regId}/dockets) to `src/Apps/Sorcha.Cli/Services/IRegisterServiceClient.cs`
- [x] T041 [P] [US3] Add `GetDocketAsync` method (GET /api/registers/{regId}/dockets/{docketId}) to `src/Apps/Sorcha.Cli/Services/IRegisterServiceClient.cs`
- [x] T042 [P] [US3] Add `GetDocketTransactionsAsync` method (GET /api/registers/{regId}/dockets/{docketId}/transactions) to `src/Apps/Sorcha.Cli/Services/IRegisterServiceClient.cs`

### Implementation for User Story 3

- [x] T043 [US3] Create `DocketCommand` parent command class in `src/Apps/Sorcha.Cli/Commands/DocketCommands.cs`
- [x] T044 [US3] Implement `DocketListCommand` with `--register-id` option in `src/Apps/Sorcha.Cli/Commands/DocketCommands.cs`
- [x] T045 [US3] Implement `DocketGetCommand` with `--register-id` and `--docket-id` options in `src/Apps/Sorcha.Cli/Commands/DocketCommands.cs`
- [x] T046 [US3] Implement `DocketTransactionsCommand` with `--register-id` and `--docket-id` options in `src/Apps/Sorcha.Cli/Commands/DocketCommands.cs`
- [x] T047 [US3] Add display logic for docket list table (ID, Hash, State, TX Count, Timestamp) in `src/Apps/Sorcha.Cli/Commands/DocketCommands.cs`
- [x] T048 [US3] Add display logic for docket details (hash, previous hash, state, metadata, votes) in `src/Apps/Sorcha.Cli/Commands/DocketCommands.cs`
- [x] T049 [US3] Support `--output json` for all docket commands in `src/Apps/Sorcha.Cli/Commands/DocketCommands.cs`
- [x] T050 [US3] Register `DocketCommand` in Program.cs: `rootCommand.Subcommands.Add(new DocketCommand(...))` in `src/Apps/Sorcha.Cli/Program.cs`

### Tests for User Story 3

- [ ] T051 [P] [US3] Add unit test for `docket list` command with mocked response in `tests/Sorcha.Cli.Tests/Commands/DocketCommandsTests.cs`
- [ ] T052 [P] [US3] Add unit test for `docket get` command in `tests/Sorcha.Cli.Tests/Commands/DocketCommandsTests.cs`
- [ ] T053 [P] [US3] Add unit test for `docket transactions` command in `tests/Sorcha.Cli.Tests/Commands/DocketCommandsTests.cs`

**Checkpoint**: User Story 3 complete - docket inspection commands working

---

## Phase 6: User Story 4 - Cross-Register Queries (Priority: P2)

**Goal**: Query transactions across registers by wallet, sender, blueprint, plus OData and stats

**Independent Test**: Run `sorcha query wallet --address <addr>` and verify transactions returned with pagination.

### Refit Endpoints for User Story 4

- [x] T054 [P] [US4] Add `QueryByWalletAsync` method (GET /api/query/wallets/{address}/transactions) to `src/Apps/Sorcha.Cli/Services/IRegisterServiceClient.cs`
- [x] T055 [P] [US4] Add `QueryBySenderAsync` method (GET /api/query/senders/{address}/transactions) to `src/Apps/Sorcha.Cli/Services/IRegisterServiceClient.cs`
- [x] T056 [P] [US4] Add `QueryByBlueprintAsync` method (GET /api/query/blueprints/{id}/transactions) to `src/Apps/Sorcha.Cli/Services/IRegisterServiceClient.cs`
- [x] T057 [P] [US4] Add `GetQueryStatsAsync` method (GET /api/query/stats) to `src/Apps/Sorcha.Cli/Services/IRegisterServiceClient.cs`
- [x] T058 [P] [US4] Add `QueryODataAsync` method (GET /odata/{resource}) returning HttpResponseMessage to `src/Apps/Sorcha.Cli/Services/IRegisterServiceClient.cs`

### Implementation for User Story 4

- [x] T059 [US4] Create `QueryCommand` parent command class in `src/Apps/Sorcha.Cli/Commands/QueryCommands.cs`
- [x] T060 [US4] Implement `QueryWalletCommand` with `--address`, `--page`, `--page-size` options in `src/Apps/Sorcha.Cli/Commands/QueryCommands.cs`
- [x] T061 [US4] Implement `QuerySenderCommand` with `--address`, `--page`, `--page-size` options in `src/Apps/Sorcha.Cli/Commands/QueryCommands.cs`
- [x] T062 [US4] Implement `QueryBlueprintCommand` with `--id`, `--page`, `--page-size` options in `src/Apps/Sorcha.Cli/Commands/QueryCommands.cs`
- [x] T063 [US4] Implement `QueryStatsCommand` in `src/Apps/Sorcha.Cli/Commands/QueryCommands.cs`
- [x] T064 [US4] Implement `QueryODataCommand` with `--resource`, `--filter`, `--orderby`, `--top`, `--skip`, `--select`, `--count` options in `src/Apps/Sorcha.Cli/Commands/QueryCommands.cs`
- [x] T065 [US4] Add pagination display with page info for query commands in `src/Apps/Sorcha.Cli/Commands/QueryCommands.cs`
- [x] T066 [US4] Support `--output json` for all query commands in `src/Apps/Sorcha.Cli/Commands/QueryCommands.cs`
- [x] T067 [US4] Register `QueryCommand` in Program.cs: `rootCommand.Subcommands.Add(new QueryCommand(...))` in `src/Apps/Sorcha.Cli/Program.cs`

### Tests for User Story 4

- [ ] T068 [P] [US4] Add unit test for `query wallet` command with pagination in `tests/Sorcha.Cli.Tests/Commands/QueryCommandsTests.cs`
- [ ] T069 [P] [US4] Add unit test for `query sender` command in `tests/Sorcha.Cli.Tests/Commands/QueryCommandsTests.cs`
- [ ] T070 [P] [US4] Add unit test for `query blueprint` command in `tests/Sorcha.Cli.Tests/Commands/QueryCommandsTests.cs`
- [ ] T071 [P] [US4] Add unit test for `query stats` command in `tests/Sorcha.Cli.Tests/Commands/QueryCommandsTests.cs`
- [ ] T072 [P] [US4] Add unit test for `query odata` command verifying query string construction in `tests/Sorcha.Cli.Tests/Commands/QueryCommandsTests.cs`

**Checkpoint**: User Story 4 complete - cross-register query commands working

---

## Phase 7: User Story 5 - Register Metadata Update (Priority: P3)

**Goal**: Update register name, status, and advertise flag after creation

**Independent Test**: Run `sorcha register update --id <id> --name "New Name"` and verify the name changes.

### Refit Endpoints for User Story 5

- [x] T073 [US5] Add `UpdateRegisterAsync` method (PUT /api/registers/{id}) to `src/Apps/Sorcha.Cli/Services/IRegisterServiceClient.cs`

### Implementation for User Story 5

- [x] T074 [US5] Implement `RegisterUpdateCommand` with `--id`, `--name`, `--status`, `--advertise` options in `src/Apps/Sorcha.Cli/Commands/RegisterCommands.cs`
- [x] T075 [US5] Add display logic showing updated register fields in `src/Apps/Sorcha.Cli/Commands/RegisterCommands.cs`
- [x] T076 [US5] Support `--output json` for update command in `src/Apps/Sorcha.Cli/Commands/RegisterCommands.cs`
- [x] T077 [US5] Register `RegisterUpdateCommand` in `RegisterCommand` constructor in `src/Apps/Sorcha.Cli/Commands/RegisterCommands.cs`

### Tests for User Story 5

- [ ] T078 [P] [US5] Add unit test for `register update` command name change in `tests/Sorcha.Cli.Tests/Commands/RegisterUpdateCommandTests.cs`
- [ ] T079 [P] [US5] Add unit test for `register update` command status change in `tests/Sorcha.Cli.Tests/Commands/RegisterUpdateCommandTests.cs`
- [ ] T080 [P] [US5] Add unit test for `register update` not found error handling in `tests/Sorcha.Cli.Tests/Commands/RegisterUpdateCommandTests.cs`

**Checkpoint**: User Story 5 complete - register metadata update working

---

## Phase 8: User Story 6 - Register Statistics (Priority: P3)

**Goal**: Display total register count for platform visibility

**Independent Test**: Run `sorcha register stats` and verify a count is returned.

### Refit Endpoints for User Story 6

- [x] T081 [US6] Add `GetRegisterStatsAsync` method (GET /api/registers/stats/count) to `src/Apps/Sorcha.Cli/Services/IRegisterServiceClient.cs`

### Implementation for User Story 6

- [x] T082 [US6] Implement `RegisterStatsCommand` in `src/Apps/Sorcha.Cli/Commands/RegisterCommands.cs`
- [x] T083 [US6] Add display logic showing total register count in `src/Apps/Sorcha.Cli/Commands/RegisterCommands.cs`
- [x] T084 [US6] Support `--output json` for stats command in `src/Apps/Sorcha.Cli/Commands/RegisterCommands.cs`
- [x] T085 [US6] Register `RegisterStatsCommand` in `RegisterCommand` constructor in `src/Apps/Sorcha.Cli/Commands/RegisterCommands.cs`

### Tests for User Story 6

- [ ] T086 [P] [US6] Add unit test for `register stats` command in `tests/Sorcha.Cli.Tests/Commands/RegisterStatsCommandTests.cs`
- [ ] T087 [P] [US6] Add unit test for `register stats` with zero registers in `tests/Sorcha.Cli.Tests/Commands/RegisterStatsCommandTests.cs`

**Checkpoint**: User Story 6 complete - register statistics working

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Error handling improvements, coverage validation, documentation

### Error Handling Tests

- [ ] T088 [P] Add unit test for 401 Unauthorized error handling across commands in `tests/Sorcha.Cli.Tests/Commands/ErrorHandlingTests.cs`
- [ ] T089 [P] Add unit test for 403 Forbidden error handling across commands in `tests/Sorcha.Cli.Tests/Commands/ErrorHandlingTests.cs`
- [ ] T090 [P] Add unit test for 404 Not Found error handling across commands in `tests/Sorcha.Cli.Tests/Commands/ErrorHandlingTests.cs`

### Validation & Documentation

- [ ] T091 Run full test suite and verify >85% coverage on new code: `dotnet test tests/Sorcha.Cli.Tests --collect:"XPlat Code Coverage"`
- [ ] T092 Run quickstart.md validation scenarios from `specs/016-cli-register-update/quickstart.md`
- [ ] T093 Update CLI help text for all new/modified commands
- [ ] T094 Final build verification: `dotnet build src/Apps/Sorcha.Cli && dotnet test tests/Sorcha.Cli.Tests`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup - **BLOCKS all user stories**
- **User Story 2 (Phase 3)**: Depends on Foundational (Phase 2) - validates shared model display
- **User Story 1 (Phase 4)**: Depends on Foundational (Phase 2) - two-phase creation
- **User Story 3 (Phase 5)**: Depends on Foundational (Phase 2) - docket inspection
- **User Story 4 (Phase 6)**: Depends on Foundational (Phase 2) - cross-register queries
- **User Story 5 (Phase 7)**: Depends on Foundational (Phase 2) - register update
- **User Story 6 (Phase 8)**: Depends on Foundational (Phase 2) - register stats
- **Polish (Phase 9)**: Depends on all user stories being complete

### User Story Independence

All user stories (Phases 3-8) depend ONLY on Foundational phase completion and can proceed in parallel:

```
Phase 2 (Foundational) ───┬──► Phase 3 (US2: Shared Models)
                          ├──► Phase 4 (US1: Two-Phase Create)
                          ├──► Phase 5 (US3: Dockets)
                          ├──► Phase 6 (US4: Queries)
                          ├──► Phase 7 (US5: Update)
                          └──► Phase 8 (US6: Stats)
                                      │
                                      ▼
                              Phase 9 (Polish)
```

### Within Each User Story

- Refit endpoints before command implementation
- Command implementation before tests
- All [P] tasks within a phase can run in parallel

### Parallel Opportunities

**Phase 2 (Foundational)**:
- T004-T007 (Refit client updates) can run in parallel
- T008-T009 (Wallet model updates) can run in parallel
- T010-T014 (Display logic updates) can run sequentially per file

**Phase 4 (User Story 1)**:
- T025-T026 (Refit endpoints) in parallel
- T036-T039 (Tests) in parallel after implementation

**Phase 5 (User Story 3)**:
- T040-T042 (Refit endpoints) in parallel
- T051-T053 (Tests) in parallel after implementation

**Phase 6 (User Story 4)**:
- T054-T058 (Refit endpoints) in parallel
- T068-T072 (Tests) in parallel after implementation

---

## Parallel Example: User Story 3 (Dockets)

```bash
# Launch all Refit endpoints in parallel:
Task: "Add ListDocketsAsync method in IRegisterServiceClient.cs"
Task: "Add GetDocketAsync method in IRegisterServiceClient.cs"
Task: "Add GetDocketTransactionsAsync method in IRegisterServiceClient.cs"

# After endpoints complete, launch tests in parallel:
Task: "Add unit test for docket list command"
Task: "Add unit test for docket get command"
Task: "Add unit test for docket transactions command"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 Only)

1. Complete Phase 1: Setup (T001-T003)
2. Complete Phase 2: Foundational (T004-T019) - **CRITICAL GATE**
3. Complete Phase 3: User Story 2 - Shared Models (T020-T024)
4. Complete Phase 4: User Story 1 - Two-Phase Create (T025-T039)
5. **STOP and VALIDATE**: Test register creation with attestation flow
6. Deploy/demo if ready

### Incremental Delivery

1. **Setup + Foundational** → Foundation ready (existing commands work with shared models)
2. **+ User Story 2** → All register fields displayed (MVP!)
3. **+ User Story 1** → Secure register creation with attestations
4. **+ User Story 3** → Docket auditing capability
5. **+ User Story 4** → Cross-register query capability
6. **+ User Story 5 + 6** → Full management (update, stats)
7. **Polish** → Production ready

### Suggested MVP Scope

**Minimum**: Phases 1-4 (Setup + Foundational + US2 + US1)
- Shared models in place
- Two-phase secure register creation
- All existing commands enhanced

---

## Summary

| Phase | User Story | Priority | Tasks | Parallel Opportunities |
|-------|------------|----------|-------|----------------------|
| 1 | Setup | - | 3 | None |
| 2 | Foundational | - | 16 | T004-T007, T008-T009 |
| 3 | US2: Shared Models | P1 | 5 | T020-T021 |
| 4 | US1: Two-Phase Create | P1 | 15 | T025-T026, T036-T039 |
| 5 | US3: Dockets | P2 | 14 | T040-T042, T051-T053 |
| 6 | US4: Queries | P2 | 19 | T054-T058, T068-T072 |
| 7 | US5: Update | P3 | 8 | T078-T080 |
| 8 | US6: Stats | P3 | 7 | T086-T087 |
| 9 | Polish | - | 7 | T088-T090 |
| **Total** | | | **94** | |

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Foundational phase (Phase 2) is the critical gate - nothing proceeds without it
