# Data Model: Content-Type Aware Payload Encoding

**Feature**: 038-content-type-payload
**Date**: 2026-02-21

## Entity Changes

### PayloadModel (Modified)

**Location**: `src/Common/Sorcha.Register.Models/PayloadModel.cs`
**Role**: Register-level representation of a transaction payload

| Field | Type | Status | Description |
|-------|------|--------|-------------|
| Type | PayloadType | Existing | Sorcha-semantic role (Data, Document, Message, Metadata, Custom) |
| Data | string / JsonElement | Modified | Wire representation depends on ContentEncoding |
| Hash | string | Existing | SHA-256 hash of canonical bytes (Base64url for new) |
| IV | Challenge? | Existing | Initialization vector for encryption |
| Challenges | Challenge[]? | Existing | Per-wallet encrypted symmetric keys |
| **ContentType** | string? | **New** | MIME type (e.g., `application/json`, `application/pdf`) |
| **ContentEncoding** | string? | **New** | Encoding scheme: `identity`, `base64url`, `base64` (legacy), `br+base64url`, `gzip+base64url` |

**Validation rules**:
- `ContentType`: Optional. When absent, infer `application/octet-stream`.
- `ContentEncoding`: Optional. When absent, infer `base64` (legacy behavior).
- `ContentEncoding: "base64"` accepted on read, never produced on write.
- When `ContentEncoding: "identity"`, `Data` MUST be valid JSON if `ContentType` is `application/json`.

### PayloadInfo (Modified)

**Location**: `src/Common/Sorcha.TransactionHandler/Models/PayloadInfo.cs`
**Role**: Metadata summary for consumer introspection

| Field | Type | Status | Description |
|-------|------|--------|-------------|
| PayloadType | PayloadType | Existing | Semantic role |
| Hash | string | Existing | Content hash |
| **ContentType** | string? | **New** | MIME type |
| **ContentEncoding** | string? | **New** | Encoding scheme |

### Payload (Internal, Modified)

**Location**: `src/Common/Sorcha.TransactionHandler/Payload/PayloadManager.cs` (internal class)
**Role**: In-memory payload during transaction construction

| Field | Type | Status | Description |
|-------|------|--------|-------------|
| Data | byte[] | Existing | Raw plaintext bytes |
| IV | byte[] | Existing | Encryption IV |
| Hash | byte[] | Existing | SHA-256 hash |
| EncryptedKeys | Dictionary<string, byte[]> | Existing | Per-wallet keys |
| **ContentType** | string | **New** | MIME type from builder |
| **ContentEncoding** | string | **New** | Resolved encoding for serialization |

### MongoPayloadDocument (New, Internal)

**Location**: `src/Core/Sorcha.Register.Storage.MongoDB/Models/MongoPayloadDocument.cs`
**Role**: MongoDB-specific document model with native binary fields

| Field | Type | Description |
|-------|------|-------------|
| Type | int | PayloadType enum value |
| Data | byte[] | Raw binary (BSON Binary subtype 0x00) |
| Hash | byte[] | SHA-256 hash bytes |
| IV | byte[]? | Encryption IV bytes |
| Challenges | MongoChallengeDocument[]? | Per-wallet encrypted keys |
| ContentType | string? | MIME type (stored as string) |
| ContentEncoding | string? | Encoding scheme (stored as string) |

### MongoChallengeDocument (New, Internal)

**Location**: `src/Core/Sorcha.Register.Storage.MongoDB/Models/MongoPayloadDocument.cs`
**Role**: MongoDB-specific challenge document with native binary key data

| Field | Type | Description |
|-------|------|-------------|
| Data | byte[]? | Encrypted symmetric key bytes (BSON Binary) |
| Address | string? | Wallet address (string, no change) |

### MongoTransactionDocument (New, Internal)

**Location**: `src/Core/Sorcha.Register.Storage.MongoDB/Models/MongoTransactionDocument.cs`
**Role**: MongoDB-specific transaction document with native binary signature

| Field | Type | Description |
|-------|------|-------------|
| (all existing TransactionModel fields) | various | Mapped from TransactionModel |
| Signature | byte[] | Signature bytes (BSON Binary) |
| Payloads | MongoPayloadDocument[] | Payload documents with binary fields |

## State Transitions

### ContentEncoding Resolution

```
Builder Input                    → Resolved ContentEncoding
─────────────────────────────────────────────────────────
JSON + unencrypted + < 4KB      → "identity"
JSON + unencrypted + >= 4KB     → "br+base64url" (Brotli)
Binary + unencrypted            → "base64url"
Binary + unencrypted + >= 4KB   → "br+base64url" (Brotli)
Any + encrypted                 → "base64url" (ciphertext is always binary)
Any + encrypted + >= 4KB        → "br+base64url" (compress plaintext before encrypt)
Legacy (no metadata)            → null (inferred as "base64" on read)
```

### MongoDB Storage Format Detection

```
Read from MongoDB:
  Field is BsonBinaryData → decode as byte[] → encode to Base64url → PayloadModel
  Field is BsonString     → use as-is (legacy Base64 string) → PayloadModel
```

## Relationships

```
TransactionModel
  ├── Signature: string (Base64url for new, Base64 for legacy)
  └── Payloads: PayloadModel[]
        ├── Data: string|JsonElement (depends on ContentEncoding)
        ├── Hash: string (Base64url)
        ├── ContentType: string? (MIME)
        ├── ContentEncoding: string? (encoding scheme)
        ├── IV: Challenge?
        │     └── Data: string (Base64url encrypted key material)
        └── Challenges: Challenge[]?
              └── Data: string (Base64url encrypted key material)

MongoTransactionDocument (internal)
  ├── Signature: byte[] (BSON Binary)
  └── Payloads: MongoPayloadDocument[]
        ├── Data: byte[] (BSON Binary)
        ├── Hash: byte[] (BSON Binary)
        ├── ContentType: string?
        ├── ContentEncoding: string?
        ├── IV: MongoChallengeDocument?
        │     └── Data: byte[]? (BSON Binary)
        └── Challenges: MongoChallengeDocument[]?
              └── Data: byte[]? (BSON Binary)
```
