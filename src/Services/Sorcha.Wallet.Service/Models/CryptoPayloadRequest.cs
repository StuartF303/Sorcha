using System.ComponentModel.DataAnnotations;

namespace Sorcha.Wallet.Service.Models;

/// <summary>
/// Request model for decrypting a payload
/// </summary>
public class DecryptPayloadRequest
{
    /// <summary>
    /// Encrypted payload data (base64 encoded)
    /// </summary>
    [Required]
    public required string EncryptedPayload { get; set; }
}

/// <summary>
/// Response model for decrypted payload
/// </summary>
public class DecryptPayloadResponse
{
    /// <summary>
    /// Decrypted payload data (base64 encoded)
    /// </summary>
    public required string DecryptedPayload { get; set; }

    /// <summary>
    /// Wallet address that decrypted the payload
    /// </summary>
    public required string DecryptedBy { get; set; }

    /// <summary>
    /// Timestamp of decryption
    /// </summary>
    public DateTime DecryptedAt { get; set; }
}

/// <summary>
/// Request model for encrypting a payload
/// </summary>
public class EncryptPayloadRequest
{
    /// <summary>
    /// Payload data to encrypt (base64 encoded)
    /// </summary>
    [Required]
    public required string Payload { get; set; }

    /// <summary>
    /// Optional recipient wallet address (if encrypting for another wallet)
    /// If not provided, encrypts for the wallet specified in the route
    /// </summary>
    public string? RecipientAddress { get; set; }
}

/// <summary>
/// Response model for encrypted payload
/// </summary>
public class EncryptPayloadResponse
{
    /// <summary>
    /// Encrypted payload data (base64 encoded)
    /// </summary>
    public required string EncryptedPayload { get; set; }

    /// <summary>
    /// Wallet address that can decrypt this payload
    /// </summary>
    public required string RecipientAddress { get; set; }

    /// <summary>
    /// Timestamp of encryption
    /// </summary>
    public DateTime EncryptedAt { get; set; }
}

/// <summary>
/// Request model for generating a new address
/// </summary>
public class GenerateAddressRequest
{
    /// <summary>
    /// BIP44 derivation path (e.g., "m/44'/0'/0'/0/0")
    /// If not provided, uses next available index
    /// </summary>
    public string? DerivationPath { get; set; }

    /// <summary>
    /// Optional label for the address
    /// </summary>
    public string? Label { get; set; }
}

/// <summary>
/// Response model for generated address
/// </summary>
public class GenerateAddressResponse
{
    /// <summary>
    /// Generated address string
    /// </summary>
    public required string Address { get; set; }

    /// <summary>
    /// BIP44 derivation path used
    /// </summary>
    public required string DerivationPath { get; set; }

    /// <summary>
    /// Public key (hex encoded)
    /// </summary>
    public required string PublicKey { get; set; }

    /// <summary>
    /// Optional label
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Timestamp of generation
    /// </summary>
    public DateTime GeneratedAt { get; set; }
}
