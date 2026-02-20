# API Contract: Tenant Service — Participant Publishing

**Branch**: `001-participant-records` | **Date**: 2026-02-20

## POST /api/organizations/{organizationId}/participants/publish

Publish a participant record to a register. Builds the transaction, signs with the requesting user's wallet, and submits via the validator pipeline.

**Authorization**: `RequireAdministrator` (org admin or designated role)

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| organizationId | GUID | Yes | Organization publishing the participant |

### Request Body

```json
{
  "registerId": "register-001",
  "participantName": "Planning Review Desk",
  "organizationName": "Council Building Department",
  "addresses": [
    {
      "walletAddress": "sra1q...",
      "publicKey": "base64...",
      "algorithm": "ED25519",
      "primary": true
    },
    {
      "walletAddress": "sra1p...",
      "publicKey": "base64...",
      "algorithm": "P-256",
      "primary": false
    }
  ],
  "signerWalletAddress": "sra1x...",
  "metadata": {
    "description": "Shared intake for planning review requests",
    "links": { "documentation": "https://..." }
  }
}
```

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| registerId | string | Yes | Must be a valid register |
| participantName | string | Yes | 1-200 chars |
| organizationName | string | Yes | 1-200 chars |
| addresses | array | Yes | 1-10 entries, each with walletAddress + publicKey + algorithm |
| signerWalletAddress | string | Yes | Must be a linked wallet of the authenticated user |
| metadata | object | No | Opaque JSON, max 10KB |

### Response: 202 Accepted

```json
{
  "transactionId": "a1b2c3...",
  "participantId": "550e8400-e29b-41d4-a716-446655440000",
  "registerId": "register-001",
  "version": 1,
  "status": "submitted",
  "message": "Participant record submitted to validation pipeline"
}
```

### Response: 400 Bad Request

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation failed",
  "status": 400,
  "errors": {
    "addresses": ["At least one address is required"],
    "signerWalletAddress": ["Wallet not linked to authenticated user"]
  }
}
```

### Response: 403 Forbidden

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Not authorized",
  "status": 403,
  "detail": "User does not have participant publishing rights in this organization"
}
```

### Response: 409 Conflict

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Address conflict",
  "status": 409,
  "detail": "Wallet address 'sra1q...' is already claimed by another participant on register 'register-001'"
}
```

---

## PUT /api/organizations/{organizationId}/participants/publish/{participantId}

Update an existing published participant record (new version).

**Authorization**: `RequireAdministrator`

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| organizationId | GUID | Yes | Organization owning the participant |
| participantId | UUID | Yes | Participant ID (from initial publication) |

### Request Body

Same as POST, plus optional:

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| registerId | string | Yes | Must match the register of the original publication |
| status | string | No | Default: "active". Set to "deprecated" or "revoked" for lifecycle changes |

### Response: 202 Accepted

```json
{
  "transactionId": "d4e5f6...",
  "participantId": "550e8400-e29b-41d4-a716-446655440000",
  "registerId": "register-001",
  "version": 2,
  "previousVersion": 1,
  "previousTxId": "a1b2c3...",
  "status": "submitted",
  "message": "Participant record update submitted to validation pipeline"
}
```

---

## DELETE /api/organizations/{organizationId}/participants/publish/{participantId}

Revoke a published participant record (publishes a new version with status "revoked").

**Authorization**: `RequireAdministrator`

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| organizationId | GUID | Yes | Organization owning the participant |
| participantId | UUID | Yes | Participant ID to revoke |

### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| registerId | string | Yes | — | Register containing the participant record |
| signerWalletAddress | string | Yes | — | Wallet for signing the revocation TX |

### Response: 202 Accepted

```json
{
  "transactionId": "g7h8i9...",
  "participantId": "550e8400-e29b-41d4-a716-446655440000",
  "registerId": "register-001",
  "version": 3,
  "status": "submitted",
  "message": "Participant revocation submitted to validation pipeline"
}
```
