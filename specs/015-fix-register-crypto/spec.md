# Feature Specification: Fix Register Creation - Fully Functional Cryptographic Register Flow

**Feature Branch**: `015-fix-register-crypto`
**Created**: 2026-01-27
**Status**: Draft
**Input**: User description: "Make the two-phase register creation flow (initiate/finalize) fully functional with real cryptographic operations end-to-end"

## Clarifications

### Session 2026-01-27

- Q: What should happen if genesis transaction submission fails after the register has been persisted? → A: Do not persist the register until genesis succeeds. Create register and submit genesis atomically; roll back register on failure. No orphaned registers without cryptographic provenance.
- Q: What should the Validator do if the system wallet is not found in the Wallet Service when processing a genesis request? → A: Return a specific error indicating platform bootstrap is incomplete (system wallet missing); log at error level. Do not auto-create or silently fail.
- Q: How should the Validator Service authenticate when calling the Wallet Service's sign endpoint? → A: Use existing service-to-service JWT authentication (request token from Tenant Service at startup or on-demand). Follow established codebase patterns.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Register Creation with Cryptographic Attestation (Priority: P1)

A platform operator creates a new register by initiating a two-phase creation flow. The system generates attestation data for each owner, returns SHA-256 hashes for signing, the operator signs each hash using their wallet, and the system verifies all signatures before creating the register with a valid genesis transaction.

**Why this priority**: This is the core flow. Without working end-to-end register creation with real cryptographic signatures, no registers can be created on the platform. Every other feature depends on registers existing.

**Independent Test**: Can be fully tested by calling the initiate endpoint, signing the returned hashes via the wallet service, calling the finalize endpoint, and verifying that a register and genesis transaction are persisted with valid cryptographic signatures.

**Acceptance Scenarios**:

1. **Given** a running platform with Wallet, Register, and Validator services, **When** an operator calls initiate with valid owner details, **Then** the system returns a hex-encoded SHA-256 hash per owner as `dataToSign`, a nonce, and an expiration time.
2. **Given** an initiate response with hex hashes, **When** the operator sends each hash (as pre-hashed data) to the wallet sign endpoint, **Then** the wallet signs the hash bytes directly without re-hashing and returns a valid signature and public key.
3. **Given** signed attestations from the wallet, **When** the operator calls finalize with the registerId, nonce, and signed attestations, **Then** the system verifies each signature against the stored hash (not re-serialized data), creates the register, and submits a genesis transaction to the Validator.
4. **Given** a finalize request with valid signatures, **When** the genesis transaction is submitted to the Validator, **Then** the Validator signs the control record with the system wallet using a real cryptographic signature (not a placeholder) and adds the transaction to the mempool.
5. **Given** a completed register creation, **When** querying the register and genesis transaction, **Then** all signatures, public keys, and payload hashes contain real cryptographic values (no empty strings, no placeholder text).

---

### User Story 2 - System Wallet Signing via Wallet Service (Priority: P1)

The Validator Service signs genesis transaction data by calling the Wallet Service over HTTP, using the configured system wallet address. This replaces the current stub that returns placeholder strings.

**Why this priority**: Co-equal with Story 1 because the genesis transaction cannot be properly formed without real system wallet signatures. The stub is the primary blocker for end-to-end register creation.

**Independent Test**: Can be tested by calling the Validator's genesis endpoint with a valid control record payload and verifying that the returned transaction contains a real cryptographic signature from the system wallet, not a placeholder string.

**Acceptance Scenarios**:

1. **Given** a Validator Service with a configured system wallet address, **When** the genesis endpoint receives a control record, **Then** it calls the Wallet Service to sign the control record hash using the system wallet.
2. **Given** the Wallet Service returns a signature and public key, **When** the Validator builds the genesis transaction, **Then** it stores the real public key bytes (not UTF-8 encoded wallet address) and real signature bytes (not placeholder text).
3. **Given** a genesis transaction with system wallet signature, **When** a third party retrieves the transaction, **Then** the signature can be independently verified using the included public key and the control record hash.

---

### User Story 3 - Pre-Hashed Signing Support in Wallet Service (Priority: P1)

The Wallet Service sign endpoint accepts an `isPreHashed` flag. When set, the service signs the provided bytes directly without applying SHA-256 first, enabling callers to sign pre-computed hashes without double-hashing.

**Why this priority**: Co-equal with Stories 1 and 2 because the hash-based signing flow depends on the wallet being able to sign pre-hashed data. Without this, the initiate/finalize flow cannot work correctly.

**Independent Test**: Can be tested by sending a known SHA-256 hash to the wallet sign endpoint with `isPreHashed: true`, then independently verifying the signature against the original hash bytes using the returned public key.

**Acceptance Scenarios**:

1. **Given** a wallet sign request with `isPreHashed` as false or omitted, **When** the wallet signs data, **Then** it applies SHA-256 to the input before signing (existing behavior preserved).
2. **Given** a wallet sign request with `isPreHashed` as true, **When** the wallet signs data, **Then** it passes the input bytes directly to the cryptographic signing algorithm without hashing.
3. **Given** a pre-hashed signature, **When** verifying the signature using the public key and original hash bytes, **Then** verification succeeds.

---

### User Story 4 - Removal of Non-Cryptographic Register Creation (Priority: P2)

The simple CRUD register creation endpoint (which creates registers without attestations or genesis transactions) is removed. All register creation must go through the two-phase cryptographic flow.

**Why this priority**: Important for security and data integrity, but can be deferred slightly since the immediate goal is making the cryptographic path work. Removing the CRUD path ensures no registers exist without proper cryptographic provenance.

**Independent Test**: Can be tested by attempting to POST to the simple register creation endpoint and verifying it returns 404 (not found), while the two-phase initiate/finalize endpoints remain functional.

**Acceptance Scenarios**:

1. **Given** a running Register Service, **When** a client sends POST to the simple register creation endpoint, **Then** the system returns HTTP 404.
2. **Given** the simple creation endpoint is removed, **When** a client calls GET, PUT, or DELETE on existing registers, **Then** those management endpoints still function normally.
3. **Given** only the two-phase flow exists, **When** a register is created, **Then** it always has associated attestations and a genesis transaction.

---

### User Story 5 - Correct API Gateway Routing for Genesis Endpoint (Priority: P2)

The API Gateway correctly routes genesis-related requests to the Validator Service's genesis endpoint, resolving the current path transformation mismatch.

**Why this priority**: Required for external clients to reach the genesis endpoint through the gateway, but internal service-to-service calls (which bypass the gateway) are unaffected. Needed for walkthrough scripts and UI integration.

**Independent Test**: Can be tested by sending a request to the gateway's validator genesis path and verifying it reaches the Validator Service's genesis endpoint (returns a valid response or expected error, not 404).

**Acceptance Scenarios**:

1. **Given** a request to the API Gateway's validator genesis path, **When** the gateway proxies the request, **Then** it reaches the Validator Service's genesis endpoint (not a 404 from path mismatch).
2. **Given** existing validator routes (validation, mempool), **When** requests are sent to those paths, **Then** they continue to route correctly.

---

### User Story 6 - Updated Walkthrough with End-to-End Verification (Priority: P3)

The RegisterCreationFlow walkthrough scripts are updated to use the new hash-based signing flow and successfully execute end-to-end, serving as both documentation and integration verification.

**Why this priority**: Walkthroughs are documentation and testing aids. The core functionality must work first (P1/P2 stories), then walkthroughs are updated to reflect and verify the working flow.

**Independent Test**: Can be tested by running the updated walkthrough script against a running Docker environment and verifying all 6 steps complete successfully with real cryptographic operations.

**Acceptance Scenarios**:

1. **Given** a running Docker environment, **When** the walkthrough script executes the full register creation flow, **Then** all steps (auth, wallet creation, initiate, sign, finalize, verify) complete without errors.
2. **Given** the updated walkthrough script, **When** it receives a hex hash from initiate, **Then** it correctly converts the hex to bytes, sends to wallet with `isPreHashed` set to true, and uses the signature in finalize.

---

### Edge Cases

- What happens when the pending registration expires (5-minute TTL) between initiate and finalize? System returns an appropriate error indicating expiration.
- What happens when the nonce in finalize does not match the stored nonce? System rejects with a replay protection error.
- What happens when a signature is invalid (wrong key, corrupted data)? System rejects with a clear signature verification failure message identifying which attestation failed.
- What happens when the system wallet is not available or not configured? Validator returns a specific error indicating platform bootstrap is incomplete (system wallet missing), logged at error level. Register creation is not attempted -- no partial state. The caller receives a clear error indicating the platform is not ready.
- What happens when the Wallet Service is unreachable from the Validator? The genesis submission fails with a service unavailability error. The register is NOT persisted -- creation is atomic (register + genesis succeed together or neither is committed). The caller receives an error and can retry the full initiate/finalize flow.
- What happens when an owner's wallet address does not exist in the Wallet Service? The sign step fails with a wallet-not-found error before finalize is attempted.
- What happens when the same register creation is finalized twice with the same nonce? The atomic remove-on-read of the pending store ensures only the first finalize succeeds; the second returns a not-found/expired error.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST return a hex-encoded SHA-256 hash as `dataToSign` during register creation initiation (not raw canonical JSON).
- **FR-002**: System MUST store the computed hash per attestation in the pending registration for verification during finalize, eliminating the need to re-serialize and re-hash.
- **FR-003**: Wallet Service MUST support an `isPreHashed` flag on the sign endpoint that, when true, skips internal SHA-256 hashing and signs the provided bytes directly.
- **FR-004**: System MUST verify attestation signatures during finalize by comparing against the stored hash bytes, not by re-serializing attestation data.
- **FR-005**: Validator Service MUST call the Wallet Service over HTTP to sign genesis transaction data with the system wallet, producing real cryptographic signatures.
- **FR-006**: Validator Service MUST use real public key bytes from the Wallet Service response, not UTF-8 encoded wallet address strings.
- **FR-007**: System MUST compute and store the actual SHA-256 hash of the genesis transaction payload (not an empty string).
- **FR-008**: System MUST NOT provide a non-cryptographic register creation endpoint. All register creation MUST go through the two-phase initiate/finalize flow.
- **FR-009**: API Gateway MUST correctly route genesis requests to the Validator Service's genesis endpoint.
- **FR-010**: System MUST preserve backward compatibility for the wallet sign endpoint when `isPreHashed` is false or omitted (existing hash-then-sign behavior unchanged).
- **FR-011**: Register management endpoints (list, get, update, delete) MUST remain functional after removal of the simple creation endpoint.
- **FR-012**: System MUST support all three signing algorithms (ED25519, NIST P-256, RSA-4096) through the pre-hashed signing path.
- **FR-013**: Register creation MUST be atomic -- the register is NOT persisted to the database until the genesis transaction is successfully submitted to the Validator. If genesis submission fails, the register creation is rolled back and the caller receives an error.
- **FR-014**: Validator Service MUST authenticate to the Wallet Service using existing service-to-service JWT authentication (token obtained from Tenant Service), following established inter-service auth patterns.

### Key Entities

- **PendingRegistration**: Temporary state holding registerId, owner details, computed attestation hashes (one per owner), nonce for replay protection, and expiration timestamp. Stored in-memory with 5-minute TTL. Consumed atomically on finalize.
- **AttestationSigningData**: Canonical form of owner attestation containing role, subject DID, registerId, registerName, and grantedAt. Serialized to canonical JSON, hashed with SHA-256, and the hash is what gets signed.
- **RegisterControlRecord**: Verified attestation bundle attached to a register, containing all owner attestation signatures, public keys, and metadata. Serialized as the genesis transaction payload.
- **GenesisTransaction**: The first transaction in a register's ledger, containing the signed control record. Has transaction type "Genesis", blueprint ID "genesis", and includes both owner attestation signatures and the system wallet's counter-signature.
- **SignedAttestation**: Combines the attestation data, the signer's public key, the cryptographic signature, and the algorithm used. Submitted during finalize for verification.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A register can be created end-to-end through the two-phase flow with all cryptographic operations using real signatures (zero placeholder values in the final transaction).
- **SC-002**: All attestation signatures in a created register can be independently verified by any party using the included public keys and the attestation data hash.
- **SC-003**: The system wallet's counter-signature on the genesis transaction can be independently verified using the system wallet's public key.
- **SC-004**: The genesis transaction's payload hash matches the SHA-256 hash of the actual serialized control record payload.
- **SC-005**: The walkthrough script completes all 6 steps successfully against a running Docker environment without manual intervention.
- **SC-006**: Attempting to create a register through any path other than the two-phase flow results in rejection.
- **SC-007**: Pre-hashed signing produces verifiable results across all three supported algorithms (ED25519, NIST P-256, RSA-4096) when given the same hash input.

## Assumptions

- The system wallet is created during platform bootstrap and its address is available in the Validator Service configuration.
- The Wallet Service is accessible over HTTP from the Validator Service (service discovery via Aspire or Docker networking).
- The in-memory PendingRegistrationStore is acceptable for the current phase; Redis migration is a separate future task.
- The existing hash-then-sign behavior in TransactionService is correct for all non-register-creation signing use cases and must not change.
- The RegisterCreationOrchestrator already correctly serializes attestation data to canonical JSON; the serialization logic itself does not need changes, only the output format (hash instead of JSON) and verification approach (stored hash instead of re-serialization).

## Scope Boundaries

### In Scope

- Making the two-phase register creation flow produce real cryptographic signatures end-to-end
- Adding pre-hashed signing support to the Wallet Service
- De-stubbing the WalletServiceClient for real HTTP calls
- Removing the simple CRUD register creation endpoint
- Fixing the API Gateway routing for the genesis endpoint
- Computing real payload hashes for genesis transactions
- Updating walkthrough scripts to work with the new flow

### Out of Scope

- Moving PendingRegistrationStore to Redis (separate task)
- Adding gRPC support for wallet service communication (HTTP is sufficient for now)
- Register deletion or lifecycle management changes
- Changes to non-genesis transaction signing flows
- UI integration with the register creation flow
- Multi-register or batch register creation
- Register template or preset configurations

## Dependencies

- Wallet Service must be running and accessible for system wallet signing
- System wallet must be bootstrapped before register creation can succeed
- Validator Service must be running to accept genesis transactions
- Docker networking or Aspire service discovery must be configured for inter-service HTTP calls
- Tenant Service must be available for the Validator to obtain service-to-service JWT tokens for Wallet Service authentication
