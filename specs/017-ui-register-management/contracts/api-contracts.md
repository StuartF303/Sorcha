# API Contracts: UI Register Management

**Feature**: 017-ui-register-management
**Date**: 2026-01-28
**Status**: Complete

## Overview

This document defines the API contracts used by the UI for register management. Most endpoints already exist; this documents their usage and any new endpoints needed.

## Base Configuration

- **Base URL**: API Gateway (`/api/`)
- **Authentication**: JWT Bearer token
- **Content-Type**: `application/json`

## Existing Endpoints

### Register List

```http
GET /api/registers
Authorization: Bearer {token}
```

**Response**: `200 OK`
```json
[
  {
    "id": "abc123def456...",
    "name": "My Register",
    "height": 150,
    "status": "Online",
    "advertise": true,
    "isFullReplica": true,
    "tenantId": "tenant-001",
    "createdAt": "2026-01-15T10:30:00Z",
    "updatedAt": "2026-01-28T14:22:00Z"
  }
]
```

**Used By**: Index.razor (US1)

### Register Detail

```http
GET /api/registers/{registerId}
Authorization: Bearer {token}
```

**Response**: `200 OK`
```json
{
  "id": "abc123def456...",
  "name": "My Register",
  "height": 150,
  "status": "Online",
  "advertise": true,
  "isFullReplica": true,
  "tenantId": "tenant-001",
  "createdAt": "2026-01-15T10:30:00Z",
  "updatedAt": "2026-01-28T14:22:00Z"
}
```

**Used By**: Detail.razor (US2)

### Register Creation - Initiate

```http
POST /api/registers/initiate
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "New Register",
  "tenantId": "tenant-001",
  "advertise": false,
  "isFullReplica": true
}
```

**Response**: `200 OK`
```json
{
  "registerId": "abc123def456...",
  "unsignedControlRecord": "eyJhbGciOiJFUzI1NiIs..."
}
```

**Used By**: CreateRegisterWizard.razor (US4)

### Register Creation - Finalize

```http
POST /api/registers/finalize
Authorization: Bearer {token}
Content-Type: application/json

{
  "registerId": "abc123def456...",
  "signedControlRecord": "eyJhbGciOiJFUzI1NiIs..."
}
```

**Response**: `200 OK`
```json
{
  "id": "abc123def456...",
  "name": "New Register",
  "height": 0,
  "status": "Online",
  "advertise": false,
  "isFullReplica": true,
  "tenantId": "tenant-001",
  "createdAt": "2026-01-28T15:00:00Z",
  "updatedAt": "2026-01-28T15:00:00Z"
}
```

**Used By**: CreateRegisterWizard.razor (US4)

### Transaction List (Paginated)

```http
GET /api/registers/{registerId}/transactions?page=1&pageSize=20
Authorization: Bearer {token}
```

**Response**: `200 OK`
```json
{
  "page": 1,
  "pageSize": 20,
  "total": 150,
  "transactions": [
    {
      "txId": "0x1234567890abcdef...",
      "registerId": "abc123def456...",
      "senderWallet": "srch1abc123...",
      "recipientsWallets": ["srch1xyz789..."],
      "timeStamp": "2026-01-28T14:00:00Z",
      "blockNumber": 100,
      "payloadCount": 1,
      "signature": "MEUCIQDKxz...",
      "prevTxId": "0xprevious...",
      "version": 1,
      "blueprintId": null,
      "instanceId": null,
      "actionId": null
    }
  ]
}
```

**Used By**: TransactionList.razor (US2)

### Transaction Detail

```http
GET /api/registers/{registerId}/transactions/{txId}
Authorization: Bearer {token}
```

**Response**: `200 OK`
```json
{
  "txId": "0x1234567890abcdef...",
  "registerId": "abc123def456...",
  "senderWallet": "srch1abc123...",
  "recipientsWallets": ["srch1xyz789..."],
  "timeStamp": "2026-01-28T14:00:00Z",
  "blockNumber": 100,
  "payloadCount": 1,
  "signature": "MEUCIQDKxz...",
  "prevTxId": "0xprevious...",
  "version": 1,
  "blueprintId": "bp-001",
  "instanceId": "inst-001",
  "actionId": 5
}
```

**Used By**: TransactionDetail.razor (US3)

## New/Enhanced Endpoints

### Wallet List (for Register Creation)

```http
GET /api/wallets
Authorization: Bearer {token}
```

**Response**: `200 OK`
```json
[
  {
    "id": "wallet-001",
    "address": "srch1abc123def456...",
    "name": "Main Signing Wallet",
    "algorithm": "ED25519",
    "canSign": true
  },
  {
    "id": "wallet-002",
    "address": "srch1xyz789...",
    "name": "Secondary Wallet",
    "algorithm": "P-256",
    "canSign": true
  }
]
```

**Used By**: CreateRegisterWizard.razor Step 2 (US4)

**Notes**: This endpoint already exists in Wallet Service. UI needs to add a service client for it.

### Wallet Sign (for Register Creation)

```http
POST /api/wallets/{walletId}/sign
Authorization: Bearer {token}
Content-Type: application/json

{
  "data": "base64-encoded-control-record",
  "isPreHashed": true
}
```

**Response**: `200 OK`
```json
{
  "signature": "MEUCIQDKxz...",
  "signedData": "base64-encoded-signed-record"
}
```

**Used By**: CreateRegisterWizard.razor (US4)

**Notes**: Called between initiate and finalize to sign the control record with the selected wallet.

### Transaction Query by Wallet (Cross-Register)

```http
GET /api/transactions/query?wallet={walletAddress}&page=1&pageSize=20
Authorization: Bearer {token}
```

**Response**: `200 OK`
```json
{
  "page": 1,
  "pageSize": 20,
  "total": 45,
  "results": [
    {
      "transaction": {
        "txId": "0x1234567890abcdef...",
        "registerId": "abc123...",
        "senderWallet": "srch1abc123...",
        "timeStamp": "2026-01-28T14:00:00Z",
        "blockNumber": 100
      },
      "registerName": "Sales Register",
      "registerId": "abc123..."
    }
  ]
}
```

**Used By**: Query.razor, TransactionQueryForm.razor (US6)

**Notes**: This endpoint may need to be added to Register Service. If not available, the UI can fall back to querying each register individually (less efficient but functional).

## SignalR Hub Contract

### Hub URL

```
/hubs/register
```

### Client → Server Methods

| Method | Parameters | Description |
|--------|------------|-------------|
| `SubscribeToRegister` | `string registerId` | Subscribe to register updates |
| `UnsubscribeFromRegister` | `string registerId` | Unsubscribe from register updates |
| `SubscribeToTenant` | `string tenantId` | Subscribe to tenant-wide updates |
| `UnsubscribeFromTenant` | `string tenantId` | Unsubscribe from tenant updates |

### Server → Client Events

| Event | Parameters | Description |
|-------|------------|-------------|
| `TransactionConfirmed` | `string registerId, string txId` | New transaction confirmed |
| `RegisterCreated` | `string registerId, string name` | New register created |
| `RegisterDeleted` | `string registerId` | Register deleted |
| `DocketSealed` | `string registerId, ulong docketId, string hash` | Docket sealed |
| `RegisterHeightUpdated` | `string registerId, uint height` | Register height changed |

## Error Responses

### 400 Bad Request

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "errors": {
    "name": ["Register name must be between 1 and 38 characters"]
  }
}
```

### 401 Unauthorized

```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Authentication required"
}
```

### 403 Forbidden

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "You do not have permission to access this register"
}
```

### 404 Not Found

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Register not found"
}
```

## UI Service Interface Changes

### ITransactionService Enhancement

```csharp
/// <summary>
/// Query transactions across registers by wallet address.
/// </summary>
/// <param name="walletAddress">Wallet address to search for</param>
/// <param name="page">Page number (1-based)</param>
/// <param name="pageSize">Items per page</param>
/// <returns>Paginated query results with register context</returns>
Task<TransactionQueryResponse> QueryByWalletAsync(
    string walletAddress,
    int page = 1,
    int pageSize = 20);
```

### New IWalletService Interface

```csharp
/// <summary>
/// UI service client for wallet operations.
/// </summary>
public interface IWalletService
{
    /// <summary>
    /// Get all wallets for the current user.
    /// </summary>
    Task<IReadOnlyList<WalletViewModel>> GetWalletsAsync();

    /// <summary>
    /// Sign data with the specified wallet.
    /// </summary>
    /// <param name="walletId">Wallet ID</param>
    /// <param name="data">Data to sign (base64 encoded)</param>
    /// <param name="isPreHashed">Whether data is already hashed</param>
    /// <returns>Signed data</returns>
    Task<SignatureResponse> SignAsync(string walletId, string data, bool isPreHashed = false);
}
```
