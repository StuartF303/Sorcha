# Feature Specification: Validator Service

**Feature Branch**: `validator-service`
**Created**: 2025-12-03
**Status**: Design Complete (0% Implementation)
**Input**: Derived from `.specify/specs/sorcha-validator-service.md`

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Build Dockets (Priority: P0)

As a validator node, I need to build dockets (blocks) from pending transactions so that the blockchain can progress.

**Why this priority**: Core blockchain functionality - without docket building, no transactions are confirmed.

**Independent Test**: Can be tested by adding transactions to MemPool and building a docket.

**Acceptance Scenarios**:

1. **Given** pending transactions in MemPool, **When** I POST to `/api/validation/dockets/build`, **Then** a new docket is created with validated transactions.
2. **Given** invalid transactions in MemPool, **When** building docket, **Then** invalid transactions are rejected and excluded.
3. **Given** no pending transactions, **When** building docket, **Then** error indicates empty MemPool.
4. **Given** transactions, **When** docket is built, **Then** correct PreviousHash links to previous docket.

---

### User Story 2 - Validate Dockets (Priority: P0)

As a validator node, I need to validate incoming dockets from peers so that I can verify blockchain integrity.

**Why this priority**: Essential for consensus and chain integrity.

**Independent Test**: Can be tested by submitting valid and invalid dockets for validation.

**Acceptance Scenarios**:

1. **Given** a valid docket, **When** I POST to `/api/validation/dockets/validate`, **Then** validation succeeds.
2. **Given** a docket with invalid hash, **When** validated, **Then** hash mismatch error is returned.
3. **Given** a docket with invalid signatures, **When** validated, **Then** signature verification fails.
4. **Given** a docket with broken chain link, **When** validated, **Then** chain integrity error is returned.

---

### User Story 3 - Transaction Submission (Priority: P0)

As a service, I need to submit transactions to the MemPool so that they can be included in future dockets.

**Why this priority**: Entry point for all blockchain transactions.

**Independent Test**: Can be tested by submitting a transaction and verifying it appears in MemPool.

**Acceptance Scenarios**:

1. **Given** a valid transaction, **When** I POST to `/api/validation/transactions/add`, **Then** transaction is added to MemPool.
2. **Given** an invalid transaction, **When** submitted, **Then** validation errors are returned.
3. **Given** full MemPool, **When** submitting, **Then** appropriate error is returned.
4. **Given** duplicate transaction, **When** submitted, **Then** duplicate is rejected.

---

### User Story 4 - Consensus Coordination (Priority: P1)

As a validator network, I need to achieve distributed consensus so that all validators agree on docket validity.

**Why this priority**: Required for multi-validator deployments.

**Independent Test**: Can be tested by building docket and collecting votes from multiple validators.

**Acceptance Scenarios**:

1. **Given** a built docket, **When** consensus is initiated, **Then** docket is broadcast to validators.
2. **Given** quorum of approving votes (67%), **When** collected, **Then** consensus is achieved.
3. **Given** insufficient votes or rejections, **When** timeout expires, **Then** consensus fails.
4. **Given** a validator vote, **When** received, **Then** vote signature is verified.

---

### User Story 5 - Genesis Block Creation (Priority: P1)

As an administrator, I need to create genesis blocks for new registers so that new ledgers can be initialized.

**Why this priority**: Required for creating new registers.

**Independent Test**: Can be tested by creating a genesis block and verifying it's accepted.

**Acceptance Scenarios**:

1. **Given** genesis configuration, **When** I POST to `/api/validation/genesis`, **Then** genesis docket is created.
2. **Given** genesis docket, **Then** DocketNumber is 0 and PreviousHash is all zeros.
3. **Given** genesis docket, **Then** initial validators are configured.
4. **Given** existing register, **When** creating genesis, **Then** error is returned.

---

### User Story 6 - Validation Lifecycle (Priority: P1)

As an administrator, I need to control validation lifecycle so that I can manage validator operations.

**Why this priority**: Required for operational control.

**Independent Test**: Can be tested by starting, stopping, pausing, and resuming validation.

**Acceptance Scenarios**:

1. **Given** a register, **When** I POST to start validation, **Then** validator begins processing.
2. **Given** running validation, **When** I POST to pause, **Then** validation pauses.
3. **Given** paused validation, **When** I POST to resume, **Then** validation continues.
4. **Given** running validation, **When** I POST to stop, **Then** validation stops gracefully.

---

### Edge Cases

- What happens during network partition when validators can't reach quorum?
- How does the system handle docket building during high transaction volume?
- What happens when validator's Wallet Service is unavailable for signing?
- How does chain recovery work after detecting fork?

**Note**: Per constitution VII (DDD terminology), "Participant" is used instead of "user" where applicable.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST build dockets from MemPool transactions
- **FR-002**: System MUST validate incoming dockets from peers
- **FR-003**: System MUST verify transaction signatures via Wallet Service
- **FR-004**: System MUST validate transaction payloads against Blueprint schemas
- **FR-005**: System MUST compute and verify SHA256 docket hashes
- **FR-006**: System MUST verify PreviousHash linkage for chain integrity
- **FR-007**: System MUST create genesis blocks for new registers
- **FR-008**: System MUST implement distributed consensus (quorum-based)
- **FR-009**: System MUST manage per-Register MemPools with size limits
- **FR-010**: System MUST support admin APIs for start/stop/pause/resume
- **FR-011**: System MUST expose metrics for docket building and validation
- **FR-012**: System SHOULD handle transaction expiration in MemPool
- **FR-013**: System SHOULD support enclave execution (SGX/SEV/HSM)
- **FR-014**: System SHOULD validate disclosure rules for privacy
- **FR-015**: System COULD support custom validation rules (pluggable)

### Key Entities

- **Docket**: Blockchain block containing validated transactions
- **Transaction**: Signed record of Blueprint Action execution
- **ConsensusVote**: Validator vote with signature
- **MemPool**: Per-register memory pool of pending transactions
- **GenesisConfig**: Configuration for genesis block creation

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Docket build time under 5 seconds for 100 transactions
- **SC-002**: Docket validation time under 2 seconds for 100 transactions
- **SC-003**: Consensus coordination under 30 seconds for 3 validators
- **SC-004**: MemPool throughput over 1000 transactions/second
- **SC-005**: API latency under 500ms (P95)
- **SC-006**: Test coverage over 90% for Core library
- **SC-007**: Test coverage over 70% for Service layer
- **SC-008**: Service uptime 99.9%
