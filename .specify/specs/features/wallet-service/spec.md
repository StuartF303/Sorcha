# Feature Specification: Wallet Service

**Feature Branch**: `wallet-service`
**Created**: 2025-12-03
**Status**: 95% Complete (HD Wallet Features Complete)
**Input**: Derived from `.specify/specs/sorcha-wallet-service.md`

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create and Recover Wallets (Priority: P0)

As a Participant, I need to create and recover cryptographic wallets so that I can securely participate in multi-party workflows and sign transactions.

**Why this priority**: Core functionality - without wallets, users cannot sign transactions or participate in workflows.

**Independent Test**: Can be tested by creating a wallet, recording the mnemonic, deleting the wallet, and recovering it from the mnemonic.

**Acceptance Scenarios**:

1. **Given** a request to create a new wallet with ED25519 algorithm, **When** I POST to `/api/wallets`, **Then** a wallet is created with a unique address, and a BIP39 mnemonic (12 or 24 words) is returned once.
2. **Given** a valid BIP39 mnemonic, **When** I POST to `/api/wallets/recover`, **Then** the wallet is recovered with the same address as the original.
3. **Given** an invalid mnemonic (wrong checksum), **When** I attempt recovery, **Then** a 400 Bad Request is returned with validation errors.
4. **Given** I request a wallet with a specific name and description, **When** creation succeeds, **Then** the metadata is persisted and returned in subsequent GET requests.

---

### User Story 2 - Sign Transactions (Priority: P0)

As a workflow participant, I need to sign transactions with my wallet so that my actions in the workflow are cryptographically authenticated.

**Why this priority**: Transaction signing is essential for blockchain participation.

**Independent Test**: Can be tested by creating a wallet, building a transaction, signing it, and verifying the signature.

**Acceptance Scenarios**:

1. **Given** a valid wallet address and transaction payload, **When** I POST to `/api/wallets/{address}/sign`, **Then** a cryptographic signature is returned.
2. **Given** a signature created by wallet A, **When** I verify the signature against wallet A's public key, **Then** verification succeeds.
3. **Given** a signature created by wallet A, **When** I verify against wallet B's public key, **Then** verification fails.
4. **Given** I am not the owner or delegate of a wallet, **When** I attempt to sign, **Then** a 403 Forbidden is returned.

---

### User Story 3 - HD Address Derivation (Priority: P1)

As a wallet owner, I need to derive multiple addresses from my HD wallet so that I can use different addresses for different purposes or transactions.

**Why this priority**: HD wallets are standard for privacy and address management.

**Independent Test**: Can be tested by deriving addresses at different BIP44 paths and verifying uniqueness.

**Acceptance Scenarios**:

1. **Given** a wallet created with HD support, **When** I POST to `/api/wallets/{address}/derive` with a BIP44 path, **Then** a new address is derived and returned.
2. **Given** the same wallet and path, **When** I derive multiple times, **Then** the same address is returned (deterministic).
3. **Given** different paths (e.g., m/44'/0'/0'/0/0 vs m/44'/0'/0'/0/1), **When** I derive, **Then** different addresses are returned.

---

### User Story 4 - Access Control and Delegation (Priority: P1)

As a wallet owner, I need to delegate access to my wallet so that trusted parties or services can sign on my behalf.

**Why this priority**: Enables enterprise use cases with service accounts and team access.

**Independent Test**: Can be tested by granting delegation, having the delegate sign, and then revoking access.

**Acceptance Scenarios**:

1. **Given** I own a wallet, **When** I POST to `/api/wallets/{address}/delegates` with a subject and access level, **Then** the delegation is created.
2. **Given** I am a read-write delegate, **When** I sign a transaction, **Then** the operation succeeds.
3. **Given** I am a read-only delegate, **When** I attempt to sign, **Then** a 403 Forbidden is returned.
4. **Given** I am the owner, **When** I DELETE a delegation, **Then** the delegate loses access immediately.

---

### User Story 5 - Verify Signatures (Priority: P1)

As a verifying party, I need to verify that a transaction signature was created by a specific wallet so that I can trust the transaction authenticity.

**Why this priority**: Verification is essential for transaction validation and trust.

**Independent Test**: Can be tested by verifying valid and invalid signatures.

**Acceptance Scenarios**:

1. **Given** a valid signature and public key, **When** I POST to `/api/signatures/verify`, **Then** the result indicates the signature is valid.
2. **Given** a tampered payload with original signature, **When** I verify, **Then** the result indicates the signature is invalid.
3. **Given** a signature with a different algorithm than the wallet, **When** I verify, **Then** an appropriate error is returned.

---

### User Story 6 - Multi-Tenant Isolation (Priority: P1)

As a tenant administrator, I need wallet operations to be isolated per tenant so that one tenant cannot access another tenant's wallets.

**Why this priority**: Security requirement for enterprise multi-tenant deployment.

**Independent Test**: Can be tested by creating wallets in different tenants and attempting cross-tenant access.

**Acceptance Scenarios**:

1. **Given** a wallet in tenant A, **When** a user from tenant B attempts to access it, **Then** a 404 Not Found is returned (no information leakage).
2. **Given** I list wallets as a tenant A user, **When** results are returned, **Then** only tenant A wallets are included.

---

### Edge Cases

- What happens when the encryption provider (Key Vault) is unavailable?
- How does the system handle concurrent wallet creation with the same mnemonic?
- What happens when a wallet address collision occurs (extremely rare)?
- How does the system handle key rotation for active wallets?

**Note**: Per constitution VII (DDD terminology), "Participant" is used instead of "user" throughout this specification.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST generate wallets with BIP39 mnemonics (12/24 words)
- **FR-002**: System MUST recover wallets from valid BIP39 mnemonics
- **FR-003**: System MUST support ED25519, SECP256K1, and RSA-4096 algorithms
- **FR-004**: System MUST derive HD addresses using BIP32/BIP44 paths
- **FR-005**: System MUST encrypt private keys at rest using AES-256-GCM
- **FR-006**: System MUST support Azure Key Vault and local DPAPI encryption
- **FR-007**: System MUST sign transactions using wallet private keys
- **FR-008**: System MUST verify signatures against public keys
- **FR-009**: System MUST enforce owner, delegate-read-write, delegate-read-only access levels
- **FR-010**: System MUST isolate wallets by tenant
- **FR-011**: System MUST audit all sensitive operations
- **FR-012**: System MUST track wallet transaction history
- **FR-013**: System MUST support wallet metadata (name, description, tags)
- **FR-014**: System MUST provide OpenAPI documentation for all endpoints

### Key Entities

- **Wallet**: Core entity with encrypted private key, address, and metadata
- **WalletAddress**: Derived addresses with BIP44 paths
- **WalletAccess**: Delegation records with subject, role, and reason
- **WalletTransaction**: Transaction history tracking

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Transaction signing completes in under 50ms (P95)
- **SC-002**: Payload decryption completes in under 100ms (P95)
- **SC-003**: System supports 10,000+ wallets per tenant
- **SC-004**: Unit test coverage exceeds 90% for core services
- **SC-005**: All APIs documented with OpenAPI and Scalar UI
- **SC-006**: Zero plaintext private keys stored or logged
- **SC-007**: Security audit passed with no critical findings
