using System;
using System.Threading;
using System.Threading.Tasks;
using Sorcha.TransactionHandler.Enums;

namespace Sorcha.TransactionHandler.Interfaces;

/// <summary>
/// Represents a transaction in the Sorcha platform.
/// </summary>
public interface ITransaction
{
    /// <summary>
    /// Gets the transaction identifier (hash).
    /// </summary>
    string? TxId { get; }

    /// <summary>
    /// Gets the transaction version.
    /// </summary>
    TransactionVersion Version { get; }

    /// <summary>
    /// Gets the hash of the previous transaction in the chain.
    /// </summary>
    string? PreviousTxHash { get; }

    /// <summary>
    /// Gets the sender's wallet address.
    /// </summary>
    string? SenderWallet { get; }

    /// <summary>
    /// Gets the recipient wallet addresses.
    /// </summary>
    string[]? Recipients { get; }

    /// <summary>
    /// Gets the JSON metadata associated with the transaction.
    /// </summary>
    string? Metadata { get; }

    /// <summary>
    /// Gets the transaction timestamp.
    /// </summary>
    DateTime? Timestamp { get; }

    /// <summary>
    /// Gets the transaction signature.
    /// </summary>
    byte[]? Signature { get; }

    /// <summary>
    /// Gets the payload manager for this transaction.
    /// </summary>
    IPayloadManager PayloadManager { get; }

    /// <summary>
    /// Signs the transaction with the sender's private key.
    /// </summary>
    /// <param name="wifPrivateKey">The WIF-encoded private key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The status of the signing operation</returns>
    Task<TransactionStatus> SignAsync(
        string wifPrivateKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the transaction signature and payloads.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The status of the verification</returns>
    Task<TransactionStatus> VerifyAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes the transaction to binary format.
    /// </summary>
    /// <returns>The binary representation</returns>
    byte[] SerializeToBinary();

    /// <summary>
    /// Serializes the transaction to JSON format.
    /// </summary>
    /// <returns>The JSON representation</returns>
    string SerializeToJson();
}
