using System.ComponentModel.DataAnnotations;

namespace Sorcha.Wallet.Service.Models;

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
