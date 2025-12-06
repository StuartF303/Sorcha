# Feature Specification: Pure Post-Quantum Cryptography Implementation

**Feature Branch**: `001-post-quantum-migration`
**Created**: 2025-12-03
**Updated**: 2025-12-03
**Status**: Draft
**Input**: User description: "Remove all non post-quantum secure algorithms. Move to clean post-quantum only cryptography with ML-KEM and ML-DSA. No legacy data exists."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Post-Quantum Key Generation (Priority: P1)

As a Sorcha platform operator, I need the system to generate cryptographic keys using only post-quantum algorithms (ML-KEM and ML-DSA) so that all cryptographic material is quantum-resistant from day one.

**Why this priority**: This is the foundation for the entire platform's quantum security. All cryptographic operations depend on quantum-resistant keys. Without this, the platform cannot protect against "harvest now, decrypt later" attacks.

**Independent Test**: Can be fully tested by generating new keys and verifying they use only ML-KEM-768 (for key encapsulation) or ML-DSA-65 (for digital signatures), and delivers immediate quantum resistance for all cryptographic operations.

**Acceptance Scenarios**:

1. **Given** a request to generate a new signature key pair, **When** the key generation operation executes, **Then** the system generates keys using ML-DSA-65 algorithm
2. **Given** a request to generate key encapsulation keys, **When** the key generation operation executes, **Then** the system generates keys using ML-KEM-768 algorithm
3. **Given** a wallet creation request, **When** the wallet is created, **Then** all wallet keys use ML-KEM-768 and ML-DSA-65 algorithms exclusively
4. **Given** multiple key generation requests, **When** keys are generated concurrently, **Then** each key pair is unique and cryptographically independent

---

### User Story 2 - Post-Quantum Digital Signatures (Priority: P1)

As a transaction creator, I need to sign transactions using ML-DSA so that my signatures are protected against quantum attacks and can be verified by other platform participants.

**Why this priority**: Digital signatures are core to the platform's integrity and authentication model. Without quantum-resistant signatures, transactions can be forged by quantum attackers.

**Independent Test**: Can be fully tested by signing data with ML-DSA keys, verifying signatures, and confirming rejection of tampered signatures, delivering secure transaction authentication.

**Acceptance Scenarios**:

1. **Given** a transaction payload and ML-DSA private key, **When** signature is requested, **Then** the system produces an ML-DSA-65 signature
2. **Given** a signed transaction and corresponding ML-DSA public key, **When** verification is performed, **Then** the system validates the signature correctly
3. **Given** a transaction with tampered data, **When** signature verification is attempted, **Then** the system rejects the signature
4. **Given** a transaction signature created with wrong key, **When** verification is attempted with different public key, **Then** the system rejects the signature

---

### User Story 3 - Post-Quantum Key Encapsulation (Priority: P1)

As a secure communication participant, I need to establish shared secrets using ML-KEM so that encrypted communications between parties are quantum-resistant.

**Why this priority**: Key encapsulation is essential for secure peer-to-peer communication and encrypted data exchange. Quantum attackers could decrypt communications without quantum-resistant key exchange.

**Independent Test**: Can be fully tested by encapsulating secrets with ML-KEM, decapsulating them, and verifying secret agreement between parties.

**Acceptance Scenarios**:

1. **Given** an ML-KEM-768 public key, **When** encapsulation is performed, **Then** the system produces ciphertext and shared secret
2. **Given** encapsulation ciphertext and ML-KEM-768 private key, **When** decapsulation is performed, **Then** the system recovers the identical shared secret
3. **Given** multiple encapsulation operations with same public key, **When** secrets are generated, **Then** each shared secret is unique
4. **Given** incorrect private key for decapsulation, **When** decapsulation is attempted, **Then** the system fails to produce correct shared secret

---

### User Story 4 - Wallet Address Encoding with Post-Quantum Keys (Priority: P2)

As a wallet owner, I need wallet addresses that encode post-quantum public keys so that I can receive transactions and others can verify my identity using quantum-resistant cryptography.

**Why this priority**: Wallet addresses are the user-facing representation of cryptographic identity. While important, this is lower priority than core cryptographic operations themselves.

**Independent Test**: Can be fully tested by encoding ML-DSA/ML-KEM public keys into addresses, decoding addresses back to keys, and verifying round-trip conversion.

**Acceptance Scenarios**:

1. **Given** an ML-DSA-65 public key, **When** wallet address encoding is performed, **Then** the system produces a unique address with post-quantum identifier prefix
2. **Given** a wallet address, **When** decoding is performed, **Then** the system extracts the ML-DSA public key and algorithm identifier
3. **Given** two different ML-DSA public keys, **When** addresses are generated, **Then** the addresses are different
4. **Given** an invalid or corrupted address, **When** validation is performed, **Then** the system rejects the address with clear error

---

### User Story 5 - Secure Key Storage with Post-Quantum Keys (Priority: P2)

As a wallet owner, I need my ML-KEM and ML-DSA private keys securely stored and encrypted so that my keys are protected from unauthorized access.

**Why this priority**: Key storage security is critical for protecting user assets, but is dependent on having working key generation and cryptographic operations first.

**Independent Test**: Can be fully tested by storing keys with encryption, retrieving and decrypting them, and verifying keys remain functional after storage/retrieval cycle.

**Acceptance Scenarios**:

1. **Given** ML-DSA and ML-KEM private keys and a password, **When** secure storage is requested, **Then** the system encrypts keys with password-derived key encryption
2. **Given** encrypted key storage and correct password, **When** key retrieval is requested, **Then** the system decrypts and returns functional keys
3. **Given** encrypted key storage and incorrect password, **When** key retrieval is attempted, **Then** the system rejects access and does not reveal key material
4. **Given** stored keys and key deletion request, **When** deletion is performed, **Then** key material is securely wiped and cannot be recovered

---

### Edge Cases

- What happens when ML-KEM or ML-DSA security parameters need to be upgraded (e.g., ML-KEM-768 to ML-KEM-1024)?
  - Algorithm security levels are configurable via system settings
  - Migration tool supports re-keying wallets to higher security levels
  - Transaction format versioning tracks algorithm parameters used

- How does the system handle performance at scale with larger post-quantum signatures and keys?
  - Performance benchmarks establish baseline metrics for key operations
  - System monitoring tracks signature size impact on storage and bandwidth
  - Signature size optimization uses ML-DSA-44 for less critical operations, ML-DSA-65 for standard use

- What happens when .NET or operating system support for ML-KEM/ML-DSA is unavailable?
  - System startup checks ML-DSA.IsSupported and ML-KEM.IsSupported
  - Clear error messages guide administrators to required platform versions
  - Documentation specifies minimum OS requirements (Windows 11 24H2+, Linux with OpenSSL 3.5+)

- How does the system handle interoperability with external systems that only support legacy algorithms?
  - External integration uses post-quantum only for internal operations
  - Gateway services handle protocol translation where necessary
  - Documentation clearly states platform requires post-quantum support

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST support ML-KEM-768 algorithm for all key encapsulation and key exchange operations
- **FR-002**: System MUST support ML-DSA-65 algorithm for all digital signature operations
- **FR-003**: System MUST support ML-DSA-44 for lower-security signature operations where signature size is critical
- **FR-004**: System MUST support ML-KEM-1024 and ML-DSA-87 as optional higher-security configurations
- **FR-005**: System MUST NOT support ED25519 key generation or signing operations
- **FR-006**: System MUST NOT support NIST P-256 (ECDSA) key generation or signing operations
- **FR-007**: System MUST NOT support RSA-4096 key generation or signing operations
- **FR-008**: System MUST use .NET 10's MLKem and MLDsa classes for cryptographic operations
- **FR-009**: System MUST generate ML-KEM and ML-DSA keys using .NET's GenerateKey() methods
- **FR-010**: System MUST implement signing using MLDsa.SignData() for message signing
- **FR-011**: System MUST implement verification using MLDsa.VerifyData() for signature validation
- **FR-012**: System MUST implement key encapsulation using MLKem.Encapsulate() for shared secret establishment
- **FR-013**: System MUST implement decapsulation using MLKem.Decapsulate() for shared secret recovery
- **FR-014**: System MUST store private keys encrypted at rest using AES-256-GCM
- **FR-015**: System MUST derive encryption keys from user passwords using Argon2id key derivation
- **FR-016**: System MUST update wallet address encoding to identify post-quantum keys with distinct prefix
- **FR-017**: System MUST include algorithm identifier in wallet addresses (e.g., "pq1" prefix for post-quantum)
- **FR-018**: System MUST validate ML-KEM and ML-DSA algorithm availability on startup using IsSupported checks
- **FR-019**: System MUST provide clear error messages when platform lacks post-quantum support
- **FR-020**: System MUST update transaction format to accommodate larger ML-DSA signatures (2420-4595 bytes)
- **FR-021**: System MUST provide audit logging for all key generation, signing, and key encapsulation operations
- **FR-022**: System MUST export keys in standard formats (PKCS#8 for private keys, SubjectPublicKeyInfo for public keys)
- **FR-023**: System MUST support configurable security levels (ML-KEM-512/768/1024, ML-DSA-44/65/87)
- **FR-024**: System MUST provide key re-keying capability to upgrade security levels

### Key Entities

- **Post-Quantum Key**: Represents a cryptographic key using ML-KEM or ML-DSA, with attributes including algorithm type (ML-KEM/ML-DSA), security level (768 for ML-KEM, 65 for ML-DSA as defaults), key material (public and private components), creation timestamp, and key identifier

- **Wallet**: Container for post-quantum keys, with attributes including unique wallet ID, ML-DSA signing key pair, ML-KEM encapsulation key pair, wallet address (encoded from public keys), creation timestamp, and encrypted private key storage

- **Transaction Signature**: Post-quantum signature for transactions, with attributes including signature bytes (2420-4595 bytes for ML-DSA), algorithm identifier (ML-DSA-44/65/87), signing key identifier, signature timestamp, and signature verification status

- **Shared Secret**: Result of ML-KEM key encapsulation, with attributes including encapsulation ciphertext, shared secret bytes, algorithm identifier (ML-KEM-768/1024), participating key identifiers, and establishment timestamp

- **Algorithm Configuration**: Defines system-wide post-quantum algorithm settings, with attributes including default ML-KEM security level, default ML-DSA security level, performance optimization settings, and platform compatibility checks

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All wallet creation operations complete using ML-KEM-768 and ML-DSA-65 algorithms with 100% success rate
- **SC-002**: Key generation operations complete within 1 second per wallet (includes both ML-KEM and ML-DSA key pairs)
- **SC-003**: Digital signature operations using ML-DSA-65 complete within 50ms per signature
- **SC-004**: Signature verification operations complete within 10ms per verification
- **SC-005**: Key encapsulation operations using ML-KEM-768 complete within 20ms per operation
- **SC-006**: System achieves 90% or better code coverage for all post-quantum cryptographic operations in test suite
- **SC-007**: Zero legacy algorithm code (ED25519, P-256, RSA) remains in production cryptography modules
- **SC-008**: API documentation includes complete examples for all ML-KEM and ML-DSA operations
- **SC-009**: System handles 1000 concurrent signature operations without performance degradation beyond 2x baseline
- **SC-010**: Post-quantum signature size overhead (2420-4595 bytes vs 64 bytes legacy) is acceptable for blockchain storage
- **SC-011**: Zero security vulnerabilities reported in post-quantum implementation during security audit
- **SC-012**: System startup validation detects platform post-quantum support with 100% accuracy

## Assumptions

1. **Clean Start**: Assumes no legacy cryptographic data exists in the system - this is a greenfield implementation with post-quantum only

2. **.NET 10 Platform Availability**: Assumes .NET 10 is deployed on all platforms with ML-KEM and ML-DSA support available

3. **Operating System Requirements**: Assumes deployment on Windows 11 24H2+ or Windows Server 2025+, or Linux with OpenSSL 3.5+

4. **No Backward Compatibility**: Assumes no requirement to interoperate with legacy ED25519/P-256/RSA systems at the cryptographic protocol level

5. **Security Levels**: Assumes ML-KEM-768 and ML-DSA-65 as default security levels, balancing quantum resistance with performance (NIST Category 3, equivalent to AES-192 security)

6. **Signature Size Acceptable**: Assumes blockchain/ledger storage can accommodate 2420-4595 byte signatures (ML-DSA-65: 3309 bytes) compared to 64-byte ED25519 signatures

7. **Key Storage**: Assumes key storage infrastructure (database, Azure Key Vault) supports larger key sizes (ML-KEM-768 keys: 2400 bytes, ML-DSA-65 keys: 4032 bytes)

8. **Performance Acceptable**: Assumes 30-50% performance overhead for post-quantum operations is acceptable trade-off for quantum resistance

9. **No Mnemonic Phrases**: Assumes post-quantum keys use secure storage with password protection rather than BIP39 mnemonic phrases (post-quantum algorithms don't map to word lists)

10. **Test Infrastructure**: Assumes NIST test vectors for ML-KEM (FIPS 203) and ML-DSA (FIPS 204) are available for validation

11. **No Migration**: Assumes no migration from existing systems - this is new implementation only

12. **Standards Stable**: Assumes NIST FIPS 203 (ML-KEM) and FIPS 204 (ML-DSA) standards are finalized and stable for production use

## Out of Scope

1. **Legacy Algorithm Support**: No support for ED25519, NIST P-256, RSA-4096, or any non-post-quantum algorithms

2. **Hybrid/Composite Modes**: No hybrid signatures combining post-quantum and legacy algorithms

3. **Legacy Data Migration**: No migration tools for existing legacy cryptographic data (none exists)

4. **Legacy Verification**: No verification capability for non-post-quantum signatures

5. **Alternative PQC Algorithms**: No support for SLH-DSA or other experimental post-quantum algorithms beyond ML-KEM and ML-DSA

6. **Hardware Acceleration**: Custom hardware accelerators or FPGA implementations are not included in initial implementation

7. **Mobile Platform Optimization**: iOS and Android platform-specific optimizations are out of scope

8. **Quantum Key Distribution (QKD)**: Integration with QKD infrastructure is not included

9. **Post-Quantum TLS Configuration**: TLS/HTTPS post-quantum cipher suite configuration is handled at infrastructure level, not application level

10. **Homomorphic Encryption**: Post-quantum homomorphic encryption schemes are out of scope

11. **Zero-Knowledge Proofs**: Post-quantum zero-knowledge proof systems are not included

12. **Certificate Authority Integration**: Integration with external CAs for post-quantum certificates is future work

## Notes

### .NET 10 Post-Quantum Implementation

This specification leverages .NET 10's native post-quantum cryptography support:

**Available Algorithms**:
- **ML-KEM** (FIPS 203): Key encapsulation with ML-KEM-512, ML-KEM-768, ML-KEM-1024 variants
- **ML-DSA** (FIPS 204): Digital signatures with ML-DSA-44, ML-DSA-65, ML-DSA-87 variants

**API Design Pattern**:
- Instances represent keys/keypairs, not algorithm factories
- Fixed-size outputs use `Span<byte>` for zero-copy operations
- Standard key formats: PKCS#8 (private), SubjectPublicKeyInfo (public)

**Example Key Generation**:
```csharp
using MLKem privateKey = MLKem.GenerateKey(MLKemAlgorithm.MLKem768);
using MLKem publicKey = MLKem.ImportEncapsulationKey(
    MLKemAlgorithm.MLKem768, privateKey.ExportEncapsulationKey());
```

**Example Signature**:
```csharp
using MLDsa signingKey = MLDsa.GenerateKey(MLDsaAlgorithm.MLDsa65);
byte[] signature = signingKey.SignData(data, MLDsaHashFunction.None);
bool valid = verifyKey.VerifyData(data, signature, MLDsaHashFunction.None);
```

### Performance Characteristics

Based on .NET benchmarks (1024-byte payload):

| Operation | ML-DSA-44 | ML-KEM-768 | Notes |
|-----------|-----------|------------|-------|
| Key Gen | 0.67 ms | ~0.5 ms | Acceptable for wallet creation |
| Sign | 0.42 ms | N/A | Faster than RSA-2048 (0.44ms) |
| Verify | 0.06 ms | N/A | Very fast verification |
| Encap/Decap | N/A | ~0.02 ms | Faster than ECDHE |

**Signature Sizes**:
- ML-DSA-44: 2,420 bytes
- ML-DSA-65: 3,309 bytes
- ML-DSA-87: 4,595 bytes

**Key Sizes**:
- ML-KEM-768 public: 1,184 bytes
- ML-KEM-768 private: 2,400 bytes
- ML-DSA-65 public: 1,952 bytes
- ML-DSA-65 private: 4,032 bytes

### Security Rationale

Post-quantum cryptography protects against quantum computer attacks:

- **ML-KEM** protects key exchange from Shor's algorithm, preventing "harvest now, decrypt later" attacks
- **ML-DSA** protects digital signatures from quantum forgery attacks
- **NIST Category 3 security** (ML-KEM-768, ML-DSA-65) provides security equivalent to AES-192, exceeding AES-128 baseline

### Platform Requirements

**Minimum Requirements**:
- .NET 10 with post-quantum cryptography support
- Windows 11 24H2 or later / Windows Server 2025 or later
- Linux with OpenSSL 3.5 or later
- Check `MLDsa.IsSupported` and `MLKem.IsSupported` before operations

### Design Principles

1. **Post-Quantum Only**: No legacy algorithm support eliminates attack surface and complexity
2. **Standards-Based**: NIST FIPS 203/204 compliance ensures long-term security and interoperability
3. **Clean Implementation**: No hybrid modes or migration code simplifies codebase
4. **Performance Acceptable**: 30-50% overhead acceptable for quantum resistance
5. **Future-Proof**: Configurable security levels allow upgrading to ML-KEM-1024/ML-DSA-87 as needs evolve
