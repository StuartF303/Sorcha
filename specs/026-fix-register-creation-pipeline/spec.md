# Feature Specification: Register Creation Pipeline Fix

**Feature Branch**: `026-fix-register-creation-pipeline`
**Created**: 2026-02-08
**Status**: Draft
**Input**: Fix 8 issues in the register creation flow affecting payload persistence, docket creation reliability, peer advertisement, and validator monitoring.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Genesis Data Persists Through Docket Pipeline (Priority: P1)

When an administrator creates a new register through the CLI or API, the genesis transaction containing the control record (ownership attestations, register metadata) must be fully preserved when the validator writes the docket to permanent storage. Currently, the control record payload is lost in transit because the docket write maps transaction data with empty payloads.

**Why this priority**: Without payload persistence, the register's founding document (control record with cryptographic attestations) is permanently lost. This makes the register's ownership unverifiable and the genesis transaction meaningless.

**Independent Test**: Create a register via the CLI, wait for the docket pipeline to complete, then retrieve the genesis docket and verify the transaction payload contains the original control record with all attestation data intact.

**Acceptance Scenarios**:

1. **Given** a register is created via the two-phase flow (initiate/sign/finalize), **When** the validator builds and writes the genesis docket to the Register Service, **Then** the genesis transaction stored in permanent storage contains the full control record payload with all attestation data.
2. **Given** a genesis transaction is in the validator memory pool, **When** the docket is written to the Register Service, **Then** the transaction's payload count, payload data, payload hash, sender wallet, and wallet access fields are all correctly populated.
3. **Given** a genesis docket has been written, **When** a user queries the docket's transactions, **Then** the control record can be deserialized back to a valid RegisterControlRecord with matching attestation signatures.

---

### User Story 2 - Genesis Docket Creation Completes Reliably (Priority: P1)

When a register is created, the genesis docket must be built and written to permanent storage without silent failures. If a write fails, the system must retry rather than permanently marking the genesis as complete. If the register height check fails, the system must propagate the error rather than silently skipping genesis docket creation.

**Why this priority**: Silent failures in genesis docket creation leave registers in a permanently broken state (height 0, no docket, no persisted transactions) with no mechanism for recovery.

**Independent Test**: Create a register, verify the genesis docket appears in the Register Service within 30 seconds, and confirm the register height is updated to 0 (genesis docket number).

**Acceptance Scenarios**:

1. **Given** a genesis transaction is in the validator memory pool and the register is monitored, **When** the docket build trigger fires, **Then** the genesis docket is built and written to the Register Service within one trigger cycle (default 10 seconds).
2. **Given** the genesis docket write to the Register Service fails (network error, service unavailable), **When** the next trigger cycle fires, **Then** the system retries the genesis docket creation (up to 3 attempts total) instead of skipping it permanently.
3. **Given** the register height check fails during genesis detection, **When** the error is logged, **Then** the error is propagated so the docket build trigger can retry on the next cycle.
4. **Given** a genesis docket write succeeds, **When** the system records completion, **Then** subsequent trigger cycles skip genesis creation and only process new transactions.

---

### User Story 3 - Register Advertise Flag Respected (Priority: P2)

When a user creates a register and specifies it should be advertised (public), the register is created with the correct advertise flag. When the advertise flag is changed after creation, the system notifies the Peer Service to update network visibility accordingly.

**Why this priority**: Without respecting the advertise flag, all registers are created as private regardless of user intent, and the peer network cannot discover any registers for replication.

**Independent Test**: Create a register with advertise=true, verify the register is stored with advertise=true, then update it to advertise=false and verify the change persists.

**Acceptance Scenarios**:

1. **Given** a user initiates register creation with advertise intent, **When** the register is finalized, **Then** the register is stored with the correct advertise flag (not hardcoded to false).
2. **Given** a register exists with advertise=false, **When** a user updates the register to advertise=true, **Then** the change is persisted and the Peer Service is notified to begin advertising the register.
3. **Given** a register exists with advertise=true, **When** a user updates the register to advertise=false, **Then** the change is persisted and the Peer Service is notified to stop advertising the register.

---

### User Story 4 - Peer Service Register Advertisement (Priority: P2)

When a register is created as public (advertise=true), the Peer Service is notified and begins advertising the register to the peer network. Other nodes can discover the register and subscribe for replication.

**Why this priority**: Without peer advertisement, registers remain invisible to the network, defeating the purpose of a distributed ledger. Replication cannot begin until peers know about the register.

**Independent Test**: Create a public register, then query the Peer Service's available registers endpoint to verify it appears in the list.

**Acceptance Scenarios**:

1. **Given** a register is created with advertise=true, **When** the creation flow completes, **Then** the Peer Service includes the register in its local advertisement list.
2. **Given** the Peer Service is advertising a register, **When** a peer exchange or heartbeat occurs, **Then** the register appears in the peer's advertised register list.
3. **Given** a register's advertise flag is changed from true to false, **When** the Peer Service is notified, **Then** the register is removed from the local advertisement list.

---

### User Story 5 - Validator Monitoring Visibility (Priority: P3)

Administrators can query the Validator Service to see which registers are currently being monitored for docket building. This provides visibility into the validator's workload and confirms that newly created registers are properly registered for processing.

**Why this priority**: Without monitoring visibility, administrators cannot verify that register creation completed successfully at the validator level, making troubleshooting difficult.

**Independent Test**: Create a register, then call the monitoring endpoint and verify the register ID appears in the monitored list.

**Acceptance Scenarios**:

1. **Given** a register has been created and its genesis transaction submitted to the validator, **When** an administrator queries the monitoring endpoint, **Then** the register ID appears in the list of monitored registers.
2. **Given** multiple registers are being monitored, **When** the monitoring endpoint is queried, **Then** all monitored register IDs are returned with a total count.

---

### User Story 6 - Register Service Test Suite Restored (Priority: P3)

The Register Service test suite compiles and runs successfully, covering the register creation orchestrator, system register repository, query API, and SignalR hub functionality.

**Why this priority**: Broken tests prevent regression detection and block confidence in the register creation pipeline fixes.

**Independent Test**: Run the Register Service test suite and verify all tests compile and pass (excluding pre-existing failures unrelated to this feature).

**Acceptance Scenarios**:

1. **Given** the Register Service test project, **When** `dotnet test` is executed, **Then** the project compiles with zero errors.
2. **Given** the test project compiles, **When** tests are run, **Then** all register creation orchestrator tests pass with the updated constructor and model changes.
3. **Given** the test project compiles, **When** tests are run, **Then** the SignalR hub tests use the correct return types for xUnit v3 compatibility.

---

### Edge Cases

- What happens when the Validator Service is unavailable during register creation? The orchestrator already throws and prevents register persistence, maintaining atomicity.
- What happens when the Peer Service is unavailable during advertisement? The register should still be created successfully; advertisement failure is logged but does not block register creation.
- What happens when a register is created but the docket build trigger hasn't fired yet? The register exists at height 0 with the genesis transaction in the memory pool; the next trigger cycle processes it.
- What happens when two registers are created simultaneously? Each gets its own monitoring entry and independent docket build cycle.
- What happens when the genesis docket write fails repeatedly? The system retries up to 3 times. After 3 failures, the register is unmonitored and a warning is logged for administrator attention.
- What happens when the register does not yet exist in the Register Service when the validator checks its height? The error is propagated rather than silently returning "no genesis needed", allowing retry on the next cycle.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST map all transaction fields (payload data, payload count, payload hash, sender wallet, wallet access) when writing docket transactions from the validator memory pool to the Register Service.
- **FR-002**: System MUST only mark a genesis docket as "written" after a confirmed successful write to the Register Service. Failed writes MUST NOT set the completion flag.
- **FR-003**: System MUST propagate errors from register height checks in genesis detection rather than catching all exceptions and returning false.
- **FR-004**: System MUST create registers with the advertise flag value specified in the creation request, not hardcoded to false.
- **FR-005**: System MUST notify the Peer Service asynchronously (fire-and-forget) when a register's advertise flag is set to true (either during creation or update). Notification failure MUST be logged but MUST NOT block the caller.
- **FR-006**: System MUST notify the Peer Service asynchronously (fire-and-forget) when a register's advertise flag is changed from true to false. Notification failure MUST be logged but MUST NOT block the caller.
- **FR-007**: System MUST provide an endpoint to query the list of registers currently monitored by the Validator Service for docket building.
- **FR-008**: System MUST compile the Register Service test suite with zero errors and pass all tests related to register creation, SignalR hubs, system register repository, and query APIs.
- **FR-009**: System MUST define a well-known constant for the genesis transaction blueprint identifier rather than using a hardcoded magic string.
- **FR-010**: System MUST retry genesis docket creation up to 3 times on failure. After 3 failed attempts, the system MUST stop monitoring the register and log a warning for administrator attention.

### Key Entities

- **RegisterControlRecord**: The founding document of a register containing ownership attestations, register name, tenant ID, and metadata. This is the payload of the genesis transaction and must survive the full docket pipeline.
- **Genesis Transaction**: The first transaction in a register's chain, containing the control record. Has a genesis transaction type, system sender wallet, and no previous transaction.
- **Genesis Docket**: The first docket (number 0) in a register's chain, containing the genesis transaction. Has no previous hash.
- **Register Advertisement**: A notification to the Peer Service that a register is available for discovery and replication by other nodes in the network.
- **Monitoring Registry**: A persistent set of register IDs that the Validator Service is actively monitoring for docket building triggers.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Register creation via CLI produces a register with a genesis docket containing the full control record payload, verifiable by querying the docket's transactions within 30 seconds of creation.
- **SC-002**: Genesis docket write failures do not permanently prevent docket creation; the system recovers and completes the genesis docket on subsequent attempts.
- **SC-003**: Registers created with advertise=true appear in the Peer Service's available registers list without manual intervention.
- **SC-004**: The validator monitoring endpoint returns all monitored register IDs, matching the count of registers that have had genesis transactions submitted.
- **SC-005**: The Register Service test suite compiles with zero errors and achieves full test passage (baseline: 0 passing due to compilation errors; target: all tests pass).
- **SC-006**: All existing validator tests continue to pass (baseline: 210 pass, 0 fail) after the pipeline fixes.

## Clarifications

### Session 2026-02-08

- Q: What retry behavior should apply when genesis docket creation fails? → A: Retry up to 3 times, then log a warning and stop monitoring the register.
- Q: How should the Register Service notify the Peer Service of advertise flag changes? → A: Fire-and-forget: notify asynchronously, log failure but don't block the caller.

## Assumptions

- The Peer Service advertisement is a best-effort notification; failure to advertise does not block register creation.
- Single-validator mode (no consensus engine) remains supported for development and testing.
- The existing two-phase register creation flow (initiate/sign/finalize) is correct and does not need structural changes.
- The DocketBuildConfiguration defaults (10-second time threshold, 50-transaction size threshold) are appropriate for genesis docket creation since the time threshold is always met for new registers (last build time initialized to epoch).
- The initiate/finalize request models do not currently include an advertise field; one will need to be added to the request model.
- The genesis blueprint identifier convention is acceptable but should be defined as a named constant rather than a magic string.
