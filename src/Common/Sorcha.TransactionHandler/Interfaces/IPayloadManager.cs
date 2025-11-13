using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sorcha.TransactionHandler.Enums;
using Sorcha.TransactionHandler.Models;

namespace Sorcha.TransactionHandler.Interfaces;

/// <summary>
/// Manages payload operations including encryption, decryption, and access control.
/// </summary>
public interface IPayloadManager
{
    /// <summary>
    /// Gets the number of payloads.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets all payloads.
    /// </summary>
    Task<IEnumerable<IPayload>> GetAllAsync();

    /// <summary>
    /// Gets payloads accessible to the specified wallet.
    /// </summary>
    /// <param name="walletAddress">The wallet address</param>
    /// <param name="wifPrivateKey">The WIF private key</param>
    /// <returns>Accessible payloads</returns>
    Task<IEnumerable<IPayload>> GetAccessibleAsync(
        string walletAddress,
        string wifPrivateKey);

    /// <summary>
    /// Adds a new encrypted payload.
    /// </summary>
    /// <param name="data">The payload data</param>
    /// <param name="recipientWallets">Recipient wallet addresses</param>
    /// <param name="options">Optional payload options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the operation</returns>
    Task<PayloadResult> AddPayloadAsync(
        byte[] data,
        string[] recipientWallets,
        PayloadOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets and decrypts payload data.
    /// </summary>
    /// <param name="payloadId">The payload ID</param>
    /// <param name="wifPrivateKey">The WIF private key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The decrypted payload data</returns>
    Task<PayloadResult<byte[]>> GetPayloadDataAsync(
        uint payloadId,
        string wifPrivateKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a payload.
    /// </summary>
    /// <param name="payloadId">The payload ID</param>
    /// <returns>The status of the operation</returns>
    Task<TransactionStatus> RemovePayloadAsync(uint payloadId);

    /// <summary>
    /// Grants access to a payload for an additional recipient.
    /// </summary>
    /// <param name="payloadId">The payload ID</param>
    /// <param name="recipientWallet">The recipient wallet address</param>
    /// <param name="ownerWifKey">The owner's WIF private key</param>
    /// <returns>The status of the operation</returns>
    Task<TransactionStatus> GrantAccessAsync(
        uint payloadId,
        string recipientWallet,
        string ownerWifKey);

    /// <summary>
    /// Revokes access to a payload for a recipient.
    /// </summary>
    /// <param name="payloadId">The payload ID</param>
    /// <param name="recipientWallet">The recipient wallet address</param>
    /// <returns>The status of the operation</returns>
    Task<TransactionStatus> RevokeAccessAsync(
        uint payloadId,
        string recipientWallet);

    /// <summary>
    /// Verifies all payloads.
    /// </summary>
    /// <returns>True if all payloads are valid</returns>
    Task<bool> VerifyAllAsync();

    /// <summary>
    /// Verifies a specific payload.
    /// </summary>
    /// <param name="payloadId">The payload ID</param>
    /// <returns>True if the payload is valid</returns>
    Task<bool> VerifyPayloadAsync(uint payloadId);
}

/// <summary>
/// Represents a payload with encrypted data.
/// </summary>
public interface IPayload
{
    /// <summary>
    /// Gets the payload identifier.
    /// </summary>
    uint Id { get; }

    /// <summary>
    /// Gets the payload type.
    /// </summary>
    PayloadType Type { get; }

    /// <summary>
    /// Gets the encrypted data.
    /// </summary>
    byte[] Data { get; }

    /// <summary>
    /// Gets the initialization vector.
    /// </summary>
    byte[] IV { get; }

    /// <summary>
    /// Gets the hash of the original data.
    /// </summary>
    byte[] Hash { get; }

    /// <summary>
    /// Gets whether the payload is compressed.
    /// </summary>
    bool IsCompressed { get; }

    /// <summary>
    /// Gets the original size of the data.
    /// </summary>
    long OriginalSize { get; }

    /// <summary>
    /// Gets information about this payload.
    /// </summary>
    PayloadInfo GetInfo();
}

/// <summary>
/// Options for payload encryption and compression.
/// </summary>
public class PayloadOptions
{
    /// <summary>
    /// Gets or sets the encryption type.
    /// </summary>
    public Sorcha.Cryptography.Enums.EncryptionType EncryptionType { get; set; }
        = Sorcha.Cryptography.Enums.EncryptionType.XCHACHA20_POLY1305;

    /// <summary>
    /// Gets or sets the compression type.
    /// </summary>
    public Sorcha.Cryptography.Enums.CompressionType CompressionType { get; set; }
        = Sorcha.Cryptography.Enums.CompressionType.Balanced;

    /// <summary>
    /// Gets or sets the hash type.
    /// </summary>
    public Sorcha.Cryptography.Enums.HashType HashType { get; set; }
        = Sorcha.Cryptography.Enums.HashType.SHA256;

    /// <summary>
    /// Gets or sets the payload type.
    /// </summary>
    public PayloadType PayloadType { get; set; }
        = PayloadType.Data;

    /// <summary>
    /// Gets or sets whether the payload is protected.
    /// </summary>
    public bool IsProtected { get; set; } = false;

    /// <summary>
    /// Gets or sets user-defined flags.
    /// </summary>
    public uint UserFlags { get; set; } = 0;
}
