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
    RSA4096 = 0x02,

    // --- Post-Quantum Cryptography (PQC) algorithms ---
    // Gap 0x03-0x0F reserved for future classical algorithms.

    /// <summary>
    /// ML-DSA-65 (FIPS 204) lattice-based digital signature.
    /// Security level 3 (192-bit). CNSA 2.0 compliant.
    /// Public key size: 1,952 bytes, Private key size: 4,032 bytes, Signature size: 3,309 bytes.
    /// </summary>
    ML_DSA_65 = 0x10,

    /// <summary>
    /// SLH-DSA-128s (FIPS 205) hash-based digital signature (SPHINCS+).
    /// Security level 1 (128-bit). Stateless, hash-based — mathematically independent of lattice assumptions.
    /// Public key size: 32 bytes, Private key size: 64 bytes, Signature size: 7,856 bytes.
    /// </summary>
    SLH_DSA_128s = 0x11,

    /// <summary>
    /// ML-KEM-768 (FIPS 203) lattice-based key encapsulation mechanism.
    /// Security level 3 (192-bit). CNSA 2.0 compliant.
    /// Public key size: 1,184 bytes, Private key size: 2,400 bytes, Ciphertext size: 1,088 bytes.
    /// </summary>
    ML_KEM_768 = 0x12
}
