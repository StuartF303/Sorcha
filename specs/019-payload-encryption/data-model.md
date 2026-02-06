# Data Model: Payload Encryption for DAD Security Model

**Feature**: 019-payload-encryption
**Date**: 2026-02-06

## Entities

### Payload (Modified)

Internal class in `PayloadManager.cs`. No schema changes — existing properties get real values instead of stubs.

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| Id | uint | Auto-incrementing payload identifier | Sequential, unique per PayloadManager |
| Type | PayloadType | Classification (Data, Document, Message, Metadata, Custom) | Valid enum value |
| Data | byte[] | Encrypted ciphertext bytes (was: plaintext stub) | Non-empty after encryption |
| IV | byte[] | Random initialization vector / nonce | 12 bytes (AES-GCM) or 24 bytes (XChaCha20); non-zero |
| Hash | byte[] | SHA-256 digest of original plaintext | 32 bytes; computed before encryption |
| IsCompressed | bool | Whether data was compressed before encryption | Set from PayloadOptions |
| OriginalSize | long | Size of original plaintext in bytes | > 0 |
| EncryptedKeys | Dictionary&lt;string, byte[]&gt; | Wallet address → asymmetrically encrypted symmetric key | At least 1 entry; each value is encrypted key bytes |
| EncryptionType | EncryptionType | Algorithm used for symmetric encryption (new field) | Valid AEAD type (AES_GCM or XCHACHA20_POLY1305) |

### RecipientKeyInfo (New - Internal)

Parameter object for passing recipient cryptographic identity to PayloadManager.

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| WalletAddress | string | Recipient wallet address (dictionary key) | Non-null, non-empty |
| PublicKey | byte[] | Recipient's public key bytes | Non-empty, valid for network type |
| Network | WalletNetworks | Cryptographic network/algorithm (ED25519=0x00, RSA4096=0x02) | Valid enum; not NISTP256 (unsupported) |

### DecryptionKeyInfo (New - Internal)

Parameter object for passing decryptor's cryptographic identity.

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| WalletAddress | string | Decryptor's wallet address | Must exist in payload's EncryptedKeys |
| PrivateKey | byte[] | Decryptor's private key bytes | Non-empty, valid for network type |
| Network | WalletNetworks | Cryptographic network/algorithm | Valid enum; must match encryption network |

## State Transitions

### Payload Lifecycle

```
[Created] → AddPayloadAsync() → [Encrypted + Stored]
                                       │
                    GrantAccessAsync() ←┘→ GetPayloadDataAsync() → [Decrypted for recipient]
                                       │
                    VerifyPayloadAsync() → [Verified / Failed]
                                       │
                    RevokeAccessAsync() → [Access removed]
                                       │
                    RemovePayloadAsync() → [Removed]
```

### Legacy Detection Flow

```
GetPayloadDataAsync() → Check IV
    ├── IV is all zeros → Return Data as-is (legacy unencrypted)
    └── IV is non-zero  → Decrypt symmetric key → Decrypt data → Return plaintext
```

## Relationships

```
PayloadManager 1──* Payload
Payload 1──* EncryptedKey (via EncryptedKeys dictionary)
Payload *──1 EncryptionType (enum)
Payload *──1 PayloadType (enum)
PayloadOptions ──> EncryptionType, HashType, CompressionType
```

## Existing Interfaces (Modified Signatures)

### IPayloadManager Changes

Current `AddPayloadAsync(byte[] data, string[] recipientWallets, ...)` needs to accept cryptographic identity for each recipient. Two approaches:

**Option A (Chosen)**: Add overload accepting `RecipientKeyInfo[]` instead of `string[]`. Keep existing signature for backward compatibility but mark as obsolete.

**Option B (Rejected)**: Modify internal Payload class only. Would require PayloadManager to resolve keys, adding unwanted service dependencies.

### Constructor Change

```
Before: PayloadManager()
After:  PayloadManager(ISymmetricCrypto, ICryptoModule, IHashProvider)
```

All 10 call sites must be updated to pass crypto dependencies.
