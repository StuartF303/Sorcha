# Feature Specification: Register Service

**Feature Branch**: `register-service`
**Created**: 2025-12-03
**Status**: Draft
**Input**: Derived from `.specify/specs/sorcha-register-service.md`

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Register Management (Priority: P0)

As a system administrator, I need to create and manage distributed ledger registers so that transactions can be organized into separate ledgers.

**Why this priority**: Core functionality - registers are the foundation for transaction storage.

**Independent Test**: Can be tested by creating a register, listing registers, and deleting a register.

**Acceptance Scenarios**:

1. **Given** a valid register name and tenant, **When** I POST to `/api/registers`, **Then** a register is created with a unique ID (32-char GUID without hyphens).
2. **Given** an existing register ID, **When** I GET `/api/registers/{id}`, **Then** the register details including height, status, and metadata are returned.
3. **Given** multiple registers in a tenant, **When** I GET `/api/registers`, **Then** only registers for my tenant are returned.
4. **Given** I am in a different tenant, **When** I attempt to access another tenant's register, **Then** a 404 Not Found is returned (no information leakage).

---

### User Story 2 - Transaction Storage (Priority: P0)

As a blockchain participant, I need to store validated transactions in registers so that they are immutable and auditable.

**Why this priority**: Core functionality - transactions are the primary data stored in registers.

**Independent Test**: Can be tested by inserting a transaction and retrieving it by ID.

**Acceptance Scenarios**:

1. **Given** a validated transaction with payloads, **When** I POST to `/api/registers/{id}/transactions`, **Then** the transaction is stored with a cryptographic hash as TxId.
2. **Given** an existing transaction ID, **When** I GET `/api/registers/{id}/transactions/{txId}`, **Then** the complete transaction including payloads is returned.
3. **Given** a transaction with blockchain metadata, **When** stored, **Then** the previous transaction ID (PrevTxId) is validated for chain integrity.
4. **Given** a transaction is stored, **When** retrieved via DID URI (`did:sorcha:register:{id}/tx/{txId}`), **Then** JSON-LD format with @context is returned.

---

### User Story 3 - Docket Management (Priority: P1)

As a consensus validator, I need to seal transactions into dockets (blocks) so that the ledger maintains chain integrity.

**Why this priority**: Essential for blockchain integrity but depends on transaction storage.

**Independent Test**: Can be tested by creating a docket with transactions and verifying the hash chain.

**Acceptance Scenarios**:

1. **Given** a list of transaction IDs, **When** I POST to `/api/registers/{id}/dockets`, **Then** a docket is created with sequential ID and hash of previous docket.
2. **Given** a docket is sealed, **When** the register height is checked, **Then** it has been incremented atomically.
3. **Given** a docket ID, **When** I GET `/api/registers/{id}/dockets/{docketId}`, **Then** the docket with all transaction IDs is returned.

---

### User Story 4 - Transaction Querying (Priority: P1)

As an application developer, I need to query transactions flexibly so that I can retrieve specific transactions based on various criteria.

**Why this priority**: Enables applications to build on top of the register service.

**Independent Test**: Can be tested by inserting transactions and querying with OData filters.

**Acceptance Scenarios**:

1. **Given** transactions in a register, **When** I GET with OData filter `$filter=SenderWallet eq '0x123'`, **Then** only transactions from that wallet are returned.
2. **Given** transactions with blueprint metadata, **When** I filter by BlueprintId, **Then** only matching transactions are returned.
3. **Given** a large result set, **When** I use `$top=10&$skip=20`, **Then** correct pagination is applied.
4. **Given** I query by recipient address, **When** GET `/api/registers/{id}/transactions/by-recipient/{address}`, **Then** all transactions to that wallet are returned.

---

### User Story 5 - Real-Time Notifications (Priority: P2)

As an application user, I need to receive real-time updates when register state changes so that I can react to new transactions and dockets.

**Why this priority**: Enhances user experience but not required for core functionality.

**Independent Test**: Can be tested by subscribing to SignalR hub and verifying notifications on transaction insert.

**Acceptance Scenarios**:

1. **Given** I am connected to the SignalR hub and subscribed to a register, **When** a new transaction is stored, **Then** I receive a notification.
2. **Given** I am subscribed, **When** a new docket is sealed, **Then** I receive a notification with the new height.
3. **Given** I disconnect and reconnect, **When** I resubscribe, **Then** I can receive notifications again.

---

### User Story 6 - Blockchain Gateway Integration (Priority: P2)

As a platform integrator, I need to interact with multiple blockchain networks through a unified gateway so that transactions can be recorded to external blockchains.

**Why this priority**: Enables enterprise interoperability but Sorcha native is primary.

**Independent Test**: Can be tested by submitting a transaction via Ethereum gateway and verifying status.

**Acceptance Scenarios**:

1. **Given** Ethereum gateway is configured, **When** I submit a transaction with blockchain target, **Then** it is submitted to Ethereum via Nethereum.
2. **Given** a blockchain transaction ID, **When** I query status, **Then** the current confirmation status is returned.
3. **Given** external gateway is unavailable, **When** I submit, **Then** graceful fallback to Sorcha native register occurs.

---

### Edge Cases

- What happens when a transaction references a non-existent PrevTxId?
- How does the system handle concurrent docket creation for the same register?
- What happens when MongoDB shard is unavailable?
- How does chain validation recover from detected corruption?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST create registers with unique 32-character IDs (GUID without hyphens)
- **FR-002**: System MUST store transactions with 64-character hex hash TxIds
- **FR-003**: System MUST maintain transaction chain integrity via PrevTxId links
- **FR-004**: System MUST seal transactions into dockets with sequential IDs
- **FR-005**: System MUST maintain docket chain integrity via PreviousHash links
- **FR-006**: System MUST update register height atomically when docket is sealed
- **FR-007**: System MUST isolate registers by tenant
- **FR-008**: System MUST support OData queries ($filter, $select, $orderby, $top, $skip)
- **FR-009**: System MUST index transactions by sender and recipient addresses
- **FR-010**: System MUST provide SignalR hub for real-time notifications
- **FR-011**: System MUST publish events for register and transaction state changes
- **FR-012**: System MUST support JSON-LD format with DID URIs
- **FR-013**: System MUST provide OpenAPI documentation for all endpoints
- **FR-014**: System MUST support pluggable storage backends (MongoDB, PostgreSQL)

### Key Entities

- **Register**: Distributed ledger with unique ID, name, height, status, and tenant
- **TransactionModel**: Signed transaction with payloads, metadata, and chain links
- **Docket**: Sealed block of transactions with hash and state
- **PayloadModel**: Encrypted data with wallet-based access control

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Transaction insert completes in under 100ms (P99)
- **SC-002**: Transaction query by ID completes in under 50ms (P99)
- **SC-003**: System supports 1,000+ transactions/second per register
- **SC-004**: System supports 10,000+ registers per installation
- **SC-005**: System supports 1,000,000+ transactions per register
- **SC-006**: Unit test coverage exceeds 90%
- **SC-007**: All APIs documented with OpenAPI and Scalar UI
- **SC-008**: 99.9% uptime SLA
