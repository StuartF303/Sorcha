# Contract: Encoding Helper

**Purpose**: Centralized binary-to-text encoding/decoding with format detection.

## Interface: IPayloadEncodingService

```
EncodeToString(bytes, contentEncoding) → string
  - "base64url" → Base64Url.EncodeToString(bytes)
  - "base64" → ERROR (never produce legacy)
  - "identity" → UTF8.GetString(bytes) (only for JSON)
  - "br+base64url" → Brotli compress → Base64Url encode
  - "gzip+base64url" → Gzip compress → Base64Url encode

DecodeToBytes(encoded, contentEncoding) → byte[]
  - "base64url" → Base64Url.DecodeFromChars(encoded)
  - "base64" → Convert.FromBase64String(encoded) (legacy read)
  - "identity" → UTF8.GetBytes(encoded)
  - "br+base64url" → Base64Url decode → Brotli decompress
  - "gzip+base64url" → Base64Url decode → Gzip decompress
  - null → Convert.FromBase64String(encoded) (legacy fallback)

DetectLegacyEncoding(encoded) → bool
  - Returns true if string contains '+' or '/' (standard Base64 chars not in Base64url)

ResolveContentEncoding(contentType, dataSize, isEncrypted) → string
  - JSON + unencrypted + < threshold → "identity"
  - >= threshold → "br+base64url"
  - binary or encrypted → "base64url"
  - binary or encrypted + >= threshold → "br+base64url"
```

## Configuration

```
CompressionThresholdBytes: int (default: 4096)
DefaultCompressionAlgorithm: string (default: "br")
```
