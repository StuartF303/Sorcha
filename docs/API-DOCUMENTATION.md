# Sorcha Platform - API Documentation

**Version:** 1.0.0
**Last Updated:** 2025-11-17
**Status:** Sprint 7 Complete

---

## Table of Contents

1. [Overview](#overview)
2. [Getting Started](#getting-started)
3. [Authentication](#authentication)
4. [Blueprint Service API](#blueprint-service-api)
5. [Wallet Service API](#wallet-service-api)
6. [Register Service API](#register-service-api)
7. [Action Workflow API](#action-workflow-api)
8. [Execution Helper API](#execution-helper-api)
9. [Real-time Notifications (SignalR)](#real-time-notifications-signalr)
10. [Error Handling](#error-handling)
11. [Rate Limiting](#rate-limiting)
12. [Code Examples](#code-examples)

---

## Overview

The Sorcha Platform provides a comprehensive REST API for building distributed ledger applications with blueprint-based workflows, secure wallet management, and transaction processing.

**Base URLs:**
- Development: `http://localhost:5000`
- Production: `https://api.sorcha.io` (when deployed)

**API Gateway:**
All services are accessed through the API Gateway which provides:
- Unified routing
- Health aggregation
- Load balancing
- Request logging

---

## Getting Started

### Prerequisites

- .NET 10 SDK
- Docker (optional, for containerized deployment)
- Redis (for caching and SignalR backplane)

### Quick Start

1. **Clone the repository:**
   ```bash
   git clone https://github.com/yourusername/Sorcha.git
   cd Sorcha
   ```

2. **Run with .NET Aspire:**
   ```bash
   dotnet run --project src/Apps/Sorcha.AppHost
   ```

3. **Access the API:**
   - API Gateway: http://localhost:5000
   - Swagger UI: http://localhost:5000/scalar/v1 (Blueprint Service)

### API Explorer

Visit the Scalar API documentation UI at `/scalar/v1` to explore all endpoints interactively.

---

## Authentication

### Current Status
⚠️ **MVP Phase:** Authentication is not yet enforced. All endpoints are publicly accessible for development.

### Future Implementation
Production will use:
- **JWT Bearer Tokens** for service-to-service communication
- **Azure AD / OAuth 2.0** for user authentication
- **API Keys** for external integrations

### Example (Future)
```http
GET /api/blueprints
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## Blueprint Service API

### Base Path: `/api/blueprints`

### Endpoints

#### 1. Get All Blueprints

```http
GET /api/blueprints
```

**Query Parameters:**
- `page` (integer): Page number (default: 1)
- `pageSize` (integer): Items per page (default: 20, max: 100)
- `search` (string): Search in title/description
- `status` (string): Filter by status

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "bp-123",
      "title": "Purchase Order Workflow",
      "description": "Multi-party purchase order process",
      "createdAt": "2025-11-17T10:30:00Z",
      "updatedAt": "2025-11-17T10:30:00Z",
      "participantCount": 2,
      "actionCount": 3
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 42,
  "totalPages": 3
}
```

#### 2. Get Blueprint by ID

```http
GET /api/blueprints/{id}
```

**Headers:**
- `Accept: application/ld+json` (optional, for JSON-LD format)

**Response:** `200 OK`
```json
{
  "id": "bp-123",
  "title": "Purchase Order Workflow",
  "description": "Multi-party purchase order process",
  "version": "1.0.0",
  "participants": [
    {
      "id": "buyer",
      "name": "Buyer Organization",
      "organisation": "ORG-001"
    },
    {
      "id": "seller",
      "name": "Seller Organization",
      "organisation": "ORG-002"
    }
  ],
  "actions": [
    {
      "id": "0",
      "title": "Submit Purchase Order",
      "description": "Buyer submits PO",
      "sender": "buyer",
      "data": {
        "$schema": "http://json-schema.org/draft-07/schema#",
        "type": "object",
        "properties": {
          "itemName": { "type": "string" },
          "quantity": { "type": "integer" },
          "unitPrice": { "type": "number" }
        },
        "required": ["itemName", "quantity", "unitPrice"]
      }
    }
  ]
}
```

#### 3. Create Blueprint

```http
POST /api/blueprints
```

**Request Body:**
```json
{
  "title": "Invoice Approval Workflow",
  "description": "Multi-step invoice approval",
  "version": "1.0.0",
  "participants": [
    {
      "id": "submitter",
      "name": "Invoice Submitter"
    },
    {
      "id": "approver",
      "name": "Finance Approver"
    }
  ],
  "actions": [
    {
      "id": "0",
      "title": "Submit Invoice",
      "sender": "submitter"
    }
  ]
}
```

**Response:** `201 Created`
```json
{
  "id": "bp-456",
  "title": "Invoice Approval Workflow",
  ...
}
```

#### 4. Publish Blueprint

```http
POST /api/blueprints/{id}/publish
```

**Response:** `200 OK`
```json
{
  "isSuccess": true,
  "publishedBlueprint": {
    "blueprintId": "bp-123",
    "version": 1,
    "publishedAt": "2025-11-17T11:00:00Z"
  },
  "errors": []
}
```

---

## Wallet Service API

### Base Path: `/api/wallets`

### Endpoints

#### 1. Create Wallet

```http
POST /api/wallets
```

**Request Body:**
```json
{
  "title": "My Secure Wallet",
  "description": "Personal wallet for transactions",
  "keyType": "ED25519"
}
```

**Key Types:**
- `ED25519`: EdDSA using Curve25519 (recommended)
- `NISTP256`: ECDSA using NIST P-256
- `RSA`: RSA-4096

**Response:** `201 Created`
```json
{
  "id": "wallet-789",
  "walletAddress": "0x1234567890abcdef",
  "title": "My Secure Wallet",
  "keyType": "ED25519",
  "createdAt": "2025-11-17T12:00:00Z"
}
```

#### 2. Sign Transaction

```http
POST /api/wallets/{id}/sign
```

**Request Body:**
```json
{
  "data": "SGVsbG8gV29ybGQ=",  // Base64-encoded data
  "algorithm": "ED25519"
}
```

**Response:** `200 OK`
```json
{
  "signature": "3045022100...",  // Base64-encoded signature
  "algorithm": "ED25519",
  "timestamp": "2025-11-17T12:05:00Z"
}
```

#### 3. Encrypt Payload

```http
POST /api/wallets/{id}/encrypt
```

**Request Body:**
```json
{
  "data": "Sensitive information to encrypt",
  "recipientWalletId": "wallet-999"
}
```

**Response:** `200 OK`
```json
{
  "encryptedData": "LS0tLS1CRUdJTi...",
  "recipientWalletId": "wallet-999",
  "algorithm": "AES-256-GCM"
}
```

#### 4. Decrypt Payload

```http
POST /api/wallets/{id}/decrypt
```

**Request Body:**
```json
{
  "encryptedData": "LS0tLS1CRUdJTi..."
}
```

**Response:** `200 OK`
```json
{
  "data": "Decrypted sensitive information",
  "timestamp": "2025-11-17T12:10:00Z"
}
```

---

## Register Service API

### Base Path: `/api/registers`

### Endpoints

#### 1. Create Register

```http
POST /api/registers
```

**Request Body:**
```json
{
  "title": "Production Register",
  "description": "Main production ledger"
}
```

**Response:** `201 Created`
```json
{
  "id": "register-101",
  "title": "Production Register",
  "createdAt": "2025-11-17T13:00:00Z"
}
```

#### 2. Submit Transaction

```http
POST /api/registers/{id}/transactions
```

**Request Body:**
```json
{
  "transactionType": "Action",
  "senderAddress": "wallet-789",
  "payload": "eyJkYXRhIjoid...",  // Base64-encoded
  "metadata": {
    "blueprintId": "bp-123",
    "actionId": "0"
  }
}
```

**Response:** `202 Accepted`
```json
{
  "transactionId": "tx-abc123",
  "status": "pending",
  "timestamp": "2025-11-17T13:05:00Z"
}
```

#### 3. Get Transaction

```http
GET /api/registers/{id}/transactions/{txId}
```

**Response:** `200 OK`
```json
{
  "transactionId": "tx-abc123",
  "transactionType": "Action",
  "senderAddress": "wallet-789",
  "timestamp": "2025-11-17T13:05:00Z",
  "docketId": "docket-001",
  "status": "confirmed"
}
```

#### 4. Query Transactions

```http
GET /api/registers/{id}/transactions
```

**Query Parameters:**
- `senderAddress` (string): Filter by sender
- `startTime` (ISO 8601): Start of time range
- `endTime` (ISO 8601): End of time range
- `page` (integer): Page number
- `pageSize` (integer): Items per page
- `$filter` (OData): OData V4 filter expression

**Example:**
```http
GET /api/registers/reg-101/transactions?senderAddress=wallet-789&startTime=2025-11-17T00:00:00Z
```

**Response:** `200 OK`
```json
{
  "items": [...],
  "page": 1,
  "totalCount": 150
}
```

#### 5. Seal Docket (Create Block)

```http
POST /api/registers/{id}/dockets/seal
```

**Response:** `201 Created`
```json
{
  "docketId": "docket-002",
  "previousHash": "0000abc123...",
  "merkleRoot": "def456...",
  "transactionCount": 25,
  "timestamp": "2025-11-17T14:00:00Z"
}
```

---

## Action Workflow API

### Base Path: `/api/actions`

### Endpoints

#### 1. Get Available Blueprints

```http
GET /api/actions/{walletAddress}/{registerAddress}/blueprints
```

**Response:** `200 OK`
```json
{
  "walletAddress": "wallet-789",
  "registerAddress": "register-101",
  "blueprints": [
    {
      "blueprintId": "bp-123",
      "title": "Purchase Order Workflow",
      "version": 1,
      "availableActions": [
        {
          "actionId": "0",
          "title": "Submit Purchase Order",
          "isAvailable": true
        }
      ]
    }
  ]
}
```

#### 2. Submit Action

```http
POST /api/actions
```

**Request Body:**
```json
{
  "blueprintId": "bp-123",
  "actionId": "0",
  "instanceId": "instance-abc",  // Optional, auto-generated if omitted
  "senderWallet": "wallet-789",
  "registerAddress": "register-101",
  "previousTransactionHash": null,  // For first action
  "payloadData": {
    "itemName": "Widget Pro",
    "quantity": 100,
    "unitPrice": 49.99
  },
  "files": [  // Optional file attachments
    {
      "fileName": "invoice.pdf",
      "contentType": "application/pdf",
      "contentBase64": "JVBERi0xLjQK..."
    }
  ]
}
```

**Response:** `200 OK`
```json
{
  "transactionHash": "0xabc123def456",
  "instanceId": "instance-abc",
  "serializedTransaction": "{...}",
  "fileTransactionHashes": ["0xfile001", "0xfile002"],
  "timestamp": "2025-11-17T15:00:00Z"
}
```

#### 3. Get Action Details

```http
GET /api/actions/{walletAddress}/{registerAddress}/{transactionHash}
```

**Response:** `200 OK`
```json
{
  "transactionHash": "0xabc123def456",
  "blueprintId": "bp-123",
  "actionId": "0",
  "instanceId": "instance-abc",
  "senderWallet": "wallet-789",
  "registerAddress": "register-101",
  "payloadData": {...},
  "timestamp": "2025-11-17T15:00:00Z"
}
```

---

## Execution Helper API

### Base Path: `/api/execution`

Client-side helpers for validating and processing actions before submission.

### Endpoints

#### 1. Validate Action Data

```http
POST /api/execution/validate
```

**Request Body:**
```json
{
  "blueprintId": "bp-123",
  "actionId": "0",
  "data": {
    "itemName": "Widget Pro",
    "quantity": 100,
    "unitPrice": 49.99
  }
}
```

**Response:** `200 OK`
```json
{
  "isValid": true,
  "errors": []
}
```

#### 2. Apply Calculations

```http
POST /api/execution/calculate
```

**Request Body:**
```json
{
  "blueprintId": "bp-123",
  "actionId": "0",
  "data": {
    "quantity": 100,
    "unitPrice": 49.99
  }
}
```

**Response:** `200 OK`
```json
{
  "processedData": {
    "quantity": 100,
    "unitPrice": 49.99,
    "totalPrice": 4999.00  // Calculated field
  },
  "calculatedFields": ["totalPrice"]
}
```

#### 3. Determine Routing

```http
POST /api/execution/route
```

**Request Body:**
```json
{
  "blueprintId": "bp-123",
  "actionId": "0",
  "data": {
    "amount": 75000
  }
}
```

**Response:** `200 OK`
```json
{
  "nextActionId": "2",
  "nextParticipantId": "director",
  "isWorkflowComplete": false,
  "matchedCondition": "amount > 50000"
}
```

#### 4. Apply Disclosure Rules

```http
POST /api/execution/disclose
```

**Request Body:**
```json
{
  "blueprintId": "bp-123",
  "actionId": "0",
  "data": {
    "itemName": "Widget Pro",
    "quantity": 100,
    "unitPrice": 49.99,
    "internalNotes": "Confidential"
  }
}
```

**Response:** `200 OK`
```json
{
  "disclosures": [
    {
      "participantId": "seller",
      "disclosedData": {
        "itemName": "Widget Pro",
        "quantity": 100,
        "unitPrice": 49.99
        // "internalNotes" not disclosed to seller
      },
      "fieldCount": 3
    }
  ]
}
```

---

## Real-time Notifications (SignalR)

### Hub Endpoint: `/actionshub`

### Events

#### 1. Subscribe to Actions

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5000/actionshub")
  .build();

// Subscribe to actions for specific wallet/register
await connection.invoke("SubscribeToActions", "wallet-789", "register-101");

// Listen for action confirmed events
connection.on("ActionConfirmed", (notification) => {
  console.log("Action confirmed:", notification);
});
```

#### 2. Notification Format

```javascript
{
  "transactionHash": "0xabc123def456",
  "walletAddress": "wallet-789",
  "registerAddress": "register-101",
  "blueprintId": "bp-123",
  "actionId": "0",
  "instanceId": "instance-abc",
  "timestamp": "2025-11-17T16:00:00Z",
  "message": "Transaction confirmed"
}
```

---

## Error Handling

### Standard Error Response

```json
{
  "error": "Invalid blueprint ID",
  "code": "BLUEPRINT_NOT_FOUND",
  "timestamp": "2025-11-17T17:00:00Z",
  "path": "/api/blueprints/invalid-id"
}
```

### Common Error Codes

| HTTP Status | Error Code | Description |
|------------|------------|-------------|
| 400 | `INVALID_REQUEST` | Malformed request body |
| 401 | `UNAUTHORIZED` | Authentication required |
| 403 | `FORBIDDEN` | Insufficient permissions |
| 404 | `NOT_FOUND` | Resource not found |
| 409 | `CONFLICT` | Resource conflict |
| 429 | `RATE_LIMITED` | Too many requests |
| 500 | `INTERNAL_ERROR` | Server error |
| 503 | `SERVICE_UNAVAILABLE` | Service temporarily unavailable |

---

## Rate Limiting

**Current Status:** Not enforced in MVP

**Future Implementation:**
- 100 requests per minute per client
- 1000 requests per hour per client
- Burst allowance: 20 requests

**Headers:**
```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1700000000
```

---

## Code Examples

### Complete Workflow Example (C#)

```csharp
using System.Net.Http.Json;

var client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

// 1. Create wallet
var walletResponse = await client.PostAsJsonAsync("/api/wallets", new
{
    title = "My Wallet",
    keyType = "ED25519"
});
var wallet = await walletResponse.Content.ReadFromJsonAsync<dynamic>();
var walletId = wallet.id;

// 2. Create register
var registerResponse = await client.PostAsJsonAsync("/api/registers", new
{
    title = "My Register"
});
var register = await registerResponse.Content.ReadFromJsonAsync<dynamic>();
var registerId = register.id;

// 3. Create and publish blueprint
var blueprint = new
{
    title = "Simple Workflow",
    participants = new[] { new { id = "p1", name = "Participant 1" } },
    actions = new[] { new { id = "0", title = "Action 1", sender = "p1" } }
};
var bpResponse = await client.PostAsJsonAsync("/api/blueprints", blueprint);
var bp = await bpResponse.Content.ReadFromJsonAsync<dynamic>();
await client.PostAsync($"/api/blueprints/{bp.id}/publish", null);

// 4. Submit action
var action = new
{
    blueprintId = bp.id,
    actionId = "0",
    senderWallet = walletId,
    registerAddress = registerId,
    payloadData = new { message = "Hello World" }
};
var actionResponse = await client.PostAsJsonAsync("/api/actions", action);
var result = await actionResponse.Content.ReadFromJsonAsync<dynamic>();

Console.WriteLine($"Transaction Hash: {result.transactionHash}");
```

### SignalR Real-time Notifications (JavaScript)

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5000/actionshub")
  .withAutomaticReconnect()
  .build();

// Handle disconnection
connection.onclose(async () => {
  console.log("Connection closed. Attempting to reconnect...");
});

// Subscribe to actions
await connection.start();
await connection.invoke("SubscribeToActions", "wallet-789", "register-101");

// Listen for notifications
connection.on("ActionConfirmed", (notification) => {
  console.log("Action confirmed:", notification);
  updateUI(notification);
});

connection.on("ActionPending", (notification) => {
  console.log("Action pending:", notification);
});
```

---

## API Versioning

**Current Version:** v1 (implicit)

**Future Versioning Strategy:**
- URL-based: `/api/v2/blueprints`
- Header-based: `X-API-Version: 2`

---

## Support and Resources

- **GitHub:** https://github.com/yourusername/Sorcha
- **Documentation:** https://docs.sorcha.io
- **API Explorer:** http://localhost:5000/scalar/v1

---

**Last Updated:** 2025-11-17
**Document Version:** 1.0.0
**Sprint:** 7
