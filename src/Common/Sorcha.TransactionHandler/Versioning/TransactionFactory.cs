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
    private readonly IVersionDetector _versionDetector;

    /// <summary>
    /// Initializes a new instance of the TransactionFactory class.
    /// </summary>
    public TransactionFactory(
        ICryptoModule cryptoModule,
        IHashProvider hashProvider,
        IVersionDetector versionDetector)
    {
        _cryptoModule = cryptoModule ?? throw new ArgumentNullException(nameof(cryptoModule));
        _hashProvider = hashProvider ?? throw new ArgumentNullException(nameof(hashProvider));
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
        var payloadManager = new PayloadManager();
        return new Transaction(_cryptoModule, _hashProvider, payloadManager, TransactionVersion.V4);
    }

    private ITransaction CreateV3Transaction()
    {
        // TODO: Implement V3 adapter for backward compatibility
        // For now, create a V4 transaction with V3 version marker
        var payloadManager = new PayloadManager();
        return new Transaction(_cryptoModule, _hashProvider, payloadManager, TransactionVersion.V3);
    }

    private ITransaction CreateV2Transaction()
    {
        // TODO: Implement V2 adapter for backward compatibility
        // For now, create a V4 transaction with V2 version marker
        var payloadManager = new PayloadManager();
        return new Transaction(_cryptoModule, _hashProvider, payloadManager, TransactionVersion.V2);
    }

    private ITransaction CreateV1Transaction()
    {
        // TODO: Implement V1 adapter for backward compatibility
        // For now, create a V4 transaction with V1 version marker
        var payloadManager = new PayloadManager();
        return new Transaction(_cryptoModule, _hashProvider, payloadManager, TransactionVersion.V1);
    }

    private ITransactionSerializer GetSerializer(TransactionVersion version)
    {
        // For now, use the same serializer for all versions
        // TODO: Implement version-specific serializers for true backward compatibility
        return version switch
        {
            TransactionVersion.V4 => new BinaryTransactionSerializer(_cryptoModule, _hashProvider),
            TransactionVersion.V3 => new BinaryTransactionSerializer(_cryptoModule, _hashProvider),
            TransactionVersion.V2 => new BinaryTransactionSerializer(_cryptoModule, _hashProvider),
            TransactionVersion.V1 => new BinaryTransactionSerializer(_cryptoModule, _hashProvider),
            _ => throw new NotSupportedException($"No serializer available for version {version}")
        };
    }

    #endregion
}
