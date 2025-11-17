namespace Sorcha.Wallet.Service.Models;

/// <summary>
/// Request model for generating a new address
/// </summary>
public class GenerateAddressRequest
{
    /// <summary>
    /// Optional derivation path (BIP44 format)
    /// </summary>
    public string? DerivationPath { get; set; }
}
