using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        // TODO: Implement keychain export with encryption
        return Task.FromResult(CryptoResult<byte[]>.Failure(
            CryptoStatus.UnexpectedError,
            "Keychain export not yet implemented"));
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
        // TODO: Implement keychain import with decryption
        return Task.FromResult(CryptoStatus.UnexpectedError);
    }
}
