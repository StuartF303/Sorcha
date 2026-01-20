# API Reference: Registers and Transactions UI

**Date**: 2026-01-20
**Feature**: 012-registers-transactions-ui

## Overview

This document references the existing Register Service APIs that the UI will consume. All endpoints are accessed via the API Gateway.

## Base URLs

| Environment | Base URL |
|-------------|----------|
| Docker/Production | `https://localhost/api/register` (via API Gateway) |
| Aspire Development | `https://localhost:7082/api/register` |
| Direct Service | `http://localhost:5290` |

## Authentication

All endpoints require JWT Bearer authentication except where noted.

```http
Authorization: Bearer <jwt_token>
```

## Registers API

### List Registers

```http
GET /api/registers?tenantId={tenantId}
```

**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| tenantId | string | No | Filter by organization |

**Response** (200 OK):
```json
[
  {
    "id": "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6",
    "name": "My Register",
    "height": 1234,
    "status": 1,
    "advertise": true,
    "isFullReplica": true,
    "tenantId": "org-123",
    "createdAt": "2026-01-15T10:30:00Z",
    "updatedAt": "2026-01-20T14:22:00Z"
  }
]
```

### Get Register

```http
GET /api/registers/{id}
```

**Response** (200 OK): Single register object (same schema as list item)

**Response** (404 Not Found): Register not found

### Create Register (Initiate)

```http
POST /api/registers/initiate
Content-Type: application/json
```

**Request Body**:
```json
{
  "name": "New Register",
  "tenantId": "org-123",
  "advertise": false,
  "isFullReplica": true
}
```

**Response** (200 OK):
```json
{
  "registerId": "a1b2c3d4...",
  "unsignedControlRecord": "base64-encoded-data",
  "expiresAt": "2026-01-20T15:30:00Z"
}
```

### Create Register (Finalize)

```http
POST /api/registers/finalize
Content-Type: application/json
```

**Request Body**:
```json
{
  "registerId": "a1b2c3d4...",
  "signedControlRecord": "base64-encoded-signed-data"
}
```

**Response** (201 Created): Created register object

## Transactions API

### List Transactions

```http
GET /api/registers/{registerId}/transactions?page={page}&pageSize={pageSize}
```

**Query Parameters**:
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| page | int | 1 | Page number (1-based) |
| pageSize | int | 20 | Items per page (max 100) |

**Response** (200 OK):
```json
{
  "page": 1,
  "pageSize": 20,
  "total": 1234,
  "transactions": [
    {
      "@context": "https://sorcha.dev/contexts/blockchain/v1.jsonld",
      "@type": "Transaction",
      "@id": "did:sorcha:register:abc123/tx/def456...",
      "registerId": "abc123...",
      "txId": "def456789...",
      "prevTxId": "abc123456...",
      "blockNumber": 42,
      "version": 1,
      "senderWallet": "5J3mBbAH...",
      "recipientsWallets": ["5K2nCcBI..."],
      "timeStamp": "2026-01-20T14:30:00Z",
      "metaData": {
        "blueprintId": "bp-123",
        "instanceId": "inst-456",
        "actionId": "action-789"
      },
      "payloadCount": 2,
      "signature": "base64-signature..."
    }
  ]
}
```

### Get Transaction

```http
GET /api/registers/{registerId}/transactions/{txId}
```

**Response** (200 OK): Single transaction object

**Response** (404 Not Found): Transaction not found

## SignalR Hub

### Connection

```
URL: /hubs/register
Transport: WebSocket (preferred), Server-Sent Events, Long Polling
```

### Server Methods (Client → Server)

```typescript
// Subscribe to register updates
hub.invoke("SubscribeToRegister", registerId: string): Promise<void>

// Unsubscribe from register updates
hub.invoke("UnsubscribeFromRegister", registerId: string): Promise<void>

// Subscribe to tenant-wide events
hub.invoke("SubscribeToTenant", tenantId: string): Promise<void>

// Unsubscribe from tenant events
hub.invoke("UnsubscribeFromTenant", tenantId: string): Promise<void>
```

### Client Methods (Server → Client)

```typescript
// New register created
hub.on("RegisterCreated", (registerId: string, name: string) => void)

// Register deleted
hub.on("RegisterDeleted", (registerId: string) => void)

// Transaction confirmed in register
hub.on("TransactionConfirmed", (registerId: string, transactionId: string) => void)

// New block sealed
hub.on("DocketSealed", (registerId: string, docketId: number, hash: string) => void)

// Register height updated
hub.on("RegisterHeightUpdated", (registerId: string, newHeight: number) => void)
```

## Error Responses

All endpoints may return:

| Status | Description |
|--------|-------------|
| 400 Bad Request | Invalid request parameters |
| 401 Unauthorized | Missing or invalid JWT |
| 403 Forbidden | Insufficient permissions |
| 404 Not Found | Resource not found |
| 500 Internal Server Error | Server error |

**Error Response Format**:
```json
{
  "error": "Error message",
  "details": "Additional context"
}
```

## API Gateway Routes

The API Gateway (YARP) routes requests as follows:

| Gateway Path | Backend Service |
|--------------|-----------------|
| `/api/register/*` | Register Service (port 5290) |
| `/hubs/register` | Register Service SignalR Hub |
