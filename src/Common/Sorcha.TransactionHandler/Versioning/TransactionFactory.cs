using System;
using Sorcha.TransactionHandler.Core;
using Sorcha.TransactionHandler.Enums;
using Sorcha.TransactionHandler.Interfaces;
using Sorcha.TransactionHandler.Payload;
using Sorcha.TransactionHandler.Serialization;
using Sorcha.Cryptography.Interfaces;

namespace Sorcha.TransactionHandler.Versioning;

/// <summary>
/// Factory for creating and deserializing transactions with version support.
/// </summary>
public class TransactionFactory : ITransactionFactory
{
    private readonly ICryptoModule _cryptoModule;
    private readonly IHashProvider _hashProvider;
    private readonly ISymmetricCrypto _symmetricCrypto;
    private readonly IVersionDetector _versionDetector;

    /// <summary>
    /// Initializes a new instance of the TransactionFactory class.
    /// </summary>
    public TransactionFactory(
        ICryptoModule cryptoModule,
        IHashProvider hashProvider,
        ISymmetricCrypto symmetricCrypto,
        IVersionDetector versionDetector)
    {
        _cryptoModule = cryptoModule ?? throw new ArgumentNullException(nameof(cryptoModule));
        _hashProvider = hashProvider ?? throw new ArgumentNullException(nameof(hashProvider));
        _symmetricCrypto = symmetricCrypto ?? throw new ArgumentNullException(nameof(symmetricCrypto));
        _versionDetector = versionDetector ?? throw new ArgumentNullException(nameof(versionDetector));
    }

    /// <inheritdoc/>
    public ITransaction Create(TransactionVersion version)
    {
        return version switch
        {
            TransactionVersion.V4 => CreateV4Transaction(),
            TransactionVersion.V3 => CreateV3Transaction(),
            TransactionVersion.V2 => CreateV2Transaction(),
            TransactionVersion.V1 => CreateV1Transaction(),
            _ => throw new NotSupportedException($"Transaction version {version} is not supported")
        };
    }

    /// <inheritdoc/>
    public ITransaction Deserialize(byte[] data)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentNullException(nameof(data));

        // Detect version from binary data
        var version = _versionDetector.DetectVersion(data);

        // Get appropriate serializer for the version
        var serializer = GetSerializer(version);

        // Deserialize using version-specific serializer
        return serializer.DeserializeFromBinary(data);
    }

    /// <inheritdoc/>
    public ITransaction Deserialize(string json)
    {
        if (string.IsNullOrEmpty(json))
            throw new ArgumentNullException(nameof(json));

        // Detect version from JSON
        var version = _versionDetector.DetectVersion(json);

        // Get appropriate serializer for the version
        var serializer = GetSerializer(version);

        // Deserialize using version-specific serializer
        return serializer.DeserializeFromJson(json);
    }

    #region Private Helper Methods

    private ITransaction CreateV4Transaction()
    {
        var payloadManager = new PayloadManager(_symmetricCrypto, _cryptoModule, _hashProvider);
        return new Transaction(_cryptoModule, _hashProvider, payloadManager, TransactionVersion.V4);
    }

    private ITransaction CreateV3Transaction()
    {
        // V3→V4 adapter: V3 is structurally identical to V4, only version marker differs.
        // All V4 fields are present; no field defaults needed.
        var payloadManager = new PayloadManager(_symmetricCrypto, _cryptoModule, _hashProvider);
        return new Transaction(_cryptoModule, _hashProvider, payloadManager, TransactionVersion.V3);
    }

    private ITransaction CreateV2Transaction()
    {
        // V2→V4 adapter: V2 transactions lack metadata support.
        // Create V4-compatible transaction with V2 marker; metadata defaults to null.
        var payloadManager = new PayloadManager(_symmetricCrypto, _cryptoModule, _hashProvider);
        return new Transaction(_cryptoModule, _hashProvider, payloadManager, TransactionVersion.V2);
    }

    private ITransaction CreateV1Transaction()
    {
        // V1→V4 adapter: V1 transactions lack metadata and multi-recipient support.
        // Create V4-compatible transaction with V1 marker; metadata and recipients default to null.
        var payloadManager = new PayloadManager(_symmetricCrypto, _cryptoModule, _hashProvider);
        return new Transaction(_cryptoModule, _hashProvider, payloadManager, TransactionVersion.V1);
    }

    private ITransactionSerializer GetSerializer(TransactionVersion version)
    {
        // All versions share the same binary wire format — the version field in the first 4 bytes
        // identifies the version. The BinaryTransactionSerializer reads/writes all fields regardless
        // of version; older versions simply have null/empty optional fields.
        return new BinaryTransactionSerializer(_cryptoModule, _hashProvider, _symmetricCrypto);
    }

    #endregion
}
