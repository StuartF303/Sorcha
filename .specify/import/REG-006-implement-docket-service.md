# Task: Implement DocketService Business Logic

**ID:** REG-006
**Status:** Not Started
**Priority:** High
**Estimate:** 8 hours
**Created:** 2025-11-13
**Specification:** [previous-codebase-register-service.md](../specs/previous-codebase-register-service.md)

## Objective

Implement the DocketService class for managing sealed transaction collections (dockets) and maintaining blockchain integrity.

## Tasks

### Interface Definition
- [ ] Create `Services/IDocketService.cs` interface
- [ ] Define docket creation methods
- [ ] Define docket retrieval methods
- [ ] Define docket validation methods
- [ ] Define chain integrity methods

### DocketService Implementation
- [ ] Create `Services/DocketService.cs` class
- [ ] Implement `IDocketService` interface
- [ ] Add constructor dependencies:
  - `IRegisterRepository` repository
  - `IRegisterService` registerService
  - `IEventPublisher` eventPublisher
  - `ILogger<DocketService>` logger

### Docket Storage
- [ ] Implement `StoreDocketAsync(Docket docket)`
- [ ] Validate docket structure
- [ ] Verify register exists
- [ ] Validate docket hash and previous hash
- [ ] Validate transaction IDs exist
- [ ] Insert docket into repository
- [ ] Increment register height atomically
- [ ] Publish `DocketConfirmed` event

### Docket Retrieval
- [ ] Implement `GetDocketAsync(string registerId, ulong docketId)`
- [ ] Implement `GetDocketsAsync(string registerId)`
- [ ] Implement `GetLatestDocketAsync(string registerId)`
- [ ] Implement `GetDocketsByStateAsync(string registerId, DocketState state)`
- [ ] Add pagination support

### Docket Validation
- [ ] Implement `ValidateDocketAsync(Docket docket)`
- [ ] Validate docket ID matches register height + 1
- [ ] Validate previous hash matches last docket
- [ ] Validate current hash calculation
- [ ] Validate transaction IDs are valid
- [ ] Validate docket state transitions
- [ ] Validate timestamp is UTC and sequential

### Chain Integrity
- [ ] Implement `ValidateDocketChainAsync(string registerId)`
- [ ] Traverse entire docket chain
- [ ] Verify hash chain integrity
- [ ] Verify height sequence
- [ ] Detect missing dockets
- [ ] Generate integrity report

### Docket State Management
- [ ] Implement `UpdateDocketStateAsync(string registerId, ulong docketId, DocketState newState)`
- [ ] Validate state transitions (Init → Proposed → Accepted → Sealed)
- [ ] Reject invalid transitions
- [ ] Publish state change events

### Transaction Sealing
- [ ] Implement `GetTransactionsForDocketAsync(string registerId, ulong docketId)`
- [ ] Validate all transactions exist
- [ ] Mark transactions as sealed
- [ ] Update transaction state

### Consensus Integration
- [ ] Handle `DocketConfirmed` event subscription
- [ ] Implement `ReceiveConfirmedDocketAsync(Docket confirmedDocket)`
- [ ] Apply consensus-approved dockets
- [ ] Update register state
- [ ] Notify participants

## Implementation Example

```csharp
public class DocketService : IDocketService
{
    private readonly IRegisterRepository _repository;
    private readonly IRegisterService _registerService;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<DocketService> _logger;

    public DocketService(
        IRegisterRepository repository,
        IRegisterService registerService,
        IEventPublisher eventPublisher,
        ILogger<DocketService> logger)
    {
        _repository = repository;
        _registerService = registerService;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Docket> StoreDocketAsync(
        Docket docket,
        CancellationToken cancellationToken = default)
    {
        await ValidateDocketAsync(docket, cancellationToken);

        // Verify register exists
        var register = await _repository.GetRegisterAsync(
            docket.RegisterId, cancellationToken);
        if (register == null)
            throw new RegisterNotFoundException(docket.RegisterId);

        // Validate docket ID matches expected height
        if (docket.Id != register.Height + 1)
        {
            throw new InvalidRegisterStateException(
                $"Docket ID {docket.Id} does not match expected height {register.Height + 1}");
        }

        // Validate previous hash if not genesis
        if (register.Height > 0)
        {
            var previousDocket = await _repository.GetDocketAsync(
                docket.RegisterId, register.Height, cancellationToken);
            if (previousDocket == null)
                throw new DocketNotFoundException(register.Height.ToString());
            if (docket.PreviousHash != previousDocket.Hash)
            {
                throw new ChainValidationException(
                    "Docket previous hash does not match last docket hash");
            }
        }

        // Validate all transactions exist
        foreach (var txId in docket.TransactionIds)
        {
            var tx = await _repository.GetTransactionAsync(
                docket.RegisterId, txId, cancellationToken);
            if (tx == null)
                throw new TransactionNotFoundException(txId);
        }

        // Store docket
        var stored = await _repository.InsertDocketAsync(docket, cancellationToken);

        // Increment register height atomically
        await _registerService.IncrementRegisterHeightAsync(
            docket.RegisterId, cancellationToken);

        // Publish event
        await _eventPublisher.PublishAsync(
            Topics.DocketConfirmedTopicName,
            new DocketConfirmed
            {
                RegisterId = stored.RegisterId,
                DocketId = stored.Id,
                TransactionIds = stored.TransactionIds,
                TimeStamp = stored.TimeStamp
            });

        _logger.LogInformation(
            "Docket {DocketId} stored in register {RegisterId}, height now {Height}",
            stored.Id, stored.RegisterId, stored.Id);

        return stored;
    }

    private async Task ValidateDocketAsync(
        Docket docket,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(docket.RegisterId))
            throw new ArgumentException("Docket RegisterId is required");
        if (docket.TransactionIds == null || !docket.TransactionIds.Any())
            throw new ArgumentException("Docket must contain at least one transaction");
        if (string.IsNullOrWhiteSpace(docket.Hash))
            throw new ArgumentException("Docket hash is required");
        // Additional validations...
    }

    public async Task<bool> ValidateDocketChainAsync(
        string registerId,
        CancellationToken cancellationToken = default)
    {
        var register = await _repository.GetRegisterAsync(registerId, cancellationToken);
        if (register == null)
            throw new RegisterNotFoundException(registerId);

        var dockets = await _repository.GetDocketsAsync(registerId, cancellationToken);
        var sortedDockets = dockets.OrderBy(d => d.Id).ToList();

        string lastHash = null;
        for (int i = 0; i < sortedDockets.Count; i++)
        {
            var docket = sortedDockets[i];

            // Check height sequence
            if (docket.Id != (ulong)(i + 1))
            {
                _logger.LogError(
                    "Docket height mismatch at position {Position}: expected {Expected}, got {Actual}",
                    i, i + 1, docket.Id);
                return false;
            }

            // Check hash chain
            if (i > 0 && docket.PreviousHash != lastHash)
            {
                _logger.LogError(
                    "Hash chain broken at docket {DocketId}: previous hash mismatch",
                    docket.Id);
                return false;
            }

            lastHash = docket.Hash;
        }

        _logger.LogInformation(
            "Docket chain validation successful for register {RegisterId}, {Count} dockets validated",
            registerId, sortedDockets.Count);

        return true;
    }
}
```

## Acceptance Criteria

- [ ] All IDocketService methods implemented
- [ ] Docket validation comprehensive
- [ ] Chain integrity validation working
- [ ] Atomic height increment
- [ ] Events published correctly
- [ ] Error handling complete
- [ ] Unit tests >90% coverage

## Definition of Done

- All methods implemented and tested
- Unit tests passing
- Code review approved
- XML documentation complete
- Chain validation verified
- Integration with RegisterService tested

---

**Dependencies:** REG-001, REG-002, REG-003, REG-004, REG-011
**Blocks:** REG-016, REG-024
