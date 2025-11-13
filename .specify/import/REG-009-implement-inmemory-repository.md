# Task: Implement In-Memory Repository

**ID:** REG-009
**Status:** Not Started
**Priority:** High
**Estimate:** 6 hours
**Created:** 2025-11-13
**Specification:** [previous-codebase-register-service.md](../specs/previous-codebase-register-service.md)

## Objective

Implement an in-memory storage provider for testing and caching scenarios, providing fast, isolated storage without external dependencies.

## Tasks

### Project Setup
- [ ] Create `Siccar.RegisterService.Storage.InMemory` project
- [ ] Add NuGet package `System.Linq.Async`
- [ ] Add NuGet package `Microsoft.Extensions.Logging`
- [ ] Reference `Siccar.RegisterService` core library
- [ ] Reference `Siccar.Platform` for models

### Repository Implementation
- [ ] Create `InMemoryRegisterRepository.cs` class
- [ ] Implement `IRegisterRepository` interface
- [ ] Add constructor with `ILogger<InMemoryRegisterRepository>` logger
- [ ] Use thread-safe collections (ConcurrentDictionary)

### Data Storage
- [ ] Create `ConcurrentDictionary<string, Register>` for registers
- [ ] Create `ConcurrentDictionary<string, List<TransactionModel>>` for transactions
- [ ] Create `ConcurrentDictionary<string, List<Docket>>` for dockets
- [ ] Implement proper locking for complex operations

### Register Operations
- [ ] Implement `GetRegisterAsync(string registerId)`
- [ ] Implement `GetRegistersAsync()`
- [ ] Implement `QueryRegisters(Func<Register, bool> predicate)`
- [ ] Implement `InsertRegisterAsync(Register newRegister)`
  - Check for duplicates
  - Initialize collections for register
- [ ] Implement `UpdateRegisterAsync(Register register)`
- [ ] Implement `DeleteRegisterAsync(string registerId)`
  - Remove register and all data
- [ ] Implement `IsLocalRegisterAsync(string registerId)`
- [ ] Implement `CountRegisters()`

### Transaction Operations
- [ ] Implement `GetTransactionsAsync(string registerId)`
  - Return IQueryable using AsQueryable()
- [ ] Implement `GetTransactionAsync(string registerId, string transactionId)`
- [ ] Implement `InsertTransactionAsync(TransactionModel transaction)`
  - Check for duplicates
  - Add to register's transaction list
- [ ] Implement `QueryTransactions(string registerId, Expression<Func<TransactionModel, bool>> predicate)`
- [ ] Implement `QueryTransactionPayload(string registerId, Expression<Func<TransactionModel, bool>> predicate)`

### Docket Operations
- [ ] Implement `GetDocketsAsync(string registerId)`
- [ ] Implement `GetDocketAsync(string registerId, ulong docketId)`
- [ ] Implement `InsertDocketAsync(Docket docket)`
  - Check for duplicates
  - Add to register's docket list

### Thread Safety
- [ ] Use ConcurrentDictionary for all storage
- [ ] Use locks for multi-step operations
- [ ] Implement atomic operations where needed
- [ ] Test concurrent access scenarios

### Query Support
- [ ] Convert stored data to IQueryable
- [ ] Support LINQ queries
- [ ] Implement efficient filtering
- [ ] Support ordering and paging

### Utility Methods
- [ ] Implement `ClearAsync()` for test cleanup
- [ ] Implement `GetStatisticsAsync()` for debugging
- [ ] Implement `ExportAsync()` for data export (optional)

## Implementation Example

```csharp
public class InMemoryRegisterRepository : IRegisterRepository
{
    private readonly ConcurrentDictionary<string, Register> _registers;
    private readonly ConcurrentDictionary<string, List<TransactionModel>> _transactions;
    private readonly ConcurrentDictionary<string, List<Docket>> _dockets;
    private readonly ILogger<InMemoryRegisterRepository> _logger;
    private readonly object _lockObject = new object();

    public InMemoryRegisterRepository(
        ILogger<InMemoryRegisterRepository> logger)
    {
        _logger = logger;
        _registers = new ConcurrentDictionary<string, Register>();
        _transactions = new ConcurrentDictionary<string, List<TransactionModel>>();
        _dockets = new ConcurrentDictionary<string, List<Docket>>();
    }

    public async Task<Register> InsertRegisterAsync(
        Register newRegister,
        CancellationToken cancellationToken = default)
    {
        if (_registers.ContainsKey(newRegister.Id))
        {
            throw new RegisterAlreadyExistsException(newRegister.Id);
        }

        lock (_lockObject)
        {
            if (!_registers.TryAdd(newRegister.Id, newRegister))
            {
                throw new RegisterAlreadyExistsException(newRegister.Id);
            }

            // Initialize collections for this register
            _transactions.TryAdd(newRegister.Id, new List<TransactionModel>());
            _dockets.TryAdd(newRegister.Id, new List<Docket>());
        }

        _logger.LogDebug("Register {RegisterId} created in memory", newRegister.Id);

        return await Task.FromResult(newRegister);
    }

    public async Task<IEnumerable<Register>> GetRegistersAsync(
        CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_registers.Values.ToList());
    }

    public async Task<Register> GetRegisterAsync(
        string registerId,
        CancellationToken cancellationToken = default)
    {
        if (!_registers.TryGetValue(registerId, out var register))
        {
            return null;
        }
        return await Task.FromResult(register);
    }

    public async Task<Register> UpdateRegisterAsync(
        Register register,
        CancellationToken cancellationToken = default)
    {
        if (!_registers.ContainsKey(register.Id))
        {
            throw new RegisterNotFoundException(register.Id);
        }

        _registers[register.Id] = register;
        _logger.LogDebug("Register {RegisterId} updated in memory", register.Id);

        return await Task.FromResult(register);
    }

    public async Task DeleteRegisterAsync(
        string registerId,
        CancellationToken cancellationToken = default)
    {
        lock (_lockObject)
        {
            if (!_registers.TryRemove(registerId, out _))
            {
                throw new RegisterNotFoundException(registerId);
            }

            // Clean up related data
            _transactions.TryRemove(registerId, out _);
            _dockets.TryRemove(registerId, out _);
        }

        _logger.LogDebug("Register {RegisterId} deleted from memory", registerId);

        await Task.CompletedTask;
    }

    public async Task<bool> IsLocalRegisterAsync(
        string registerId,
        CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_registers.ContainsKey(registerId));
    }

    public async Task<int> CountRegisters(
        CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_registers.Count);
    }

    public async Task<TransactionModel> InsertTransactionAsync(
        TransactionModel transaction,
        CancellationToken cancellationToken = default)
    {
        var registerId = transaction.MetaData?.RegisterId;
        if (string.IsNullOrEmpty(registerId))
            throw new ArgumentException("Transaction must have RegisterId in metadata");

        if (!_registers.ContainsKey(registerId))
            throw new RegisterNotFoundException(registerId);

        if (!_transactions.TryGetValue(registerId, out var txList))
        {
            txList = new List<TransactionModel>();
            _transactions[registerId] = txList;
        }

        lock (txList)
        {
            // Check for duplicate
            if (txList.Any(t => t.TxId == transaction.TxId))
            {
                _logger.LogWarning(
                    "Transaction {TxId} already exists in register {RegisterId}",
                    transaction.TxId, registerId);
                return txList.First(t => t.TxId == transaction.TxId);
            }

            txList.Add(transaction);
        }

        _logger.LogDebug(
            "Transaction {TxId} inserted into register {RegisterId}",
            transaction.TxId, registerId);

        return await Task.FromResult(transaction);
    }

    public async Task<IQueryable<TransactionModel>> GetTransactionsAsync(
        string registerId,
        CancellationToken cancellationToken = default)
    {
        if (!_registers.ContainsKey(registerId))
            throw new RegisterNotFoundException(registerId);

        if (!_transactions.TryGetValue(registerId, out var txList))
        {
            return await Task.FromResult(new List<TransactionModel>().AsQueryable());
        }

        return await Task.FromResult(txList.AsQueryable());
    }

    public async Task<TransactionModel> GetTransactionAsync(
        string registerId,
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        if (!_registers.ContainsKey(registerId))
            throw new RegisterNotFoundException(registerId);

        if (!_transactions.TryGetValue(registerId, out var txList))
        {
            return null;
        }

        var transaction = txList.FirstOrDefault(t => t.TxId == transactionId);
        return await Task.FromResult(transaction);
    }

    public async Task<Docket> InsertDocketAsync(
        Docket docket,
        CancellationToken cancellationToken = default)
    {
        if (!_registers.ContainsKey(docket.RegisterId))
            throw new RegisterNotFoundException(docket.RegisterId);

        if (!_dockets.TryGetValue(docket.RegisterId, out var docketList))
        {
            docketList = new List<Docket>();
            _dockets[docket.RegisterId] = docketList;
        }

        lock (docketList)
        {
            // Check for duplicate
            if (docketList.Any(d => d.Id == docket.Id))
            {
                throw new InvalidOperationException(
                    $"Docket {docket.Id} already exists in register {docket.RegisterId}");
            }

            docketList.Add(docket);
        }

        _logger.LogDebug(
            "Docket {DocketId} inserted into register {RegisterId}",
            docket.Id, docket.RegisterId);

        return await Task.FromResult(docket);
    }

    // Utility for testing
    public async Task ClearAsync()
    {
        lock (_lockObject)
        {
            _registers.Clear();
            _transactions.Clear();
            _dockets.Clear();
        }
        await Task.CompletedTask;
    }
}
```

## Acceptance Criteria

- [ ] All IRegisterRepository methods implemented
- [ ] Thread-safe operations
- [ ] IQueryable support working
- [ ] No external dependencies
- [ ] Fast performance
- [ ] Clear method for test cleanup
- [ ] Unit tests >95% coverage

## Definition of Done

- All methods implemented
- Unit tests passing
- Thread safety verified
- Performance benchmarks meet requirements
- Code review approved
- XML documentation complete

---

**Dependencies:** REG-001, REG-003
**Blocks:** REG-022, REG-023, REG-024
