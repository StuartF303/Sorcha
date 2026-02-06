# Internal Contracts: Validation Engine

**Branch**: `020-validator-engine-validation` | **Date**: 2026-02-06

This feature modifies internal service methods only — no new REST/gRPC endpoints are added. The Validator Service is an internal service that processes transactions from the memory pool.

## Modified Method Contracts

### IValidationEngine.ValidateSchemaAsync

```
Input:  Transaction (with Payload as JsonElement, BlueprintId, ActionId)
Output: ValidationEngineResult
  - IsValid: true if payload conforms to all action schemas (or action has no schemas)
  - Errors: List of ValidationEngineError with Schema/Blueprint category
```

**Behavior contract**:
- Retrieve blueprint via IBlueprintCache.GetBlueprintAsync(blueprintId)
- Locate action by parsing ActionId to int and matching Action.Id
- If action has no DataSchemas → return Success (schema not enforced)
- For each schema in DataSchemas:
  - Parse schema text → JsonSchema
  - Evaluate transaction.Payload against schema
  - Collect all violations
- If any schema fails → return Failure with all errors
- All errors use category `Schema` or `Blueprint` (for malformed schemas)

**Error codes**:
- VAL_SCHEMA_001: Blueprint not found (existing)
- VAL_SCHEMA_002: Invalid action ID format (existing)
- VAL_SCHEMA_003: Action not found in blueprint (existing)
- VAL_SCHEMA_004: Payload does not conform to schema (NEW)
- VAL_SCHEMA_005: Action schema is malformed (NEW)

### IValidationEngine.ValidateChainAsync

```
Input:  Transaction (with RegisterId, PreviousTransactionId)
Output: ValidationEngineResult
  - IsValid: true if chain integrity verified
  - Errors: List of ValidationEngineError with Chain category
```

**Behavior contract**:
- **Transaction-level**: If PreviousTransactionId is present:
  - Call IRegisterServiceClient.GetTransactionAsync(registerId, previousTransactionId)
  - Verify previous transaction exists → VAL_CHAIN_001 if not
  - Verify previous transaction's register matches → VAL_CHAIN_002 if mismatch
- **Docket-level**: Always:
  - Call IRegisterServiceClient.GetRegisterHeightAsync(registerId)
  - If height > 0, read latest docket and predecessor
  - Verify PreviousHash linkage and sequential numbering
  - VAL_CHAIN_003 for gaps, VAL_CHAIN_004 for hash mismatch
- **Transient failures**: Network errors → VAL_CHAIN_TRANSIENT (non-fatal, retryable)

## Dependency Injection Contract

### ValidationEngine Constructor (Modified)

```
Before: ValidationEngine(IOptions<Config>, IBlueprintCache, IHashProvider, ICryptoModule, ILogger)
After:  ValidationEngine(IOptions<Config>, IBlueprintCache, IHashProvider, ICryptoModule, IRegisterServiceClient, ILogger)
```

### Program.cs Registration (Required)

```
builder.Services.AddValidationEngine(builder.Configuration);
```

This calls `ValidationEngineExtensions.AddValidationEngine()` which registers:
- `ValidationEngineConfiguration` from config section
- `IValidationEngine` as singleton `ValidationEngine`
- `ValidationEngineService` as hosted service
