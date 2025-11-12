namespace Sorcha.Cryptography.Models;

/// <summary>
/// Represents a public/private key pair.
/// </summary>
public struct KeySet
{
    /// <summary>
    /// Gets or sets the private key.
    /// </summary>
    public CryptoKey PrivateKey { get; set; }

    /// <summary>
    /// Gets or sets the public key.
    /// </summary>
    public CryptoKey PublicKey { get; set; }

    /// <summary>
    /// Zeroes out sensitive key material.
    /// </summary>
    public void Zeroize()
    {
        PrivateKey.Zeroize();
        PublicKey.Zeroize();
    }
}
