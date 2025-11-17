using System.ComponentModel.DataAnnotations;

namespace Sorcha.WalletService.Api.Models;

/// <summary>
/// Request model for signing a transaction
/// </summary>
public class SignTransactionRequest
{
    /// <summary>
    /// Transaction data to sign (base64 encoded)
    /// </summary>
    [Required]
    public required string TransactionData { get; set; }
}

/// <summary>
/// Response model for signed transaction
/// </summary>
public class SignTransactionResponse
{
    /// <summary>
    /// Digital signature (base64 encoded)
    /// </summary>
    public required string Signature { get; set; }

    /// <summary>
    /// Wallet address that signed the transaction
    /// </summary>
    public required string SignedBy { get; set; }

    /// <summary>
    /// Timestamp of signing
    /// </summary>
    public DateTime SignedAt { get; set; }
}
