namespace Sorcha.WalletService.Domain.Entities;

/// <summary>
/// Represents a derived address from an HD wallet
/// </summary>
public class WalletAddress
{
    /// <summary>
    /// Unique identifier for this address
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Parent wallet address
    /// </summary>
    public required string ParentWalletAddress { get; set; }

    /// <summary>
    /// The derived public address
    /// </summary>
    public required string Address { get; set; }

    /// <summary>
    /// BIP44 derivation path (e.g., m/44'/0'/0'/0/0)
    /// </summary>
    public required string DerivationPath { get; set; }

    /// <summary>
    /// Index in the derivation sequence
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Is this a change address? (BIP44 change parameter)
    /// </summary>
    public bool IsChange { get; set; }

    /// <summary>
    /// Optional label for this address
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Whether this address has been used in a transaction
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Timestamp when address was generated
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to parent wallet
    /// </summary>
    public Wallet? Wallet { get; set; }
}
