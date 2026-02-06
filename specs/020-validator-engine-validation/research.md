# Research: Validator Engine - Schema & Chain Validation

**Branch**: `020-validator-engine-validation` | **Date**: 2026-02-06

## R1: JSON Schema Validation Pattern

**Decision**: Use JsonSchema.Net (v8.0.5) with `JsonElement`-based evaluation, following the pattern established in `Blueprint.Engine/Implementation/SchemaValidator.cs`.

**Rationale**: The codebase already uses JsonSchema.Net in the Blueprint Engine. Using the same library and pattern ensures consistency and avoids introducing competing schema validation libraries. The `Evaluate()` method with `OutputFormat.List` provides all violations at once (not fail-fast), which matches FR-004.

**Alternatives considered**:
- NJsonSchema: Different API surface, not used elsewhere in codebase
- Manual validation: Not maintainable for complex schemas
- Reusing SchemaValidator directly: Blueprint.Engine's SchemaValidator operates on `Dictionary<string, object>` + `JsonNode` — the Validator Service has `JsonElement` payloads directly, so it's simpler to use JsonSchema.Net directly rather than adapting the input format

**Key implementation details**:
- `Action.DataSchemas` is `IEnumerable<JsonDocument>?` — each `JsonDocument` is a JSON Schema
- Convert each `JsonDocument` to text, then `JsonSchema.FromText()` to parse
- Transaction `Payload` is already `JsonElement` — pass directly to `Evaluate()`
- Use `EvaluationOptions { OutputFormat = OutputFormat.List, RequireFormatValidation = true }`
- Map `EvaluationResults` details to `ValidationEngineError` with JSON paths and error codes

## R2: Chain Validation — Transaction Level

**Decision**: Add `string? PreviousTransactionId` to the Validator's `Transaction` model and use `IRegisterServiceClient.GetTransactionAsync()` to verify the referenced transaction exists in the same register.

**Rationale**: The Validator's `Transaction` model currently has no chain linkage field. The Register's `TransactionModel` has `PrevTxId` (64-char hex). Adding an explicit property is type-safe and makes the chain relationship visible in the model. Using `IRegisterServiceClient.GetTransactionAsync()` is the correct way to verify existence — it queries the Register Service which is the source of truth.

**Alternatives considered**:
- Using `Metadata["PrevTxId"]`: Fragile, no type safety, easy to misspell key
- Querying by instance ID: Too broad — `GetTransactionsByInstanceIdAsync` returns all workflow transactions, not just the specific predecessor

**Key implementation details**:
- If `PreviousTransactionId` is null or empty → skip transaction-level chain check (genesis)
- Call `IRegisterServiceClient.GetTransactionAsync(registerId, previousTransactionId, ct)`
- If null returned → error VAL_CHAIN_001 "Previous transaction not found"
- If returned transaction's register ID doesn't match → error VAL_CHAIN_002 "Register mismatch"

## R3: Chain Validation — Docket Level

**Decision**: Use `IRegisterServiceClient.ReadLatestDocketAsync()` and `ReadDocketAsync()` to verify docket chain integrity — sequential numbering and hash linkage.

**Rationale**: Docket-level chain validation ensures the register's overall integrity beyond individual transactions. The `Docket` model has `PreviousHash` and `DocketHash` fields explicitly designed for this purpose. The `IRegisterServiceClient` provides `ReadDocketAsync(registerId, docketNumber)` to read specific dockets for comparison.

**Alternatives considered**:
- Only checking latest docket: Insufficient — doesn't detect mid-chain breaks
- Full chain replay on every validation: Too expensive. Instead, validate the most recent N dockets (configurable)
- Deferring entirely to consensus: Misses structural corruption that could affect all validators

**Key implementation details**:
- Get register height via `GetRegisterHeightAsync(registerId)`
- Read latest docket and its predecessor via `ReadDocketAsync(registerId, height)` and `ReadDocketAsync(registerId, height - 1)`
- Verify: `latestDocket.PreviousHash == previousDocket.DocketHash`
- Verify: `latestDocket.DocketNumber == previousDocket.DocketNumber + 1`
- For genesis (height 0): `PreviousHash` should be null → valid
- Limit chain depth check to avoid unbounded reads (check last 2 dockets by default)

## R4: IRegisterServiceClient Injection

**Decision**: Add `IRegisterServiceClient` as a constructor parameter to `ValidationEngine`.

**Rationale**: `IRegisterServiceClient` is already registered in the Validator Service's DI container via `AddServiceClients()` at line 94 of Program.cs. The ValidationEngine currently has 5 constructor parameters; adding a 6th is acceptable given this is a core integration dependency.

**Alternatives considered**:
- Method parameter injection: Breaks the `IValidationEngine` interface contract
- Service locator: Anti-pattern, violates DI principles

## R5: JsonSchema.Net Package Dependency

**Decision**: Add `JsonSchema.Net` (v8.0.5) as a NuGet dependency to `Sorcha.Validator.Service.csproj`.

**Rationale**: The Validator Service does not currently reference JsonSchema.Net. The Blueprint.Engine project uses v8.0.5. Using the same version avoids version conflicts in the solution.

## R6: ValidationEngine DI Registration

**Decision**: Ensure `AddValidationEngine()` is called in Program.cs.

**Rationale**: The extension method `ValidationEngineExtensions.AddValidationEngine()` exists but is NOT called in Program.cs. This must be fixed as part of this feature — the ValidationEngine needs to be registered as a singleton service with its hosted service.

## R7: Error Handling for Register Service Unavailability

**Decision**: Catch `HttpRequestException` and `TaskCanceledException` from IRegisterServiceClient calls during chain validation and return a transient error (non-fatal, retryable).

**Rationale**: The Register Service may be temporarily unavailable. Per FR-010, the system should not permanently reject transactions due to transient infrastructure failures. The existing `MaxRetries` and `RetryDelay` configuration handles retry logic at the batch level.

**Key implementation details**:
- Catch network-level exceptions from RegisterServiceClient
- Return `ValidationEngineError` with code "VAL_CHAIN_TRANSIENT" and `IsFatal = false`
- The validation pipeline treats non-fatal errors as retryable
- Log at Warning level (not Error) for transient failures
