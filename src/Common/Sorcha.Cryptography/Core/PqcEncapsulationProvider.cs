// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Kems;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Sorcha.Cryptography.Enums;
using Sorcha.Cryptography.Models;

namespace Sorcha.Cryptography.Core;

/// <summary>
/// Provides ML-KEM-768 (FIPS 203) key encapsulation mechanism for quantum-safe key exchange.
/// <para>This provider is stateless — no key material is retained between calls.
/// Shared secrets returned in <see cref="EncapsulationResult"/> MUST be disposed to zeroize
/// the secret. Callers are responsible for zeroizing private key byte arrays via
/// <see cref="KeySet.Zeroize"/> or <see cref="CryptographicOperations.ZeroMemory"/>.</para>
/// </summary>
public sealed class PqcEncapsulationProvider : IDisposable
{
    /// <summary>ML-KEM-768 ciphertext size in bytes.</summary>
    public const int CiphertextSize = 1088;

    /// <summary>ML-KEM-768 shared secret size in bytes.</summary>
    public const int SharedSecretSize = 32;

    private bool _disposed;

    /// <summary>
    /// Generates an ML-KEM-768 key pair.
    /// </summary>
    public CryptoResult<KeySet> GenerateMlKem768KeyPair()
    {
        try
        {
            var generator = new MLKemKeyPairGenerator();
            generator.Init(new MLKemKeyGenerationParameters(new SecureRandom(), MLKemParameters.ml_kem_768));
            var keyPair = generator.GenerateKeyPair();

            var publicKey = ((MLKemPublicKeyParameters)keyPair.Public).GetEncoded();
            var privateKey = ((MLKemPrivateKeyParameters)keyPair.Private).GetEncoded();

            return CryptoResult<KeySet>.Success(new KeySet
            {
                PublicKey = new CryptoKey(WalletNetworks.ML_KEM_768, publicKey),
                PrivateKey = new CryptoKey(WalletNetworks.ML_KEM_768, privateKey)
            });
        }
        catch (Exception ex)
        {
            return CryptoResult<KeySet>.Failure(CryptoStatus.KeyGenerationFailed,
                $"ML-KEM-768 key generation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Encapsulates a shared secret using the recipient's ML-KEM-768 public key.
    /// Returns the ciphertext and the shared secret.
    /// </summary>
    public CryptoResult<EncapsulationResult> Encapsulate(byte[] recipientPublicKey)
    {
        if (recipientPublicKey == null || recipientPublicKey.Length == 0)
            return CryptoResult<EncapsulationResult>.Failure(CryptoStatus.InvalidKey, "Recipient public key cannot be null or empty");

        try
        {
            var pubKeyParams = MLKemPublicKeyParameters.FromEncoding(MLKemParameters.ml_kem_768, recipientPublicKey);
            var encapsulator = new MLKemEncapsulator(MLKemParameters.ml_kem_768);
            encapsulator.Init(pubKeyParams);

            var ciphertext = new byte[CiphertextSize];
            var sharedSecret = new byte[SharedSecretSize];
            encapsulator.Encapsulate(ciphertext, 0, ciphertext.Length, sharedSecret, 0, sharedSecret.Length);

            return CryptoResult<EncapsulationResult>.Success(new EncapsulationResult(ciphertext, sharedSecret));
        }
        catch (Exception ex)
        {
            return CryptoResult<EncapsulationResult>.Failure(CryptoStatus.EncryptionFailed,
                $"ML-KEM-768 encapsulation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Decapsulates a shared secret from ciphertext using the recipient's ML-KEM-768 private key.
    /// </summary>
    public CryptoResult<byte[]> Decapsulate(byte[] ciphertext, byte[] privateKey)
    {
        if (ciphertext == null || ciphertext.Length == 0)
            return CryptoResult<byte[]>.Failure(CryptoStatus.InvalidParameter, "Ciphertext cannot be null or empty");
        if (privateKey == null || privateKey.Length == 0)
            return CryptoResult<byte[]>.Failure(CryptoStatus.InvalidKey, "Private key cannot be null or empty");

        try
        {
            var privKeyParams = MLKemPrivateKeyParameters.FromEncoding(MLKemParameters.ml_kem_768, privateKey);
            var decapsulator = new MLKemDecapsulator(MLKemParameters.ml_kem_768);
            decapsulator.Init(privKeyParams);

            var sharedSecret = new byte[SharedSecretSize];
            decapsulator.Decapsulate(ciphertext, 0, ciphertext.Length, sharedSecret, 0, sharedSecret.Length);

            return CryptoResult<byte[]>.Success(sharedSecret);
        }
        catch (Exception ex)
        {
            return CryptoResult<byte[]>.Failure(CryptoStatus.DecryptionFailed,
                $"ML-KEM-768 decapsulation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculates the ML-KEM-768 public key from a private key.
    /// </summary>
    public CryptoResult<byte[]> CalculateMlKem768PublicKey(byte[] privateKey)
    {
        if (privateKey == null || privateKey.Length == 0)
            return CryptoResult<byte[]>.Failure(CryptoStatus.InvalidKey, "Private key cannot be null or empty");

        try
        {
            var privKeyParams = MLKemPrivateKeyParameters.FromEncoding(MLKemParameters.ml_kem_768, privateKey);
            return CryptoResult<byte[]>.Success(privKeyParams.GetPublicKeyEncoded());
        }
        catch (Exception ex)
        {
            return CryptoResult<byte[]>.Failure(CryptoStatus.InvalidKey,
                $"Failed to calculate ML-KEM-768 public key: {ex.Message}");
        }
    }

    /// <summary>
    /// Zeroizes any retained key material and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}

/// <summary>
/// Result of an ML-KEM encapsulation operation.
/// </summary>
/// <param name="Ciphertext">The encapsulated ciphertext to send to the recipient.</param>
/// <param name="SharedSecret">The shared secret derived from encapsulation.</param>
public record EncapsulationResult(byte[] Ciphertext, byte[] SharedSecret) : IDisposable
{
    /// <summary>
    /// Zeroizes the shared secret.
    /// </summary>
    public void Dispose()
    {
        CryptographicOperations.ZeroMemory(SharedSecret);
    }
}
