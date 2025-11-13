# Task: Implement TransactionService Business Logic

**ID:** REG-005
**Status:** Not Started
**Priority:** Critical
**Estimate:** 10 hours
**Created:** 2025-11-13
**Specification:** [previous-codebase-register-service.md](../specs/previous-codebase-register-service.md)

## Objective

Implement the TransactionService class for managing transaction storage, retrieval, and validation within registers.

## Tasks

### Interface Definition
- [ ] Create `Services/ITransactionService.cs` interface
- [ ] Define transaction storage methods
- [ ] Define transaction retrieval methods
- [ ] Define transaction query methods
- [ ] Define transaction validation methods

### TransactionService Implementation
- [ ] Create `Services/TransactionService.cs` class
- [ ] Implement `ITransactionService` interface
- [ ] Add constructor dependencies:
  - `IRegisterRepository` repository
  - `IEventPublisher` eventPublisher
  - `ILogger<TransactionService>` logger

### Transaction Storage
- [ ] Implement `StoreTransactionAsync(TransactionModel transaction)`
- [ ] Validate transaction structure
- [ ] Verify register exists
- [ ] Check transaction doesn't already exist
- [ ] Validate transaction chain (prevTxId)
- [ ] Insert transaction into repository
- [ ] Publish `TransactionConfirmed` event
- [ ] Log transaction storage

### Transaction Retrieval
- [ ] Implement `GetTransactionAsync(string registerId, string transactionId)`
- [ ] Implement `GetTransactionsAsync(string registerId)`
- [ ] Implement `GetTransactionsByDocketAsync(string registerId, ulong docketId)`
- [ ] Handle not found cases
- [ ] Add pagination support

### Transaction Queries
- [ ] Implement `QueryTransactionsAsync(string registerId, Expression<Func<TransactionModel, bool>> predicate)`
- [ ] Implement `GetTransactionsByBlueprintAsync(string registerId, string blueprintId)`
- [ ] Implement `GetTransactionsByInstanceAsync(string registerId, string instanceId)`
- [ ] Implement `GetTransactionsByWalletAsync(string registerId, string walletAddress)`
- [ ] Add filtering by date range
- [ ] Add sorting options

### Transaction Validation
- [ ] Implement `ValidateTransactionAsync(TransactionModel transaction)`
- [ ] Validate transaction ID format (64 char hex)
- [ ] Validate previous transaction exists (if not genesis)
- [ ] Validate sender wallet format
- [ ] Validate recipient wallets format
- [ ] Validate metadata presence
- [ ] Validate payload structure
- [ ] Validate timestamp is UTC

### Chain Validation
- [ ] Implement `ValidateTransactionChainAsync(string registerId, string transactionId)`
- [ ] Traverse transaction chain backwards
- [ ] Verify prevTxId links
- [ ] Detect broken chains
- [ ] Report chain integrity status

### Payload Operations
- [ ] Implement `GetTransactionPayloadsAsync(string registerId, string transactionId)`
- [ ] Implement `QueryPayloadsByHashAsync(string registerId, string payloadHash)`
- [ ] Validate wallet access to payloads
- [ ] Handle encrypted payload metadata

### Transaction Events
- [ ] Handle `TransactionValidationCompleted` event subscription
- [ ] Handle `TransactionSubmitted` event subscription
- [ ] Publish `TransactionConfirmed` event with proper payload
- [ ] Include recipient wallets in confirmation event
- [ ] Include metadata in event payload

## Implementation Example

```csharp
public class TransactionService : ITransactionService
{
    private readonly IRegisterRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        IRegisterRepository repository,
        IEventPublisher eventPublisher,
        ILogger<TransactionService> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<TransactionModel> StoreTransactionAsync(
        TransactionModel transaction,
        CancellationToken cancellationToken = default)
    {
        await ValidateTransactionAsync(transaction, cancellationToken);

        var registerId = transaction.MetaData?.RegisterId;
        if (string.IsNullOrEmpty(registerId))
            throw new ArgumentException("Transaction metadata must include RegisterId");

        // Verify register exists
        if (!await _repository.IsLocalRegisterAsync(registerId, cancellationToken))
            throw new RegisterNotFoundException(registerId);

        // Check for duplicate
        var existing = await _repository.GetTransactionAsync(
            registerId, transaction.TxId, cancellationToken);
        if (existing != null)
        {
            _logger.LogWarning(
                "Transaction {TxId} already exists in register {RegisterId}",
                transaction.TxId, registerId);
            return existing;
        }

        // Validate chain
        if (!string.IsNullOrEmpty(transaction.PrevTxId))
        {
            var previous = await _repository.GetTransactionAsync(
                registerId, transaction.PrevTxId, cancellationToken);
            if (previous == null)
                throw new TransactionNotFoundException(transaction.PrevTxId);
        }

        // Store transaction
        var stored = await _repository.InsertTransactionAsync(
            transaction, cancellationToken);

        // Publish event
        await _eventPublisher.PublishAsync(
            Topics.TransactionConfirmedTopicName,
            new TransactionConfirmed
            {
                TransactionId = stored.TxId,
                ToWallets = stored.RecipientsWallets.ToList(),
                Sender = stored.SenderWallet,
                PreviousTransactionId = stored.PrevTxId,
                MetaData = stored.MetaData
            });

        _logger.LogInformation(
            "Transaction {TxId} stored in register {RegisterId}",
            stored.TxId, registerId);

        return stored;
    }

    private async Task ValidateTransactionAsync(
        TransactionModel transaction,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(transaction.TxId))
            throw new ArgumentException("Transaction ID is required");
        if (!System.Text.RegularExpressions.Regex.IsMatch(
            transaction.TxId, "^[a-fA-F0-9]{64}$"))
            throw new ArgumentException("Transaction ID must be 64 char hex");
        if (string.IsNullOrWhiteSpace(transaction.SenderWallet))
            throw new ArgumentException("Sender wallet is required");
        if (transaction.MetaData == null)
            throw new ArgumentException("Transaction metadata is required");
        // Additional validations...
    }
}
```

## Acceptance Criteria

- [ ] All ITransactionService methods implemented
- [ ] Transaction validation comprehensive
- [ ] Chain validation working
- [ ] Events published correctly
- [ ] Error handling complete
- [ ] Logging for all operations
- [ ] Unit tests >90% coverage

## Definition of Done

- All methods implemented and tested
- Unit tests passing
- Code review approved
- XML documentation complete
- Event integration verified
- Chain validation tested

---

**Dependencies:** REG-001, REG-002, REG-003, REG-011
**Blocks:** REG-016, REG-023
