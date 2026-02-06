# Tasks: Validator Engine - Schema & Chain Validation

**Input**: Design documents from `/specs/020-validator-engine-validation/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Included — the spec requires >85% test coverage (SC-008) and the constitution mandates testing.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Phase 1: Setup

**Purpose**: Add dependencies and prepare infrastructure for schema and chain validation

- [x] T001 Add JsonSchema.Net 8.0.5 package reference to `src/Services/Sorcha.Validator.Service/Sorcha.Validator.Service.csproj`
- [x] T002 Add `string? PreviousTransactionId` property to the Transaction model in `src/Services/Sorcha.Validator.Service/Models/Transaction.cs` with XML doc comment describing chain linkage (null = genesis, empty string treated as null per FR-009)
- [x] T003 Add `IRegisterServiceClient` as constructor parameter to `ValidationEngine` in `src/Services/Sorcha.Validator.Service/Services/ValidationEngine.cs` — add `using Sorcha.ServiceClients.Register;`, add `private readonly IRegisterServiceClient _registerClient;` field, add null-check in constructor, store reference
- [x] T004 Ensure `AddValidationEngine(builder.Configuration)` is called in `src/Services/Sorcha.Validator.Service/Program.cs` — check if the call exists and add it if missing (the extension method exists in `Extensions/ValidationEngineExtensions.cs`)
- [x] T005 Build `src/Services/Sorcha.Validator.Service` and verify no compilation errors after setup changes
- [x] T006 Update test constructor in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs` — add `Mock<IRegisterServiceClient>` field, pass `.Object` as new constructor argument, verify all existing tests still pass

**Checkpoint**: Infrastructure ready — all existing tests pass with the new constructor parameter mocked.

---

## Phase 2: User Story 1 — Schema Validation Rejects Invalid Payload Data (Priority: P1)

**Goal**: Replace the schema validation stub in `ValidateSchemaAsync` with real JsonSchema.Net evaluation against blueprint action `DataSchemas`.

**Independent Test**: Submit transactions with invalid payloads (missing fields, wrong types) and verify rejection with schema error codes.

### Tests for User Story 1

- [x] T007 [P] [US1] Write test `ValidateSchemaAsync_ValidPayload_ReturnsSuccess` — mock `IBlueprintCache` to return a blueprint with an action containing a JSON schema requiring `name` (string) and `amount` (number), create a transaction with conforming payload, assert `IsValid == true` in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`
- [x] T008 [P] [US1] Write test `ValidateSchemaAsync_MissingRequiredField_ReturnsSchemaError` — same blueprint as T007, payload missing `amount`, assert `IsValid == false` with error code `VAL_SCHEMA_004` and error category `Schema` in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`
- [x] T009 [P] [US1] Write test `ValidateSchemaAsync_WrongType_ReturnsSchemaError` — same blueprint, payload has `amount: "not-a-number"`, assert `IsValid == false` with error code `VAL_SCHEMA_004` in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`
- [x] T010 [P] [US1] Write test `ValidateSchemaAsync_NoSchemas_ReturnsSuccess` — mock blueprint action with `DataSchemas = null`, assert `IsValid == true` (schema not enforced per FR-006) in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`
- [x] T011 [P] [US1] Write test `ValidateSchemaAsync_EmptySchemas_ReturnsSuccess` — mock blueprint action with `DataSchemas` as empty list, assert `IsValid == true` in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`
- [x] T012 [P] [US1] Write test `ValidateSchemaAsync_DisabledByConfig_ReturnsSuccess` — set `EnableSchemaValidation = false` in config, assert `IsValid == true` without checking blueprint cache in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`
- [x] T013 [P] [US1] Write test `ValidateSchemaAsync_MalformedSchema_ReturnsBlueprintError` — mock action with invalid JSON schema text, assert error code `VAL_SCHEMA_005` with category `Blueprint` in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`
- [x] T014 [P] [US1] Write test `ValidateSchemaAsync_MultipleSchemas_AllMustPass` — mock action with 2 schemas, payload passes first but fails second, assert `IsValid == false` per clarification (payload must pass ALL schemas) in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`

### Implementation for User Story 1

- [x] T015 [US1] Implement `ValidateSchemaAsync` in `src/Services/Sorcha.Validator.Service/Services/ValidationEngine.cs` — replace the TODO stub (line ~317) with real schema validation: after retrieving the blueprint action (existing code), check if `action.DataSchemas` is null or empty (return Success), otherwise iterate each `JsonDocument` in `DataSchemas`, convert to schema text via `schema.RootElement.GetRawText()`, parse with `JsonSchema.FromText()`, evaluate `transaction.Payload` with `EvaluationOptions { OutputFormat = OutputFormat.List, RequireFormatValidation = true }`, collect all violations as `ValidationEngineError` entries with code `VAL_SCHEMA_004`, category `Schema`, and the evaluation result's instance location as the `Field`. Wrap schema parsing in try-catch to handle malformed schemas (error code `VAL_SCHEMA_005`, category `Blueprint`). Add `using Json.Schema;` import.
- [x] T016 [US1] Add config check at the top of `ValidateSchemaAsync` — if `_config.EnableSchemaValidation == false`, return `ValidationEngineResult.Success()` immediately with a debug log "Schema validation disabled by configuration" in `src/Services/Sorcha.Validator.Service/Services/ValidationEngine.cs`
- [x] T017 [US1] Build and run all US1 tests — verify T007-T014 pass, verify all pre-existing tests still pass in `tests/Sorcha.Validator.Service.Tests`

**Checkpoint**: Schema validation fully functional — invalid payloads are rejected, valid payloads pass, no-schema actions skip validation.

---

## Phase 3: User Story 2 — Chain Validation Verifies Transaction Linkage (Priority: P1)

**Goal**: Replace the chain validation stub in `ValidateChainAsync` with real Register Service calls for both transaction-level and docket-level chain integrity verification.

**Independent Test**: Submit transactions with various PreviousTransactionId references and verify correct acceptance/rejection based on Register Service state.

### Tests for User Story 2

- [x] T018 [P] [US2] Write test `ValidateChainAsync_NoPreviousTransaction_ReturnsSuccess` — transaction with `PreviousTransactionId = null`, assert `IsValid == true` (genesis accepted per FR-008) in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`
- [x] T019 [P] [US2] Write test `ValidateChainAsync_EmptyPreviousTransaction_ReturnsSuccess` — transaction with `PreviousTransactionId = ""`, assert treated as null (FR-009) and returns success in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`
- [x] T020 [P] [US2] Write test `ValidateChainAsync_ValidPreviousTransaction_ReturnsSuccess` — mock `IRegisterServiceClient.GetTransactionAsync()` to return a transaction with matching `RegisterId`, assert `IsValid == true` in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`
- [x] T021 [P] [US2] Write test `ValidateChainAsync_PreviousTransactionNotFound_ReturnsChainError` — mock `GetTransactionAsync()` to return null, assert error code `VAL_CHAIN_001` in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`
- [x] T022 [P] [US2] Write test `ValidateChainAsync_PreviousTransactionWrongRegister_ReturnsChainError` — mock `GetTransactionAsync()` to return transaction with different `RegisterId`, assert error code `VAL_CHAIN_002` in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`
- [x] T023 [P] [US2] Write test `ValidateChainAsync_DisabledByConfig_ReturnsSuccess` — set `EnableChainValidation = false`, assert `IsValid == true` without calling Register Service in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`
- [x] T024 [P] [US2] Write test `ValidateChainAsync_RegisterServiceUnavailable_ReturnsTransientError` — mock `GetTransactionAsync()` to throw `HttpRequestException`, assert error code `VAL_CHAIN_TRANSIENT` with `IsFatal == false` in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`
- [x] T025 [P] [US2] Write test `ValidateChainAsync_DocketChainIntact_ReturnsSuccess` — mock `GetRegisterHeightAsync()` returning 2, mock `ReadDocketAsync(registerId, 2)` and `ReadDocketAsync(registerId, 1)` with valid hash linkage, assert `IsValid == true` in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`
- [x] T026 [P] [US2] Write test `ValidateChainAsync_DocketHashMismatch_ReturnsChainError` — mock two sequential dockets where `latestDocket.PreviousHash != predecessorDocket.DocketHash`, assert error code `VAL_CHAIN_004` in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`
- [x] T027 [P] [US2] Write test `ValidateChainAsync_DocketGap_ReturnsChainError` — mock `GetRegisterHeightAsync()` returning 3, mock `ReadDocketAsync(registerId, 3)` with `DocketNumber=3` and `ReadDocketAsync(registerId, 2)` returning null (gap), assert error code `VAL_CHAIN_003` in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`
- [x] T028 [P] [US2] Write test `ValidateChainAsync_GenesisRegister_ReturnsSuccess` — mock `GetRegisterHeightAsync()` returning 0 (empty register), assert docket-level check succeeds in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`

### Implementation for User Story 2

- [x] T029 [US2] Implement transaction-level chain validation in `ValidateChainAsync` in `src/Services/Sorcha.Validator.Service/Services/ValidationEngine.cs` — replace the TODO stub (line ~437): check if `PreviousTransactionId` is null or whitespace (treat as genesis, skip transaction-level check), otherwise call `_registerClient.GetTransactionAsync(transaction.RegisterId, transaction.PreviousTransactionId, ct)`, if null return error VAL_CHAIN_001, if returned transaction's RegisterId doesn't match return error VAL_CHAIN_002
- [x] T030 [US2] Implement docket-level chain validation in `ValidateChainAsync` in `src/Services/Sorcha.Validator.Service/Services/ValidationEngine.cs` — after transaction-level check: call `_registerClient.GetRegisterHeightAsync(transaction.RegisterId, ct)`, if height > 0 call `_registerClient.ReadDocketAsync(transaction.RegisterId, height, ct)` and `ReadDocketAsync(transaction.RegisterId, height - 1, ct)`, verify predecessor exists (VAL_CHAIN_003 if null), verify `latestDocket.PreviousHash == predecessor.DocketHash` (VAL_CHAIN_004 if mismatch), verify `latestDocket.DocketNumber == predecessor.DocketNumber + 1` (VAL_CHAIN_003 if gap). If height == 0 skip docket checks (genesis register)
- [x] T031 [US2] Add config check at top of `ValidateChainAsync` — if `_config.EnableChainValidation == false`, return `ValidationEngineResult.Success()` immediately with debug log in `src/Services/Sorcha.Validator.Service/Services/ValidationEngine.cs`
- [x] T032 [US2] Add transient error handling in `ValidateChainAsync` — wrap all `_registerClient` calls in try-catch for `HttpRequestException` and `TaskCanceledException`, return `ValidationEngineError` with code `VAL_CHAIN_TRANSIENT`, `IsFatal = false`, category `Chain`, log at Warning level in `src/Services/Sorcha.Validator.Service/Services/ValidationEngine.cs`
- [x] T033 [US2] Build and run all US2 tests — verify T018-T028 pass, verify all US1 tests and pre-existing tests still pass in `tests/Sorcha.Validator.Service.Tests`

**Checkpoint**: Chain validation fully functional — transaction linkage verified, docket chain integrity checked, transient errors handled gracefully.

---

## Phase 4: User Story 3 — Schema Validation Reports Detailed Errors (Priority: P2)

**Goal**: Ensure schema validation errors include JSON paths, violation descriptions, and support for multiple simultaneous violations.

**Independent Test**: Submit payloads with various types of violations and verify each produces a distinct, descriptive error with full JSON path.

### Tests for User Story 3

- [x] T034 [P] [US3] Write test `ValidateSchemaAsync_MultipleViolations_AllReported` — payload missing one required field AND having wrong type on another, assert error list contains 2+ errors with distinct `Field` values in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`
- [x] T035 [P] [US3] Write test `ValidateSchemaAsync_NestedObjectViolation_IncludesJsonPath` — schema with nested object requirement, payload with invalid nested property, assert error `Field` contains JSON path like `/address/zipCode` in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`
- [x] T036 [P] [US3] Write test `ValidateSchemaAsync_EnumViolation_ReportsAllowedValues` — schema with enum constraint, payload with disallowed value, assert error message mentions the property and the constraint in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`

### Implementation for User Story 3

- [x] T037 [US3] Enhance error mapping in `ValidateSchemaAsync` in `src/Services/Sorcha.Validator.Service/Services/ValidationEngine.cs` — when iterating `EvaluationResults.Details`, map each failed evaluation to a `ValidationEngineError` with: `Code = "VAL_SCHEMA_004"`, `Field = evaluation.InstanceLocation.ToString()` (JSON pointer path), `Message` including the keyword and error detail from the evaluation, `Category = ValidationErrorCategory.Schema`, `Details` dictionary containing the schema location and keyword. Ensure `OutputFormat.List` is used so nested violations are flattened.
- [x] T038 [US3] Build and run all US3 tests — verify T034-T036 pass, verify all previous tests still pass in `tests/Sorcha.Validator.Service.Tests`

**Checkpoint**: Schema errors are detailed with JSON paths and specific violation descriptions.

---

## Phase 5: User Story 4 — Chain Validation Detects Fork Attempts (Priority: P3)

**Goal**: Detect when two transactions reference the same predecessor, indicating a potential fork.

**Independent Test**: Submit a transaction referencing a previous transaction that already has a known successor, verify a warning/error is returned.

### Tests for User Story 4

- [ ] T039 [P] [US4] Write test `ValidateChainAsync_ForkDetected_ReturnsWarning` — mock `GetTransactionAsync()` to return the previous transaction, and mock an additional call (e.g., `GetTransactionsAsync` or check Metadata) indicating the predecessor already has a successor, assert error code `VAL_CHAIN_005` with `IsFatal == false` in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`

### Implementation for User Story 4

- [ ] T040 [US4] Implement fork detection in `ValidateChainAsync` in `src/Services/Sorcha.Validator.Service/Services/ValidationEngine.cs` — after verifying the previous transaction exists (T029), query the register for transactions that also reference the same `PreviousTransactionId` (use `GetTransactionsAsync` or metadata check). If any other transaction already claims the same predecessor, add a non-fatal warning error with code `VAL_CHAIN_005`, category `Chain`, `IsFatal = false`, message "Potential fork detected: predecessor transaction already has a known successor"
- [ ] T041 [US4] Build and run all US4 tests — verify T039 passes, verify all previous tests still pass in `tests/Sorcha.Validator.Service.Tests`

**Checkpoint**: Fork detection provides defense-in-depth warning for duplicate predecessor references.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final verification, regression testing, and documentation

- [x] T042 Run full Validator Service test suite — `dotnet test tests/Sorcha.Validator.Service.Tests --verbosity normal` and verify all tests pass (pre-existing + new)
- [x] T043 Run full solution build — `dotnet build` from repo root and verify no compilation errors or warnings across all projects
- [x] T044 Verify no regressions in dependent test suites — run `dotnet test tests/Sorcha.Blueprint.Service.Tests` and `dotnet test tests/Sorcha.Blueprint.Engine.Tests` to confirm no impact
- [x] T045 Update `specs/020-validator-engine-validation/tasks.md` — mark all tasks complete

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **US1 Schema Validation (Phase 2)**: Depends on Setup (T001-T006) completion
- **US2 Chain Validation (Phase 3)**: Depends on Setup (T001-T006) completion; independent of US1
- **US3 Detailed Errors (Phase 4)**: Depends on US1 (Phase 2) — enhances schema error mapping
- **US4 Fork Detection (Phase 5)**: Depends on US2 (Phase 3) — extends chain validation
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **US1 (P1)**: Can start after Setup — no dependencies on other stories
- **US2 (P1)**: Can start after Setup — no dependencies on other stories, can run in parallel with US1
- **US3 (P2)**: Depends on US1 — refines error reporting from US1's implementation
- **US4 (P3)**: Depends on US2 — extends chain validation from US2's implementation

### Within Each User Story

- Tests written FIRST and verified to FAIL before implementation
- Implementation completes all tests
- Build verification confirms no regressions

### Parallel Opportunities

- T001-T004 in Setup can run in sequence (same or dependent files)
- T007-T014 (US1 tests) can ALL run in parallel (all write to same test file but different test methods)
- T018-T028 (US2 tests) can ALL run in parallel
- **US1 and US2 can run in parallel** after Setup completes (different methods in same file, but independent logic)
- T034-T036 (US3 tests) can run in parallel
- T042-T044 (Polish) can run in parallel

---

## Parallel Example: User Story 1 + 2

```bash
# After Setup (Phase 1) completes, US1 and US2 can start in parallel:

# Agent A works on US1:
# T007-T014: Write all schema validation tests
# T015-T016: Implement ValidateSchemaAsync
# T017: Verify US1 tests pass

# Agent B works on US2 simultaneously:
# T018-T028: Write all chain validation tests
# T029-T032: Implement ValidateChainAsync
# T033: Verify US2 tests pass
```

---

## Implementation Strategy

### MVP First (US1 + US2 — Both P1)

1. Complete Phase 1: Setup (T001-T006)
2. Complete Phase 2: US1 Schema Validation (T007-T017)
3. Complete Phase 3: US2 Chain Validation (T018-T033)
4. **STOP and VALIDATE**: Both P1 stories independently functional
5. Run full regression (T042-T044)

### Incremental Delivery

1. Setup → Foundation ready
2. US1 Schema Validation → Invalid payloads rejected (MVP core)
3. US2 Chain Validation → Chain integrity verified (MVP complete)
4. US3 Detailed Errors → Better developer experience
5. US4 Fork Detection → Defense-in-depth
6. Polish → Full regression, docs

---

## Notes

- [P] tasks = different files or independent test methods, no dependencies
- [Story] label maps task to specific user story for traceability
- All tests are in `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs`
- All implementation is in `src/Services/Sorcha.Validator.Service/Services/ValidationEngine.cs`
- The existing `CreateError()` helper method in ValidationEngine should be used for consistent error creation
- The existing `CreateFailureResult()` helper should be used for consistent failure result creation
- JsonSchema.Net uses `Json.Schema` namespace (not `JsonSchema.Net`)
- `EvaluationResults.IsValid` is the top-level pass/fail; `.Details` contains per-node results
