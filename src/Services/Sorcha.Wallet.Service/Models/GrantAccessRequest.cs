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
