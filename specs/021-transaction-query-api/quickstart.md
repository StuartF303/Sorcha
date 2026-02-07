# Quickstart: Transaction Query API

**Feature**: 021-transaction-query-api
**Date**: 2026-02-06

## Overview

This feature adds a query-by-PreviousTransactionId capability across the Register Service stack, enabling fork detection in the ValidationEngine.

## Implementation Order

Follow this bottom-up order to ensure each layer compiles and tests pass incrementally:

### Phase 1: Storage Layer
1. Add `PrevTxId` index to `MongoRegisterRepository.CreateTransactionIndexesAsync()`
2. Add `GetTransactionsByPrevTxIdAsync` to `IRegisterRepository`
3. Implement in `MongoRegisterRepository` (using `Filter.Eq`)
4. Implement in `InMemoryRegisterRepository` (using LINQ `.Where`)

### Phase 2: Business Logic
5. Add `GetTransactionsByPrevTxIdPaginatedAsync` to `QueryManager`
6. Follow pagination pattern from `GetTransactionsByWalletPaginatedAsync`

### Phase 3: REST Endpoint
7. Add `GET /api/query/previous/{prevTxId}/transactions` to Register Service Program.cs
8. Add to `/api/query` group with `CanReadTransactions` authorization

### Phase 4: Service Client
9. Add `GetTransactionsByPrevTxIdAsync` to `IRegisterServiceClient`
10. Implement HTTP call in `RegisterServiceClient`

### Phase 5: Fork Detection Integration
11. Update `ValidationEngine.ValidateChainAsync` to call the new method
12. Add fork detection error code (VAL_CHAIN_FORK)

### Phase 6: Tests
13. Unit tests for QueryManager, repository, service client
14. Integration tests for MongoDB index and query
15. ValidationEngine fork detection tests

## Key Patterns to Follow

**Repository query** (follow `GetAllTransactionsBySenderAddressAsync`):
```
Filter.Eq(t => t.PrevTxId, prevTxId) → Find → SortByDescending(TimeStamp) → ToListAsync
```

**QueryManager pagination** (follow `GetTransactionsByWalletPaginatedAsync`):
```
repository.GetTransactionsByPrevTxIdAsync → OrderByDescending → Skip/Take → PaginatedResult
```

**Endpoint** (follow `/api/query/wallets/{address}/transactions`):
```
QueryManager.GetTransactionsByPrevTxIdPaginatedAsync → Results.Ok(result)
```

**Service client** (follow `GetTransactionsByWalletAsync`):
```
GET /api/query/previous/{prevTxId}/transactions?registerId=X&page=1&pageSize=20 → TransactionPage
```

## Verification

After implementation, run:
```bash
dotnet test tests/Sorcha.Register.Core.Tests
dotnet test tests/Sorcha.Validator.Service.Tests
dotnet test tests/Sorcha.ServiceClients.Tests
```
