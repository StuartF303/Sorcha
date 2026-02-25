# Feature Specification: Quantum-Safe Cryptography Upgrade

**Feature Branch**: `040-quantum-safe-crypto`
**Created**: 2026-02-25
**Status**: Draft
**Input**: Upgrade Sorcha's cryptography layer to support post-quantum cryptographic algorithms running concurrently alongside existing classical algorithms, with register-level crypto policy control and quantum-safe wallet addresses.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Hybrid Quantum-Safe Signing (Priority: P1)

A register administrator creates a new register and specifies that it should accept both classical (ED25519) and post-quantum (ML-DSA-65) signatures. Participants create wallets that generate both classical and PQC key pairs. When a participant submits a transaction, the system produces both signatures concurrently. Validators accept transactions where either signature is valid, enabling a gradual migration to quantum-safe cryptography without disrupting existing workflows.

**Why this priority**: This is the foundational capability. Without hybrid signing, no other PQC feature can function. It provides immediate quantum resistance while maintaining backward compatibility with existing wallets and registers.

**Independent Test**: Can be fully tested by creating a wallet with PQC keys, signing a message, and verifying the signature using both classical and PQC algorithms independently.

**Acceptance Scenarios**:

1. **Given** a wallet configured with ML-DSA-65, **When** a user signs transaction data, **Then** a valid ML-DSA-65 signature is produced that can be independently verified.
2. **Given** a wallet configured in hybrid mode (ED25519 + ML-DSA-65), **When** a user signs transaction data, **Then** both signatures are produced concurrently and stored together in the transaction.
3. **Given** an existing ED25519-only wallet, **When** a transaction is submitted to a register accepting hybrid signatures, **Then** the transaction is accepted with the classical signature alone.
4. **Given** a transaction with both classical and PQC signatures, **When** a validator verifies it, **Then** verification succeeds if either signature is valid.

---

### User Story 2 - Register Crypto Policy via Control Transactions (Priority: P1)

A register owner creates a register and its genesis control transaction includes a crypto policy specifying which signature algorithms, encryption schemes, and hash functions are permitted on that register. Over time, as PQC adoption increases, the owner upgrades the register's crypto policy by submitting a new control transaction that adds PQC-only requirements or deprecates classical algorithms — without creating a new register or migrating data.

**Why this priority**: This is co-equal with P1 because it defines how registers govern cryptographic requirements. Without policy control, there is no way to enforce PQC usage or manage the migration per-register.

**Independent Test**: Can be tested by creating a register with a specific crypto policy, submitting transactions matching the policy (accepted) and violating the policy (rejected), then upgrading the policy via control transaction and verifying the new rules apply.

**Acceptance Scenarios**:

1. **Given** a new register, **When** the genesis control transaction is created, **Then** it includes a crypto policy section specifying accepted algorithms with sensible defaults (classical + PQC hybrid).
2. **Given** a register with a crypto policy accepting ED25519 only, **When** a transaction signed with ML-DSA-65 only is submitted, **Then** the transaction is rejected with a clear policy violation message.
3. **Given** an active register, **When** the register owner submits a control transaction upgrading the crypto policy to require PQC signatures, **Then** all subsequent transactions must include a PQC signature to be accepted.
4. **Given** a register with an upgraded crypto policy, **When** a validator processes older transactions (pre-upgrade), **Then** they are validated against the policy that was active at the time of submission.

---

### User Story 3 - Quantum-Safe Wallet Addresses (Priority: P2)

A participant creates a new PQC-enabled wallet. Since PQC public keys are significantly larger (1,952 bytes for ML-DSA-65 vs 32 bytes for ED25519), the system generates a compact wallet address by hashing the PQC public key and encoding the hash. The full public key is stored separately and included as witness data in transactions. Existing classical wallet addresses continue to work unchanged.

**Why this priority**: Wallet addresses are the user-facing identity. Without a compact PQC address format, the system would require impractically long addresses. This depends on P1 (PQC key generation) being in place.

**Independent Test**: Can be tested by generating a PQC wallet, verifying the address is compact and human-usable, then recovering the full public key from a transaction's witness data and verifying it matches the address hash.

**Acceptance Scenarios**:

1. **Given** a PQC key pair, **When** a wallet address is generated, **Then** the address is compact (under 100 characters) and follows a distinct prefix convention that identifies it as PQC-based.
2. **Given** a PQC wallet address, **When** a transaction is created, **Then** the full public key is included in the transaction as witness data for verification.
3. **Given** a classical wallet address (ws1...), **When** processed alongside PQC addresses, **Then** both formats are recognized and validated correctly.
4. **Given** a PQC wallet address and a transaction, **When** a verifier extracts the witness public key, **Then** hashing that key produces the same hash embedded in the address, confirming the key-address binding.

---

### User Story 4 - Quantum-Safe Payload Encryption (Priority: P2)

A register is configured to use PQC key encapsulation for field-level payload encryption. When a participant encrypts a payload for a recipient, the system uses ML-KEM-768 to establish a shared secret, which feeds into the existing symmetric encryption layer (XChaCha20-Poly1305). The recipient decapsulates to recover the shared secret and decrypt the payload. The symmetric encryption itself remains unchanged and quantum-safe.

**Why this priority**: Encryption protects data confidentiality, which is vulnerable to "harvest now, decrypt later" attacks. This is critical but depends on PQC key infrastructure (P1) being established.

**Independent Test**: Can be tested by encrypting a payload with ML-KEM-768 encapsulation, transmitting the ciphertext, and decrypting with the corresponding private key — verifying the plaintext matches.

**Acceptance Scenarios**:

1. **Given** a recipient's PQC public key, **When** a participant encrypts a payload, **Then** the system uses PQC key encapsulation to wrap the symmetric key and the payload is encrypted with the existing symmetric algorithm.
2. **Given** an ML-KEM-768 encapsulated payload, **When** the recipient decapsulates and decrypts, **Then** the original plaintext is recovered.
3. **Given** a register configured for classical encryption only, **When** a payload encrypted with PQC encapsulation is submitted, **Then** the system rejects it per the register's crypto policy.

---

### User Story 5 - BLS Threshold Signatures for Distributed Validation (Priority: P3)

When multiple validator nodes participate in docket signing, instead of a single validator signing the entire docket, t-of-n validators each produce a signing share using BLS threshold signatures. The shares combine into a single compact aggregate signature that any node can verify against a single shared public key. This removes the single point of failure in docket signing and reduces aggregate signature storage size.

**Why this priority**: This is an architectural advancement for the peer network. It depends on the core PQC infrastructure and crypto policy system being established first.

**Independent Test**: Can be tested by generating n signing shares, combining t of them into an aggregate signature, and verifying that the aggregate validates against the shared public key — while t-1 shares fail to produce a valid aggregate.

**Acceptance Scenarios**:

1. **Given** n validator nodes with BLS signing shares, **When** t of them sign a docket, **Then** a single aggregate signature is produced that verifies against the shared public key.
2. **Given** t-1 validator signing shares, **When** aggregation is attempted, **Then** the resulting signature fails verification.
3. **Given** a docket signed with BLS aggregate signature, **When** stored on the register, **Then** the signature size is constant regardless of how many validators participated (approximately 33 bytes).

---

### User Story 6 - Zero-Knowledge Register Verification (Priority: P3)

An auditor or compliance officer needs to verify that a specific transaction exists in a register and passes certain constraints without seeing the transaction's payload data. The system generates a zero-knowledge proof combining the existing Merkle tree with a ZK proof that demonstrates transaction inclusion and constraint satisfaction without revealing the underlying data.

**Why this priority**: This enhances the DAD model's Disclosure pillar with cryptographic privacy guarantees. It builds on top of all other cryptographic infrastructure and is an advanced capability.

**Independent Test**: Can be tested by creating a Merkle proof for a transaction, generating a ZK proof of inclusion, and verifying the proof without access to the original transaction data.

**Acceptance Scenarios**:

1. **Given** a register with transactions in a docket, **When** an auditor requests a proof of transaction inclusion, **Then** a zero-knowledge proof is generated that proves inclusion without revealing the transaction payload.
2. **Given** a ZK proof of transaction inclusion, **When** the auditor verifies it against the docket's Merkle root, **Then** verification succeeds without the auditor learning any payload data.
3. **Given** a numeric field constraint in a blueprint action (e.g., value within a range), **When** a range proof is requested, **Then** the system proves the constraint is satisfied without revealing the actual value.

---

### Edge Cases

- What happens when a register's crypto policy is upgraded but pending transactions were signed with the old policy? Answer: Transactions are validated against the policy version active at their submission timestamp.
- How does the system handle a wallet that has both classical and PQC keys when one key is compromised? Answer: Either key can be independently revoked through the existing wallet-link revocation mechanism, and the crypto policy enforcement determines which key types remain acceptable.
- What happens when a PQC algorithm is later found to have vulnerabilities? Answer: The control transaction upgrade mechanism allows registers to deprecate specific algorithms and require migration to alternatives (e.g., SLH-DSA as backup for ML-DSA).
- How does the system handle transaction size limits when PQC signatures are ~50x larger? Answer: Infrastructure capacity limits (MongoDB document size, gRPC message limits) must be assessed and adjusted. Transaction model accommodates multiple signatures via a structured signature field.
- What happens when a BLS threshold signing round has network partitions? Answer: The threshold parameter t is chosen to tolerate expected failure scenarios. If fewer than t validators are reachable, the docket cannot be sealed until quorum is restored.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST support ML-DSA-65 (FIPS 204) digital signatures for transaction signing and verification within the cryptography library.
- **FR-002**: System MUST support SLH-DSA (FIPS 205) hash-based digital signatures as a backup post-quantum signature algorithm.
- **FR-003**: System MUST support ML-KEM-768 (FIPS 203) key encapsulation for establishing shared secrets used in payload encryption.
- **FR-004**: System MUST support hybrid signing mode where both a classical signature and a PQC signature are produced concurrently for the same transaction data.
- **FR-005**: System MUST generate compact wallet addresses for PQC keys by hashing the public key and encoding the hash, with full public keys stored as transaction witness data.
- **FR-006**: System MUST distinguish PQC wallet addresses from classical addresses via a distinct prefix or version identifier.
- **FR-007**: System MUST include a crypto policy section in register genesis control transactions that specifies accepted signature algorithms, encryption schemes, and hash functions.
- **FR-008**: System MUST allow register crypto policies to be upgraded via subsequent control transactions without requiring register recreation.
- **FR-009**: System MUST validate incoming transactions against the crypto policy active at the time of transaction submission.
- **FR-010**: System MUST maintain backward compatibility — existing registers with classical-only crypto continue to function without changes.
- **FR-011**: System MUST support BLS threshold signatures where t-of-n validator shares combine into a single aggregate signature for docket signing.
- **FR-012**: System MUST support zero-knowledge proofs of transaction inclusion in dockets using Merkle tree proofs.
- **FR-013**: System MUST support range proofs (Bulletproofs) for numeric constraint verification without revealing actual values.
- **FR-014**: All PQC cryptographic operations MUST be encapsulated in the cryptography library with no PQC-specific dependencies leaking into service projects.
- **FR-015**: System MUST provide sensible defaults for register crypto policies — new registers default to hybrid mode (classical + PQC accepted) unless explicitly configured otherwise.
- **FR-016**: System MUST zeroize PQC key material from memory after use, consistent with existing key material handling.

### Key Entities

- **Crypto Policy**: A per-register configuration specifying accepted signature algorithms, encryption schemes, hash functions, and enforcement level (permissive/strict). Embedded in control transactions.
- **PQC Key Pair**: A post-quantum key pair (ML-DSA-65 or SLH-DSA) associated with a wallet, containing a public key (1,952+ bytes) and private key (4,032+ bytes).
- **Hybrid Signature**: A composite signature structure containing both a classical signature and a PQC signature for the same data, enabling dual verification.
- **Witness Data**: The full PQC public key included alongside a transaction, enabling verification when the wallet address contains only a hash of the key.
- **BLS Signing Share**: A partial signature produced by one validator node in a threshold scheme, usable for aggregation into a compact docket signature.
- **ZK Inclusion Proof**: A zero-knowledge proof demonstrating that a transaction exists within a docket's Merkle tree without revealing the transaction content.
- **Range Proof**: A Bulletproof demonstrating that a numeric value satisfies a constraint without revealing the value itself.

## Assumptions

- BouncyCastle.NET 2.5.0+ provides production-ready implementations of ML-DSA, ML-KEM, and SLH-DSA suitable for use in Sorcha.
- PQC key generation does not require deterministic seeded generation from BIP39 mnemonics in the initial release. HD derivation for PQC keys may use a separate derivation scheme from the BIP32/44 paths used for classical keys.
- MongoDB's default 16MB document size limit is sufficient for transactions with PQC signatures (~3.3KB per signature) without requiring GridFS.
- The gRPC default message size limit (4MB) is sufficient for peer-to-peer transaction replication with PQC signatures.
- BLS threshold signatures and ZK proofs are additive features that do not require changes to the existing transaction model structure — they extend it.
- Register crypto policy versioning is sequential (monotonically increasing version number per register), and policies are append-only (new policy supersedes old, old policy remains for historical validation).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Wallets can generate, sign, and verify with post-quantum algorithms, with all existing cryptography tests continuing to pass (zero regression).
- **SC-002**: Hybrid-signed transactions complete the full lifecycle (create, sign, submit, validate, seal in docket) on a PQC-enabled register.
- **SC-003**: Register crypto policy changes via control transactions take effect for all subsequent transactions within one docket cycle.
- **SC-004**: PQC wallet addresses are under 100 characters in length and are visually distinguishable from classical addresses.
- **SC-005**: Transaction throughput with hybrid signing remains within 50% of classical-only throughput (acceptable performance overhead for dual signatures).
- **SC-006**: All PQC signing and verification operations complete in under 500 milliseconds per operation.
- **SC-007**: BLS threshold signatures for docket signing produce a constant-size output regardless of the number of participating validators.
- **SC-008**: Zero-knowledge inclusion proofs verify in under 1 second without access to the original transaction data.
- **SC-009**: 100% of existing walkthroughs continue to pass on registers with default (hybrid) crypto policy.
