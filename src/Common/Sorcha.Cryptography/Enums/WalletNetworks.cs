namespace Sorcha.Cryptography.Enums;

/// <summary>
/// Supported cryptographic networks/algorithms for wallet operations.
/// </summary>
public enum WalletNetworks : byte
{
    /// <summary>
    /// Ed25519 elliptic curve signature algorithm.
    /// Public key size: 32 bytes, Private key size: 64 bytes.
    /// </summary>
    ED25519 = 0x00,

    /// <summary>
    /// NIST P-256 elliptic curve (secp256r1).
    /// Public key size: 64 bytes (X,Y coordinates), Private key size: 32 bytes.
    /// </summary>
    NISTP256 = 0x01,

    /// <summary>
    /// RSA 4096-bit key.
    /// Public key size: variable (ASN.1 DER), Private key size: variable (ASN.1 DER).
    /// </summary>
    RSA4096 = 0x02
}
