using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sorcha.Cryptography.Enums;

namespace Sorcha.Cryptography.Models;

/// <summary>
/// Manages multiple key rings with encryption support.
/// </summary>
public class KeyChain
{
    private readonly ConcurrentDictionary<string, KeyRing> _keyRings = new();

    /// <summary>
    /// Gets the number of key rings in the chain.
    /// </summary>
    public int Count => _keyRings.Count;

    /// <summary>
    /// Gets all key ring names.
    /// </summary>
    public IEnumerable<string> KeyRingNames => _keyRings.Keys;

    /// <summary>
    /// Adds a key ring to the chain.
    /// </summary>
    /// <param name="name">The name for the key ring.</param>
    /// <param name="keyRing">The key ring to add.</param>
    /// <returns>Success if added, DuplicateKeyRing if name exists.</returns>
    public CryptoStatus AddKeyRing(string name, KeyRing keyRing)
    {
        if (string.IsNullOrWhiteSpace(name))
            return CryptoStatus.InvalidParameter;

        if (keyRing == null)
            return CryptoStatus.InvalidParameter;

        bool added = _keyRings.TryAdd(name, keyRing);
        return added ? CryptoStatus.Success : CryptoStatus.DuplicateKeyRing;
    }

    /// <summary>
    /// Retrieves a key ring by name.
    /// </summary>
    /// <param name="name">The name of the key ring.</param>
    /// <returns>A result containing the key ring or error status.</returns>
    public CryptoResult<KeyRing> GetKeyRing(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return CryptoResult<KeyRing>.Failure(CryptoStatus.InvalidParameter, "Name cannot be null or empty");

        if (_keyRings.TryGetValue(name, out var keyRing))
            return CryptoResult<KeyRing>.Success(keyRing);

        return CryptoResult<KeyRing>.Failure(CryptoStatus.UnknownKeyRing, $"Key ring '{name}' not found");
    }

    /// <summary>
    /// Removes a key ring from the chain.
    /// </summary>
    /// <param name="name">The name of the key ring to remove.</param>
    /// <returns>Success if removed, UnknownKeyRing if not found.</returns>
    public CryptoStatus RemoveKeyRing(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return CryptoStatus.InvalidParameter;

        if (_keyRings.TryRemove(name, out var keyRing))
        {
            keyRing.Zeroize();
            return CryptoStatus.Success;
        }

        return CryptoStatus.UnknownKeyRing;
    }

    /// <summary>
    /// Checks if a key ring exists.
    /// </summary>
    /// <param name="name">The name of the key ring.</param>
    /// <returns>True if the key ring exists.</returns>
    public bool ContainsKeyRing(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && _keyRings.ContainsKey(name);
    }

    /// <summary>
    /// Clears all key rings from the chain.
    /// </summary>
    public void Clear()
    {
        foreach (var keyRing in _keyRings.Values)
        {
            keyRing.Zeroize();
        }
        _keyRings.Clear();
    }

    /// <summary>
    /// Exports the entire keychain with password protection.
    /// </summary>
    /// <param name="password">The password to protect the export.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the encrypted keychain data or error status.</returns>
    public Task<CryptoResult<byte[]>> ExportAsync(
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(password))
                return Task.FromResult(CryptoResult<byte[]>.Failure(
                    CryptoStatus.InvalidParameter, "Password is required for export"));

            cancellationToken.ThrowIfCancellationRequested();

            // Serialize all key rings to JSON
            var exportData = new Dictionary<string, KeyRingExportDto>();
            foreach (var name in _keyRings.Keys)
            {
                if (_keyRings.TryGetValue(name, out var ring))
                {
                    exportData[name] = new KeyRingExportDto
                    {
                        Network = ring.Network,
                        Mnemonic = ring.Mnemonic,
                        PrivateKey = ring.MasterKeySet.PrivateKey.Key,
                        PublicKey = ring.MasterKeySet.PublicKey.Key,
                        PasswordHint = ring.PasswordHint,
                        CreatedAt = ring.CreatedAt
                    };
                }
            }

            var json = JsonSerializer.SerializeToUtf8Bytes(exportData);

            // Generate salt and nonce
            var salt = RandomNumberGenerator.GetBytes(16);
            var nonce = RandomNumberGenerator.GetBytes(12);

            // Derive key from password via PBKDF2
            var key = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, 100_000, HashAlgorithmName.SHA256, 32);

            // Encrypt with AES-256-GCM
            var ciphertext = new byte[json.Length];
            var tag = new byte[16];
            using var aesGcm = new AesGcm(key, 16);
            aesGcm.Encrypt(nonce, json, ciphertext, tag);

            // Append checksum of plaintext for integrity
            var checksum = SHA256.HashData(json);

            // Format: [salt:16][nonce:12][tag:16][checksum:32][ciphertext]
            var result = new byte[16 + 12 + 16 + 32 + ciphertext.Length];
            salt.CopyTo(result, 0);
            nonce.CopyTo(result, 16);
            tag.CopyTo(result, 28);
            checksum.CopyTo(result, 44);
            ciphertext.CopyTo(result, 76);

            return Task.FromResult(CryptoResult<byte[]>.Success(result));
        }
        catch (OperationCanceledException)
        {
            return Task.FromResult(CryptoResult<byte[]>.Failure(
                CryptoStatus.Cancelled, "Operation was cancelled"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(CryptoResult<byte[]>.Failure(
                CryptoStatus.UnexpectedError, $"Export failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Imports a keychain from encrypted data.
    /// </summary>
    /// <param name="encryptedData">The encrypted keychain data.</param>
    /// <param name="password">The password to decrypt the data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success if imported, otherwise error status.</returns>
    public Task<CryptoStatus> ImportAsync(
        byte[] encryptedData,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (encryptedData == null || encryptedData.Length == 0)
                return Task.FromResult(CryptoStatus.InvalidParameter);

            if (string.IsNullOrEmpty(password))
                return Task.FromResult(CryptoStatus.InvalidParameter);

            cancellationToken.ThrowIfCancellationRequested();

            // Format: [salt:16][nonce:12][tag:16][checksum:32][ciphertext]
            const int headerLen = 16 + 12 + 16 + 32;
            if (encryptedData.Length < headerLen + 1)
                return Task.FromResult(CryptoStatus.DecryptionFailed);

            var salt = encryptedData.AsSpan(0, 16);
            var nonce = encryptedData.AsSpan(16, 12);
            var tag = encryptedData.AsSpan(28, 16);
            var checksum = encryptedData.AsSpan(44, 32);
            var ciphertext = encryptedData.AsSpan(76);

            // Derive key from password
            var key = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, 100_000, HashAlgorithmName.SHA256, 32);

            // Decrypt
            var plaintext = new byte[ciphertext.Length];
            using var aesGcm = new AesGcm(key, 16);
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

            // Verify checksum
            var actualChecksum = SHA256.HashData(plaintext);
            if (!CryptographicOperations.FixedTimeEquals(checksum, actualChecksum))
                return Task.FromResult(CryptoStatus.DecryptionFailed);

            // Deserialize key rings
            var exportData = JsonSerializer.Deserialize<Dictionary<string, KeyRingExportDto>>(plaintext);
            if (exportData == null)
                return Task.FromResult(CryptoStatus.DecryptionFailed);

            // Clear existing and import
            Clear();
            foreach (var (name, dto) in exportData)
            {
                var keyRing = new KeyRing
                {
                    Network = dto.Network,
                    Mnemonic = dto.Mnemonic,
                    MasterKeySet = new KeySet
                    {
                        PrivateKey = new CryptoKey(dto.Network, dto.PrivateKey),
                        PublicKey = new CryptoKey(dto.Network, dto.PublicKey)
                    },
                    PasswordHint = dto.PasswordHint,
                    CreatedAt = dto.CreatedAt
                };
                _keyRings[name] = keyRing;
            }

            return Task.FromResult(CryptoStatus.Success);
        }
        catch (CryptographicException)
        {
            return Task.FromResult(CryptoStatus.DecryptionFailed);
        }
        catch (OperationCanceledException)
        {
            return Task.FromResult(CryptoStatus.Cancelled);
        }
        catch
        {
            return Task.FromResult(CryptoStatus.UnexpectedError);
        }
    }

    /// <summary>
    /// DTO for serializing key ring data during export/import.
    /// </summary>
    private class KeyRingExportDto
    {
        public WalletNetworks Network { get; set; }
        public string? Mnemonic { get; set; }
        public byte[]? PrivateKey { get; set; }
        public byte[]? PublicKey { get; set; }
        public string? PasswordHint { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
