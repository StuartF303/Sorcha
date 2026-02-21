# Contract: PayloadModel Extensions

**Purpose**: New fields on PayloadModel and related models.

## PayloadModel (Register.Models)

New optional properties:

```
ContentType: string?
  - MIME type describing the plaintext data
  - Examples: "application/json", "application/pdf", "application/octet-stream"
  - Default inference when absent: "application/octet-stream"

ContentEncoding: string?
  - How the Data field is represented on the wire
  - Valid values: "identity", "base64url", "base64", "br+base64url", "gzip+base64url"
  - Default inference when absent: "base64" (legacy)
  - "base64" is read-only — never produced by new writes
```

## PayloadInfo (TransactionHandler)

New optional properties (mirrors PayloadModel):

```
ContentType: string?
ContentEncoding: string?
```

## JSON Serialization

New payload with identity encoding:
```json
{
  "type": 0,
  "data": { "field": "value" },
  "hash": "dGVzdC1oYXNo",
  "contentType": "application/json",
  "contentEncoding": "identity"
}
```

New payload with base64url encoding:
```json
{
  "type": 1,
  "data": "ZW5jcnlwdGVkLWRhdGE",
  "hash": "dGVzdC1oYXNo",
  "iv": { "data": "ZW5jcnlwdGVkLWtleQ", "address": "5Hq3wP8d..." },
  "challenges": [
    { "data": "cGVyLXdhbGxldC1rZXk", "address": "addr1..." }
  ],
  "contentType": "application/octet-stream",
  "contentEncoding": "base64url"
}
```

Legacy payload (no new fields):
```json
{
  "type": 0,
  "data": "eyJmaWVsZCI6InZhbHVlIn0=",
  "hash": "dGVzdC1oYXNo"
}
```

Compressed payload:
```json
{
  "type": 0,
  "data": "G0oAABheyJmaWVsZCI6InZhbHVlIn0D",
  "hash": "Y29tcHJlc3NlZC1oYXNo",
  "contentType": "application/json",
  "contentEncoding": "br+base64url"
}
```
