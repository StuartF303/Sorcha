namespace Sorcha.Cryptography.Enums;

/// <summary>
/// Status codes for cryptographic operations.
/// </summary>
public enum CryptoStatus
{
    /// <summary>
    /// Operation completed successfully.
    /// </summary>
    Success = 0,

    // Key Management Errors (100-199)
    /// <summary>
    /// Failed to generate key pair.
    /// </summary>
    KeyGenerationFailed = 100,

    /// <summary>
    /// Invalid or corrupted mnemonic phrase.
    /// </summary>
    InvalidMnemonic = 101,

    /// <summary>
    /// Incorrect password provided.
    /// </summary>
    InvalidPassword = 102,

    /// <summary>
    /// Invalid cryptographic key.
    /// </summary>
    InvalidKey = 103,

    /// <summary>
    /// Unknown or missing key ring.
    /// </summary>
    UnknownKeyRing = 104,

    /// <summary>
    /// Key ring already exists.
    /// </summary>
    DuplicateKeyRing = 105,

    /// <summary>
    /// Key chain is empty.
    /// </summary>
    EmptyKeyChain = 106,

    // Cryptographic Operation Errors (200-299)
    /// <summary>
    /// Signature creation failed.
    /// </summary>
    SigningFailed = 200,

    /// <summary>
    /// Signature verification failed.
    /// </summary>
    InvalidSignature = 201,

    /// <summary>
    /// Encryption operation failed.
    /// </summary>
    EncryptionFailed = 202,

    /// <summary>
    /// Decryption operation failed.
    /// </summary>
    DecryptionFailed = 203,

    /// <summary>
    /// Hash computation failed.
    /// </summary>
    HashingFailed = 204,

    // General Errors (900-999)
    /// <summary>
    /// Invalid input parameter provided.
    /// </summary>
    InvalidParameter = 900,

    /// <summary>
    /// Operation was cancelled.
    /// </summary>
    Cancelled = 901,

    /// <summary>
    /// Unexpected error occurred.
    /// </summary>
    UnexpectedError = 999
}
