// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Sorcha.Cryptography.Enums;
using Sorcha.Cryptography.Interfaces;
using Sorcha.Cryptography.Models;
using Sorcha.TransactionHandler.Enums;
using Sorcha.TransactionHandler.Interfaces;
using Sorcha.TransactionHandler.Models;

namespace Sorcha.TransactionHandler.Payload;

/// <summary>
/// Manages transaction payloads with real encryption, decryption, and verification.
/// </summary>
public class PayloadManager : IPayloadManager
{
    private readonly List<Payload> _payloads = new();
    private readonly object _lock = new();
    private uint _nextPayloadId = 0;
    private readonly ISymmetricCrypto _symmetricCrypto;
    private readonly ICryptoModule _cryptoModule;
    private readonly IHashProvider _hashProvider;

    /// <summary>
    /// Initializes a new instance of the PayloadManager class with cryptographic dependencies.
    /// </summary>
    /// <param name="symmetricCrypto">Symmetric encryption provider.</param>
    /// <param name="cryptoModule">Asymmetric encryption provider for key wrapping.</param>
    /// <param name="hashProvider">Hash provider for integrity verification.</param>
    public PayloadManager(
        ISymmetricCrypto symmetricCrypto,
        ICryptoModule cryptoModule,
        IHashProvider hashProvider)
    {
        _symmetricCrypto = symmetricCrypto ?? throw new ArgumentNullException(nameof(symmetricCrypto));
        _cryptoModule = cryptoModule ?? throw new ArgumentNullException(nameof(cryptoModule));
        _hashProvider = hashProvider ?? throw new ArgumentNullException(nameof(hashProvider));
    }

    /// <inheritdoc/>
    public int Count
    {
        get { lock (_lock) { return _payloads.Count; } }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<IPayload>> GetAllAsync()
    {
        lock (_lock)
        {
            return Task.FromResult<IEnumerable<IPayload>>(_payloads.ToList());
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<IPayload>> GetAccessibleAsync(
        string walletAddress,
        string wifPrivateKey)
    {
        lock (_lock)
        {
            var accessible = _payloads
                .Where(p => p.EncryptedKeys.ContainsKey(walletAddress))
                .Cast<IPayload>()
                .ToList();

            return Task.FromResult<IEnumerable<IPayload>>(accessible);
        }
    }

    /// <inheritdoc/>
    public async Task<PayloadResult> AddPayloadAsync(
        byte[] data,
        RecipientKeyInfo[] recipients,
        PayloadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (data == null || data.Length == 0)
            return PayloadResult.Failure(
                TransactionStatus.InvalidPayload,
                "Data cannot be null or empty");

        if (recipients == null || recipients.Length == 0)
            return PayloadResult.Failure(
                TransactionStatus.InvalidRecipients,
                "At least one recipient is required");

        var opts = options ?? new PayloadOptions();

        // FR-003: Compute hash of original plaintext before encryption
        var hash = _hashProvider.ComputeHash(data, opts.HashType);

        // FR-001/FR-004: Encrypt data with a per-payload symmetric key
        var encryptResult = await _symmetricCrypto.EncryptAsync(
            data, opts.EncryptionType, null, cancellationToken);

        if (!encryptResult.IsSuccess || encryptResult.Value == null)
            return PayloadResult.Failure(
                TransactionStatus.InvalidPayload,
                $"Encryption failed: {encryptResult.ErrorMessage}");

        var ciphertext = encryptResult.Value;
        var symmetricKey = ciphertext.Key;

        try
        {
            // FR-004: Encrypt symmetric key for each recipient
            var encryptedKeys = new Dictionary<string, byte[]>();

            foreach (var recipient in recipients)
            {
                var keyResult = await _cryptoModule.EncryptAsync(
                    symmetricKey,
                    (byte)recipient.Network,
                    recipient.PublicKey,
                    cancellationToken);

                if (!keyResult.IsSuccess || keyResult.Value == null)
                    return PayloadResult.Failure(
                        TransactionStatus.InvalidRecipients,
                        $"Key encryption failed for recipient {recipient.WalletAddress}: {keyResult.ErrorMessage}");

                encryptedKeys[recipient.WalletAddress] = keyResult.Value;
            }

            // Store payload
            Payload payload;
            lock (_lock)
            {
                payload = new Payload
                {
                    Id = _nextPayloadId++,
                    Type = opts.PayloadType,
                    Data = ciphertext.Data,
                    IV = ciphertext.IV,
                    Hash = hash,
                    IsCompressed = false,
                    OriginalSize = data.Length,
                    EncryptedKeys = encryptedKeys,
                    EncryptionType = opts.EncryptionType,
                    ContentType = opts.ContentType
                };
                _payloads.Add(payload);
            }

            return PayloadResult.Success(payload.Id);
        }
        finally
        {
            // FR-013: Zeroize symmetric key material
            Array.Clear(symmetricKey, 0, symmetricKey.Length);
        }
    }

    /// <inheritdoc/>
    public Task<PayloadResult> AddPayloadAsync(
        byte[] data,
        string[] recipientWallets,
        PayloadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (data == null || data.Length == 0)
            return Task.FromResult(PayloadResult.Failure(
                TransactionStatus.InvalidPayload,
                "Data cannot be null or empty"));

        if (recipientWallets == null || recipientWallets.Length == 0)
            return Task.FromResult(PayloadResult.Failure(
                TransactionStatus.InvalidRecipients,
                "At least one recipient is required"));

        // Legacy string-based overload — stores unencrypted for backward compat
        // Callers should migrate to the RecipientKeyInfo overload for real encryption
        var opts = options ?? new PayloadOptions();

        Payload payload;
        lock (_lock)
        {
            payload = new Payload
            {
                Id = _nextPayloadId++,
                Type = opts.PayloadType,
                Data = data,
                IV = new byte[12],
                Hash = new byte[32],
                IsCompressed = false,
                OriginalSize = data.Length,
                EncryptedKeys = recipientWallets.ToDictionary(w => w, w => new byte[32]),
                EncryptionType = opts.EncryptionType,
                ContentType = opts.ContentType
            };
            _payloads.Add(payload);
        }

        return Task.FromResult(PayloadResult.Success(payload.Id));
    }

    /// <inheritdoc/>
    public async Task<PayloadResult<byte[]>> GetPayloadDataAsync(
        uint payloadId,
        DecryptionKeyInfo keyInfo,
        CancellationToken cancellationToken = default)
    {
        Payload? payload;
        byte[]? encryptedKey;

        lock (_lock)
        {
            payload = _payloads.FirstOrDefault(p => p.Id == payloadId);
        }

        if (payload == null)
            return PayloadResult<byte[]>.Failure(
                TransactionStatus.InvalidPayload,
                "Payload not found");

        // FR-006: Check authorization
        if (!payload.EncryptedKeys.TryGetValue(keyInfo.WalletAddress, out encryptedKey))
            return PayloadResult<byte[]>.Failure(
                TransactionStatus.AccessDenied,
                "Wallet not authorized to access this payload");

        // FR-009: Legacy check — zeroed IV means unencrypted payload
        if (payload.IsLegacy())
            return PayloadResult<byte[]>.Success(payload.Data);

        // FR-005: Decrypt symmetric key using recipient's private key
        var keyResult = await _cryptoModule.DecryptAsync(
            encryptedKey,
            (byte)keyInfo.Network,
            keyInfo.PrivateKey,
            cancellationToken);

        if (!keyResult.IsSuccess || keyResult.Value == null)
            return PayloadResult<byte[]>.Failure(
                TransactionStatus.AccessDenied,
                $"Decryption failed: {keyResult.ErrorMessage}");

        var symmetricKey = keyResult.Value;

        try
        {
            // Reconstruct SymmetricCiphertext and decrypt
            var ciphertext = new SymmetricCiphertext
            {
                Data = payload.Data,
                Key = symmetricKey,
                IV = payload.IV,
                Type = payload.EncryptionType
            };

            var decryptResult = await _symmetricCrypto.DecryptAsync(ciphertext, cancellationToken);

            if (!decryptResult.IsSuccess || decryptResult.Value == null)
                return PayloadResult<byte[]>.Failure(
                    TransactionStatus.InvalidPayload,
                    $"Data decryption failed: {decryptResult.ErrorMessage}");

            return PayloadResult<byte[]>.Success(decryptResult.Value);
        }
        finally
        {
            // FR-013: Zeroize symmetric key material
            Array.Clear(symmetricKey, 0, symmetricKey.Length);
        }
    }

    /// <inheritdoc/>
    public Task<PayloadResult<byte[]>> GetPayloadDataAsync(
        uint payloadId,
        string wifPrivateKey,
        CancellationToken cancellationToken = default)
    {
        Payload? payload;
        lock (_lock)
        {
            payload = _payloads.FirstOrDefault(p => p.Id == payloadId);
        }

        if (payload == null)
            return Task.FromResult(PayloadResult<byte[]>.Failure(
                TransactionStatus.InvalidPayload,
                "Payload not found"));

        // Legacy string-based overload — returns raw data for backward compat
        return Task.FromResult(PayloadResult<byte[]>.Success(payload.Data));
    }

    /// <inheritdoc/>
    public Task<TransactionStatus> RemovePayloadAsync(uint payloadId)
    {
        lock (_lock)
        {
            var payload = _payloads.FirstOrDefault(p => p.Id == payloadId);
            if (payload == null)
                return Task.FromResult(TransactionStatus.InvalidPayload);

            _payloads.Remove(payload);
            return Task.FromResult(TransactionStatus.Success);
        }
    }

    /// <inheritdoc/>
    public async Task<TransactionStatus> GrantAccessAsync(
        uint payloadId,
        RecipientKeyInfo newRecipient,
        DecryptionKeyInfo ownerKeyInfo)
    {
        Payload? payload;
        byte[]? ownerEncryptedKey;

        lock (_lock)
        {
            payload = _payloads.FirstOrDefault(p => p.Id == payloadId);
        }

        if (payload == null)
            return TransactionStatus.InvalidPayload;

        // Idempotent — already has access
        if (payload.EncryptedKeys.ContainsKey(newRecipient.WalletAddress))
            return TransactionStatus.Success;

        // Verify owner has access
        if (!payload.EncryptedKeys.TryGetValue(ownerKeyInfo.WalletAddress, out ownerEncryptedKey))
            return TransactionStatus.AccessDenied;

        // FR-008: Decrypt owner's copy of the symmetric key
        var keyResult = await _cryptoModule.DecryptAsync(
            ownerEncryptedKey,
            (byte)ownerKeyInfo.Network,
            ownerKeyInfo.PrivateKey);

        if (!keyResult.IsSuccess || keyResult.Value == null)
            return TransactionStatus.AccessDenied;

        var symmetricKey = keyResult.Value;

        try
        {
            // Encrypt symmetric key for new recipient
            var encryptResult = await _cryptoModule.EncryptAsync(
                symmetricKey,
                (byte)newRecipient.Network,
                newRecipient.PublicKey);

            if (!encryptResult.IsSuccess || encryptResult.Value == null)
                return TransactionStatus.InvalidRecipients;

            lock (_lock)
            {
                payload.EncryptedKeys[newRecipient.WalletAddress] = encryptResult.Value;
            }

            return TransactionStatus.Success;
        }
        finally
        {
            // FR-013: Zeroize symmetric key material
            Array.Clear(symmetricKey, 0, symmetricKey.Length);
        }
    }

    /// <inheritdoc/>
    public Task<TransactionStatus> GrantAccessAsync(
        uint payloadId,
        string recipientWallet,
        string ownerWifKey)
    {
        lock (_lock)
        {
            var payload = _payloads.FirstOrDefault(p => p.Id == payloadId);
            if (payload == null)
                return Task.FromResult(TransactionStatus.InvalidPayload);

            if (payload.EncryptedKeys.ContainsKey(recipientWallet))
                return Task.FromResult(TransactionStatus.Success);

            // Legacy string-based overload — dummy key for backward compat
            payload.EncryptedKeys[recipientWallet] = new byte[32];
            return Task.FromResult(TransactionStatus.Success);
        }
    }

    /// <inheritdoc/>
    public Task<TransactionStatus> RevokeAccessAsync(
        uint payloadId,
        string recipientWallet)
    {
        lock (_lock)
        {
            var payload = _payloads.FirstOrDefault(p => p.Id == payloadId);
            if (payload == null)
                return Task.FromResult(TransactionStatus.InvalidPayload);

            payload.EncryptedKeys.Remove(recipientWallet);
            return Task.FromResult(TransactionStatus.Success);
        }
    }

    /// <inheritdoc/>
    public Task<bool> VerifyAllAsync()
    {
        List<Payload> snapshot;
        lock (_lock)
        {
            snapshot = _payloads.ToList();
        }

        foreach (var payload in snapshot)
        {
            // Legacy payloads pass verification
            if (payload.IsLegacy())
                continue;

            // Structural check: hash must be non-zero for encrypted payloads
            if (payload.Hash.All(b => b == 0))
                return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task<bool> VerifyPayloadAsync(uint payloadId)
    {
        Payload? payload;
        lock (_lock)
        {
            payload = _payloads.FirstOrDefault(p => p.Id == payloadId);
        }

        if (payload == null)
            return Task.FromResult(false);

        // Legacy payloads pass structural verification
        if (payload.IsLegacy())
            return Task.FromResult(true);

        // Structural check: hash must be non-zero for encrypted payloads
        if (payload.Hash.All(b => b == 0))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyPayloadAsync(uint payloadId, DecryptionKeyInfo keyInfo)
    {
        Payload? payload;
        lock (_lock)
        {
            payload = _payloads.FirstOrDefault(p => p.Id == payloadId);
        }

        if (payload == null)
            return false;

        // Legacy payloads pass verification
        if (payload.IsLegacy())
            return true;

        // Decrypt payload data
        var decryptResult = await GetPayloadDataAsync(payloadId, keyInfo);
        if (!decryptResult.IsSuccess || decryptResult.Value == null)
            return false;

        // FR-003/FR-007: Compute hash of decrypted data and compare
        var computedHash = _hashProvider.ComputeHash(decryptResult.Value, HashType.SHA256);

        // FR-007 clarification: Use constant-time comparison
        return CryptographicOperations.FixedTimeEquals(computedHash, payload.Hash);
    }
}

/// <summary>
/// Represents a payload with encrypted data.
/// </summary>
internal class Payload : IPayload
{
    public uint Id { get; set; }
    public PayloadType Type { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public byte[] IV { get; set; } = Array.Empty<byte>();
    public byte[] Hash { get; set; } = Array.Empty<byte>();
    public bool IsCompressed { get; set; }
    public long OriginalSize { get; set; }
    public Dictionary<string, byte[]> EncryptedKeys { get; set; } = new();
    public EncryptionType EncryptionType { get; set; } = EncryptionType.XCHACHA20_POLY1305;

    /// <summary>
    /// MIME type describing the plaintext data (e.g., "application/json").
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Encoding scheme for the serialized Data field (e.g., "base64url", "identity").
    /// </summary>
    public string? ContentEncoding { get; set; }

    /// <summary>
    /// Determines if this is a legacy (unencrypted) payload by checking for zeroed IV.
    /// </summary>
    public bool IsLegacy() => IV.Length == 0 || IV.All(b => b == 0);

    public PayloadInfo GetInfo()
    {
        return new PayloadInfo
        {
            Id = Id,
            Type = Type,
            OriginalSize = OriginalSize,
            CompressedSize = Data.Length,
            IsCompressed = IsCompressed,
            IsEncrypted = !IsLegacy(),
            EncryptionType = EncryptionType,
            HashType = Sorcha.Cryptography.Enums.HashType.SHA256,
            AccessibleBy = EncryptedKeys.Keys.ToArray(),
            ContentType = ContentType,
            ContentEncoding = ContentEncoding
        };
    }
}
