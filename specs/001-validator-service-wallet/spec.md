# Feature Specification: Validator Service Wallet Access

**Feature Branch**: `001-validator-service-wallet`
**Created**: 2026-01-04
**Status**: Draft
**Input**: User description: "the validator service should have access to it own wallet in the wallet service, all the services belong to the default system organisation. the validator will use this wallet for its signing, validation etc"

## Clarifications

### Session 2026-01-04

- Q: How should the Validator Service behave if its wallet is deleted from the Wallet Service while it's actively running? → A: Detect on next signing attempt, log critical error, gracefully shut down
- Q: What retry policy parameters should be used for Wallet Service communication failures? → A: 3 max retries, 2x backoff multiplier, 1s initial delay
- Q: How long should the Validator Service cache wallet details before re-fetching them? → A: Cache for service lifetime unless wallet rotation detected
- Q: Which communication protocol should the Validator Service use for Wallet Service operations? → A: gRPC for all wallet operations
- Q: How does the system prevent multiple Validator Service instances from using the same wallet simultaneously? → A: No prevention needed; validators process different registers in parallel
- Q: Can the Validator Service perform cryptographic operations locally or must all crypto be delegated to Wallet Service? → A: Validator may retrieve derived path private keys for performance and perform crypto using Sorcha.Cryptography library, but never accesses root private key

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Validator Service Initialization with System Wallet (Priority: P1)

When the Validator Service starts up, it must authenticate to the Wallet Service using system organization credentials and establish access to its dedicated wallet. This wallet will be used for all cryptographic operations including signing dockets, validating consensus votes, and creating genesis blocks.

**Why this priority**: This is foundational - without wallet access, the Validator Service cannot perform any of its core cryptographic functions. All other validator operations depend on this capability.

**Independent Test**: Can be fully tested by starting the Validator Service and verifying it successfully retrieves its wallet ID and can sign a test message using the wallet service. Delivers immediate value by establishing the cryptographic identity of the validator.

**Acceptance Scenarios**:

1. **Given** the Validator Service is starting up and the system organization exists in the Tenant Service with a configured validator wallet, **When** the service initializes, **Then** it successfully authenticates to the Wallet Service and retrieves its wallet details (wallet ID, address, algorithm)
2. **Given** the Validator Service has successfully connected to its wallet, **When** it needs to sign a docket, **Then** it calls the Wallet Service sign endpoint with its wallet ID and receives a valid cryptographic signature
3. **Given** the Validator Service starts but the system organization does not have a validator wallet configured, **When** initialization occurs, **Then** the service logs a clear error and fails to start with instructions for wallet setup

---

### User Story 2 - Docket Signing with Validator Wallet (Priority: P1)

When the Validator Service builds a new docket, it must sign the docket using its system wallet to prove the docket's authenticity and origin. The signature becomes part of the docket's permanent record.

**Why this priority**: Docket signing is a core security requirement for blockchain integrity. Without signed dockets, the blockchain cannot establish trust or verify validator identity.

**Independent Test**: Can be tested by triggering docket building, examining the resulting docket, and verifying it contains a valid signature from the validator's wallet address. Delivers the critical security feature of signed blocks.

**Acceptance Scenarios**:

1. **Given** the Validator Service has built a new docket with transactions, **When** it finalizes the docket, **Then** it calls the Wallet Service to sign the docket hash and includes the signature in the docket structure
2. **Given** a signed docket is broadcast to peer validators, **When** peers receive it, **Then** they can verify the signature using the validator's public wallet address
3. **Given** the Wallet Service is temporarily unavailable, **When** the Validator Service attempts to sign a docket, **Then** it retries with exponential backoff and logs the signing failure without losing the docket

---

### User Story 3 - Consensus Vote Signing (Priority: P2)

When the Validator Service participates in consensus by voting on proposed dockets from peers, it must sign each vote using its wallet to prove the vote's authenticity and prevent vote tampering.

**Why this priority**: While critical for multi-validator scenarios, this is P2 because single-validator deployments (MVD) can function without vote signing. Required for production distributed consensus.

**Independent Test**: Can be tested by having the validator receive a vote request, generate a vote response, and verify the vote contains a signature from the validator's wallet. Delivers secure consensus voting capability.

**Acceptance Scenarios**:

1. **Given** a peer validator requests a consensus vote via gRPC RequestVote, **When** the Validator Service approves the docket, **Then** it signs the vote decision with its wallet and includes the signature in the VoteResponse
2. **Given** the Validator Service receives consensus votes from peers, **When** it tallies votes, **Then** it verifies each vote signature against the voting validator's wallet address before counting it
3. **Given** a vote signature verification fails, **When** processing consensus votes, **Then** the invalid vote is rejected and logged with details of the signature mismatch

---

### User Story 4 - System Organization Wallet Configuration (Priority: P3)

System administrators need the ability to configure which wallet in the Wallet Service should be used by the Validator Service. This configuration should be stored in the system organization's settings and be retrievable by the Validator Service.

**Why this priority**: This is P3 because for MVD we can use a hardcoded or environment-variable-based wallet address. Production deployments will benefit from centralized configuration management.

**Independent Test**: Can be tested by updating the system organization's validator wallet configuration through the Tenant Service and verifying the Validator Service picks up the change on restart. Delivers flexible wallet management.

**Acceptance Scenarios**:

1. **Given** a system administrator updates the system organization's validator wallet address via Tenant Service API, **When** the Validator Service restarts, **Then** it uses the newly configured wallet for all signing operations
2. **Given** the system organization configuration specifies a wallet ID that doesn't exist, **When** the Validator Service initializes, **Then** it fails to start with a clear error message indicating the wallet ID is invalid
3. **Given** no validator wallet is configured for the system organization, **When** the Validator Service starts, **Then** it falls back to the wallet address specified in environment variables (if present) or fails with setup instructions

---

### Edge Cases

- **Runtime wallet deletion**: If the Validator Service's wallet is deleted from the Wallet Service while running, the validator will detect the missing wallet on the next signing attempt, log a critical error, and gracefully shut down to prevent operating without cryptographic identity.
- **Wallet key rotation**: When the validator's cryptographic keys are rotated in the Wallet Service, the Validator Service will detect the rotation on the next wallet operation, refresh its cached wallet details, and continue operating with the new keys without requiring restart.
- **Network connectivity loss**: If the Validator Service loses network connectivity to the Wallet Service mid-signing operation, it will apply the retry logic (3 attempts over 7 seconds) and if all retries fail, mark the signing operation as failed and log the error without losing the docket data.
- **Concurrent validator instances**: Multiple Validator Service instances can safely use the same wallet simultaneously because each validator instance processes only one register at a time, ensuring validators work on different registers in parallel without conflicts.
- **System organization deletion**: If the system organization is deleted or deactivated while the Validator Service is running, the service will detect this on the next configuration check or wallet operation and gracefully shut down with appropriate error logging.
- **Wallet Service error responses**: When the Wallet Service returns errors during signature verification (malformed response, timeout), the Validator Service will treat the signature as invalid, reject the associated vote/docket, and log the error with full context for troubleshooting.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Validator Service MUST authenticate to the Wallet Service using system organization credentials at startup
- **FR-002**: Validator Service MUST retrieve and cache its designated wallet details (wallet ID, address, algorithm) from the Wallet Service during initialization and maintain this cache for the service lifetime unless wallet rotation is detected
- **FR-003**: Validator Service MUST use its system wallet to sign all dockets before broadcasting them to peer validators via gRPC calls to the Wallet Service
- **FR-004**: Validator Service MUST use its system wallet to sign all consensus votes before sending vote responses to peers via gRPC calls to the Wallet Service
- **FR-005**: Validator Service MUST verify consensus vote signatures from peer validators using the Wallet Service gRPC signature verification endpoint
- **FR-006**: Validator Service MUST include its wallet address in the ProposerValidatorId field of all dockets it creates
- **FR-007**: System MUST provide a configuration mechanism (via Tenant Service or environment variables) to specify which wallet the Validator Service should use
- **FR-008**: Validator Service MUST fail gracefully at startup if it cannot access its configured wallet in the Wallet Service
- **FR-009**: Validator Service MUST implement retry logic with exponential backoff when Wallet Service calls fail temporarily (3 max retries, 2x backoff multiplier, 1 second initial delay, total retry window up to 7 seconds)
- **FR-010**: Validator Service MUST log all wallet-related operations (initialization, signing, verification) for audit purposes
- **FR-011**: System organization in Tenant Service MUST have a field to store the validator wallet address/ID
- **FR-012**: Validator Service MUST NOT access or store the wallet root private key; however, it MAY retrieve derived path private keys from the Wallet Service and perform cryptographic operations locally using the Sorcha.Cryptography library for performance optimization
- **FR-013**: Validator Service MUST support retrieving its wallet configuration from environment variables as a fallback if Tenant Service configuration is unavailable
- **FR-014**: Validator Service MUST detect wallet deletion during runtime on the next signing attempt, log a critical error with wallet ID and timestamp, and initiate graceful shutdown to prevent operating without cryptographic identity
- **FR-015**: Validator Service MUST detect wallet key rotation during runtime (when wallet version or key material changes), refresh cached wallet details, and continue operations seamlessly with the new keys
- **FR-016**: Each Validator Service instance MUST process only one register at a time, allowing multiple validator instances to use the same wallet for different registers in parallel without conflicts
- **FR-017**: Validator Service MAY use the Sorcha.Cryptography library to perform signing, verification, and other cryptographic operations locally with derived path private keys for performance optimization, eliminating the need for Wallet Service calls on every cryptographic operation

### Key Entities

- **System Organization**: The default organization in the Tenant Service that owns all system services. Contains configuration for the validator wallet address.
- **Validator Wallet**: A wallet in the Wallet Service specifically designated for use by the Validator Service. Contains the cryptographic keys used for signing dockets and consensus votes.
- **Docket Signature**: Cryptographic signature created by the Validator Service's wallet over the docket hash. Proves the docket was created by a specific validator.
- **Consensus Vote Signature**: Cryptographic signature created by the Validator Service's wallet over the vote decision. Proves the vote came from a specific validator and hasn't been tampered with.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Validator Service successfully initializes and retrieves wallet details from Wallet Service in under 5 seconds on startup
- **SC-002**: 100% of dockets created by the Validator Service contain valid signatures that can be verified by peer validators
- **SC-003**: Validator Service can sign dockets at a rate of at least 10 dockets per second without performance degradation
- **SC-004**: Signature verification for consensus votes completes in under 100ms per vote
- **SC-005**: Validator Service handles Wallet Service temporary unavailability gracefully with automatic retry (3 attempts over 7 seconds) and recovery without requiring manual intervention
- **SC-006**: Wallet root private key is never accessed or stored by Validator Service; derived path private keys may be cached in memory for performance but are never persisted to configuration, logs, or disk storage
- **SC-007**: System administrators can change the validator wallet configuration and have the change take effect on next Validator Service restart without code changes

## Assumptions *(optional)*

- The system organization already exists in the Tenant Service with a unique ID (e.g., organization ID: "00000000-0000-0000-0000-000000000001")
- The Wallet Service provides gRPC endpoints for signature creation, verification, wallet detail retrieval, and derived key retrieval (BIP32 derivation paths) that accept wallet ID and data to sign
- The Validator Service has network access to the Wallet Service via gRPC
- Wallet addresses are unique identifiers that can be used to identify validators in the peer network
- The cryptographic algorithms supported by the Wallet Service (ED25519, NISTP256, RSA4096) are acceptable for validator signing operations and are supported by the Sorcha.Cryptography library for local operations
- Wallet Service returns signatures in a format compatible with blockchain storage (e.g., Bech32m, hex-encoded)
- The Tenant Service provides an API to retrieve organization configuration including validator wallet settings
- The Wallet Service implements hierarchical deterministic (HD) wallet support (BIP32) allowing derivation of child keys from the master key without exposing the root private key

## Dependencies *(optional)*

- **Wallet Service**: Must be running and accessible for the Validator Service to initialize and retrieve derived keys
- **Tenant Service**: Must be running to retrieve system organization configuration (if not using environment variables)
- **System Organization Setup**: A system organization must exist with a validator wallet configured before the Validator Service can start
- **Wallet Creation**: A wallet must be created in the Wallet Service and associated with the system organization before validator initialization
- **Service Discovery**: Validator Service must be able to discover the Wallet Service endpoint (via .NET Aspire service discovery or configuration)
- **Sorcha.Cryptography Library**: Validator Service depends on Sorcha.Cryptography for local cryptographic operations using derived path private keys

## Scope *(optional)*

### In Scope

- Configuration of validator wallet address in system organization settings
- Validator Service initialization with wallet retrieval
- Docket signing using validator wallet (local crypto with derived keys or via Wallet Service)
- Consensus vote signing using validator wallet (local crypto with derived keys or via Wallet Service)
- Signature verification for peer consensus votes (local crypto with derived keys or via Wallet Service)
- Retrieval of derived path private keys from Wallet Service for performance optimization
- Local cryptographic operations using Sorcha.Cryptography library with derived keys
- Retry logic and error handling for Wallet Service communication
- Audit logging of wallet operations
- Environment variable fallback for wallet configuration

### Out of Scope

- Creation of the system organization (assumed to exist)
- Creation of the validator wallet (assumed to be done via bootstrap or admin scripts)
- Wallet key rotation mechanism (future enhancement)
- Multi-wallet support for a single validator (validator uses exactly one wallet)
- Wallet backup and recovery procedures
- Hardware Security Module (HSM) integration for wallet storage (handled by Wallet Service)
- Wallet access control and permissions beyond system organization membership
- Automatic wallet creation if none is configured (must be explicitly created by administrator)
