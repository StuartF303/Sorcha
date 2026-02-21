# Quickstart: Content-Type Aware Payload Encoding

**Feature**: 038-content-type-payload

## What Changes

1. **PayloadModel** gets two new optional fields: `ContentType` (MIME string) and `ContentEncoding` (encoding scheme)
2. **All binary-to-text encoding** migrates from Base64 to Base64url (RFC 4648 Section 5)
3. **MongoDB** stores binary payload fields as BSON Binary internally (33% smaller)
4. **JSON payloads** can be embedded natively (no Base64 wrapping) when unencrypted
5. **Large payloads** (>4KB) are compressed with Brotli before encoding
6. **Legacy transactions** remain fully readable — zero migration needed

## Implementation Order

```
Phase 1: Foundation
  ├── Add ContentType/ContentEncoding to PayloadModel, PayloadInfo
  ├── Create IPayloadEncodingService (centralized encode/decode)
  └── Create conformance test infrastructure with known test vectors

Phase 2: Base64url Migration
  ├── Replace Convert.ToBase64String → Base64Url.EncodeToString (24 files, 124 sites)
  ├── Replace Convert.FromBase64String → Base64Url.DecodeFromChars (with legacy fallback)
  ├── Update JsonTransactionSerializer (core write path)
  ├── Update PayloadManager (encryption path)
  ├── Update ITransactionBuilderService (Blueprint Service)
  ├── Update DocketBuildTriggerService + DocketSerializer (Validator)
  └── Run full conformance test suite

Phase 3: MongoDB BSON Binary
  ├── Create MongoPayloadDocument / MongoTransactionDocument (internal)
  ├── Add conversion logic in MongoRegisterRepository
  ├── Add legacy BsonString detection for backward compatibility
  └── Verify via direct MongoDB inspection

Phase 4: Native JSON + Compression
  ├── Implement identity encoding for JSON payloads
  ├── Implement Brotli/Gzip compression with threshold
  ├── Update ValidationEngine to decompress before schema validation
  └── Hash-on-compressed-bytes implementation

Phase 5: Cross-Cutting Verification
  ├── Full pipeline walkthrough (build → sign → validate → store → retrieve)
  ├── Legacy + new mixed transaction verification
  ├── Cross-encoding interoperability tests
  └── Known test vector verification
```

## Key Design Decisions

| Decision | Choice | Reason |
|----------|--------|--------|
| Base64url API | `System.Buffers.Text.Base64Url` | Built-in .NET 7+, no dependency |
| Compression default | Brotli | 70-80% on JSON, built-in .NET |
| Compression threshold | 4KB | Below this, overhead exceeds savings |
| Hash target | Compressed bytes | Verification without decompression |
| BSON Binary scope | MongoRegisterRepository only | Behind IRegisterRepository abstraction |
| Legacy handling | Detect + fallback | No migration script, read-compatible |

## Files to Touch (by risk)

**High risk** (core serialization):
- `JsonTransactionSerializer.cs` — all encode/decode paths
- `PayloadManager.cs` — encryption flow, content-type awareness
- `ITransactionBuilderService.cs` — TX construction, signing
- `ValidationEngine.cs` — signature verify, schema validation

**Medium risk** (validator/register construction):
- `DocketBuildTriggerService.cs` — register model from validated TX
- `DocketSerializer.cs` — docket serialization
- `RegisterCreationOrchestrator.cs` — genesis TX
- `MongoRegisterRepository.cs` — BSON Binary internals

**Low risk** (models, read paths):
- `PayloadModel.cs` — add 2 properties
- `PayloadInfo.cs` — add 2 properties
- `Challenge.cs` — no structural change, encoding changes in serializer
- UI components — display only
