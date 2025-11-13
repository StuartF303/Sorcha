using System;
using Sorcha.TransactionHandler.Enums;
using Sorcha.Cryptography.Enums;

namespace Sorcha.TransactionHandler.Models;

/// <summary>
/// Represents information about a payload.
/// </summary>
public sealed class PayloadInfo
{
    /// <summary>
    /// Gets the payload identifier.
    /// </summary>
    public uint Id { get; init; }

    /// <summary>
    /// Gets the payload type.
    /// </summary>
    public PayloadType Type { get; init; }

    /// <summary>
    /// Gets the original size of the payload data in bytes.
    /// </summary>
    public long OriginalSize { get; init; }

    /// <summary>
    /// Gets the compressed size of the payload data in bytes.
    /// </summary>
    public long CompressedSize { get; init; }

    /// <summary>
    /// Gets a value indicating whether the payload is compressed.
    /// </summary>
    public bool IsCompressed { get; init; }

    /// <summary>
    /// Gets a value indicating whether the payload is encrypted.
    /// </summary>
    public bool IsEncrypted { get; init; }

    /// <summary>
    /// Gets the encryption type used for the payload.
    /// </summary>
    public EncryptionType EncryptionType { get; init; }

    /// <summary>
    /// Gets the hash type used for payload verification.
    /// </summary>
    public HashType HashType { get; init; }

    /// <summary>
    /// Gets the wallet addresses that can access this payload.
    /// </summary>
    public string[] AccessibleBy { get; init; } = Array.Empty<string>();
}
