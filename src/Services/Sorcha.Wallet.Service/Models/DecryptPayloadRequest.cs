using System.ComponentModel.DataAnnotations;

namespace Sorcha.Wallet.Service.Models;

/// <summary>
/// Request model for decrypting a payload
/// </summary>
public class DecryptPayloadRequest
{
    /// <summary>
    /// Base64-encoded encrypted payload
    /// </summary>
    [Required]
    public required string EncryptedPayload { get; set; }
}
