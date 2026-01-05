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

    /// <summary>
    /// Optional key derivation path for signing
    /// </summary>
    /// <remarks>
    /// Can be a custom path (e.g., "m/44'/0'/0'/0/5") or a Sorcha system path (e.g., "sorcha:register-attestation").
    /// If not specified, the wallet's default signing key is used.
    /// </remarks>
    public string? DerivationPath { get; set; }
}
