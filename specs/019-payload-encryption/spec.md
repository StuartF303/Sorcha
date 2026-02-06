# Feature Specification: Payload Encryption for DAD Security Model

**Feature Branch**: `019-payload-encryption`
**Created**: 2026-02-06
**Status**: Draft
**Input**: User description: "Implement real payload encryption and decryption in PayloadManager"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Encrypted Payload Storage (Priority: P1)

When a participant submits action data in a workflow, the system encrypts the payload before storing it on the distributed ledger. Each payload is encrypted with a unique symmetric key, ensuring that raw data is never written to the ledger in plaintext. Only authorized recipients can decrypt the data using their private keys.

**Why this priority**: This is the core of the DAD (Disclosure, Alteration, Destruction) security model. Without encryption, all data on the ledger is visible to anyone with ledger access, completely undermining the platform's privacy guarantees.

**Independent Test**: Submit a payload through the PayloadManager, then verify the stored data differs from the original plaintext and that the IV is non-zero. Retrieve the raw payload bytes and confirm they cannot be interpreted as the original data without decryption.

**Acceptance Scenarios**:

1. **Given** a participant submits 1KB of action data with two recipients, **When** the payload is added via PayloadManager, **Then** the stored data is encrypted (differs from input), the IV is a cryptographically random value of algorithm-dependent size (24 bytes for XChaCha20-Poly1305, 12 bytes for AES-GCM), and the hash is a valid 32-byte SHA-256 digest of the original data.
2. **Given** a payload is encrypted for recipients Alice and Bob, **When** the payload is stored, **Then** each recipient has a unique encrypted symmetric key in `EncryptedKeys` that differs from the other recipient's encrypted key.
3. **Given** a payload with zero-length data, **When** encryption is attempted, **Then** the system rejects the payload with an appropriate error.

---

### User Story 2 - Authorized Payload Decryption (Priority: P1)

An authorized recipient retrieves encrypted payload data from the ledger and decrypts it using their private key. The system first decrypts the per-recipient symmetric key using the recipient's private key, then uses that symmetric key with the stored IV to decrypt the payload data, returning the original plaintext.

**Why this priority**: Encryption without decryption is useless. Recipients must be able to read data disclosed to them. This completes the encrypt/decrypt round-trip that makes the DAD model functional.

**Independent Test**: Encrypt a payload for a recipient, then decrypt it using that recipient's private key and verify the output matches the original plaintext byte-for-byte.

**Acceptance Scenarios**:

1. **Given** an encrypted payload accessible to a recipient, **When** the recipient decrypts using their private key, **Then** the decrypted data matches the original plaintext exactly.
2. **Given** an encrypted payload, **When** a non-authorized wallet attempts decryption, **Then** the system returns an access denied error without exposing any data.
3. **Given** an encrypted payload, **When** decryption is attempted with an incorrect private key, **Then** the system returns a decryption failure error.

---

### User Story 3 - Payload Integrity Verification (Priority: P2)

The system verifies the integrity of stored payloads by comparing the stored hash against a freshly computed hash of the decrypted data. This detects any tampering or corruption that may have occurred during storage or transmission across the peer network.

**Why this priority**: Integrity verification is essential for the "Alteration" guarantee in DAD — ensuring data has not been modified after being committed to the ledger.

**Independent Test**: Encrypt and store a payload, then call verification and confirm it passes. Modify the stored encrypted data, then call verification and confirm it fails.

**Acceptance Scenarios**:

1. **Given** a properly encrypted payload, **When** verification is performed, **Then** the hash check passes and the method returns true.
2. **Given** a payload whose stored encrypted data has been tampered with, **When** verification is performed, **Then** the hash check fails and the method returns false.
3. **Given** multiple payloads in a transaction, **When** VerifyAll is called, **Then** all payloads are individually verified and the result reflects any single failure.

---

### User Story 4 - Recipient Access Grant After Encryption (Priority: P2)

After a payload has been encrypted, an existing authorized user can grant access to a new recipient. The system decrypts the symmetric key using the granting user's private key, then re-encrypts it with the new recipient's public key, allowing the new recipient to decrypt the payload without re-encrypting the entire payload.

**Why this priority**: Dynamic access control is needed for workflow scenarios where participants join after initial action submission (e.g., escalation, delegation).

**Independent Test**: Encrypt a payload for Alice, then grant access to Bob using Alice's private key. Verify Bob can decrypt the payload and gets the same plaintext as Alice.

**Acceptance Scenarios**:

1. **Given** a payload encrypted for Alice, **When** Alice grants access to Bob, **Then** Bob's wallet appears in EncryptedKeys with a valid encrypted symmetric key.
2. **Given** the newly granted access, **When** Bob decrypts the payload, **Then** the decrypted data matches the original plaintext.
3. **Given** a payload, **When** access is granted to a wallet that already has access, **Then** the system returns success without modifying the existing key.

---

### User Story 5 - Backward-Compatible Upgrade (Priority: P3)

The encryption implementation must coexist with existing unencrypted payloads in the system. Payloads created before encryption was implemented (with zeroed IVs and dummy hashes) should still be readable, while all new payloads use proper encryption.

**Why this priority**: The system may have existing test data or development payloads that were created with the stub implementation. A hard cutover would break existing workflows in progress.

**Independent Test**: Create a payload with zeroed IV (legacy format), verify it can still be read. Create a new payload with encryption, verify it is properly encrypted. Both coexist in the same PayloadManager instance.

**Acceptance Scenarios**:

1. **Given** a legacy payload with zeroed IV, **When** decryption is requested, **Then** the system detects the legacy format and returns the raw data without attempting decryption.
2. **Given** a new payload with proper encryption, **When** decryption is requested, **Then** the system performs full cryptographic decryption.
3. **Given** a mix of legacy and encrypted payloads, **When** VerifyAll is called, **Then** legacy payloads pass verification (hash check skipped for zeroed hashes) and encrypted payloads are fully verified.

---

### Edge Cases

- What happens when the recipient's public key is invalid or malformed? System must reject with a clear error, not crash.
- What happens when payload data exceeds available memory? The system should handle large payloads (up to 10MB) without memory exhaustion.
- What happens when the same data is encrypted twice? Each encryption must produce different ciphertext (due to random IV), ensuring no pattern analysis is possible.
- What happens during concurrent payload additions? Thread safety must be maintained for the payload list and ID counter.
- What happens when a payload is compressed before encryption? Compression (if enabled via PayloadOptions) should occur before encryption, as encrypted data cannot be compressed effectively.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST encrypt all new payload data using authenticated encryption before storage, ensuring plaintext data is never stored on the ledger.
- **FR-002**: System MUST generate a cryptographically random initialization vector (IV) for each payload encryption operation. No IV may be reused across payloads.
- **FR-003**: System MUST compute a cryptographic hash of the original plaintext data before encryption and store the hash alongside the payload for integrity verification.
- **FR-004**: System MUST generate a unique symmetric encryption key per payload and encrypt that key separately for each authorized recipient using the recipient's public key.
- **FR-005**: System MUST decrypt payload data when requested by an authorized recipient, using the recipient's private key to first recover the symmetric key, then using that key with the stored IV to decrypt the data.
- **FR-006**: System MUST reject decryption attempts from wallets not listed in the payload's authorized recipients, returning an access denied error.
- **FR-007**: System MUST verify payload integrity by comparing the stored hash against a hash computed from the decrypted data. Hash comparison MUST use constant-time byte comparison to prevent timing side-channel attacks.
- **FR-008**: System MUST support granting access to new recipients after encryption by re-encrypting the symmetric key with the new recipient's public key.
- **FR-009**: System MUST maintain backward compatibility with existing unencrypted (legacy) payloads by detecting zeroed IVs and bypassing decryption for those payloads.
- **FR-010**: System MUST handle encryption failures gracefully, returning appropriate error statuses without exposing partial data or internal state.
- **FR-011**: System MUST ensure that encrypting the same plaintext twice produces different ciphertext (non-deterministic encryption via random IV).
- **FR-012**: System MUST support the authenticated encryption algorithm specified in PayloadOptions, defaulting to the platform standard if not specified.
- **FR-013**: System MUST zeroize symmetric key material from memory immediately after each encrypt or decrypt operation completes, using explicit clearing (not relying on garbage collection).

### Key Entities

- **Payload**: The encrypted data unit stored on the ledger. Contains encrypted data bytes, initialization vector, plaintext hash, compression flag, original size, and a dictionary of per-recipient encrypted symmetric keys.
- **Symmetric Key**: A randomly generated per-payload key used for authenticated encryption of the payload data. Never stored in plaintext — only exists encrypted per-recipient.
- **Encrypted Key**: The symmetric key encrypted with a specific recipient's public key. Each recipient gets their own copy. Allows the recipient to recover the symmetric key using their private key.
- **Initialization Vector (IV)**: A random value generated per payload to ensure encryption non-determinism. Size is algorithm-dependent: 24 bytes for XChaCha20-Poly1305, 12 bytes for AES-GCM. Stored alongside the payload in plaintext (this is safe — IVs do not need to be secret).
- **Content Hash**: A SHA-256 digest of the original plaintext data, computed before encryption. Used for integrity verification after decryption.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All new payloads stored on the ledger are encrypted — no plaintext data is written after this feature is deployed. Verifiable by inspecting stored payload bytes and confirming they differ from input data.
- **SC-002**: Authorized recipients can decrypt payloads and recover the exact original data with 100% fidelity (byte-for-byte match).
- **SC-003**: Unauthorized decryption attempts are rejected with zero data leakage — no partial data, no timing side-channels that reveal payload content.
- **SC-004**: Payload integrity verification detects 100% of single-bit changes to stored encrypted data.
- **SC-005**: Encryption and decryption of a 1MB payload completes within 100 milliseconds on standard hardware.
- **SC-006**: Existing workflows with legacy (unencrypted) payloads continue to function without modification after deployment.
- **SC-007**: All cryptographic operations maintain at least 85% unit test coverage on new code.

## Clarifications

### Session 2026-02-06

- Q: IV size inconsistency — spec says "12-byte" but default algorithm XChaCha20-Poly1305 uses 24 bytes. Should IV size be algorithm-dependent? → A: Yes, IV size is algorithm-dependent: 24 bytes for XChaCha20-Poly1305, 12 bytes for AES-GCM.
- Q: Should implementation use constant-time comparison for hash verification to prevent timing side-channels? → A: Yes, use constant-time comparison for hash verification and key lookups; rely on library AEAD for encryption timing.
- Q: Should symmetric keys be explicitly zeroized from memory after each encrypt/decrypt operation? → A: Yes, always zeroize immediately after operation completes using explicit clearing.

## Assumptions

- The platform's existing cryptographic library (`Sorcha.Cryptography`) provides working asymmetric encrypt/decrypt operations via `ICryptoModule` that can be used for symmetric key wrapping.
- Recipient public keys are available at encryption time via wallet addresses resolved through the existing participant/wallet infrastructure.
- The `PayloadOptions.EncryptionType` enum value maps to a specific authenticated encryption algorithm. The default (`XCHACHA20_POLY1305`) is the preferred algorithm, but the implementation should support `AES_GCM` as well since the feature description mentions AES-256-GCM.
- Payload sizes in typical workflows are under 1MB. The system should handle payloads up to 10MB but is not optimized for streaming encryption of very large files.
- The `IHashProvider` interface from `Sorcha.Cryptography` is available for SHA-256 hash computation.

## Out of Scope

- Streaming encryption for payloads larger than 10MB
- Key rotation for already-encrypted payloads (re-encrypting with new keys)
- Hardware Security Module (HSM) integration for key management
- Compression implementation (PayloadOptions supports it, but compression is a separate feature)
- Binary transaction serialization (separate stub identified in analysis)
