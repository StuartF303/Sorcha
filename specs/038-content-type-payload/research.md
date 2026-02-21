# Research: Content-Type Aware Payload Encoding

**Feature**: 038-content-type-payload
**Date**: 2026-02-21

## R-001: Base64 Call Site Inventory

**Decision**: Migrate all 124 Base64 call sites across 24 source files to Base64url using `System.Buffers.Text.Base64Url`.

**Rationale**: Comprehensive audit found `Convert.ToBase64String` / `Convert.FromBase64String` in 24 source files. The migration must be atomic per-file to avoid mixed encoding within a single code path. Three files already contain manual Base64url implementations (`DatabaseInitializer.cs`, `ServiceAuthService.cs`, `SdJwtService.cs`) — these should be consolidated to use the standard .NET API.

**Key call sites by component**:

| Component | Files | Call Sites | Notes |
|-----------|-------|-----------|-------|
| TransactionHandler | JsonTransactionSerializer.cs, PayloadManager.cs | ~12 | Core serialization — highest risk |
| Blueprint Service | Program.cs, ITransactionBuilderService.cs | ~8 | Action TX construction, signing |
| Validator Service | DocketBuildTriggerService.cs, DocketSerializer.cs, ValidationEngine.cs | ~10 | Register model construction, verification |
| Register Service | RegisterCreationOrchestrator.cs, endpoints | ~6 | Genesis TX, query responses |
| Wallet Service | Various signing/verification paths | ~8 | Key material encoding |
| Tenant Service | ServiceAuthService.cs, SdJwtService.cs | ~6 | Already has manual Base64url |
| Cryptography lib | Sorcha.Cryptography | ~8 | Low-level crypto operations |
| UI Core | Various display components | ~4 | Display-only, read path |

**Alternatives considered**:
- Manual `Replace('+', '-').Replace('/', '_').TrimEnd('=')` — fragile, already done inconsistently in 3 files
- Third-party library (e.g., `Microsoft.IdentityModel.Tokens.Base64UrlEncoder`) — unnecessary, .NET 7+ has built-in

## R-002: .NET Base64Url API Availability

**Decision**: Use `System.Buffers.Text.Base64Url` (available since .NET 7, built-in to .NET 10).

**Rationale**: The `Base64Url` class provides `EncodeToString(ReadOnlySpan<byte>)` and `DecodeFromChars(ReadOnlySpan<char>)`. No NuGet dependency needed. The codebase currently targets .NET 10 across all projects.

**Key APIs**:
- `Base64Url.EncodeToString(byte[])` — replaces `Convert.ToBase64String(byte[])`
- `Base64Url.DecodeFromChars(string)` — replaces `Convert.FromBase64String(string)`
- No padding characters produced (RFC 4648 Section 5 compliant)

**Alternatives considered**:
- `Microsoft.IdentityModel.Tokens.Base64UrlEncoder` — adds a dependency for something already in the BCL
- Custom helper class — unnecessary given built-in support

## R-003: Challenge Model Structure

**Decision**: `Challenge.Data` (encrypted key material) must migrate to Base64url encoding alongside other binary fields.

**Rationale**: `Challenge` model (in `Sorcha.Register.Models`) has `Data` (string? — Base64-encoded encrypted symmetric key) and `Address` (string? — wallet address, not binary). The `Data` field is produced by `PayloadManager.Encrypt()` and consumed by decryption. `Address` is a wallet address string and does NOT need encoding changes.

**Impact**: `Challenge.Data` is written in `JsonTransactionSerializer.cs` line 64: `Data = Convert.ToBase64String(c.Value)` where `c.Value` is the asymmetrically-encrypted symmetric key bytes.

## R-004: TransactionModel.Signature Encoding Paths

**Decision**: 4 code paths that set `TransactionModel.Signature` must all migrate to Base64url.

**Rationale**: TransactionModel.Signature is set via `Convert.ToBase64String` in:
1. `ITransactionBuilderService.cs` (Blueprint Service) — line 323, action/control TX construction
2. `Blueprint.Service/Program.cs` — line 886, genesis TX
3. `DocketBuildTriggerService.cs` (Validator) — line 282, register model from validated TX
4. `DocketSerializer.cs` (Validator) — line 93, docket serialization

All 4 paths must use Base64url. The Validator also verifies signatures by decoding — `Convert.FromBase64String` in `ValidationEngine.cs` line 512 area.

## R-005: InMemoryRegisterRepository Impact

**Decision**: No changes needed to `InMemoryRegisterRepository`.

**Rationale**: Stores `TransactionModel` objects directly in `ConcurrentDictionary`. No serialization/deserialization — objects are held in memory as-is. The `TransactionModel.Payloads[].Data` will contain Base64url strings (new) or Base64 strings (legacy) but the InMemory implementation doesn't inspect or transform them.

## R-006: MongoRegisterRepository BSON Binary Strategy

**Decision**: Introduce internal `MongoTransactionDocument` / `MongoPayloadDocument` types with `byte[]` fields. Convert at repository boundary.

**Rationale**: MongoDB C# driver auto-serializes `byte[]` as BSON Binary (BinData subtype 0x00). Creating internal document types keeps the BSON optimization invisible to `IRegisterRepository`. Legacy detection: check if the stored field is a `BsonString` (legacy) or `BsonBinaryData` (new) and handle accordingly.

**Key patterns**:
- Write path: `PayloadModel.Data` (Base64url string) → decode to `byte[]` → store as BSON Binary
- Read path: BSON Binary → `byte[]` → encode to Base64url string → `PayloadModel.Data`
- Legacy read: BsonString → `string` (already Base64) → `PayloadModel.Data` (preserve as-is or transcode)

## R-007: Compression Implementation

**Decision**: Use `System.IO.Compression.BrotliStream` (default) and `System.IO.Compression.GZipStream` (alternative). 4KB threshold.

**Rationale**: Both are built-in to .NET. Brotli achieves ~70-80% compression on JSON. The 4KB threshold prevents compression overhead on small payloads (Brotli header + dictionary overhead can exceed savings for payloads under ~1KB, 4KB provides safety margin).

**Compression flow**: plaintext bytes → compress → Base64url encode → store
**Decompression flow**: retrieve → Base64url decode → decompress → plaintext bytes
**Hash**: computed on compressed bytes (post-compression, pre-Base64url-encoding)

## R-008: Validator Schema Validation with Compression

**Decision**: Validator must decompress before schema validation when `ContentEncoding` indicates compression.

**Rationale**: `ValidationEngine.cs` lines 406-422 extract user payload for JSON schema validation. Currently assumes Base64 string → decode → JSON. With compression: Base64url decode → decompress → JSON. The `ContentEncoding` field drives the decode pipeline.

## R-009: Canonical Hash Stability

**Decision**: No hash computation changes needed. Hashes already operate on raw bytes, not on text encoding.

**Rationale**: The signature contract `SHA256("{TxId}:{PayloadHash}")` uses `PayloadHash` which is computed on canonical bytes before any encoding. Changing from Base64 to Base64url for the wire representation does NOT change the bytes being hashed. This is the critical invariant: hashes and signatures are encoding-agnostic.

For compressed payloads (FR-018), the hash covers compressed bytes — this is a new behavior, but it means verification never requires decompression.
