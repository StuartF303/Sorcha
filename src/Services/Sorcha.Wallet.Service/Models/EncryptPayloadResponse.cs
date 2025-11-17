namespace Sorcha.Wallet.Service.Models;

/// <summary>
/// Response model for payload encryption
/// </summary>
public class EncryptPayloadResponse
{
    /// <summary>
    /// Base64-encoded encrypted payload
    /// </summary>
    public required string EncryptedPayload { get; set; }

    /// <summary>
    /// Address of the recipient wallet
    /// </summary>
    public required string RecipientAddress { get; set; }

    /// <summary>
    /// Timestamp when encryption occurred
    /// </summary>
    public DateTime EncryptedAt { get; set; }
}
