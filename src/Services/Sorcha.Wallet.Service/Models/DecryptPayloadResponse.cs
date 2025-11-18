namespace Sorcha.Wallet.Service.Models;

/// <summary>
/// Response model for payload decryption
/// </summary>
public class DecryptPayloadResponse
{
    /// <summary>
    /// Base64-encoded decrypted payload
    /// </summary>
    public required string DecryptedPayload { get; set; }

    /// <summary>
    /// Address of the wallet that decrypted the payload
    /// </summary>
    public required string DecryptedBy { get; set; }

    /// <summary>
    /// Timestamp when decryption occurred
    /// </summary>
    public DateTime DecryptedAt { get; set; }
}
