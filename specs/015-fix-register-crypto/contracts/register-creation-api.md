# API Contract: Register Creation (Two-Phase)

**Service:** Register Service
**Base Path:** `/api/registers`
**Auth:** Anonymous (AllowAnonymous)

---

## POST /api/registers/initiate

Initiates a new register creation, generating attestation data and SHA-256 hashes for signing.

### Request

```json
{
  "name": "string (1-38 chars, required)",
  "description": "string (0-500 chars, optional)",
  "tenantId": "string (required)",
  "owners": [
    {
      "userId": "string (required)",
      "walletId": "string (required)"
    }
  ],
  "additionalAdmins": [
    {
      "userId": "string (required)",
      "walletId": "string (required)",
      "role": "Admin | Auditor (default: Admin)"
    }
  ],
  "metadata": { "key": "value (optional)" }
}
```

### Response (200 OK)

```json
{
  "registerId": "string (32-char hex GUID)",
  "attestationsToSign": [
    {
      "userId": "string",
      "walletId": "string",
      "role": "Owner | Admin | Auditor",
      "attestationData": {
        "role": "Owner",
        "subject": "did:sorcha:{userId}",
        "registerId": "string",
        "registerName": "string",
        "grantedAt": "2026-01-27T00:00:00Z"
      },
      "dataToSign": "string (hex-encoded SHA-256 hash of canonical JSON attestation data)"
    }
  ],
  "expiresAt": "2026-01-27T00:05:00Z",
  "nonce": "string (Base64-encoded 32 random bytes)"
}
```

### Errors

| Status | Condition |
|--------|-----------|
| 400 | Invalid request (missing name, no owners, name too long) |

---

## POST /api/registers/finalize

Verifies signed attestations, submits genesis transaction, and creates the register atomically.

### Request

```json
{
  "registerId": "string (32-char hex, required)",
  "nonce": "string (required, must match initiate nonce)",
  "signedAttestations": [
    {
      "attestationData": {
        "role": "Owner",
        "subject": "did:sorcha:{userId}",
        "registerId": "string",
        "registerName": "string",
        "grantedAt": "2026-01-27T00:00:00Z"
      },
      "publicKey": "string (Base64-encoded public key)",
      "signature": "string (Base64-encoded signature)",
      "algorithm": "ED25519 | NISTP256 | RSA4096"
    }
  ]
}
```

### Response (201 Created)

```json
{
  "registerId": "string",
  "status": "created",
  "genesisTransactionId": "genesis-{registerId}",
  "genesisDocketId": "0",
  "createdAt": "2026-01-27T00:00:00Z"
}
```

### Errors

| Status | Condition |
|--------|-----------|
| 400 | Invalid request, missing fields, validation failure |
| 401 | Signature verification failed (invalid signature, wrong key) |
| 404 | Pending registration not found (already finalized or never initiated) |
| 408 | Pending registration expired (>5 minutes since initiate) |
| 502 | Genesis transaction submission to Validator failed |
| 503 | Validator Service or Wallet Service unreachable |

### Atomicity Guarantee

The register is NOT persisted to the database until the genesis transaction is successfully submitted to the Validator Service. If genesis submission fails, the register does not exist and the caller receives an error. The caller may retry the full initiate/finalize flow.

---

## Removed Endpoint

### ~~POST /api/registers/~~ (REMOVED)

Previously created registers without cryptographic attestations or genesis transactions. Removed per FR-008. All register creation must use the two-phase initiate/finalize flow.

**Retained endpoints** (under `/api/registers` with `CanManageRegisters` auth):
- `GET /api/registers/` - List registers
- `GET /api/registers/{id}` - Get register by ID
- `PUT /api/registers/{id}` - Update register
- `DELETE /api/registers/{id}` - Delete register
- `GET /api/registers/stats/count` - Register count
