# Feature Specification: Validator Engine - Schema & Chain Validation

**Feature Branch**: `020-validator-engine-validation`
**Created**: 2026-02-06
**Status**: Draft
**Input**: User description: "Implement real schema validation and chain validation in the Validator Service's ValidationEngine. The ValidationEngine currently has two critical stubs: (1) Schema validation only checks that an action exists but never validates submitted payload data against the action's JSON schema definition. (2) Chain validation is just a placeholder — it never calls the Register Service to verify blockchain integrity (previous transaction hash linkage, no gaps, no forks). Replace both stubs with real implementations."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Schema Validation Rejects Invalid Payload Data (Priority: P1)

When a participant submits a transaction containing payload data for a blueprint action, the Validator Service must evaluate that payload against the action's defined JSON schema. If the data does not conform to the schema (missing required fields, wrong types, invalid values), the transaction must be rejected before it reaches the ledger. Today, the validator only checks that the action exists in the blueprint — it never inspects the payload data itself, meaning malformed or incomplete data can be committed to the ledger.

**Why this priority**: This is the most critical gap. Without schema validation, the ledger has no data quality guarantee — any arbitrary JSON can be committed as long as the action ID is valid. This undermines the entire purpose of blueprint-defined schemas.

**Independent Test**: Can be fully tested by submitting transactions with intentionally invalid payload data (missing required fields, wrong types, extra disallowed properties) and verifying they are rejected with clear error messages identifying which schema rules were violated.

**Acceptance Scenarios**:

1. **Given** a blueprint action defines a JSON schema requiring fields `name` (string) and `amount` (number), **When** a transaction is submitted with payload `{"name": "test"}` (missing `amount`), **Then** schema validation fails with an error indicating the missing required property.
2. **Given** a blueprint action defines a JSON schema requiring `amount` as a number, **When** a transaction is submitted with payload `{"name": "test", "amount": "not-a-number"}`, **Then** schema validation fails with a type mismatch error.
3. **Given** a blueprint action defines a JSON schema, **When** a transaction is submitted with payload that fully conforms to the schema, **Then** schema validation succeeds and the transaction proceeds to the next validation step.
4. **Given** a blueprint action has no defined schema (DataSchemas is null or empty), **When** a transaction is submitted, **Then** schema validation is skipped (not enforced) and the transaction proceeds.
5. **Given** schema validation is disabled via configuration (`EnableSchemaValidation = false`), **When** any transaction is submitted, **Then** schema validation is skipped entirely.

---

### User Story 2 - Chain Validation Verifies Transaction Linkage (Priority: P1)

When a transaction references a previous transaction (via previous hash or chain linkage), the Validator Service must verify that the referenced transaction actually exists in the Register Service and belongs to the same register. This ensures the ledger maintains an unbroken, verifiable chain of transactions. Today, chain validation is a no-op stub — it always returns success without contacting the Register Service, meaning broken chains, gaps, and invalid references go undetected.

**Why this priority**: Chain integrity is fundamental to a distributed ledger. Without chain validation, transactions can reference non-existent predecessors, creating orphaned records and undermining auditability and trust in the ledger.

**Independent Test**: Can be fully tested by submitting transactions with various previous-hash references (valid, invalid, non-existent, wrong register) and verifying the validator correctly accepts or rejects based on actual Register Service state.

**Acceptance Scenarios**:

1. **Given** a transaction references a previous transaction hash that exists in the Register Service for the same register, **When** chain validation runs, **Then** validation succeeds.
2. **Given** a transaction references a previous transaction hash that does not exist in the Register Service, **When** chain validation runs, **Then** validation fails with an error indicating the previous transaction was not found.
3. **Given** a transaction references a previous transaction that belongs to a different register, **When** chain validation runs, **Then** validation fails with a register mismatch error.
4. **Given** a transaction has no previous transaction reference (it is the first transaction in a chain or an independent transaction), **When** chain validation runs, **Then** validation succeeds (genesis transactions are valid).
5. **Given** chain validation is disabled via configuration (`EnableChainValidation = false`), **When** any transaction is submitted, **Then** chain validation is skipped entirely.
6. **Given** a register has dockets numbered 0 through 5 with valid hash linkage, **When** a transaction is submitted to that register, **Then** docket chain validation confirms the chain is intact and validation succeeds.
7. **Given** a register has a gap in its docket sequence (e.g., docket 3 missing between 2 and 4), **When** a transaction is submitted, **Then** docket chain validation fails with a chain integrity error.
8. **Given** a docket's `PreviousHash` does not match the hash of the preceding docket, **When** a transaction is submitted, **Then** docket chain validation fails with a hash mismatch error.

---

### User Story 3 - Schema Validation Reports Detailed Errors (Priority: P2)

When schema validation detects issues, the error messages must be specific and actionable — identifying which schema rule was violated, which property failed, and what value was expected. This allows participants to understand exactly what went wrong and correct their submissions without guesswork.

**Why this priority**: Usability of the validation system depends on error quality. Generic "validation failed" messages would frustrate users and slow down workflows. Clear, detailed errors are essential for practical use.

**Independent Test**: Can be tested by submitting payloads with various types of schema violations and verifying each produces a distinct, descriptive error message.

**Acceptance Scenarios**:

1. **Given** a payload violates multiple schema rules simultaneously (missing field + wrong type on another field), **When** schema validation runs, **Then** all violations are reported in a single response (not just the first one found).
2. **Given** a payload has a nested object that violates a sub-schema, **When** schema validation runs, **Then** the error message includes the full JSON path to the offending property (e.g., `$.address.zipCode`).
3. **Given** a payload violates an enum constraint (value not in allowed set), **When** schema validation runs, **Then** the error message identifies the property, the submitted value, and the allowed values.

---

### User Story 4 - Chain Validation Detects Fork Attempts (Priority: P3)

If two different transactions both claim to follow the same previous transaction (a fork attempt), the chain validation should detect this scenario. While the current scope focuses on single-transaction validation, the system should record sufficient information that fork detection can be enforced at the docket/consensus level.

**Why this priority**: Fork prevention is important for ledger integrity but is partially handled by the consensus mechanism. Providing fork detection at the validation layer adds defense-in-depth.

**Independent Test**: Can be tested by submitting two transactions that both reference the same previous hash and verifying the system flags this as a potential conflict.

**Acceptance Scenarios**:

1. **Given** a previous transaction already has a known successor in the register, **When** a new transaction references the same previous hash, **Then** chain validation reports a warning or error about potential fork.
2. **Given** the configuration allows duplicate chain references (for re-validation scenarios), **When** a duplicate reference is submitted, **Then** the system handles it according to configuration.

---

### Edge Cases

- What happens when the Register Service is unavailable during chain validation? The system should fail gracefully with a transient error (retryable), not a permanent rejection.
- What happens when a blueprint's action schema is malformed JSON? The system should report a schema parsing error attributed to the blueprint, not the transaction.
- What happens when the payload is an empty JSON object `{}` and the schema has no required properties? Validation should succeed.
- What happens when the payload contains additional properties not defined in the schema? Behavior depends on the schema's `additionalProperties` setting — if not specified, additional properties are allowed by default per JSON Schema specification.
- What happens when a transaction's previous hash reference is an empty string vs null? Empty string should be treated as "no previous transaction" (same as null).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST evaluate transaction payload data against ALL of the blueprint action's JSON schema definitions when `EnableSchemaValidation` is enabled (if multiple schemas exist, the payload must conform to every one)
- **FR-002**: System MUST retrieve the action's schema from the blueprint obtained via the blueprint cache, using the transaction's `ActionId` to locate the correct action
- **FR-003**: System MUST support JSON Schema Draft 2020-12 for payload validation (as supported by JsonSchema.Net)
- **FR-004**: System MUST report all schema violations in a single validation result (not fail on the first violation)
- **FR-005**: System MUST include the JSON path, violation description, and error category in each schema validation error
- **FR-006**: System MUST skip schema validation when the action has no defined schemas (`DataSchemas` is null or empty)
- **FR-007**: System MUST call the Register Service to verify that a transaction's referenced previous transaction exists and belongs to the same register when `EnableChainValidation` is enabled (transaction-level chain validation)
- **FR-008**: System MUST accept transactions with no previous transaction reference (genesis/independent transactions) as valid for chain validation
- **FR-009**: System MUST treat empty-string previous hash references the same as null (no previous transaction)
- **FR-010**: System MUST handle Register Service unavailability during chain validation as a transient error, allowing the transaction to be retried
- **FR-015**: System MUST verify docket-level chain integrity when `EnableChainValidation` is enabled — confirming the register's latest docket hash matches the `PreviousHash` of any subsequent docket and that docket numbers are sequential with no gaps
- **FR-016**: System MUST report docket chain breaks (missing dockets, hash mismatches, non-sequential numbering) as fatal validation errors
- **FR-011**: System MUST log all validation outcomes (success and failure) with transaction ID, register ID, and validation duration
- **FR-012**: System MUST respect the existing `EnableSchemaValidation` and `EnableChainValidation` configuration flags to allow disabling either validation independently
- **FR-013**: System MUST handle malformed action schemas gracefully, reporting the error as a blueprint configuration issue rather than a transaction error
- **FR-014**: System MUST extend the Validator's Transaction model with a `PreviousTransactionId` field to explicitly represent chain linkage (mirroring the Register's `TransactionModel.PrevTxId`)

### Key Entities

- **Transaction**: The unit of work being validated — contains payload data, action reference, signatures, and an optional `PreviousTransactionId` linking to the prior transaction in the chain
- **Blueprint Action**: Defines the expected schema for transaction payloads — accessed via blueprint cache using blueprint ID and action ID
- **Docket**: A confirmed block of validated transactions in the register — used for chain height and hash verification
- **Validation Result**: The outcome of validation — includes success/failure status, error details with categories, and processing duration

## Clarifications

### Session 2026-02-06

- Q: The Validator's Transaction model has no previous transaction field — how should chain validation access the reference? → A: Add a `string? PreviousTransactionId` property to the Validator's `Transaction` model (mirrors Register's `TransactionModel.PrevTxId`).
- Q: When an action defines multiple DataSchemas, must the payload pass all of them or just one? → A: Payload must validate against ALL schemas (conjunction — all constraints apply).
- Q: Should chain validation operate at transaction-level, docket-level, or both? → A: Both. Verify `PreviousTransactionId` linkage at the transaction level AND verify docket hash chain and height continuity at the docket level.

## Assumptions

- The blueprint cache (`IBlueprintCache`) is already populated and functional — no need to implement blueprint fetching
- The `IRegisterServiceClient` interface already provides the necessary methods for chain validation (`GetTransactionAsync`, `ReadLatestDocketAsync`, `GetRegisterHeightAsync`)
- The `IRegisterServiceClient` is already registered in the DI container for the Validator Service
- JSON Schema evaluation follows the existing pattern established in `SchemaValidator` (Blueprint Engine) — using `JsonSchema.FromText()` and evaluating against `JsonElement`
- The `Action.DataSchemas` property (`IEnumerable<JsonDocument>?`) contains one or more JSON Schema documents that the payload must validate against
- The existing `ValidationEngineConfiguration` flags (`EnableSchemaValidation`, `EnableChainValidation`) are already wired up in configuration — no new configuration is needed

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Transactions with invalid payload data are rejected 100% of the time when schema validation is enabled and the action defines a schema
- **SC-002**: Transactions with valid payload data pass schema validation without false rejections
- **SC-003**: Transactions referencing non-existent previous transactions are rejected when chain validation is enabled
- **SC-004**: Genesis transactions (no previous reference) pass chain validation without errors
- **SC-005**: Schema validation errors include specific property paths and violation descriptions sufficient for a user to correct the payload without additional debugging
- **SC-006**: When the Register Service is temporarily unavailable, chain validation returns a retryable error rather than permanently rejecting the transaction
- **SC-007**: All existing validator tests continue to pass (no regressions)
- **SC-008**: New validation logic achieves at least 85% test coverage
