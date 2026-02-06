# Quickstart: Validator Engine - Schema & Chain Validation

**Branch**: `020-validator-engine-validation` | **Date**: 2026-02-06

## What This Feature Does

Replaces two stub implementations in the Validator Service's `ValidationEngine`:

1. **Schema Validation** (`ValidateSchemaAsync`): Evaluates transaction payload data against the blueprint action's JSON Schema definitions using JsonSchema.Net. Rejects transactions with invalid data before they reach the ledger.

2. **Chain Validation** (`ValidateChainAsync`): Verifies transaction chain integrity by calling the Register Service to confirm previous transaction references exist and docket hash chains are unbroken.

## Files Changed

### Source (4 files modified, 0 new)

| File | Change |
|------|--------|
| `src/Services/Sorcha.Validator.Service/Services/ValidationEngine.cs` | Replace schema and chain stubs with real implementations; add IRegisterServiceClient dependency |
| `src/Services/Sorcha.Validator.Service/Models/Transaction.cs` | Add `PreviousTransactionId` property |
| `src/Services/Sorcha.Validator.Service/Program.cs` | Add `AddValidationEngine()` call if missing |
| `src/Services/Sorcha.Validator.Service/Sorcha.Validator.Service.csproj` | Add JsonSchema.Net package reference |

### Tests (1 file modified, potentially new test files)

| File | Change |
|------|--------|
| `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs` | Add schema and chain validation tests; update constructor mocks |

## Build & Test

```bash
# Build the Validator Service
dotnet build src/Services/Sorcha.Validator.Service

# Run Validator Service tests
dotnet test tests/Sorcha.Validator.Service.Tests

# Verify no regressions
dotnet test tests/Sorcha.Validator.Service.Tests --verbosity normal
```

## Key Design Decisions

1. **JsonSchema.Net directly** (not reusing Blueprint.Engine's SchemaValidator): The Validator has `JsonElement` payloads already — simpler to call `JsonSchema.Evaluate(jsonElement)` directly than adapting to SchemaValidator's `Dictionary<string, object>` input format.

2. **IRegisterServiceClient injected into constructor**: Standard DI pattern. The client is already registered via `AddServiceClients()`.

3. **Docket chain check limited to last 2 dockets**: Avoids unbounded reads. Checks latest docket's `PreviousHash` matches predecessor's `DocketHash`.

4. **Transient errors are non-fatal**: Register Service unavailability returns retryable error (VAL_CHAIN_TRANSIENT) — the transaction stays in the mempool for retry.
