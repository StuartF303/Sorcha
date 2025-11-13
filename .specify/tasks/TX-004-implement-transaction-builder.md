# Task: Implement TransactionBuilder (Fluent API)

**ID:** TX-004
**Status:** Not Started
**Priority:** Critical
**Estimate:** 10 hours
**Created:** 2025-11-12

## Objective

Implement fluent, builder-pattern API for intuitive transaction creation.

## Implementation Details

### Interfaces/ITransactionBuilder.cs
```csharp
public interface ITransactionBuilder
{
    ITransactionBuilder Create(TransactionVersion version = TransactionVersion.V4);
    ITransactionBuilder WithPreviousTransaction(string txHash);
    ITransactionBuilder WithRecipients(params string[] walletAddresses);
    ITransactionBuilder WithMetadata(string jsonMetadata);
    ITransactionBuilder WithMetadata<T>(T metadata) where T : class;

    ITransactionBuilder AddPayload(
        byte[] data,
        string[] recipientWallets,
        PayloadOptions? options = null);

    Task<ITransactionBuilder> SignAsync(
        string wifPrivateKey,
        CancellationToken cancellationToken = default);

    TransactionResult<ITransaction> Build();
}
```

### Core/TransactionBuilder.cs

**Fluent API Pattern:**
```csharp
public class TransactionBuilder : ITransactionBuilder
{
    private ITransaction _transaction;
    private bool _isSigned = false;

    public ITransactionBuilder Create(TransactionVersion version)
    {
        _transaction = TransactionFactory.Create(version);
        _isSigned = false;
        return this;
    }

    public ITransactionBuilder WithRecipients(params string[] wallets)
    {
        ValidateNotSigned();
        ValidateWallets(wallets);
        _transaction.SetRecipients(wallets);
        return this;
    }

    public ITransactionBuilder AddPayload(
        byte[] data,
        string[] recipientWallets,
        PayloadOptions? options = null)
    {
        ValidateNotSigned();
        await _transaction.PayloadManager.AddPayloadAsync(
            data, recipientWallets, options);
        return this;
    }

    public async Task<ITransactionBuilder> SignAsync(string wifKey)
    {
        await _transaction.SignAsync(wifKey);
        _isSigned = true;
        return this;
    }

    public TransactionResult<ITransaction> Build()
    {
        if (!_isSigned)
            return TransactionResult<ITransaction>.Failure(
                TransactionStatus.NotSigned);

        return TransactionResult<ITransaction>.Success(_transaction);
    }

    private void ValidateNotSigned()
    {
        if (_isSigned)
            throw new InvalidOperationException(
                "Cannot modify transaction after signing");
    }
}
```

## Usage Example
```csharp
var result = await new TransactionBuilder()
    .Create(TransactionVersion.V4)
    .WithRecipients("ws1qyqszqgp...", "ws1pqpszqgp...")
    .WithMetadata(new { type = "document", id = "doc123" })
    .AddPayload(documentData, new[] { "ws1qyqszqgp..." })
    .SignAsync(senderWifKey)
    .Build();

if (result.IsSuccess)
{
    var tx = result.Value;
    Console.WriteLine($"Created transaction: {tx.TxId}");
}
```

## Acceptance Criteria

- [ ] Fluent API implemented
- [ ] All builder methods working
- [ ] Method chaining working
- [ ] Validates state (no modifications after signing)
- [ ] Generic metadata serialization working
- [ ] All unit tests passing

---

**Dependencies:** TX-001, TX-002, TX-003
