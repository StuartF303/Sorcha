using System.ComponentModel.DataAnnotations;

namespace Sorcha.Wallet.Service.Models;

/// <summary>
/// Request model for granting wallet access
/// </summary>
public class GrantAccessRequest
{
    /// <summary>
    /// Subject identifier (user/service principal)
    /// </summary>
    [Required]
    public required string Subject { get; set; }

    /// <summary>
    /// Access right to grant (Owner, ReadWrite, ReadOnly)
    /// </summary>
    [Required]
    public required string AccessRight { get; set; }

    /// <summary>
    /// Optional reason for granting access
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Optional expiration date/time
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Response model for wallet access
/// </summary>
public class WalletAccessDto
{
    /// <summary>
    /// Access entry identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Subject identifier
    /// </summary>
    public required string Subject { get; set; }

    /// <summary>
    /// Access right granted
    /// </summary>
    public required string AccessRight { get; set; }

    /// <summary>
    /// Granted by identifier
    /// </summary>
    public required string GrantedBy { get; set; }

    /// <summary>
    /// Grant timestamp
    /// </summary>
    public DateTime GrantedAt { get; set; }

    /// <summary>
    /// Optional expiration timestamp
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Is access currently active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Optional reason
    /// </summary>
    public string? Reason { get; set; }
}
