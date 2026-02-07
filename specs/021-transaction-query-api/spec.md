# Feature Specification: Transaction Query API

**Feature Branch**: `021-transaction-query-api`
**Created**: 2026-02-06
**Status**: Draft
**Input**: User description: "Extend IRegisterServiceClient API to support querying transactions by PreviousTransactionId for fork detection and chain integrity auditing."

## Clarifications

### Session 2026-02-06

- Q: Should the predecessor query return paginated or unpaginated results? → A: Paginated — follow existing pattern with page/pageSize parameters for consistency.
- Q: Should the query layer emit observability signals (logs/metrics) when fork-like results are detected? → A: No — the query layer is silent; callers (ValidationEngine) handle all fork observability.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Fork Detection During Validation (Priority: P1)

The validation engine needs to detect forks in the transaction chain — situations where two or more transactions claim the same predecessor. When validating a new transaction, the system queries all existing transactions that reference the same previous transaction ID. If more than one transaction is found, the validator identifies a fork and flags it for resolution.

**Why this priority**: Fork detection is the primary motivator for this feature. Without it, the ledger can silently accept conflicting transaction chains, undermining data integrity. This directly enables the Phase 5 fork detection capability deferred from the 020-validator-engine-validation feature.

**Independent Test**: Can be fully tested by submitting two transactions with the same previous transaction ID and verifying the system detects and reports the fork.

**Acceptance Scenarios**:

1. **Given** a register with a transaction chain A → B, **When** a new transaction C claims A as its predecessor (same as B), **Then** querying by A's transaction ID returns a paginated result containing both B and C, indicating a fork.
2. **Given** a register with a linear transaction chain, **When** the validator queries by any previous transaction ID, **Then** a paginated result with exactly one successor transaction is returned (no fork).
3. **Given** a previous transaction ID that no transaction references, **When** the query is executed, **Then** an empty paginated result set is returned.

---

### User Story 2 - Chain Integrity Auditing (Priority: P2)

System administrators need to audit the integrity of a register's transaction chain. By querying transactions by their predecessor links, auditors can walk the chain forward from any point and verify that no gaps, duplicates, or forks exist. The query capability provides the foundation for comprehensive chain health reports.

**Why this priority**: Auditing is a natural extension of fork detection and essential for operational confidence, but it is not blocking validation functionality.

**Independent Test**: Can be tested by walking a known chain forward using successive predecessor queries and verifying completeness and correctness.

**Acceptance Scenarios**:

1. **Given** a register with 100 sequential transactions, **When** an auditor queries forward from the genesis transaction following predecessor links, **Then** all 100 transactions are traversable in order via paginated results.
2. **Given** a register with a known gap (missing transaction), **When** the chain is walked forward, **Then** the gap is detected when a predecessor query returns no successors before the chain end.

---

### User Story 3 - Efficient Query Performance (Priority: P2)

When querying transactions by predecessor, the system must return results quickly even on registers with thousands of transactions. The query must not degrade overall system performance or require scanning all transactions in a register.

**Why this priority**: Performance is critical for real-time validation but is a quality attribute of US1, not standalone functionality.

**Independent Test**: Can be tested by querying against a register with 10,000+ transactions and measuring response time stays under acceptable thresholds.

**Acceptance Scenarios**:

1. **Given** a register with 10,000 transactions, **When** a query by previous transaction ID is executed, **Then** results are returned within 500 milliseconds.
2. **Given** concurrent validation requests each querying by predecessor, **When** 50 simultaneous queries execute, **Then** all complete within 2 seconds with no errors.

---

### Edge Cases

- What happens when the previous transaction ID is null or empty? The query should return an empty paginated result set (genesis transactions have no predecessor).
- What happens when the register ID does not exist? The query should return an empty paginated result set.
- What happens when a single previous transaction ID has more than 10 successors (severe fork)? The system should return all matching transactions across pages without truncation.
- How does the system handle a query during active write operations? The query should return consistent results using the data store's default read isolation.
- What happens when the backing data store is temporarily unavailable? The query should propagate the error as a transient failure so callers can handle retry logic.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a way to query all transactions within a given register that reference a specific previous transaction ID.
- **FR-002**: System MUST return an empty paginated result set when no transactions reference the given previous transaction ID.
- **FR-003**: System MUST return all matching transactions (across pages) when multiple transactions reference the same previous transaction ID (fork scenario).
- **FR-004**: System MUST scope queries to a single register — transactions from other registers MUST NOT appear in results.
- **FR-005**: System MUST treat null or empty previous transaction ID queries as valid, returning an empty paginated result set.
- **FR-006**: System MUST support the query through the existing service client interface used by the validation engine and other consumers.
- **FR-007**: System MUST ensure query performance does not degrade linearly with total transaction count — an efficient lookup mechanism is required.
- **FR-008**: System MUST propagate data store unavailability as a transient error to callers.
- **FR-009**: System MUST return results using the same paginated response format (page, pageSize, total count) as other multi-transaction query methods for consistency.

### Key Entities

- **Transaction**: A ledger entry within a register. Contains a unique transaction ID, the register it belongs to, and an optional reference to a previous transaction ID forming the chain.
- **Register**: A collection of transactions forming a ledger. Each transaction query is scoped to a single register.
- **Fork**: A condition where two or more transactions within the same register reference the same previous transaction ID, indicating a chain split.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Validators can detect transaction chain forks within a single validation cycle, with zero false negatives.
- **SC-002**: Predecessor-based transaction queries return results within 500 milliseconds for registers containing up to 10,000 transactions.
- **SC-003**: Chain audits can traverse an entire register's transaction chain (up to 10,000 transactions) in under 30 seconds using successive predecessor queries.
- **SC-004**: The feature introduces no regressions in existing validation, ledger, or service client functionality — all pre-existing tests continue to pass.

## Scope

### In Scope

- New query capability on the service client interface for retrieving transactions by previous transaction ID
- Corresponding backend query endpoint on the Register Service
- Efficient data lookup mechanism for the predecessor field in the storage layer
- Integration with the validation engine for fork detection

### Out of Scope

- Automatic fork resolution (detecting forks is in scope; resolving them is not)
- Changes to the transaction data model (PreviousTransactionId / PrevTxId already exists)
- UI or CLI tooling for chain auditing (API only)
- Cross-register fork detection (each query is scoped to a single register)

## Assumptions

- The PrevTxId field already exists on the transaction data model and is populated during transaction creation.
- The Register Service already stores transactions and has an existing query infrastructure that can be extended.
- The service client interface is the standard integration point for consumers and already has patterns for similar query methods.
- Genesis transactions have a null or empty PrevTxId and are not expected to appear in predecessor query results.
- Read consistency during concurrent writes follows the existing data store's default isolation level.
- The query layer is a pure data retrieval mechanism — it does not interpret results (e.g., detecting forks). All observability (logging, metrics, alerts) for fork detection is the responsibility of the calling component (e.g., ValidationEngine).

## Dependencies

- **020-validator-engine-validation**: This feature directly enables the Phase 5 fork detection that was deferred due to missing API support.
- **Register Service**: The backend query endpoint must be implemented in the Register Service.
- **Storage Layer**: The backing data store must support efficient lookup by the predecessor field.
