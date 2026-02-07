# API Contract: Transaction Query by Previous Transaction ID

**Feature**: 021-transaction-query-api
**Date**: 2026-02-06

## Endpoint

### GET /api/query/previous/{prevTxId}/transactions

Query all transactions within a register that reference a given previous transaction ID.

**Group**: `/api/query` (Query)
**Authorization**: `CanReadTransactions`
**Tags**: Query

### Parameters

| Parameter | Location | Type | Required | Default | Description |
|-----------|----------|------|----------|---------|-------------|
| prevTxId | path | string (64 hex) | Yes | — | The previous transaction ID to search for |
| registerId | query | string | Yes | — | The register to search within |
| page | query | int | No | 1 | Page number (1-based, min: 1) |
| pageSize | query | int | No | 20 | Items per page (1-100) |

### Responses

#### 200 OK — Paginated transaction results

```json
{
  "items": [
    {
      "txId": "a1b2c3...64chars",
      "prevTxId": "d4e5f6...64chars",
      "registerId": "register-001",
      "senderWallet": "0x1234...",
      "recipientsWallets": ["0x5678..."],
      "timeStamp": "2026-02-06T12:00:00Z",
      "blockNumber": 42,
      "version": 1,
      "metaData": { "blueprintId": "bp-001", "instanceId": "inst-001" },
      "payloadCount": 1,
      "payloads": [],
      "signature": "abc123..."
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 2,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

#### 400 Bad Request — Missing or invalid parameters

```json
{
  "error": "registerId is required"
}
```

### Examples

**Normal query (no fork)**:
```
GET /api/query/previous/a1b2c3d4.../transactions?registerId=reg-001&page=1&pageSize=20
→ 200 OK, totalCount: 1 (single successor = healthy chain)
```

**Fork detected**:
```
GET /api/query/previous/a1b2c3d4.../transactions?registerId=reg-001&page=1&pageSize=20
→ 200 OK, totalCount: 2 (multiple successors = fork)
```

**No successors**:
```
GET /api/query/previous/a1b2c3d4.../transactions?registerId=reg-001&page=1&pageSize=20
→ 200 OK, totalCount: 0 (chain tip or unreferenced transaction)
```

---

## Service Client Interface

### IRegisterServiceClient

```
GetTransactionsByPrevTxIdAsync(
    registerId: string,
    prevTxId: string,
    page: int = 1,
    pageSize: int = 20,
    cancellationToken: CancellationToken = default
) → TransactionPage
```

Maps to: `GET /api/query/previous/{prevTxId}/transactions?registerId={registerId}&page={page}&pageSize={pageSize}`

---

## Repository Interface

### IRegisterRepository

```
GetTransactionsByPrevTxIdAsync(
    registerId: string,
    prevTxId: string,
    cancellationToken: CancellationToken = default
) → IEnumerable<TransactionModel>
```

Returns all matching transactions (unpaginated). Pagination is applied by `QueryManager`.

---

## Business Logic

### QueryManager

```
GetTransactionsByPrevTxIdPaginatedAsync(
    registerId: string,
    prevTxId: string,
    page: int = 1,
    pageSize: int = 20,
    cancellationToken: CancellationToken = default
) → PaginatedResult<TransactionModel>
```

Delegates to `IRegisterRepository.GetTransactionsByPrevTxIdAsync`, applies ordering by TimeStamp descending, then Skip/Take pagination.
