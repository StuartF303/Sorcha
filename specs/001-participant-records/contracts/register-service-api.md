# API Contract: Register Service — Participant Query Endpoints

**Branch**: `001-participant-records` | **Date**: 2026-02-20

## GET /api/registers/{registerId}/participants

List all published participants on a register. Returns the latest version of each participant.

**Authorization**: `CanReadTransactions`

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| registerId | string | Yes | Register to query |

### Query Parameters (OData)

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| $skip | int | No | 0 | Number of records to skip |
| $top | int | No | 20 | Number of records to return (max 100) |
| $count | bool | No | false | Include total count in response |
| status | string | No | "active" | Filter: "active", "deprecated", "revoked", or "all" |

### Response: 200 OK

```json
{
  "page": 1,
  "pageSize": 20,
  "total": 3,
  "participants": [
    {
      "participantId": "550e8400-e29b-41d4-a716-446655440000",
      "organizationName": "Council Building Department",
      "participantName": "Planning Review Desk",
      "status": "active",
      "version": 2,
      "latestTxId": "d4e5f6...",
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
      "metadata": {
        "description": "Shared intake for planning review requests"
      },
      "publishedAt": "2026-02-20T14:30:00Z"
    }
  ]
}
```

---

## GET /api/registers/{registerId}/participants/by-address/{walletAddress}

Look up a published participant by wallet address. Returns the latest version of the participant record containing that address.

**Authorization**: `CanReadTransactions`

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| registerId | string | Yes | Register to query |
| walletAddress | string | Yes | Any wallet address from the participant's addresses array |

### Response: 200 OK

```json
{
  "participantId": "550e8400-e29b-41d4-a716-446655440000",
  "organizationName": "Council Building Department",
  "participantName": "Planning Review Desk",
  "status": "active",
  "version": 2,
  "latestTxId": "d4e5f6...",
  "addresses": [
    {
      "walletAddress": "sra1q...",
      "publicKey": "base64...",
      "algorithm": "ED25519",
      "primary": true
    }
  ],
  "metadata": {},
  "publishedAt": "2026-02-20T14:30:00Z"
}
```

### Response: 404 Not Found

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Participant not found",
  "status": 404,
  "detail": "No active participant found with address 'sra1z...' on register 'register-001'"
}
```

---

## GET /api/registers/{registerId}/participants/{participantId}

Get a specific published participant by ID. Returns the latest version.

**Authorization**: `CanReadTransactions`

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| registerId | string | Yes | Register to query |
| participantId | UUID | Yes | Participant ID |

### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| includeHistory | bool | No | false | Include all versions (not just latest) |

### Response: 200 OK

Same as by-address response. When `includeHistory=true`:

```json
{
  "participantId": "550e8400-e29b-41d4-a716-446655440000",
  "organizationName": "Council Building Department",
  "participantName": "Planning Review Desk",
  "status": "active",
  "version": 2,
  "latestTxId": "d4e5f6...",
  "addresses": [...],
  "metadata": {},
  "publishedAt": "2026-02-20T14:30:00Z",
  "history": [
    {
      "version": 1,
      "txId": "a1b2c3...",
      "status": "active",
      "participantName": "Planning Desk",
      "timestamp": "2026-02-20T10:00:00Z"
    },
    {
      "version": 2,
      "txId": "d4e5f6...",
      "status": "active",
      "participantName": "Planning Review Desk",
      "timestamp": "2026-02-20T14:30:00Z"
    }
  ]
}
```

---

## GET /api/registers/{registerId}/participants/by-address/{walletAddress}/public-key

Resolve a participant's public key by wallet address and optional algorithm preference.

**Authorization**: `CanReadTransactions`

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| registerId | string | Yes | Register to query |
| walletAddress | string | Yes | Wallet address to resolve |

### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| algorithm | string | No | (primary) | Preferred algorithm: ED25519, P-256, RSA-4096 |

### Response: 200 OK

```json
{
  "participantId": "550e8400-e29b-41d4-a716-446655440000",
  "participantName": "Planning Review Desk",
  "walletAddress": "sra1q...",
  "publicKey": "base64...",
  "algorithm": "ED25519",
  "status": "active"
}
```

### Response: 410 Gone

Returned when participant is revoked:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.11",
  "title": "Participant revoked",
  "status": 410,
  "detail": "Participant 'Planning Review Desk' has been revoked"
}
```
