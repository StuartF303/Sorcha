# CLI Command Contracts

**Branch**: `016-cli-register-update` | **Date**: 2026-01-28

## Modified Commands

### `sorcha register create` (BREAKING CHANGE)

**Old signature**: `--name --org-id [--description]`
**New signature**: `--name --tenant-id --owner-wallet [--description] [--metadata key=value]`

```
sorcha register create --name "My Register" --tenant-id <tenantId> --owner-wallet <walletAddress> [--description "..."] [--metadata key1=val1 --metadata key2=val2]
```

**Flow**: Initiate → Sign attestation via wallet → Finalize
**Output**: Register ID, genesis transaction ID, genesis docket ID

### `sorcha register list` (ENHANCED)

**New fields displayed**: Height, Status, TenantId, Advertise, CreatedAt, UpdatedAt

### `sorcha register get` (ENHANCED)

**New fields displayed**: Height, Status, TenantId, Advertise, IsFullReplica, Votes, CreatedAt, UpdatedAt

### `sorcha tx list` (BREAKING CHANGE - pagination)

**Old**: `--skip --take`
**New**: `--page --page-size`

## New Register Subcommands

### `sorcha register update`

```
sorcha register update --id <registerId> [--name "New Name"] [--status Online|Offline|Archived] [--advertise true|false]
```

**Refit**: `PUT /api/registers/{id}`

### `sorcha register stats`

```
sorcha register stats
```

**Refit**: `GET /api/registers/stats/count`
**Output**: Total register count

## New Docket Command Group

### `sorcha docket list`

```
sorcha docket list --register-id <registerId>
```

**Refit**: `GET /api/registers/{registerId}/dockets`
**Output**: Table of dockets (ID, Hash, State, Transaction Count, Timestamp)

### `sorcha docket get`

```
sorcha docket get --register-id <registerId> --docket-id <docketId>
```

**Refit**: `GET /api/registers/{registerId}/dockets/{docketId}`
**Output**: Full docket details

### `sorcha docket transactions`

```
sorcha docket transactions --register-id <registerId> --docket-id <docketId>
```

**Refit**: `GET /api/registers/{registerId}/dockets/{docketId}/transactions`
**Output**: Transaction list within docket

## New Query Command Group

### `sorcha query wallet`

```
sorcha query wallet --address <walletAddress> [--page 1] [--page-size 50]
```

**Refit**: `GET /api/query/wallets/{address}/transactions?page={page}&pageSize={pageSize}`

### `sorcha query sender`

```
sorcha query sender --address <senderAddress> [--page 1] [--page-size 50]
```

**Refit**: `GET /api/query/senders/{address}/transactions?page={page}&pageSize={pageSize}`

### `sorcha query blueprint`

```
sorcha query blueprint --id <blueprintId> [--page 1] [--page-size 50]
```

**Refit**: `GET /api/query/blueprints/{blueprintId}/transactions?page={page}&pageSize={pageSize}`

### `sorcha query stats`

```
sorcha query stats
```

**Refit**: `GET /api/query/stats`
**Output**: Aggregate transaction statistics

### `sorcha query odata`

```
sorcha query odata --resource Transactions|Registers|Dockets [--filter "..."] [--orderby "..."] [--top N] [--skip N] [--select "field1,field2"] [--count]
```

**Refit**: `GET /odata/{resource}?$filter={filter}&$orderby={orderby}&$top={top}&$skip={skip}&$select={select}&$count={count}`
**Output**: Query results in table or JSON format

## Refit Client Interface Extensions

### IRegisterServiceClient - New Methods

```
// Register management
PUT  /api/registers/{id}                    → UpdateRegisterAsync
GET  /api/registers/stats/count             → GetRegisterStatsAsync

// Two-phase creation
POST /api/registers/initiate                → InitiateRegisterCreationAsync
POST /api/registers/finalize                → FinalizeRegisterCreationAsync

// Dockets
GET  /api/registers/{regId}/dockets                           → ListDocketsAsync
GET  /api/registers/{regId}/dockets/{docketId}                → GetDocketAsync
GET  /api/registers/{regId}/dockets/{docketId}/transactions   → GetDocketTransactionsAsync

// Query API
GET  /api/query/wallets/{address}/transactions    → QueryByWalletAsync
GET  /api/query/senders/{address}/transactions    → QueryBySenderAsync
GET  /api/query/blueprints/{id}/transactions      → QueryByBlueprintAsync
GET  /api/query/stats                             → GetQueryStatsAsync

// OData
GET  /odata/{resource}                            → QueryODataAsync (raw HttpResponseMessage)
```

### IWalletServiceClient - SignTransactionRequest Update

Add `IsPreHashed` and `DerivationPath` fields to CLI's `SignTransactionRequest` model.
