namespace Sorcha.Wallet.Service.Models;

/// <summary>
/// Response model for address generation
/// </summary>
public class GenerateAddressResponse
{
    /// <summary>
    /// Generated address
    /// </summary>
    public required string Address { get; set; }

    /// <summary>
    /// Derivation path used
    /// </summary>
    public required string DerivationPath { get; set; }

    /// <summary>
    /// Public key for the address
    /// </summary>
    public required string PublicKey { get; set; }
}
