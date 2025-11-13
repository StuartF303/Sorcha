# Task: Define Storage Abstractions

**ID:** REG-003
**Status:** Not Started
**Priority:** Critical
**Estimate:** 6 hours
**Created:** 2025-11-13
**Specification:** [previous-codebase-register-service.md](../specs/previous-codebase-register-service.md)

## Objective

Define comprehensive storage abstraction interfaces for register, transaction, and docket operations, enabling pluggable storage implementations.

## Tasks

### Repository Interfaces

#### IRegisterRepository
- [ ] Create `Storage/IRegisterRepository.cs` interface
- [ ] Define register CRUD methods:
  - `Task<Register> GetRegisterAsync(string registerId)`
  - `Task<IEnumerable<Register>> GetRegistersAsync()`
  - `Task<IEnumerable<Register>> QueryRegisters(Func<Register, bool> predicate)`
  - `Task<Register> InsertRegisterAsync(Register newRegister)`
  - `Task<Register> UpdateRegisterAsync(Register register)`
  - `Task DeleteRegisterAsync(string registerId)`
  - `Task<bool> IsLocalRegisterAsync(string registerId)`
  - `Task<int> CountRegisters()`

#### Transaction Methods
- [ ] Add transaction methods to `IRegisterRepository`:
  - `Task<IQueryable<TransactionModel>> GetTransactionsAsync(string registerId)`
  - `Task<TransactionModel> GetTransactionAsync(string registerId, string transactionId)`
  - `Task<TransactionModel> InsertTransactionAsync(TransactionModel transaction)`
  - `Task<IEnumerable<TransactionModel>> QueryTransactions(string registerId, Expression<Func<TransactionModel, bool>> predicate)`
  - `Task<IEnumerable<TransactionModel>> QueryTransactionPayload(string registerId, Expression<Func<TransactionModel, bool>> predicate)`

#### Docket Methods
- [ ] Add docket methods to `IRegisterRepository`:
  - `Task<IEnumerable<Docket>> GetDocketsAsync(string registerId)`
  - `Task<Docket> GetDocketAsync(string registerId, ulong docketId)`
  - `Task<Docket> InsertDocketAsync(Docket docket)`

#### Advanced Query Support
- [ ] Define `IQueryBuilder<T>` interface for fluent queries
- [ ] Define pagination support (`PagedResult<T>` class)
- [ ] Define sorting and filtering options

### Query Models
- [ ] Create `Storage/QueryModels/PagedResult.cs`
- [ ] Create `Storage/QueryModels/QueryOptions.cs`
- [ ] Create `Storage/QueryModels/SortOptions.cs`
- [ ] Create `Storage/QueryModels/FilterOptions.cs`

### Repository Configuration
- [ ] Create `Storage/RegisterRepositoryOptions.cs`
- [ ] Define connection string configuration
- [ ] Define database name configuration
- [ ] Define retry policy configuration
- [ ] Define timeout configuration

### Storage Provider Interface
- [ ] Create `Storage/IStorageProvider.cs` (optional abstraction)
- [ ] Define provider initialization methods
- [ ] Define health check methods
- [ ] Define connection lifecycle management

## Implementation Example

```csharp
public interface IRegisterRepository
{
    // Register operations
    Task<Register> GetRegisterAsync(string registerId,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<Register>> GetRegistersAsync(
        CancellationToken cancellationToken = default);
    Task<IEnumerable<Register>> QueryRegisters(
        Func<Register, bool> predicate,
        CancellationToken cancellationToken = default);
    Task<Register> InsertRegisterAsync(Register newRegister,
        CancellationToken cancellationToken = default);
    Task<Register> UpdateRegisterAsync(Register register,
        CancellationToken cancellationToken = default);
    Task DeleteRegisterAsync(string registerId,
        CancellationToken cancellationToken = default);
    Task<bool> IsLocalRegisterAsync(string registerId,
        CancellationToken cancellationToken = default);
    Task<int> CountRegisters(
        CancellationToken cancellationToken = default);

    // Transaction operations
    Task<IQueryable<TransactionModel>> GetTransactionsAsync(
        string registerId,
        CancellationToken cancellationToken = default);
    Task<TransactionModel> GetTransactionAsync(
        string registerId, string transactionId,
        CancellationToken cancellationToken = default);
    Task<TransactionModel> InsertTransactionAsync(
        TransactionModel transaction,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<TransactionModel>> QueryTransactions(
        string registerId,
        Expression<Func<TransactionModel, bool>> predicate,
        CancellationToken cancellationToken = default);

    // Docket operations
    Task<IEnumerable<Docket>> GetDocketsAsync(
        string registerId,
        CancellationToken cancellationToken = default);
    Task<Docket> GetDocketAsync(
        string registerId, ulong docketId,
        CancellationToken cancellationToken = default);
    Task<Docket> InsertDocketAsync(Docket docket,
        CancellationToken cancellationToken = default);
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage => PageNumber * PageSize < TotalCount;
    public bool HasPreviousPage => PageNumber > 1;
}
```

## Acceptance Criteria

- [ ] All repository interfaces defined
- [ ] CancellationToken support added to all methods
- [ ] XML documentation complete for all interfaces
- [ ] Query models created
- [ ] Configuration classes defined
- [ ] Interface versioning considered

## Definition of Done

- All interfaces compile without errors
- XML documentation complete
- No circular dependencies
- README updated with interface descriptions
- Design review approved

---

**Dependencies:** REG-001
**Blocks:** REG-004, REG-008, REG-009
