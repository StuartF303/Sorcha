using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sorcha.TransactionHandler.Enums;
using Sorcha.TransactionHandler.Interfaces;
using Sorcha.Cryptography.Interfaces;
using Sorcha.Cryptography.Enums;

namespace Sorcha.TransactionHandler.Core;

/// <summary>
/// Represents a transaction in the Siccar platform.
/// </summary>
public class Transaction : ITransaction
{
    private readonly ICryptoModule _cryptoModule;
    private readonly IHashProvider _hashProvider;
    private bool _isSigned = false;

    /// <summary>
    /// Initializes a new instance of the Transaction class.
    /// </summary>
    /// <param name="cryptoModule">The cryptography module</param>
    /// <param name="hashProvider">The hash provider</param>
    /// <param name="payloadManager">The payload manager</param>
    /// <param name="version">The transaction version</param>
    public Transaction(
        ICryptoModule cryptoModule,
        IHashProvider hashProvider,
        IPayloadManager payloadManager,
        TransactionVersion version = TransactionVersion.V4)
    {
        _cryptoModule = cryptoModule ?? throw new ArgumentNullException(nameof(cryptoModule));
        _hashProvider = hashProvider ?? throw new ArgumentNullException(nameof(hashProvider));
        PayloadManager = payloadManager ?? throw new ArgumentNullException(nameof(payloadManager));
        Version = version;
        Timestamp = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public string? TxId { get; private set; }

    /// <inheritdoc/>
    public TransactionVersion Version { get; }

    /// <inheritdoc/>
    public string? PreviousTxHash { get; set; }

    /// <inheritdoc/>
    public string? SenderWallet { get; private set; }

    /// <inheritdoc/>
    public string[]? Recipients { get; set; }

    /// <inheritdoc/>
    public string? Metadata { get; set; }

    /// <inheritdoc/>
    public DateTime? Timestamp { get; set; }

    /// <inheritdoc/>
    public byte[]? Signature { get; private set; }

    /// <inheritdoc/>
    public IPayloadManager PayloadManager { get; }

    /// <inheritdoc/>
    public async Task<TransactionStatus> SignAsync(
        string wifPrivateKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(wifPrivateKey))
            return TransactionStatus.InvalidSignature;

        try
        {
            // Compute signing hash (double SHA-256)
            var signingData = SerializeForSigning();
            var hash1 = _hashProvider.ComputeHash(signingData, HashType.SHA256);
            var hash2 = _hashProvider.ComputeHash(hash1, HashType.SHA256);

            // TODO: Extract network and key from WIF
            // For now, use ED25519
            var network = WalletNetworks.ED25519;

            // Sign the hash
            var signResult = await _cryptoModule.SignAsync(
                hash2,
                (byte)network,
                Convert.FromBase64String(wifPrivateKey), // Simplified - should decode WIF properly
                cancellationToken);

            if (!signResult.IsSuccess || signResult.Value == null)
                return TransactionStatus.InvalidSignature;

            Signature = signResult.Value;

            // TODO: Calculate sender wallet from private key
            SenderWallet = "ws1temp"; // Placeholder

            // Calculate transaction ID
            TxId = Convert.ToBase64String(_hashProvider.ComputeHash(
                Signature,
                HashType.SHA256));

            _isSigned = true;
            return TransactionStatus.Success;
        }
        catch
        {
            return TransactionStatus.InvalidSignature;
        }
    }

    /// <inheritdoc/>
    public async Task<TransactionStatus> VerifyAsync(
        CancellationToken cancellationToken = default)
    {
        if (Signature == null || string.IsNullOrEmpty(SenderWallet))
            return TransactionStatus.NotSigned;

        try
        {
            // Compute signing hash
            var signingData = SerializeForSigning();
            var hash1 = _hashProvider.ComputeHash(signingData, HashType.SHA256);
            var hash2 = _hashProvider.ComputeHash(hash1, HashType.SHA256);

            // TODO: Extract public key from wallet address
            // For now, return success as placeholder

            // Verify payloads
            var payloadsValid = await PayloadManager.VerifyAllAsync();
            if (!payloadsValid)
                return TransactionStatus.InvalidPayload;

            return TransactionStatus.Success;
        }
        catch
        {
            return TransactionStatus.InvalidSignature;
        }
    }

    /// <inheritdoc/>
    public byte[] SerializeToBinary()
    {
        // TODO: Implement binary serialization
        throw new NotImplementedException("Binary serialization not yet implemented");
    }

    /// <inheritdoc/>
    public string SerializeToJson()
    {
        var obj = new
        {
            txId = TxId,
            version = (uint)Version,
            timestamp = Timestamp,
            previousTxHash = PreviousTxHash,
            senderWallet = SenderWallet,
            recipients = Recipients,
            metadata = Metadata != null ? JsonSerializer.Deserialize<object>(Metadata) : null,
            signature = Signature != null ? Convert.ToBase64String(Signature) : null,
            payloadCount = PayloadManager.Count
        };

        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Serializes transaction data for signing.
    /// </summary>
    private byte[] SerializeForSigning()
    {
        // Simplified serialization - should match spec
        var json = new
        {
            version = (uint)Version,
            timestamp = Timestamp,
            previousTxHash = PreviousTxHash ?? "",
            recipients = Recipients ?? Array.Empty<string>(),
            metadata = Metadata ?? "",
            payloadCount = PayloadManager.Count
        };

        var jsonString = JsonSerializer.Serialize(json);
        return System.Text.Encoding.UTF8.GetBytes(jsonString);
    }
}
