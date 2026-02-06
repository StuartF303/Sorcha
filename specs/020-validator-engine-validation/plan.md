# Implementation Plan: Validator Engine - Schema & Chain Validation

**Branch**: `020-validator-engine-validation` | **Date**: 2026-02-06 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/020-validator-engine-validation/spec.md`

## Summary

Replace two stub implementations in `ValidationEngine`: (1) schema validation that evaluates transaction payloads against blueprint action JSON schemas using JsonSchema.Net, and (2) chain validation that calls the Register Service to verify transaction linkage and docket hash chain integrity. This ensures data quality on the ledger and chain continuity in the distributed register.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: JsonSchema.Net 8.0.5 (new), Sorcha.ServiceClients (existing), Sorcha.Blueprint.Models (existing), Sorcha.Cryptography (existing)
**Storage**: N/A (reads from Register Service via IRegisterServiceClient, reads blueprints via IBlueprintCache)
**Testing**: xUnit + Moq + FluentAssertions (existing test infrastructure in `tests/Sorcha.Validator.Service.Tests`)
**Target Platform**: Linux containers / Windows server
**Project Type**: Microservice (internal validation engine)
**Performance Goals**: Individual transaction validation < 100ms (existing ValidationTimeout = 30s is the hard limit, but schema + chain checks should be fast)
**Constraints**: Must not break existing 50+ validation tests; must handle Register Service unavailability gracefully
**Scale/Scope**: 4 source files modified, 0 new source files, ~200 lines of new implementation code, ~400 lines of new test code

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|-----------|--------|----------|
| I. Microservices-First | PASS | No new services created; modifying existing ValidationEngine within Validator Service; uses IRegisterServiceClient for cross-service communication |
| II. Security First | PASS | JSON Schema validation enforces data integrity at input boundary; chain validation prevents ledger corruption |
| III. API Documentation | N/A | No new public APIs; internal service methods only |
| IV. Testing Requirements | PASS | New tests planned for all schema and chain validation paths; target >85% coverage for new code |
| V. Code Quality | PASS | Async/await for I/O (Register Service calls); DI for IRegisterServiceClient; nullable reference types enabled |
| VI. Blueprint Creation | N/A | Not creating blueprints; reading existing blueprint schemas |
| VII. Domain-Driven Design | PASS | Uses "Blueprint", "Action", "Disclosure" terminology consistently; Transaction model extended with explicit chain field |
| VIII. Observability | PASS | Existing structured logging preserved; validation outcomes logged with TransactionId, RegisterId, duration |

**Gate result**: ALL PASS — proceed to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/020-validator-engine-validation/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 research decisions
├── data-model.md        # Phase 1 data model changes
├── quickstart.md        # Phase 1 quick reference
├── contracts/           # Phase 1 internal contracts
│   └── validation-engine-internal.md
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
src/Services/Sorcha.Validator.Service/
├── Models/
│   └── Transaction.cs              # MODIFY: Add PreviousTransactionId
├── Services/
│   └── ValidationEngine.cs         # MODIFY: Replace schema + chain stubs
├── Program.cs                      # MODIFY: Add AddValidationEngine() call
└── Sorcha.Validator.Service.csproj  # MODIFY: Add JsonSchema.Net dependency

tests/Sorcha.Validator.Service.Tests/
└── Services/
    └── ValidationEngineTests.cs    # MODIFY: Add schema + chain tests, update constructor mocks
```

**Structure Decision**: Existing microservice structure. All changes are within the Validator Service project and its test project. No new projects, files, or architectural layers needed.

## Phase 0: Research Summary

All research documented in [research.md](research.md). Key decisions:

| # | Decision | Rationale |
|---|----------|-----------|
| R1 | Use JsonSchema.Net directly (not Blueprint.Engine's SchemaValidator) | Validator has JsonElement payloads — simpler than adapting to SchemaValidator's Dictionary input |
| R2 | Add PreviousTransactionId to Transaction model | Type-safe chain linkage; mirrors Register's TransactionModel.PrevTxId |
| R3 | Check last 2 dockets for docket-level chain validation | Avoids unbounded reads while catching recent chain breaks |
| R4 | Inject IRegisterServiceClient into ValidationEngine constructor | Standard DI; client already registered via AddServiceClients() |
| R5 | Add JsonSchema.Net 8.0.5 to Validator Service csproj | Not currently a dependency; matches Blueprint.Engine version |
| R6 | Call AddValidationEngine() in Program.cs | Extension exists but is not called — must be registered for DI |
| R7 | Transient errors are non-fatal (retryable) | Register Service unavailability shouldn't permanently reject transactions |

## Phase 1: Design Summary

All design documented in [data-model.md](data-model.md) and [contracts/](contracts/).

### Key Design Points

1. **ValidateSchemaAsync implementation**:
   - Retrieve blueprint and action (existing code stays)
   - Check if `action.DataSchemas` is null/empty → skip validation
   - For each schema in DataSchemas:
     - `JsonSchema.FromText(schema.RootElement.GetRawText())`
     - `jsonSchema.Evaluate(transaction.Payload, options)` with `OutputFormat.List`
     - Map failed evaluations to `ValidationEngineError` with JSON paths
   - All violations collected and returned together (FR-004)

2. **ValidateChainAsync implementation**:
   - **Transaction-level**: If `PreviousTransactionId` is set:
     - `GetTransactionAsync(registerId, previousTransactionId)` → verify exists + same register
   - **Docket-level**: Always:
     - `GetRegisterHeightAsync(registerId)` → get current height
     - If height > 0: read latest and predecessor dockets, verify hash linkage and sequential numbering
   - **Error handling**: Network errors → VAL_CHAIN_TRANSIENT (non-fatal)

3. **Constructor change**: Add `IRegisterServiceClient` as 6th parameter

4. **Error codes**: 6 new codes (VAL_SCHEMA_004-006, VAL_CHAIN_001-005, VAL_CHAIN_TRANSIENT)

### Constitution Re-check (Post-Design)

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | IRegisterServiceClient used for cross-service communication; no tight coupling |
| II. Security First | PASS | Schema validation at input boundary; chain validation prevents ledger corruption |
| IV. Testing Requirements | PASS | Tests planned for all new code paths with mocked dependencies |
| V. Code Quality | PASS | Async I/O, DI, nullable types all followed |
| VIII. Observability | PASS | Structured logging with TransactionId correlation maintained |

**Gate result**: ALL PASS — ready for Phase 2 (task generation via `/speckit.tasks`).

## Complexity Tracking

No constitution violations to justify. All changes are within existing patterns and infrastructure.
