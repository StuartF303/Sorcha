# Data Model: Quantum-Safe Cryptography Upgrade

**Feature Branch**: `040-quantum-safe-crypto`
**Date**: 2026-02-25

## Entities

### WalletNetworks (Extended Enum)

Extends existing `Sorcha.Cryptography.Enums.WalletNetworks` byte enum.

| Value | Name | Description |
|-------|------|-------------|
| 0x00 | ED25519 | Existing — Ed25519 curve signatures |
| 0x01 | NISTP256 | Existing — NIST P-256 curve signatures |
| 0x02 | RSA4096 | Existing — RSA 4096-bit signatures |
| 0x10 | ML_DSA_65 | New — ML-DSA-65 (FIPS 204) lattice-based signatures |
| 0x11 | SLH_DSA_128s | New — SLH-DSA-128s (FIPS 205) hash-based signatures |
| 0x12 | ML_KEM_768 | New — ML-KEM-768 (FIPS 203) key encapsulation |

**Validation**: Network byte must be a defined enum value. Gap between 0x02 and 0x10 reserves space for future classical algorithms.

### CryptoPolicy

Per-register cryptographic policy embedded in control transaction payloads.

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| Version | uint | Policy version (monotonically increasing) | >= 1, > previous version |
| AcceptedSignatureAlgorithms | string[] | Algorithm identifiers accepted for transaction signing | At least one; must be from known set |
| RequiredSignatureAlgorithms | string[] | Algorithms that MUST be present on every transaction | Subset of AcceptedSignatureAlgorithms |
| AcceptedEncryptionSchemes | string[] | Encryption schemes accepted for payload encryption | At least one |
| AcceptedHashFunctions | string[] | Hash functions accepted for TxId computation | At least one |
| EnforcementMode | enum | Permissive (warn) or Strict (reject) | Required |
| EffectiveFrom | DateTime | UTC timestamp when policy takes effect | Must be >= current time |
| DeprecatedAlgorithms | string[] | Algorithms being phased out (warning on use) | Optional |

**Algorithm Identifiers**: `"ED25519"`, `"NISTP256"`, `"RSA4096"`, `"ML-DSA-65"`, `"SLH-DSA-128s"`, `"ML-KEM-768"`, `"XCHACHA20-POLY1305"`, `"AES-256-GCM"`, `"SHA-256"`, `"SHA-512"`, `"BLAKE2B-256"`

**Default Policy** (for new registers):
- AcceptedSignatureAlgorithms: `["ED25519", "NISTP256", "RSA4096", "ML-DSA-65", "SLH-DSA-128"]`
- RequiredSignatureAlgorithms: `[]` (none required — any accepted algorithm suffices)
- AcceptedEncryptionSchemes: `["XCHACHA20-POLY1305", "AES-256-GCM", "ML-KEM-768"]`
- AcceptedHashFunctions: `["SHA-256", "BLAKE2B-256"]`
- EnforcementMode: Permissive
- DeprecatedAlgorithms: `[]`

### HybridSignature

Structured signature format supporting classical + PQC concurrent signatures.

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| Classical | string? | Base64-encoded classical signature (ED25519/P-256/RSA) | Optional if PQC present |
| ClassicalAlgorithm | string? | Algorithm used for classical signature | Required if Classical present |
| Pqc | string? | Base64-encoded PQC signature (ML-DSA/SLH-DSA) | Optional if Classical present |
| PqcAlgorithm | string? | Algorithm used for PQC signature | Required if Pqc present |
| WitnessPublicKey | string? | Base64-encoded full PQC public key | Required if Pqc present |

**Wire Format**: JSON serialized to string for TransactionModel.Signature field. Backward compatible — old parsers see a JSON string; new parsers deserialize the structure.

**Validation**: At least one of Classical or Pqc must be present. If both present, both must sign the same data.

### PqcWalletAddress

Hash-based wallet address for PQC keys.

| Component | Size | Description |
|-----------|------|-------------|
| HRP | 3 chars | `ws2` — Sorcha Wallet v2 (PQC) |
| Separator | 1 char | `1` (Bech32m standard) |
| Network byte | 1 byte | PQC algorithm identifier (0x10, 0x11, 0x12) |
| Public key hash | 32 bytes | SHA-256(network_byte + full_public_key) |
| Checksum | 6 chars | Bech32m checksum |

**Total**: ~62 characters (compact, human-readable)

**Address ↔ Key Binding**: The full public key is NOT derivable from the address. The address only contains the hash. Full public key MUST be included as witness data in every transaction from this address.

### ControlActionType (Extended)

Extends the existing control action type detection in ControlDocketProcessor.

| Action | Prefix | Description |
|--------|--------|-------------|
| Existing | `control.validator.*` | Validator management |
| Existing | `control.config.update` | Configuration update |
| Existing | `control.blueprint.publish` | Blueprint publishing |
| Existing | `control.register.updatemetadata` | Metadata update |
| **New** | `control.crypto.update` | Crypto policy update |

### BLSSigningShare

Threshold signature share for distributed docket signing.

| Field | Type | Description |
|-------|------|-------------|
| ValidatorId | string | Identifier of the signing validator |
| ShareIndex | uint | Share index in the (t,n) scheme |
| PartialSignature | byte[] | BLS partial signature |
| DocketHash | string | Hash of the docket being signed |

### BLSAggregateSignature

Combined threshold signature for a docket.

| Field | Type | Description |
|-------|------|-------------|
| Signature | byte[] | Aggregated BLS signature (~33 bytes) |
| SignerBitfield | byte[] | Bitfield indicating which validators signed |
| Threshold | uint | Minimum signers required (t) |
| TotalSigners | uint | Total possible signers (n) |

### ZKInclusionProof

Zero-knowledge proof of transaction inclusion in a docket.

| Field | Type | Description |
|-------|------|-------------|
| DocketId | string | Docket containing the transaction |
| MerkleRoot | string | Root hash of the docket's Merkle tree |
| ProofData | byte[] | Serialized ZK proof |
| VerificationKey | byte[] | Key needed to verify the proof |

### RangeProof

Bulletproof for numeric constraint verification.

| Field | Type | Description |
|-------|------|-------------|
| Commitment | byte[] | Pedersen commitment to the value |
| ProofData | byte[] | Serialized Bulletproof |
| BitLength | uint | Number of bits in the range (e.g., 64 for 0..2^64-1) |

## State Transitions

### CryptoPolicy Lifecycle

```
[Genesis TX creates register]
    → CryptoPolicy v1 (defaults or explicit)
        → [control.crypto.update TX]
            → CryptoPolicy v2 (upgraded)
                → [control.crypto.update TX]
                    → CryptoPolicy v3 (further changes)
```

Each version is immutable once written to the register. Validators use the policy version active at the transaction's submission timestamp.

### Wallet Key Lifecycle (PQC)

```
[Create wallet]
    → Generate classical key pair (ED25519)
    → Generate PQC key pair (ML-DSA-65)
    → Compute ws1 address (classical)
    → Compute ws2 address (PQC hash-based)
    → Store both keys encrypted at rest
```

### Hybrid Signature Flow

```
[Transaction data ready]
    → Hash data (SHA-256)
    → Sign with classical key → classical signature
    → Sign with PQC key → PQC signature (concurrent)
    → Package as HybridSignature JSON
    → Include PQC witness public key
    → Store in TransactionModel.Signature
```

## Relationships

```
Register ──1:1──→ CryptoPolicy (latest version)
         ──1:N──→ CryptoPolicy versions (historical, via Control TX chain)

Wallet ──1:1──→ Classical KeyPair (ED25519/P-256/RSA)
       ──0:1──→ PQC KeyPair (ML-DSA-65 or SLH-DSA-128)
       ──1:1──→ ws1 Address (classical)
       ──0:1──→ ws2 Address (PQC, hash-based)

Transaction ──1:1──→ HybridSignature (classical + PQC)
            ──0:1──→ WitnessPublicKey (PQC full key)

Docket ──1:1──→ ProposerSignature (classical or BLS aggregate)
       ──0:1──→ BLSAggregateSignature (when threshold signing active)
       ──0:N──→ ZKInclusionProof (on-demand verification)
```
