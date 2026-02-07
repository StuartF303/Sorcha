# Data Model: Transaction Query API

**Feature**: 021-transaction-query-api
**Date**: 2026-02-06

## Entities

### TransactionModel (existing — no changes)

The `TransactionModel` already contains the `PrevTxId` field used for chain linkage. No modifications to the data model are required.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| TxId | string (64 hex chars) | Yes | Unique transaction identifier (hash) |
| PrevTxId | string (64 hex chars) | No | Previous transaction ID for chain linkage. Empty for genesis transactions |
| RegisterId | string | Yes | Register this transaction belongs to |
| BlockNumber | ulong? | No | Docket number if transaction has been sealed |
| SenderWallet | string | Yes | Sender wallet address |
| RecipientsWallets | IEnumerable\<string\> | No | Recipient wallet addresses |
| TimeStamp | DateTime | Yes | UTC timestamp of creation |
| MetaData | TransactionMetaData? | No | Blueprint/workflow metadata |
| Payloads | PayloadModel[] | No | Transaction payload data |
| Signature | string | Yes | Cryptographic signature |

### PaginatedResult\<T\> (existing — no changes)

Used by `QueryManager` for internal pagination.

| Field | Type | Description |
|-------|------|-------------|
| Items | List\<T\> | Page of results |
| Page | int | Current page (1-based) |
| PageSize | int | Items per page |
| TotalCount | int | Total matching items |
| TotalPages | int | Calculated total pages |
| HasPreviousPage | bool | Page > 1 |
| HasNextPage | bool | Page < TotalPages |

### TransactionPage (existing — no changes)

Used by `IRegisterServiceClient` for external API pagination.

| Field | Type | Description |
|-------|------|-------------|
| Transactions | List\<TransactionModel\> | Page of transactions |
| Page | int | Current page (1-based) |
| PageSize | int | Items per page |
| Total | int | Total matching transactions |
| TotalPages | int | Calculated (ceiling of Total/PageSize) |

## Relationships

```
Register (1) ──────── (*) Transaction
                           │
                           │ PrevTxId references TxId
                           │
Transaction (1) ◄──── (*) Transaction (successors)
                           │
                           │ When count > 1: FORK
```

- A Register contains many Transactions
- A Transaction optionally references one predecessor via `PrevTxId → TxId`
- A Transaction may have zero or more successors (other transactions that reference it as their `PrevTxId`)
- When a Transaction has more than one successor: this is a **Fork**
- Genesis transactions have empty `PrevTxId`

## Storage Index

New index to be added to MongoDB `transactions` collection (per-register database):

| Index | Fields | Type | Purpose |
|-------|--------|------|---------|
| PrevTxId_1 | PrevTxId (ascending) | Non-unique | Efficient lookup of successors by predecessor ID |

This joins the existing indexes:
- TxId (unique ascending)
- SenderWallet (ascending)
- TimeStamp (descending)
- BlockNumber (ascending)
- MetaData.BlueprintId + MetaData.InstanceId (compound ascending)

## Validation Rules

| Rule | Scope | Description |
|------|-------|-------------|
| RegisterId required | Query input | Must be non-null and non-whitespace |
| PrevTxId format | Query input | If provided, must be exactly 64 hex characters; null/empty returns empty result |
| Page bounds | Query input | Clamped to minimum 1 |
| PageSize bounds | Query input | Clamped to range 1-100, default 20 |
