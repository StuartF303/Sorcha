using System.ComponentModel.DataAnnotations;

namespace Sorcha.WalletService.Api.Models;

/// <summary>
/// Request model for encrypting a payload
/// </summary>
public class EncryptPayloadRequest
{
    /// <summary>
    /// Base64-encoded payload to encrypt
    /// </summary>
    [Required]
    public required string Payload { get; set; }

    /// <summary>
    /// Optional recipient address (if not using route parameter)
    /// </summary>
    public string? RecipientAddress { get; set; }
}
