namespace Sorcha.WalletService.Domain.Events;

/// <summary>
/// Base class for wallet domain events
/// </summary>
public abstract record WalletEvent
{
    /// <summary>
    /// Event identifier
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Wallet address this event relates to
    /// </summary>
    public required string WalletAddress { get; init; }

    /// <summary>
    /// Timestamp when event occurred
    /// </summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Subject who triggered the event
    /// </summary>
    public string? TriggeredBy { get; init; }
}

/// <summary>
/// Event raised when a new wallet is created
/// </summary>
public record WalletCreatedEvent : WalletEvent
{
    public required string Owner { get; init; }
    public required string Tenant { get; init; }
    public required string Algorithm { get; init; }
    public required string Name { get; init; }
}

/// <summary>
/// Event raised when a wallet is recovered from a mnemonic
/// </summary>
public record WalletRecoveredEvent : WalletEvent
{
    public required string Owner { get; init; }
    public required string Tenant { get; init; }
    public required string Algorithm { get; init; }
}

/// <summary>
/// Event raised when a new address is generated
/// </summary>
public record AddressGeneratedEvent : WalletEvent
{
    public required string Address { get; init; }
    public required string DerivationPath { get; init; }
    public int Index { get; init; }
}

/// <summary>
/// Event raised when a transaction is signed
/// </summary>
public record TransactionSignedEvent : WalletEvent
{
    public required string TransactionId { get; init; }
    public required string SignedBy { get; init; }
}

/// <summary>
/// Event raised when access is granted to a wallet
/// </summary>
public record DelegateAddedEvent : WalletEvent
{
    public required string Subject { get; init; }
    public required AccessRight AccessRight { get; init; }
    public required string GrantedBy { get; init; }
}

/// <summary>
/// Event raised when access is revoked
/// </summary>
public record DelegateRemovedEvent : WalletEvent
{
    public required string Subject { get; init; }
    public required string RevokedBy { get; init; }
}

/// <summary>
/// Event raised when wallet status changes
/// </summary>
public record WalletStatusChangedEvent : WalletEvent
{
    public required WalletStatus OldStatus { get; init; }
    public required WalletStatus NewStatus { get; init; }
}

/// <summary>
/// Event raised when encryption key is rotated
/// </summary>
public record KeyRotatedEvent : WalletEvent
{
    public required string OldKeyId { get; init; }
    public required string NewKeyId { get; init; }
}
