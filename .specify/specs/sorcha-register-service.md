# Sorcha.Register.Service Specification (Boilerplate)

**Version:** 0.1
**Date:** 2025-11-13
**Status:** To Be Specified
**Related Constitution:** [constitution.md](../constitution.md)

## Executive Summary

This specification will define the Sorcha.Register.Service - the distributed ledger and block management service for the Sorcha platform. This service is responsible for:

- Block creation and validation
- Transaction registry
- Merkle tree management
- Block synchronization
- Ledger query capabilities

## Current Status

⚠️ **This service specification is a placeholder for future development**

The Register Service is referenced by the Wallet Service for retrieving transaction history during wallet recovery. The Wallet Service will gracefully degrade if the Register Service is unavailable.

## Minimal Boilerplate Interface

Until fully specified and implemented, the Register Service should provide the following minimal interface:

### IRegisterServiceClient (Placeholder)

```csharp
namespace Sorcha.Register.Client;

/// <summary>
/// Client interface for Register Service
/// </summary>
public interface IRegisterServiceClient
{
    /// <summary>
    /// Gets all transactions where the specified address is a recipient
    /// </summary>
    Task<IEnumerable<TransactionModel>> GetAllTransactionsByRecipientAddressAsync(
        string registerId,
        string address,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all transactions where the specified address is the sender
    /// </summary>
    Task<IEnumerable<TransactionModel>> GetAllTransactionsBySenderAddressAsync(
        string registerId,
        string address,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific transaction by ID
    /// </summary>
    Task<TransactionModel?> GetTransactionAsync(
        string registerId,
        string transactionId,
        CancellationToken cancellationToken = default);
}
```

### Stub Implementation

For development purposes, provide a stub implementation that returns empty collections:

```csharp
namespace Sorcha.Register.Client;

public class RegisterServiceStub : IRegisterServiceClient
{
    private readonly ILogger<RegisterServiceStub> _logger;

    public RegisterServiceStub(ILogger<RegisterServiceStub> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<TransactionModel>> GetAllTransactionsByRecipientAddressAsync(
        string registerId,
        string address,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Register Service stub called - returning empty collection");
        return Task.FromResult(Enumerable.Empty<TransactionModel>());
    }

    public Task<IEnumerable<TransactionModel>> GetAllTransactionsBySenderAddressAsync(
        string registerId,
        string address,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Register Service stub called - returning empty collection");
        return Task.FromResult(Enumerable.Empty<TransactionModel>());
    }

    public Task<TransactionModel?> GetTransactionAsync(
        string registerId,
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Register Service stub called - returning null");
        return Task.FromResult<TransactionModel?>(null);
    }
}
```

## Integration Points

### Wallet Service Integration

The Wallet Service will:
- Accept `IRegisterServiceClient` as an optional dependency
- Gracefully handle when Register Service is unavailable
- Log warnings when transaction history cannot be retrieved
- Allow wallet creation/recovery without transaction history

### Future Development

When fully specified, the Register Service will provide:
- Complete ledger management
- Block validation and consensus
- Transaction indexing
- Merkle proof generation
- Query optimization
- Multi-register support

## Dependencies

To be defined when specification is complete.

## Timeline

To be determined based on priority and roadmap.

---

**Document Status:** Placeholder - Awaiting Full Specification
**Priority:** Medium (Not blocking Wallet Service development)
**Assigned To:** To Be Determined
