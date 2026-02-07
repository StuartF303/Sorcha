# Feature Specification: Resolve Runtime Stubs and Production-Critical TODOs

**Feature Branch**: `022-resolve-runtime-stubs`
**Created**: 2026-02-07
**Status**: Draft
**Input**: User description: "Resolve all runtime stubs and production-critical TODOs across the Sorcha platform"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Eliminate Runtime Exceptions from Stubs (Priority: P1)

A platform operator deploys Sorcha services and encounters unexpected crashes when users invoke wallet delegation updates, key recovery, or binary serialization paths. All code paths that currently throw `NotImplementedException` must either be fully implemented or gracefully degraded with clear error messaging so that no user action causes an unhandled crash.

**Why this priority**: Runtime exceptions from stubs are the highest-severity issue — they crash user workflows, produce 500 errors, and erode trust in the platform. Eliminating these is essential before any production deployment.

**Independent Test**: Can be tested by invoking each previously-stubbed operation and verifying it either completes successfully or returns a structured error response (never an unhandled exception).

**Acceptance Scenarios**:

1. **Given** a user attempts to update wallet delegation access, **When** they call the update access operation, **Then** the system processes the request without throwing an unhandled exception.
2. **Given** a user attempts key recovery with valid key data, **When** the recovery operation is invoked, **Then** the system either recovers the keys or returns a clear, actionable error.
3. **Given** a system component requests binary serialization of a transaction, **When** the serialization path is invoked, **Then** the system either serializes the data or returns an "unsupported format" response without crashing.
4. **Given** a wallet owner calls the address generation endpoint, **When** the server-side derivation path is reached, **Then** the system responds with a clear redirect to client-side derivation rather than an unhandled exception.

---

### User Story 2 - Secure Wallet and Delegation Access (Priority: P1)

A user accesses the wallet service to view wallet details or manage delegated access. Currently, any authenticated user can view any wallet regardless of ownership. The system must enforce authorization — only wallet owners and explicitly delegated users may access wallet data, and user identity must be reliably extracted from authentication tokens.

**Why this priority**: Authorization gaps are a security vulnerability. Without ownership checks, any authenticated user can view or manipulate any wallet, which is unacceptable for a financial/cryptographic platform.

**Independent Test**: Can be tested by attempting wallet operations as different users and verifying that unauthorized access is rejected with a 403 response.

**Acceptance Scenarios**:

1. **Given** a user requests wallet details, **When** they are the wallet owner, **Then** the wallet details are returned successfully.
2. **Given** a user requests wallet details, **When** they are NOT the wallet owner and have no delegation, **Then** a 403 Forbidden response is returned.
3. **Given** a user has delegated read access to a wallet, **When** they request that wallet's details, **Then** the wallet details are returned successfully.
4. **Given** a bootstrap operation creates an admin user, **When** the operation completes, **Then** a valid authentication token is returned (not deferred to a separate login call).

---

### User Story 3 - Validator Network Integration with Peer Service (Priority: P2)

Validators in the network must communicate with each other through the Peer Service for consensus operations. Currently, the signature collection, leader election heartbeats, and validator registration are simulated with stubs. Validators must be able to request docket signatures from peers via the network, broadcast heartbeats to maintain leader status, and register themselves on-chain.

**Why this priority**: Without peer integration, the consensus mechanism operates in simulation mode only. This is required for multi-validator deployments but not for single-validator development/demo environments.

**Independent Test**: Can be tested by deploying two or more validator instances and verifying that consensus rounds complete with real inter-validator communication.

**Acceptance Scenarios**:

1. **Given** a validator proposes a docket, **When** it requests signatures from peer validators, **Then** the request is sent via the peer network and real responses are collected.
2. **Given** a validator is elected leader, **When** the heartbeat interval elapses, **Then** the heartbeat is broadcast to all known peers via the peer network.
3. **Given** a new validator joins the network, **When** it registers, **Then** a registration record is persisted on-chain (not just in-memory).
4. **Given** consensus fails for a docket, **When** the failure is recorded, **Then** the failure status and reason are persisted for audit purposes.

---

### User Story 4 - Accurate Peer Node Status Reporting (Priority: P2)

Network operators monitoring the peer mesh see hardcoded zero values for system register version, total blueprints, uptime, and other operational metrics. These metrics must reflect actual system state so operators can assess node health, synchronization status, and network capacity.

**Why this priority**: Operational visibility is essential for network management. Hardcoded zeros make monitoring dashboards useless and hide real problems.

**Independent Test**: Can be tested by querying peer node status and verifying that returned values match actual system state (non-zero where applicable, changing over time).

**Acceptance Scenarios**:

1. **Given** a peer node has been running for 5 minutes, **When** its status is queried, **Then** the uptime value reflects approximately 300 seconds (not zero).
2. **Given** a system register contains 3 published blueprints, **When** a peer node reports its status, **Then** the total blueprints count is 3.
3. **Given** the system register is at version 7, **When** a heartbeat or connection status is reported, **Then** the system register version field reads 7.

---

### User Story 5 - Production-Ready Data Persistence (Priority: P2)

The pending registration store and memory pool use in-memory storage that is lost on service restart. In a multi-instance deployment, instances cannot share state. These stores must use distributed, persistent storage so that data survives restarts and is shared across instances.

**Why this priority**: Data loss on restart is acceptable for demos but not for production. Multi-instance deployments require shared state.

**Independent Test**: Can be tested by storing data, restarting the service, and verifying data survives. Also testable by running two instances and verifying both see the same data.

**Acceptance Scenarios**:

1. **Given** a pending registration is stored, **When** the service is restarted, **Then** the pending registration is still available.
2. **Given** two service instances are running, **When** one instance stores a pending registration, **Then** the other instance can retrieve it.
3. **Given** memory pool transactions are persisted, **When** the validator service restarts, **Then** previously pooled transactions are recovered.

---

### User Story 6 - Cryptographic Key Recovery and Keychain Portability (Priority: P3)

A user who has lost access to their wallet needs to recover their keys using backup key data. Additionally, users should be able to export their keychain (password-protected) for backup and import it on another device. Currently these operations return failure stubs.

**Why this priority**: Key recovery and portability are important for user self-sovereignty but are not blocking for initial production deployment. Users can work around this by creating new wallets.

**Independent Test**: Can be tested by creating a wallet, exporting the keychain, importing it on a fresh instance, and verifying the recovered keys match.

**Acceptance Scenarios**:

1. **Given** a user has valid key backup data, **When** they invoke key recovery, **Then** the original key set is reconstructed and usable for signing.
2. **Given** a user exports their keychain with a password, **When** they import it with the same password, **Then** all keys are restored.
3. **Given** a user exports their keychain, **When** import is attempted with the wrong password, **Then** the import fails with a clear error and no data corruption.

---

### User Story 7 - Transaction Version Backward Compatibility (Priority: P3)

As the transaction format evolves across versions, the system must be able to read and process transactions created with older format versions. Currently, version adapters for V1, V2, and V3 are unimplemented, and binary serialization is not available.

**Why this priority**: Version compatibility is needed for long-running production ledgers but not for new deployments. Binary serialization is an optimization, not a functional requirement for initial deployment.

**Independent Test**: Can be tested by creating transactions in each supported version format and verifying they can be deserialized and processed by the current version.

**Acceptance Scenarios**:

1. **Given** a transaction was created in V3 format, **When** the current system reads it, **Then** it is successfully deserialized and all fields are accessible.
2. **Given** a transaction was created in V1 format, **When** the current system reads it, **Then** it is adapted to the current model with appropriate defaults for missing fields.
3. **Given** binary serialization is requested, **When** the system processes the request, **Then** the system returns a structured "unsupported format" error without crashing.

---

### Edge Cases

- What happens when key recovery is attempted with corrupted key data? System must return a clear error, not crash.
- What happens when a validator tries to reach a peer that is offline? Signature collection must handle timeouts and partial responses gracefully.
- What happens when the distributed store (for pending registrations) is temporarily unavailable? The service must degrade gracefully with clear error logging rather than crash.
- What happens when a keychain export is interrupted mid-write? The partial export must not be importable (integrity check required).
- What happens when binary deserialization encounters data from an unknown future version? System must reject with a version-not-supported error.
- What happens when two validators attempt to register simultaneously with the same ID? The system must handle the conflict deterministically.

## Requirements *(mandatory)*

### Functional Requirements

**Runtime Stub Elimination:**

- **FR-001**: The system MUST NOT throw `NotImplementedException` from any code path reachable by a user or system operation.
- **FR-002**: The delegation access update operation MUST retrieve, validate, and persist access record changes using the data layer.
- **FR-003**: The wallet address generation endpoint MUST return a structured error response (not an exception) explaining that server-side derivation is not supported and directing users to client-side derivation.

**Authorization and Security:**

- **FR-004**: The wallet detail retrieval operation MUST verify that the requesting user is either the wallet owner or has been granted delegated access before returning wallet data.
- **FR-005**: The delegation management endpoints MUST extract the requesting user's identity from the authentication token, not from hardcoded or placeholder values.
- **FR-006**: The bootstrap endpoint MUST return a valid authentication token upon successful admin user creation, eliminating the need for a separate login step.

**Validator-Peer Integration:**

- **FR-007**: The signature collector MUST send docket signature requests to peer validators via the peer network service, replacing the current simulated responses.
- **FR-008**: The leader election service MUST broadcast heartbeats to peer validators via the peer network service.
- **FR-009**: The validator registry MUST persist validator registrations and approvals as on-chain transactions, not just in-memory records.
- **FR-010**: The validation engine service MUST discover active registers dynamically rather than using a static or empty list.
- **FR-011**: The consensus failure handler MUST persist failure status and reason for audit and diagnostics.
- **FR-012**: The docket build trigger MUST initiate the consensus process after building a docket.

**Peer Service Operational Data:**

- **FR-013**: Peer node status reports MUST include the actual system register version from the local data store.
- **FR-014**: Peer node status reports MUST include accurate blueprint counts and last-published timestamps.
- **FR-015**: Peer node status reports MUST include actual uptime and session identifiers.

**Data Layer Production Readiness:**

- **FR-016**: The pending registration store MUST use distributed persistent storage that survives service restarts and is accessible from multiple service instances.
- **FR-017**: The memory pool MUST support optional persistence so that pooled transactions can be recovered after a validator restart.

**Cryptographic Operations:**

- **FR-018**: The key recovery operation MUST reconstruct a key set from valid backup key data and a password.
- **FR-019**: The keychain export operation MUST produce a password-encrypted portable format.
- **FR-020**: The keychain import operation MUST restore keys from the encrypted export format with the correct password.

**Transaction Versioning:**

- **FR-021**: The transaction factory MUST provide adapters for reading transactions in V1, V2, and V3 formats and converting them to the current internal model.
- **FR-022**: The transaction serializer MUST replace binary serialization stubs with a graceful "unsupported format" error response (not an exception). Full binary serialization implementation is deferred to a future feature.

### Key Entities

- **WalletAccess**: Represents delegated access to a wallet — includes access rights, expiration, and the grantee identity.
- **KeySet**: A recovered or generated set of cryptographic keys — includes private key, public key, and derivation metadata.
- **KeyChain**: A portable, password-encrypted container of multiple key sets for backup and restore.
- **ValidatorRegistration**: An on-chain record of a validator's identity, endpoint, and approval status in the network.
- **ConsensusFailure**: A persisted record of a failed consensus round — includes docket ID, failure reason, participating validators, and timestamp.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Zero `NotImplementedException` occurrences across all service code paths — verified by static analysis and runtime testing of all previously-stubbed operations.
- **SC-002**: 100% of wallet access operations enforce ownership or delegation checks — verified by authorization test suite covering owner, delegate, and unauthorized user scenarios.
- **SC-003**: Validator consensus rounds complete with real peer-to-peer communication in a multi-validator deployment — verified by integration test with 2+ validators achieving consensus on a docket.
- **SC-004**: All peer node operational metrics reflect actual system state (non-zero where applicable) — verified by status query returning values that match known system state.
- **SC-005**: Pending registrations survive service restart — verified by storing a registration, restarting the service, and retrieving the registration.
- **SC-006**: Keychain round-trip (export then import) preserves all keys — verified by comparing original and restored key sets.
- **SC-007**: Transactions created in all supported format versions (V1-V4) can be read by the current system — verified by deserialization test suite.
- **SC-008**: All previously-identified TODO comments for production-critical items are resolved — verified by source scan showing zero production-blocking TODOs remaining.

## Assumptions

- The Peer Service gRPC contracts already define the necessary message types for signature requests and heartbeats (existing proto definitions will be used).
- The existing JWT authentication middleware is already deployed and functional in the Wallet and Tenant services; this work adds authorization checks on top of existing authentication.
- Cosmetic/UI TODOs (clipboard copy, file download, org ID from context) are explicitly out of scope for this feature.
- Blueprint Service TODOs (Redis backplane for SignalR, routing rule application, blueprint version tracking) are out of scope — these are tracked separately.
- The `ControlBlueprintVersionResolver` historical state reconstruction TODO is deferred — the current behavior (returning current config for all versions) is acceptable for initial production.
- Key recovery assumes the user provides the original mnemonic or encrypted key data — the platform does not store mnemonics.

## Scope Boundaries

**In Scope:**
- All 5 `NotImplementedException` stubs
- Authorization checks in Wallet and Delegation endpoints
- JWT claim extraction in Wallet and Delegation endpoints
- Bootstrap token generation
- Validator-Peer integration (signatures, heartbeats, registration)
- Peer node operational metric accuracy
- Distributed pending registration store
- Memory pool persistence option
- Key recovery, keychain export/import
- Transaction version adapters (V1-V3)
- Consensus failure persistence
- Register discovery for validation engine

**Out of Scope:**
- UI/cosmetic TODOs (clipboard, file download, org ID)
- Blueprint Service TODOs (Redis backplane, routing rules)
- Historical control blueprint version reconstruction
- Performance optimization TODOs
- Demo app TODOs (custom blueprint loading)
