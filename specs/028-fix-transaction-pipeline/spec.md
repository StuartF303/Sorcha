# Feature Specification: Fix Transaction Submission Pipeline

**Feature Branch**: `028-fix-transaction-pipeline`
**Created**: 2026-02-09
**Status**: Draft
**Input**: User description: "Fix the transaction submission pipeline so action transactions flow through the Validator Service mempool and docket sealing process before being persisted to the Register database."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Action Transactions Flow Through Validator Before Persistence (Priority: P1)

When a blueprint action is executed, the resulting transaction must be submitted to the Validator Service for validation and mempool staging, rather than being written directly to the Register database. The transaction remains in the Validator mempool until a docket is built, consensus is achieved, and the sealed docket (with its transactions) is written back to the Register Service. This ensures that no unvalidated or unsealed transactions exist in the Register database.

**Why this priority**: This is the core defect. Without this fix, action transactions bypass the entire validation and consensus pipeline, meaning the distributed ledger has no integrity guarantees for action data. Genesis transactions already follow the correct path — action transactions must do the same.

**Independent Test**: Can be fully tested by executing a blueprint action (e.g., Ping-Pong) and verifying that the transaction appears in the Validator mempool before it appears in the Register database, and that it only appears in the Register database after a docket is sealed.

**Acceptance Scenarios**:

1. **Given** a running blueprint instance with a valid register, **When** a participant executes an action, **Then** the transaction is submitted to the Validator Service (not the Register Service) and enters the Validator mempool.
2. **Given** an action transaction in the Validator mempool, **When** the docket build trigger fires and consensus is achieved, **Then** the transaction is written to the Register database with a docket number assigned.
3. **Given** an action transaction in the Validator mempool, **When** querying the Register Service for that transaction, **Then** the transaction is not found (it has not been persisted yet).
4. **Given** an action transaction has been submitted to the Validator, **When** the Validator rejects the transaction (invalid signature, malformed data, etc.), **Then** the Blueprint Service receives an error response and the transaction is not added to the mempool or Register database.
5. **Given** the Validator Service is temporarily unavailable, **When** an action is executed, **Then** the Blueprint Service receives a clear error and the transaction is not silently lost.

---

### User Story 2 - Register Monitors Active for Action Transactions (Priority: P1)

The Validator Service must recognise that a register has pending action transactions and include them in the next docket build cycle. Currently, only registers with genesis transactions in the mempool are monitored for docket building.

**Why this priority**: Without monitoring, even if transactions enter the mempool, no docket will ever be built for them. This is inseparable from Story 1.

**Independent Test**: Submit an action transaction to the Validator mempool and verify that the DocketBuildTriggerService detects the register, builds a docket containing the transaction, and writes it to the Register Service.

**Acceptance Scenarios**:

1. **Given** a register that already has a genesis docket sealed, **When** an action transaction is added to the Validator mempool for that register, **Then** the register is monitored and the next docket build cycle includes the transaction.
2. **Given** multiple action transactions for the same register in the mempool, **When** the docket build trigger fires, **Then** all pending transactions are included in a single docket.
3. **Given** action transactions for different registers in the mempool, **When** the docket build trigger fires, **Then** separate dockets are built per register.

---

### User Story 3 - Transaction Status Reflects Pending vs Confirmed (Priority: P2)

Users and the UI should be able to distinguish between pending transactions (submitted but not yet sealed in a docket) and confirmed transactions (sealed in a docket with a docket number). The current system publishes a "transaction:confirmed" event immediately on submission, which is incorrect.

**Why this priority**: Important for user trust and UI accuracy, but secondary to getting the pipeline itself working correctly.

**Independent Test**: Execute an action, then query the transaction status before and after docket sealing to verify the status transitions from "pending" to "confirmed".

**Acceptance Scenarios**:

1. **Given** an action transaction has been submitted to the Validator, **When** querying transaction status from the Blueprint Service or UI, **Then** the status is reported as "pending" (not yet in any docket).
2. **Given** a pending action transaction, **When** it is sealed in a docket and written to the Register database, **Then** its status changes to "confirmed" and includes the docket number.
3. **Given** a pending action transaction, **When** a "transaction:confirmed" event is published, **Then** it is published only after the transaction is written to the Register database as part of a sealed docket — not on initial submission.

---

### User Story 4 - Peer Gossip for Pending Transactions on Public Registers (Priority: P3)

For registers marked as public (advertised), pending transactions should be gossiped to remote validator nodes via the Peer Service so that multiple validators can include them in their mempool and participate in consensus.

**Why this priority**: Required for multi-validator consensus on public registers, but the current development environment uses single-validator mode, so this can be deferred without blocking the core pipeline fix.

**Independent Test**: Submit a transaction to the Validator for a public register and verify that the Peer Service receives a gossip notification containing the transaction data.

**Acceptance Scenarios**:

1. **Given** a public register with multiple validators, **When** an action transaction enters the local Validator mempool, **Then** the transaction is gossiped to peer validators via the Peer Service.
2. **Given** a private register, **When** an action transaction enters the local Validator mempool, **Then** no gossip occurs — the transaction remains local only.
3. **Given** a remote validator receives a gossiped transaction, **When** processing the gossip message, **Then** the transaction is validated and added to that validator's mempool.

---

### Edge Cases

- What happens when the Validator mempool is full? The system must reject the transaction with a clear "mempool full" error rather than silently dropping it.
- What happens when a register has no active validator? The transaction submission must fail with a meaningful error indicating that validation is unavailable.
- What happens when the same transaction is submitted twice? The Validator mempool must detect duplicates and return success (idempotent) without adding a second copy.
- What happens when a docket build fails after transactions are in the mempool? Transactions must remain in the mempool and be retried in the next build cycle.
- What happens when the Register Service rejects a sealed docket write-back? The docket distributor must retry or escalate, and transactions must not be removed from the mempool until persistence is confirmed.
- What happens to accumulated state reconstruction in the Blueprint Service when transactions are pending (not yet in the Register database)? The system must wait for the previous transaction to be sealed in a docket before allowing the next action to execute. This ensures state reconstruction always has complete, confirmed data from the Register database.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Blueprint Service MUST submit action transactions to the Validator Service instead of the Register Service.
- **FR-002**: The Validator Service MUST validate action transactions (structure, signature, payload hash) before adding them to the mempool.
- **FR-003**: The Validator Service MUST support a transaction submission endpoint that accepts action transactions from the Blueprint Service.
- **FR-004**: The Register Service MUST NOT persist action transactions on direct submission. Action transactions MUST only be written to the Register database as part of a sealed docket write-back from the Validator Service.
- **FR-005**: The Validator Service MUST monitor registers that have pending action transactions in the mempool, triggering docket builds when time or size thresholds are met.
- **FR-006**: The DocketBuildTriggerService MUST include action transactions from the mempool when building dockets for registers that already have a genesis docket.
- **FR-007**: The DocketDistributor MUST write sealed dockets (including action transactions) to the Register Service with docket numbers assigned.
- **FR-008**: The "transaction:confirmed" event MUST only be published after a transaction is persisted in the Register database as part of a sealed docket — not on initial submission.
- **FR-009**: The IValidatorServiceClient interface MUST include a method for submitting action transactions to the Validator Service.
- **FR-010**: The Peer Service MUST support gossiping pending transactions to remote validators for public registers.
- **FR-011**: The system MUST return clear error responses when transaction validation fails, the mempool is full, or the Validator Service is unavailable.
- **FR-012**: The Validator mempool MUST handle duplicate transaction submissions idempotently.
- **FR-013**: The Blueprint Service MUST wait for the previous action's transaction to be sealed in a docket before allowing the next action on the same instance to execute. This ensures state reconstruction always operates on confirmed data from the Register database.
- **FR-014**: The Blueprint Service MUST provide a mechanism to poll or be notified when a submitted transaction has been confirmed (sealed in a docket), so that subsequent actions can proceed.

### Key Entities

- **Action Transaction**: A signed transaction produced by blueprint action execution, containing payload data, sender wallet, recipients, signature, and metadata (blueprint ID, instance ID, action ID). Currently incorrectly written directly to Register; must flow through Validator mempool first.
- **Validator Mempool**: In-memory staging area in the Validator Service where transactions await docket inclusion. Currently only receives genesis transactions; must be extended to receive action transactions.
- **Docket**: A sealed group of transactions that have achieved consensus. Written to the Register database as a unit with a docket number. The DocketBuildTriggerService polls periodically to build dockets from mempool contents.
- **Pending Transaction Status**: A new concept representing a transaction that has been submitted to the Validator but not yet sealed in a docket. Must be queryable for UI display and state reconstruction.

## Assumptions

- The existing genesis transaction pipeline (RegisterCreationOrchestrator -> Validator -> Docket -> Register) is correct and serves as the reference implementation.
- The Validator's existing `POST /api/v1/transactions/validate` endpoint can be adapted or used as-is for action transaction submission.
- Single-validator consensus (auto-approve) remains the default for development; multi-validator consensus is an enhancement.
- The Validator mempool already has logic for transaction storage, deduplication, and removal — it just needs to receive action transactions.
- The DocketBuildTriggerService already handles post-genesis docket builds when the mempool has transactions — it just never gets action transactions into the mempool.
- The existing walkthrough scripts (PingPong, OrganizationPingPong) will serve as integration test cases for the fixed pipeline.
- The Register Service's direct transaction submission endpoint (`POST /api/registers/{registerId}/transactions`) will be repurposed as an internal-only endpoint for the Validator's docket write-back, or removed entirely if the docket write-back endpoint already covers the use case.
- Action execution is sequential per instance: the next action cannot execute until the previous action's transaction is sealed in a docket and written to the Register database. This simplifies state reconstruction at the cost of added latency (equal to the docket build interval) between consecutive actions on the same instance.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Action transactions are never found in the Register database without a docket number — 100% of action transactions in the database have been sealed in a docket.
- **SC-002**: The Ping-Pong walkthrough completes with all 10 action transactions appearing in the Register database with docket numbers assigned (currently 0 of 10 have docket numbers).
- **SC-003**: The time from action execution to transaction appearing in the Register database (with docket number) is within 2x the configured docket build time threshold.
- **SC-004**: Transaction validation failures (bad signature, malformed data) return clear error messages to the Blueprint Service within 2 seconds.
- **SC-005**: All existing tests continue to pass after the pipeline changes.
- **SC-006**: The Organization Ping-Pong walkthrough passes end-to-end with the new pipeline (10/10 steps, 40/40 actions, all transactions sealed in dockets).
