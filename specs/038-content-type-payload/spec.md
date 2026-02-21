# Feature Specification: Content-Type Aware Payload Encoding

**Feature Branch**: `038-content-type-payload`
**Created**: 2026-02-21
**Status**: Draft
**Input**: User description: "Add ContentType and ContentEncoding metadata fields to transaction payloads, migrate from Base64 to Base64url encoding, and optimize MongoDB storage using BSON Binary for payload data."

## Clarifications

### Session 2026-02-21

- Q: Should Base64url migration cover only PayloadModel fields or also transaction-level Signature, PublicKey, and SignatureValue? → A: Migrate all binary fields — payload and transaction-level (Signature, PublicKey, SignatureValue) — to Base64url together.
- Q: Are compression encodings (Brotli, Gzip) in scope or deferred? → A: Include compression encodings (`br+base64url`, `gzip+base64url`) in this feature.
- Note: Testing must be extensive to ensure cryptographic operations do not break. Both existing and new encoding paths must be tested for conformance and edge cases to ensure consistent, reliable service.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Self-Describing Payloads (Priority: P1)

When a developer, register explorer, or UI component reads a payload from the register, the payload metadata tells them what the data is (JSON, PDF, binary) and how it is encoded (raw, base64url, compressed). Today all payloads appear as opaque Base64 strings with no indication of content. This forces consumers to guess or hard-code assumptions about payload format.

**Why this priority**: This is the foundation for all other stories. Without content-type metadata, consumers cannot make encoding or rendering decisions. Every downstream improvement depends on this metadata existing.

**Independent Test**: Publish a transaction with a JSON payload. Read the transaction back. The payload metadata includes `contentType: "application/json"` and `contentEncoding` reflecting its wire format. A consumer can programmatically determine the payload is JSON without inspecting the data.

**Acceptance Scenarios**:

1. **Given** a new transaction payload is created, **When** the builder specifies the content type and encoding, **Then** the persisted payload includes `ContentType` and `ContentEncoding` fields alongside the existing data.
2. **Given** a payload with no content-type metadata (legacy), **When** a consumer reads the payload, **Then** the system treats it as `application/octet-stream` with `base64` encoding — identical to current behavior.
3. **Given** a transaction with `ContentType: "application/json"` and `ContentEncoding: "identity"`, **When** a consumer reads the payload data, **Then** the data is a native JSON object, not a Base64-encoded string of JSON.

---

### User Story 2 - Base64url Encoding Migration (Priority: P1)

All new binary-to-text encoding in the transaction pipeline uses Base64url (RFC 4648 Section 5) instead of standard Base64. This aligns with JWT conventions (already used for authentication tokens) and wallet address patterns, and eliminates the `+` character that currently requires `UnsafeRelaxedJsonEscaping` workarounds for hash stability.

**Why this priority**: Encoding consistency is a cross-cutting concern. If done alongside the content-type metadata, it avoids a second migration pass. The `+` character in standard Base64 is an active source of hash fragility across serialization boundaries.

**Independent Test**: Submit a transaction through the full pipeline (build, sign, validate, store, retrieve). All binary fields (payload data, IV, hash, signatures, public keys) use Base64url encoding. The validator successfully verifies signatures. Existing Base64-encoded transactions on the register remain readable.

**Acceptance Scenarios**:

1. **Given** a new transaction is built, **When** binary fields are serialized for JSON transport, **Then** they use Base64url encoding (alphabet: `A-Z a-z 0-9 - _`, no padding `=`).
2. **Given** a legacy transaction stored with standard Base64 encoding, **When** it is read from the register, **Then** the system decodes it correctly using standard Base64 (backward compatible).
3. **Given** a Base64url-encoded payload hash, **When** the validator computes and compares the hash, **Then** signature verification succeeds because the canonical bytes (not the encoding) determine the hash.

---

### User Story 3 - MongoDB BSON Binary Storage Optimization (Priority: P2)

The MongoDB storage layer stores binary payload data (encrypted ciphertext, hashes, IVs) as native BSON Binary (`BinData`) instead of Base64 strings. This reduces storage volume by approximately 33% for binary fields. The optimization is internal to the MongoDB repository implementation and invisible to business logic or API consumers.

**Why this priority**: Storage efficiency matters at scale but does not change external behavior. It is behind the `IRegisterRepository` abstraction and can be deployed independently. It depends on the content-type metadata (P1) to correctly identify which fields carry binary data.

**Independent Test**: Store a transaction via the Register Service. Inspect the MongoDB document directly — payload `Data`, `Hash`, and `IV` fields are stored as BSON Binary type. Read the transaction back via the API — the response contains Base64url-encoded strings as before. No change visible to API consumers.

**Acceptance Scenarios**:

1. **Given** a new transaction is persisted to MongoDB, **When** the payload contains encrypted binary data, **Then** the `Data`, `Hash`, and `IV` fields are stored as BSON Binary (subtype 0x00) internally.
2. **Given** a transaction is read from MongoDB, **When** the repository converts it to the API model, **Then** binary fields are re-encoded to Base64url strings in `PayloadModel`.
3. **Given** an existing transaction stored with Base64 string fields (pre-migration), **When** it is read by the updated repository, **Then** it is decoded correctly from the legacy string format — no data loss.
4. **Given** the storage backend is swapped from MongoDB to Azure Cosmos DB (MongoDB API), **When** the same repository implementation is used, **Then** BSON Binary storage works identically with no code changes.

---

### User Story 4 - Native JSON Payload Embedding (Priority: P3)

When an unencrypted payload's content type is `application/json`, it is stored and transmitted as a native JSON object rather than a Base64-encoded string of JSON. This eliminates the 33% Base64 overhead and removes an unnecessary encode/decode step for the most common payload type in blueprint action transactions.

**Why this priority**: This is an optimization for the disclosed/envelope layer. Encrypted payloads (the majority of register data) remain binary and cannot benefit from this. The value is highest for the transaction submission pipeline where disclosed payloads are already JSON objects.

**Independent Test**: Execute a blueprint action. The `TransactionSubmission.Payload` contains native JSON objects in the `payloads` map — not Base64-encoded strings. The validator receives and schema-validates the JSON directly. The register stores the JSON natively.

**Acceptance Scenarios**:

1. **Given** a blueprint action produces JSON payload data, **When** the transaction builder serializes the payload, **Then** JSON data is embedded as a native JSON object with `ContentEncoding: "identity"`.
2. **Given** a payload with `ContentType: "application/json"` and `ContentEncoding: "identity"`, **When** the validator performs schema validation, **Then** it validates the JSON directly without any decoding step.
3. **Given** a payload with `ContentType: "application/pdf"` (binary), **When** the transaction builder serializes it, **Then** it uses `ContentEncoding: "base64url"` because binary data cannot be embedded natively in JSON.

---

### User Story 5 - Payload Compression (Priority: P3)

Payloads larger than a configurable size threshold are compressed before encoding, reducing storage volume and network bandwidth. Brotli (`br+base64url`) and Gzip (`gzip+base64url`) are supported. The `ContentEncoding` field tells consumers which decompression to apply. Compression is applied to the plaintext data before encryption for encrypted payloads, and to the serialized data for unencrypted payloads.

**Why this priority**: JSON payloads compress 70-80% with Brotli. Combined with native JSON embedding (P3), this significantly reduces register storage and peer gossip bandwidth for large action payloads. Depends on ContentType/ContentEncoding metadata (P1) being in place.

**Independent Test**: Submit a blueprint action with a JSON payload larger than the compression threshold. The stored payload has `ContentEncoding: "br+base64url"`. Read it back — the system decompresses transparently and returns the original JSON. The payload hash verifies against the compressed representation.

**Acceptance Scenarios**:

1. **Given** a payload larger than the compression threshold, **When** the builder serializes it, **Then** the data is compressed (Brotli by default) and `ContentEncoding` is set to `br+base64url`.
2. **Given** a payload smaller than the compression threshold, **When** the builder serializes it, **Then** no compression is applied — the overhead is not worthwhile for small payloads.
3. **Given** a compressed payload with `ContentEncoding: "br+base64url"`, **When** the validator performs schema validation, **Then** it decompresses the data before validating against the JSON schema.
4. **Given** a compressed payload, **When** the cryptographic hash is computed, **Then** the hash covers the compressed bytes (the stored representation), not the original plaintext.
5. **Given** a consumer reads a compressed payload, **When** it decodes based on `ContentEncoding`, **Then** it applies Base64url decoding followed by Brotli decompression to recover the original data.

---

### User Story 6 - Cryptographic Integrity Conformance Testing (Priority: P1)

Every encoding path (Base64url, identity, br+base64url, gzip+base64url) and every cryptographic operation (signing, verification, hashing, encryption, decryption) must be extensively tested to prove the encoding migration does not break cryptographic guarantees. Tests must cover both new transactions using the updated encoding and legacy transactions using the original Base64 encoding, verifying that the system handles both correctly and consistently.

**Why this priority**: This feature changes how binary data is represented at every serialization boundary in the transaction pipeline. A subtle encoding error could silently break signature verification, hash integrity, or payload decryption — failures that may not be immediately visible but undermine the entire trust model. Cryptographic conformance testing is not optional; it gates the release of all other stories.

**Independent Test**: Run the full cryptographic test suite covering: (1) sign-then-verify round-trip with Base64url-encoded signatures, (2) hash stability across encode/decode/re-encode cycles, (3) encrypt-then-decrypt round-trip with Base64url-encoded ciphertext/IV/keys, (4) legacy Base64 transactions verify correctly alongside new Base64url transactions, (5) compressed payload hash integrity, (6) cross-encoding interop (legacy producer, new consumer and vice versa).

**Acceptance Scenarios**:

1. **Given** a transaction signed with Base64url-encoded signature fields, **When** the validator verifies the signature, **Then** verification succeeds and produces identical results to the same transaction signed with the previous Base64 encoding.
2. **Given** an encrypted payload with Base64url-encoded ciphertext, IV, and per-wallet encrypted keys, **When** an authorized wallet decrypts the payload, **Then** the decrypted plaintext matches the original input byte-for-byte.
3. **Given** a payload hash computed on canonical bytes, **When** the bytes are encoded to Base64url, stored, retrieved, and decoded back, **Then** recomputing the hash on the decoded bytes produces an identical hash — proving encoding round-trip is lossless.
4. **Given** a register containing both legacy (Base64) and new (Base64url) transactions, **When** the system reads and verifies both, **Then** all signatures and hashes verify correctly — no cross-contamination between encoding formats.
5. **Given** a compressed payload (Brotli or Gzip), **When** the hash is computed on the compressed bytes and later verified after retrieval, **Then** the hash matches — proving compression is deterministic and the stored representation is stable.
6. **Given** known test vectors (fixed input bytes, expected Base64url output, expected hash), **When** the encoding and hashing functions are applied, **Then** the outputs match the expected values exactly — preventing implementation drift.

---

### User Story 7 - Cloud Document Store Portability (Priority: P4 — Future)

The storage abstraction supports alternative cloud document stores without requiring changes to the content-type or encoding model. Each cloud provider implementation handles binary storage according to its native capabilities, while the `IRegisterRepository` interface remains unchanged.

**Why this priority**: This is future work that validates the architectural decision. No implementation is needed now, but the design must not preclude these implementations.

**Independent Test**: (Future) Deploy the Register Service against Azure Cosmos DB, AWS DynamoDB, or Google Firestore. The content-type metadata and payload encoding work correctly with each backend's native binary handling.

**Acceptance Scenarios**:

1. **Given** an Azure Cosmos DB (MongoDB API) backend, **When** the existing `MongoRegisterRepository` is used with a Cosmos connection string, **Then** BSON Binary storage works identically — zero code changes required.
2. **Given** an Azure Cosmos DB (NoSQL API) backend, **When** a future `CosmosRegisterRepository` is implemented against `IRegisterRepository`, **Then** it stores `byte[]` fields natively (Cosmos handles serialization) and converts at the repository boundary.
3. **Given** an AWS DynamoDB backend, **When** a future `DynamoRegisterRepository` is implemented against `IRegisterRepository`, **Then** it uses DynamoDB's native `B` (binary) attribute type for payload fields and converts at the repository boundary.
4. **Given** a Google Firestore backend, **When** a future `FirestoreRegisterRepository` is implemented against `IRegisterRepository`, **Then** it uses Firestore's native `Bytes` type for payload fields and converts at the repository boundary.

---

### Edge Cases

- What happens when `ContentType` is present but `ContentEncoding` is null? The system infers encoding based on content type: `application/json` defaults to `identity`; all others default to `base64url`.
- What happens when a payload claims `ContentEncoding: "identity"` but the data contains invalid JSON? The system rejects the payload at validation time with a descriptive error.
- What happens during a rolling deployment where some services produce Base64 and others produce Base64url? The validator and register accept both encodings. The `ContentEncoding` field (or its absence for legacy) determines which decoder to use.
- What happens if `ContentType` contains an unsupported MIME type? The system stores and retrieves it as-is — content type is informational metadata, not a processing directive. Unknown types are treated as opaque binary.
- What happens to the cryptographic hash when encoding changes from Base64 to Base64url? No impact. Hashes are always computed on the canonical byte representation (pre-encoding), not on the encoded text. Changing the text encoding does not change the bytes being hashed.
- What happens if a MongoDB document contains a mix of string-format (legacy) and BSON Binary-format fields? The repository detects each field's storage format individually and converts accordingly. Mixed documents are supported.
- What happens if compression produces output larger than the input (small payloads)? The size threshold (default 4KB) prevents this. If compression is forced and output exceeds input, the system stores uncompressed with `base64url` encoding instead.
- What happens if decompression fails (corrupted compressed data)? The system returns an error. The integrity hash (computed on compressed bytes) would have already detected corruption if checked first.
- What happens if a Base64url string is accidentally decoded with a standard Base64 decoder (or vice versa)? The `-` and `_` characters in Base64url are not valid in standard Base64 (and `+` and `/` are not in Base64url). Decoders reject invalid characters, producing an explicit error rather than silent corruption. Conformance tests verify this boundary.
- What happens if the same plaintext is compressed with Brotli on two different platforms — do the compressed bytes differ? Brotli compression is not guaranteed to be deterministic across implementations. However, the hash is computed on the compressed output from the producing service, so verification always succeeds regardless. Decompression is deterministic — the same compressed bytes always decompress to the same plaintext.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Payloads MUST support an optional `ContentType` field containing a standard MIME type string (e.g., `application/json`, `application/pdf`, `application/octet-stream`).
- **FR-002**: Payloads MUST support an optional `ContentEncoding` field indicating how the `Data` field is represented. Supported values: `identity` (raw/native), `base64url`, `base64` (legacy read-only), `br+base64url` (Brotli-compressed then Base64url-encoded), `gzip+base64url` (Gzip-compressed then Base64url-encoded).
- **FR-003**: When `ContentType` and `ContentEncoding` are absent (legacy payloads), the system MUST treat the payload as `application/octet-stream` with `base64` encoding — preserving exact current behavior.
- **FR-004**: All new binary-to-text encoding — including payload `Data`, `Hash`, `IV`, `Challenges`, and transaction-level `Signature`, `SignatureInfo.PublicKey`, and `SignatureInfo.SignatureValue` — MUST use Base64url (RFC 4648 Section 5, no padding) instead of standard Base64.
- **FR-005**: The system MUST continue to accept and correctly decode standard Base64-encoded data in legacy transactions.
- **FR-006**: The MongoDB storage layer MUST store binary payload fields (`Data`, `Hash`, `IV`) as BSON Binary (subtype 0x00) internally, converting to/from the appropriate text encoding at the repository boundary.
- **FR-007**: The `PayloadType` enum (Data, Document, Message, Metadata, Custom) MUST be retained as-is. `ContentType` is orthogonal — it describes the data format, while `PayloadType` describes the Sorcha-semantic role.
- **FR-008**: When `ContentType` is `application/json` and `ContentEncoding` is `identity`, the `Data` field MUST contain a native JSON value (object, array, or primitive) — not a string-encoded representation.
- **FR-009**: Cryptographic hash computation MUST always operate on canonical byte representations, never on the text encoding. Changing from Base64 to Base64url MUST NOT alter any hash values.
- **FR-010**: The gRPC transport layer MUST continue to use native `bytes` fields for binary data — no text encoding on the wire.
- **FR-011**: The `IRegisterRepository` interface and all non-MongoDB implementations (InMemory, future cloud stores) MUST NOT be affected by the BSON Binary optimization. The optimization is internal to `MongoRegisterRepository`.
- **FR-012**: Legacy transactions already persisted in MongoDB as Base64 strings MUST remain readable without migration. The repository MUST detect and handle both string-format and BSON Binary-format documents.
- **FR-013**: The design MUST NOT preclude future implementations of `IRegisterRepository` for cloud document stores (Azure Cosmos DB NoSQL API, AWS DynamoDB, Google Firestore). Each implementation may use its provider's native binary storage capabilities.
- **FR-014**: The `ContentEncoding` value `base64` MUST be accepted for reading legacy data but MUST NOT be produced by new write operations. New writes MUST use `base64url` for binary data or `identity` for native JSON.
- **FR-015**: When `ContentEncoding` is `br+base64url` or `gzip+base64url`, the `Data` field contains compressed-then-Base64url-encoded bytes. Consumers MUST Base64url-decode then decompress to recover the original content.
- **FR-016**: Compression MUST only be applied to payloads exceeding a configurable size threshold (default: 4KB). Payloads below the threshold MUST NOT be compressed, as compression overhead may increase size for small inputs.
- **FR-017**: The default compression algorithm MUST be Brotli. Gzip MUST be supported as an alternative for interoperability with systems that do not support Brotli.
- **FR-018**: For compressed payloads, the cryptographic integrity hash MUST be computed on the compressed bytes (the stored representation). This ensures hash verification does not require decompression.
- **FR-019**: The validator MUST decompress payloads before performing JSON schema validation when `ContentEncoding` indicates compression and `ContentType` is `application/json`.
- **FR-020**: The feature MUST include extensive cryptographic conformance tests covering every encoding path (`base64url`, `identity`, `br+base64url`, `gzip+base64url`) combined with every cryptographic operation (signing, verification, hashing, symmetric encryption/decryption, asymmetric key wrapping/unwrapping).
- **FR-021**: Conformance tests MUST verify that legacy transactions (Base64-encoded) continue to pass signature verification, hash integrity checks, and payload decryption after the encoding migration — ensuring zero regression.
- **FR-022**: Conformance tests MUST include known test vectors with fixed inputs and expected outputs for Base64url encoding, hash computation, and compression, preventing implementation drift across future changes.
- **FR-023**: Conformance tests MUST cover cross-encoding interoperability: a transaction produced by a legacy (Base64) producer MUST be consumable by an updated (Base64url) consumer, and vice versa.
- **FR-024**: Conformance tests MUST verify encoding round-trip stability: data encoded to any supported `ContentEncoding`, stored, retrieved, and decoded MUST produce byte-identical output to the original input.

### Key Entities

- **PayloadModel**: The register-level representation of a transaction payload. Gains `ContentType` (MIME string) and `ContentEncoding` (encoding scheme) fields. `Data` field interpretation depends on `ContentEncoding`.
- **Payload (internal)**: The in-memory payload managed by `PayloadManager`. Gains content-type awareness so builders can specify the semantic type of plaintext data before encryption.
- **PayloadInfo**: The metadata summary of a payload. Extended with `ContentType` and `ContentEncoding` for consumer introspection.
- **MongoPayloadDocument (new, internal)**: An internal MongoDB-specific document model where binary fields use native binary types instead of strings. Exists only within `MongoRegisterRepository`.

### Future Cloud Store Implementations (Deferred)

The following implementations are out of scope for this feature but are documented here as future work items that the design must accommodate:

- **CosmosRegisterRepository** (Azure Cosmos DB NoSQL API): Would use the Cosmos SDK with native JSON serialization. Binary fields stored as Base64 strings in the JSON document (Cosmos handles this transparently for `byte[]` properties). Repository boundary converts to/from `PayloadModel`.
- **DynamoRegisterRepository** (AWS DynamoDB): Would use the DynamoDB SDK with native `B` (binary) attribute type for payload fields. Repository boundary converts to/from `PayloadModel`.
- **FirestoreRegisterRepository** (Google Firestore): Would use the Firestore SDK with native `Bytes` type for payload fields (up to 1MB). Repository boundary converts to/from `PayloadModel`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All existing transactions on the register remain readable after the change — zero data loss, zero behavioral regression. Verified by running the Medical Equipment Refurbishment walkthrough end-to-end.
- **SC-002**: New transactions carry `ContentType` and `ContentEncoding` metadata that consumers can read programmatically, enabling format-aware rendering.
- **SC-003**: MongoDB storage volume for binary payload fields decreases by approximately 33% for new transactions (BSON Binary vs Base64 string).
- **SC-004**: The full transaction pipeline (build, sign, validate, store, retrieve) works correctly with Base64url encoding — signature verification passes, hash integrity holds.
- **SC-005**: The `UnsafeRelaxedJsonEscaping` workaround for the `+` character becomes unnecessary for new Base64url-encoded fields (no `+` in Base64url alphabet).
- **SC-006**: All existing unit and integration tests continue to pass without modification (backward compatibility).
- **SC-007**: Swapping MongoDB for Azure Cosmos DB (MongoDB API) requires zero code changes to the storage optimization — the BSON Binary approach works identically.
- **SC-008**: Brotli-compressed JSON payloads achieve 70-80% size reduction compared to uncompressed Base64url encoding for payloads over 4KB.
- **SC-009**: Compressed payloads round-trip correctly through the full pipeline — compress, encode, sign, validate (decompress for schema check), store, retrieve, decode, decompress — with hash integrity preserved.
- **SC-010**: Cryptographic conformance test suite covers all encoding-path × crypto-operation combinations (minimum 5 encoding paths × 4 crypto operations = 20 test scenarios) with zero failures.
- **SC-011**: Legacy transaction conformance tests verify that transactions created before the migration continue to pass all cryptographic checks (signature, hash, decryption) without modification.
- **SC-012**: Known test vectors produce byte-identical outputs across repeated runs and across different platforms, proving deterministic behavior.
