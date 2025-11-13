# WALLET-004: Implement WalletManager

**Status:** Not Started
**Priority:** Critical
**Estimated Hours:** 20
**Dependencies:** WALLET-001, WALLET-002, WALLET-003
**Related Spec:** [siccar-wallet-service.md](../specs/siccar-wallet-service.md#1-wallet-lifecycle)

## Objective

Implement the WalletManager service responsible for wallet lifecycle management including creation, recovery, updating, and deletion operations.

## Requirements

### Interface Definition

```csharp
namespace Siccar.WalletService.Services.Interfaces
{
    /// <summary>
    /// Manages wallet lifecycle operations
    /// </summary>
    public interface IWalletManager
    {
        /// <summary>
        /// Creates a new wallet with generated mnemonic
        /// </summary>
        Task<WalletCreationResult> CreateWalletAsync(
            CreateWalletRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Recovers an existing wallet from mnemonic
        /// </summary>
        Task<WalletRecoveryResult> RecoverWalletAsync(
            RecoverWalletRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a wallet by address
        /// </summary>
        Task<Wallet?> GetWalletAsync(
            string address,
            string? userSubject = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all wallets owned by a user
        /// </summary>
        Task<IEnumerable<Wallet>> GetWalletsByOwnerAsync(
            string ownerSubject,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all wallets in a tenant
        /// </summary>
        Task<IEnumerable<Wallet>> GetWalletsByTenantAsync(
            string tenantId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates wallet metadata
        /// </summary>
        Task<Wallet> UpdateWalletAsync(
            string address,
            UpdateWalletRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Soft deletes a wallet
        /// </summary>
        Task DeleteWalletAsync(
            string address,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Permanently deletes a wallet (use with caution)
        /// </summary>
        Task PermanentlyDeleteWalletAsync(
            string address,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Archives a wallet (marks as inactive)
        /// </summary>
        Task<Wallet> ArchiveWalletAsync(
            string address,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Restores an archived wallet
        /// </summary>
        Task<Wallet> RestoreWalletAsync(
            string address,
            CancellationToken cancellationToken = default);
    }
}
```

### Implementation

**File:** `Services/Implementation/WalletManager.cs`

Key responsibilities:
1. **Wallet Creation**
   - Validate request parameters
   - Generate mnemonic using NBitcoin (12 or 24 words)
   - Create master key using IKeyManager
   - Encrypt private key using IEncryptionProvider
   - Create initial wallet entity
   - Add owner as primary delegate
   - Save to repository
   - Publish WalletCreatedEvent
   - Return wallet with mnemonic (user must backup!)

2. **Wallet Recovery**
   - Validate mnemonic (BIP39 checksum)
   - Recover keys from mnemonic
   - Optionally retrieve transaction history from Register Service
   - Encrypt recovered keys
   - Create wallet entity with history
   - Save to repository
   - Publish WalletRecoveredEvent
   - Return recovered wallet

3. **Wallet Retrieval**
   - Validate access permissions (via IDelegationManager)
   - Query repository
   - Decrypt private key if user has appropriate access
   - Return wallet entity

4. **Wallet Updates**
   - Validate access permissions (owner or delegate-rw)
   - Update allowed fields (name, description, tags, status)
   - Validate state transitions (e.g., can't activate a deleted wallet)
   - Save changes with optimistic concurrency
   - Publish WalletUpdatedEvent
   - Return updated wallet

5. **Wallet Deletion**
   - Soft delete: Set DeletedAt timestamp, change status to Deleted
   - Archive: Set status to Archived
   - Permanent delete: Remove from repository (requires confirmation)
   - Publish WalletDeletedEvent or WalletArchivedEvent

### Dependencies to Inject

```csharp
public class WalletManager : IWalletManager
{
    private readonly IWalletRepository _repository;
    private readonly IKeyManager _keyManager;
    private readonly IEncryptionProvider _encryptionProvider;
    private readonly IDelegationManager _delegationManager;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<WalletManager> _logger;
    private readonly IRegisterServiceClient? _registerClient;  // Optional

    public WalletManager(
        IWalletRepository repository,
        IKeyManager keyManager,
        IEncryptionProvider encryptionProvider,
        IDelegationManager delegationManager,
        IEventPublisher eventPublisher,
        ILogger<WalletManager> logger,
        IRegisterServiceClient? registerClient = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _keyManager = keyManager ?? throw new ArgumentNullException(nameof(keyManager));
        // ... initialize all dependencies
    }
}
```

### Validation Rules

**CreateWalletRequest:**
- Name: Required, max 200 characters
- Tenant: Required, non-empty
- Owner: Required, valid subject format
- Algorithm: Must be supported (ED25519, SECP256K1, RSA)
- Mnemonic: If provided, must be valid BIP39 (12 or 24 words)

**RecoverWalletRequest:**
- Name: Required, max 200 characters
- Mnemonic: Required, valid BIP39 with correct checksum
- RegisterId: Optional, but required for transaction history
- Algorithm: Must match algorithm used to generate mnemonic

**UpdateWalletRequest:**
- At least one field must be specified
- Name: Max 200 characters if provided
- Status transitions must be valid:
  - Active → Archived ✓
  - Archived → Active ✓
  - Active → Deleted ✓
  - Deleted → Active ✗ (use RestoreWalletAsync)

### Error Handling

```csharp
// Custom exceptions
public class WalletException : Exception
{
    public string ErrorCode { get; }
    public HttpStatusCode StatusCode { get; }

    public WalletException(string errorCode, string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}

// Error codes
public static class WalletErrorCodes
{
    public const string WalletNotFound = "WALLET_NOT_FOUND";
    public const string WalletAlreadyExists = "WALLET_ALREADY_EXISTS";
    public const string InvalidMnemonic = "INVALID_MNEMONIC";
    public const string InvalidAccess = "INVALID_ACCESS";
    public const string InvalidStateTransition = "INVALID_STATE_TRANSITION";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
}
```

### Event Publishing

```csharp
// After successful wallet creation
await _eventPublisher.PublishAsync(new WalletCreatedEvent
{
    WalletAddress = wallet.Address,
    Owner = wallet.Owner,
    Tenant = wallet.Tenant,
    Algorithm = wallet.Algorithm,
    CreatedAt = wallet.CreatedAt
}, Topics.WalletCreatedTopicName);

// After successful recovery
await _eventPublisher.PublishAsync(new WalletRecoveredEvent
{
    WalletAddress = wallet.Address,
    Owner = wallet.Owner,
    Tenant = wallet.Tenant,
    TransactionCount = wallet.Transactions.Count,
    RecoveredAt = DateTime.UtcNow
}, Topics.WalletRecoveredTopicName);
```

### Transaction History Integration

```csharp
private async Task<List<WalletTransaction>> GetTransactionHistoryAsync(
    string registerId,
    string publicKey,
    CancellationToken cancellationToken)
{
    if (_registerClient == null)
    {
        _logger.LogWarning("Register client not configured, skipping transaction history");
        return new List<WalletTransaction>();
    }

    try
    {
        var received = await _registerClient.GetAllTransactionsByRecipientAddress(
            registerId, publicKey, cancellationToken);
        var sent = await _registerClient.GetAllTransactionsBySenderAddress(
            registerId, publicKey, cancellationToken);

        // Merge and deduplicate transactions
        // Mark spent transactions
        // Return combined list
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to retrieve transaction history, wallet will be created without history");
        return new List<WalletTransaction>();
    }
}
```

## Acceptance Criteria

- [ ] IWalletManager interface defined
- [ ] WalletManager implementation complete
- [ ] All CRUD operations implemented
- [ ] Mnemonic generation using NBitcoin (BIP39)
- [ ] Key encryption/decryption integrated
- [ ] Access control validation on all operations
- [ ] Event publishing on state changes
- [ ] Transaction history retrieval (optional, graceful degradation)
- [ ] Proper error handling with custom exceptions
- [ ] Comprehensive logging (Info, Warning, Error)
- [ ] Unit tests with >90% coverage
- [ ] Mock all dependencies in tests
- [ ] Integration tests with real repository

## Testing

### Unit Tests

**File:** `Tests/Services/WalletManagerTests.cs`

```csharp
public class WalletManagerTests
{
    [Fact]
    public async Task CreateWallet_WithValidRequest_ReturnsWalletWithMnemonic()
    {
        // Arrange
        var request = new CreateWalletRequest { ... };
        var mockRepo = new Mock<IWalletRepository>();
        var mockKeyManager = new Mock<IKeyManager>();
        // ... setup mocks
        var sut = new WalletManager(...);

        // Act
        var result = await sut.CreateWalletAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Wallet.Should().NotBeNull();
        result.Mnemonic.Should().NotBeNullOrEmpty();
        result.Mnemonic.Split(' ').Should().HaveCount(24);  // Default 24 words
    }

    [Fact]
    public async Task RecoverWallet_WithInvalidMnemonic_ThrowsWalletException()
    {
        // Arrange
        var request = new RecoverWalletRequest
        {
            Mnemonic = "invalid mnemonic phrase"
        };
        var sut = new WalletManager(...);

        // Act & Assert
        await Assert.ThrowsAsync<WalletException>(
            () => sut.RecoverWalletAsync(request));
    }

    [Fact]
    public async Task GetWallet_WithoutAccess_ThrowsWalletException()
    {
        // Arrange
        var mockDelegation = new Mock<IDelegationManager>();
        mockDelegation.Setup(x => x.CanAccessAsync(...)).ReturnsAsync(false);
        var sut = new WalletManager(...);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<WalletException>(
            () => sut.GetWalletAsync("address", "unauthorized-user"));

        exception.ErrorCode.Should().Be(WalletErrorCodes.InvalidAccess);
    }

    // Add tests for:
    // - Update wallet
    // - Delete wallet (soft and permanent)
    // - Archive/restore wallet
    // - Concurrent updates (optimistic concurrency)
    // - Event publishing
    // - Transaction history retrieval with failures
}
```

## Dependencies

- NBitcoin 7.0.37 (BIP39 mnemonic generation)
- IKeyManager (from WALLET-005)
- IEncryptionProvider (from WALLET-011)
- IDelegationManager (from WALLET-007)
- IWalletRepository (from WALLET-008)
- IEventPublisher (from WALLET-016)
- IRegisterServiceClient (optional, from existing codebase)

## Notes

- Mnemonics are NEVER stored in the database - user responsibility
- Private keys are always encrypted before storage
- All operations must be tenant-isolated
- Event publishing should not block main operation (fire-and-forget)
- Transaction history retrieval is optional and should degrade gracefully

## Next Steps

After completing this task:
1. Implement WALLET-005 (KeyManager)
2. Implement WALLET-007 (DelegationManager)
3. Write integration tests with real database
