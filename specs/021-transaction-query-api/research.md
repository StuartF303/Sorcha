# Research: Transaction Query API

**Feature**: 021-transaction-query-api
**Date**: 2026-02-06

## R1: MongoDB Index Strategy for PrevTxId

**Decision**: Add a single ascending index on `PrevTxId` to the existing `CreateTransactionIndexesAsync` method.

**Rationale**: The query filters by exact match on `PrevTxId` within a per-register database. Since each register has its own MongoDB database (`sorcha_register_{registerId}`), the register scoping is implicit — no compound index with RegisterId is needed. A simple ascending index on `PrevTxId` is sufficient for `Filter.Eq()` lookups.

**Alternatives considered**:
- Compound index `(PrevTxId, TimeStamp)`: Rejected — the sort by TimeStamp is applied after filtering and the result set is typically 1-2 documents (forks are rare). A compound index adds write overhead for minimal read benefit.
- Using the generic `QueryTransactionsAsync(predicate)` with no index: Rejected — would cause collection scans on every fork detection check during validation, violating FR-007.

## R2: Repository Method Pattern

**Decision**: Add a dedicated `GetTransactionsByPrevTxIdAsync` method to `IRegisterRepository` that returns `IEnumerable<TransactionModel>`, following the pattern of `GetAllTransactionsBySenderAddressAsync`.

**Rationale**: While the generic `QueryTransactionsAsync(Expression<Func<TransactionModel, bool>>)` could technically serve this purpose, dedicated methods provide:
1. Clear API contracts for each query use case
2. Implementation-specific optimizations (MongoDB filter builders vs LINQ compilation)
3. Consistent naming with existing methods

**Alternatives considered**:
- Using `QueryTransactionsAsync` with a LINQ predicate: Rejected — LINQ-to-MongoDB translation may not efficiently use the index. Direct `Filter.Eq()` is preferred.
- Adding pagination at the repository level: Rejected — pagination belongs in `QueryManager`, matching the existing pattern where repositories return full result sets and managers apply Skip/Take.

## R3: Endpoint URL Pattern

**Decision**: `GET /api/query/previous/{prevTxId}/transactions?registerId={registerId}&page=1&pageSize=20`

**Rationale**: Follows the established `/api/query/{entity}/{identifier}/transactions` pattern used by:
- `/api/query/wallets/{address}/transactions`
- `/api/query/senders/{address}/transactions`
- `/api/query/blueprints/{blueprintId}/transactions`

The `registerId` is a required query parameter (same pattern as the wallet endpoint).

**Alternatives considered**:
- `/api/registers/{registerId}/transactions?prevTxId={txId}`: Rejected — mixes with the existing transaction CRUD group which uses `CanSubmitTransactions` authorization, while queries use `CanReadTransactions`.
- OData `$filter=PrevTxId eq '{txId}'`: Available via existing OData endpoint but doesn't provide the same authorization boundaries or structured response format.

## R4: Pagination Model Alignment

**Decision**: Use `PaginatedResult<TransactionModel>` at the `QueryManager` level, mapped to `TransactionPage` at the service client level — matching existing patterns.

**Rationale**: The system already has two pagination models:
- `PaginatedResult<T>` (QueryManager, internal) with `Items` property
- `TransactionPage` (IRegisterServiceClient, external) with `Transactions` property

Both are used by existing paginated methods. Maintaining this separation keeps the internal business logic decoupled from the external API contract.

**Alternatives considered**:
- Unifying to a single pagination model: Rejected — would require changes across all existing methods, violating the "no regressions" success criterion (SC-004).

## R5: ValidationEngine Fork Detection Integration

**Decision**: Add fork detection to `ValidateChainAsync` in `ValidationEngine` by calling the new `GetTransactionsByPrevTxIdAsync` method on `IRegisterServiceClient` when a transaction specifies a `PreviousTransactionId`.

**Rationale**: The ValidationEngine already has chain validation infrastructure (VAL_CHAIN_* error codes from 020). Fork detection is a natural extension — if querying by the incoming transaction's `PreviousTransactionId` returns any existing transactions, a fork exists (the incoming transaction would be a second successor to the same predecessor).

**Alternatives considered**:
- Separate fork detection step in the validation pipeline: Rejected — fork detection is logically part of chain validation, not a standalone step.
- Detecting forks at the Register Service submission endpoint: Rejected — validation is the Validator Service's responsibility per the microservices architecture.
