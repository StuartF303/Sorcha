namespace Sorcha.Cryptography.Enums;

/// <summary>
/// Supported symmetric encryption algorithms.
/// </summary>
public enum EncryptionType
{
    /// <summary>
    /// AES-128 in CBC mode.
    /// </summary>
    AES_128,

    /// <summary>
    /// AES-256 in CBC mode.
    /// </summary>
    AES_256,

    /// <summary>
    /// AES-256 in GCM mode (authenticated encryption).
    /// </summary>
    AES_GCM,

    /// <summary>
    /// ChaCha20-Poly1305 (authenticated encryption).
    /// </summary>
    CHACHA20_POLY1305,

    /// <summary>
    /// XChaCha20-Poly1305 (authenticated encryption with extended nonce).
    /// </summary>
    XCHACHA20_POLY1305
}
