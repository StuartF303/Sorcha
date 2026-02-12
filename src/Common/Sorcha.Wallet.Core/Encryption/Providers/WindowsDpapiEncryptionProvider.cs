// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Sorcha.Wallet.Core.Encryption.Interfaces;
using Sorcha.Wallet.Core.Encryption.Logging;

namespace Sorcha.Wallet.Core.Encryption.Providers;

/// <summary>
/// Windows Data Protection API (DPAPI) encryption provider for production use
///
/// Implementation Strategy (from 2026-01-11 clarifications):
/// - API: System.Security.Cryptography.ProtectedData (built-in .NET)
/// - Key Storage: File-based on persistent Docker volume
/// - Scope: DataProtectionScope.LocalMachine for service accounts
/// - Encryption: DPAPI encrypts DEK, AES-256-GCM encrypts wallet data with DEK
/// - Persistence: DEKs stored in {KeyStorePath}/*.key files
///
/// Docker Volume Configuration:
/// - Mount persistent volume to /app/keys or C:\app\keys
/// - Ensures DEKs survive container restarts
/// - Example: docker-compose.yml volumes: wallet-encryption-keys:/app/keys
///
/// Security Properties:
/// - DEKs protected by Windows machine credentials (DPAPI LocalMachine)
/// - Cannot decrypt DEKs on different machine without same credentials
/// - Additional entropy per key for defense-in-depth
/// - AES-256-GCM provides authenticated encryption for wallet data
///
/// Limitations:
/// - Windows-only (checked via IsAvailable property)
/// - Machine-specific (encrypted DEKs tied to Windows machine identity)
/// - No built-in key rotation (manual via CreateKeyAsync)
/// </summary>
public sealed class WindowsDpapiEncryptionProvider : IEncryptionProvider
{
    private readonly string _keyStorePath;
    private readonly DataProtectionScope _scope;
    private readonly string _defaultKeyId;
    private readonly ConcurrentDictionary<string, byte[]> _keyCache;
    private readonly EncryptionAuditLogger _auditLogger;
    private readonly ILogger<WindowsDpapiEncryptionProvider> _logger;

    /// <summary>
    /// Checks if Windows DPAPI is available on current platform
    /// </summary>
    public static bool IsAvailable => OperatingSystem.IsWindows();

    /// <summary>
    /// Initializes Windows DPAPI encryption provider
    /// </summary>
    /// <param name="keyStorePath">Directory path for encrypted DEK storage (must exist or be creatable)</param>
    /// <param name="defaultKeyId">Default key identifier for new encryptions</param>
    /// <param name="scope">DPAPI scope (LocalMachine recommended for services, CurrentUser for desktop apps)</param>
    /// <param name="logger">Logger for diagnostics and audit trail</param>
    /// <exception cref="PlatformNotSupportedException">Thrown if not running on Windows</exception>
    public WindowsDpapiEncryptionProvider(
        string keyStorePath,
        string defaultKeyId,
        DataProtectionScope scope,
        ILogger<WindowsDpapiEncryptionProvider> logger)
    {
        if (!IsAvailable)
        {
            throw new PlatformNotSupportedException(
                "Windows DPAPI encryption provider is only available on Windows platforms.");
        }

        _keyStorePath = keyStorePath ?? throw new ArgumentNullException(nameof(keyStorePath));
        _defaultKeyId = defaultKeyId ?? throw new ArgumentNullException(nameof(defaultKeyId));
        _scope = scope;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyCache = new ConcurrentDictionary<string, byte[]>();
        _auditLogger = new EncryptionAuditLogger(logger, "WindowsDpapi");

        // Ensure key storage directory exists
        Directory.CreateDirectory(_keyStorePath);

        // Load existing keys from disk
        LoadKeysFromDisk();

        _auditLogger.LogProviderInitialized(
            $"KeyStorePath={_keyStorePath}, Scope={_scope}, DefaultKeyId={_defaultKeyId}");
    }

    /// <inheritdoc />
    public string GetDefaultKeyId() => _defaultKeyId;

    /// <inheritdoc />
    public async Task<string> EncryptAsync(
        byte[] plaintext,
        string keyId,
        CancellationToken cancellationToken = default)
    {
        if (plaintext == null || plaintext.Length == 0)
            throw new ArgumentException("Plaintext cannot be null or empty.", nameof(plaintext));

        if (string.IsNullOrWhiteSpace(keyId))
            throw new ArgumentException("Key ID cannot be null or empty.", nameof(keyId));

        using var timer = EncryptionOperationTimer.Start();

        try
        {
            // Get or create DEK
            var dek = await GetOrCreateKeyAsync(keyId, cancellationToken);

            // Encrypt data with AES-256-GCM using DEK
            using var aes = new AesGcm(dek, AesGcm.TagByteSizes.MaxSize);

            // Generate random nonce (12 bytes for GCM)
            var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);

            // Allocate space for ciphertext and authentication tag
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[AesGcm.TagByteSizes.MaxSize];

            // Encrypt with authenticated encryption
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

            // Combine: nonce (12) + tag (16) + ciphertext
            var combined = new byte[nonce.Length + tag.Length + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertext, 0, combined, nonce.Length + tag.Length, ciphertext.Length);

            // Return base64-encoded result (for database storage)
            var result = Convert.ToBase64String(combined);

            _auditLogger.LogEncryptSuccess(keyId, timer.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _auditLogger.LogEncryptFailure(keyId, ex, timer.ElapsedMilliseconds);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> DecryptAsync(
        string ciphertext,
        string keyId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ciphertext))
            throw new ArgumentException("Ciphertext cannot be null or empty.", nameof(ciphertext));

        if (string.IsNullOrWhiteSpace(keyId))
            throw new ArgumentException("Key ID cannot be null or empty.", nameof(keyId));

        using var timer = EncryptionOperationTimer.Start();

        try
        {
            // Get DEK from cache or disk
            var dek = await GetOrCreateKeyAsync(keyId, cancellationToken);

            // Decode base64 ciphertext
            var combined = Convert.FromBase64String(ciphertext);

            // Validate minimum length (12 nonce + 16 tag)
            var nonceSize = AesGcm.NonceByteSizes.MaxSize;
            var tagSize = AesGcm.TagByteSizes.MaxSize;

            if (combined.Length < nonceSize + tagSize)
            {
                throw new CryptographicException(
                    $"Ciphertext too short. Expected at least {nonceSize + tagSize} bytes, got {combined.Length} bytes.");
            }

            // Extract components: nonce + tag + ciphertext
            var nonce = new byte[nonceSize];
            var tag = new byte[tagSize];
            var encryptedData = new byte[combined.Length - nonceSize - tagSize];

            Buffer.BlockCopy(combined, 0, nonce, 0, nonceSize);
            Buffer.BlockCopy(combined, nonceSize, tag, 0, tagSize);
            Buffer.BlockCopy(combined, nonceSize + tagSize, encryptedData, 0, encryptedData.Length);

            // Decrypt with AES-256-GCM
            using var aes = new AesGcm(dek, AesGcm.TagByteSizes.MaxSize);
            var plaintext = new byte[encryptedData.Length];

            aes.Decrypt(nonce, encryptedData, tag, plaintext);

            _auditLogger.LogDecryptSuccess(keyId, timer.ElapsedMilliseconds);

            return plaintext;
        }
        catch (Exception ex)
        {
            _auditLogger.LogDecryptFailure(keyId, ex, timer.ElapsedMilliseconds);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> KeyExistsAsync(string keyId, CancellationToken cancellationToken = default)
    {
        using var timer = EncryptionOperationTimer.Start();

        // Check in-memory cache first
        if (_keyCache.ContainsKey(keyId))
        {
            _auditLogger.LogKeyExists(keyId, exists: true, timer.ElapsedMilliseconds);
            return true;
        }

        // Check on disk
        var keyFilePath = GetKeyFilePath(keyId);
        var exists = File.Exists(keyFilePath);

        _auditLogger.LogKeyExists(keyId, exists, timer.ElapsedMilliseconds);

        return await Task.FromResult(exists);
    }

    /// <inheritdoc />
    public async Task CreateKeyAsync(string keyId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyId))
            throw new ArgumentException("Key ID cannot be null or empty.", nameof(keyId));

        using var timer = EncryptionOperationTimer.Start();

        try
        {
            // Generate random 256-bit DEK (32 bytes for AES-256)
            var dek = RandomNumberGenerator.GetBytes(32);

            // Encrypt DEK with Windows DPAPI
            var entropy = Encoding.UTF8.GetBytes($"sorcha-wallet-{keyId}");
            var encryptedDek = ProtectedData.Protect(dek, entropy, _scope);

            // Store encrypted DEK to disk
            var keyFilePath = GetKeyFilePath(keyId);
            await File.WriteAllBytesAsync(keyFilePath, encryptedDek, cancellationToken);

            // Cache decrypted DEK in memory for performance
            _keyCache[keyId] = dek;

            _auditLogger.LogCreateKeySuccess(keyId, timer.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _auditLogger.LogCreateKeyFailure(keyId, ex, timer.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Gets or creates encryption key (DEK)
    /// </summary>
    private async Task<byte[]> GetOrCreateKeyAsync(string keyId, CancellationToken cancellationToken)
    {
        // Check cache first
        if (_keyCache.TryGetValue(keyId, out var cachedKey))
        {
            return cachedKey;
        }

        // Check disk
        var keyFilePath = GetKeyFilePath(keyId);

        if (!File.Exists(keyFilePath))
        {
            // Create new key if not exists
            await CreateKeyAsync(keyId, cancellationToken);
            return _keyCache[keyId];
        }

        // Load and decrypt DEK from disk
        var encryptedDek = await File.ReadAllBytesAsync(keyFilePath, cancellationToken);
        var entropy = Encoding.UTF8.GetBytes($"sorcha-wallet-{keyId}");
        var dek = ProtectedData.Unprotect(encryptedDek, entropy, _scope);

        // Cache for future operations
        _keyCache[keyId] = dek;

        return dek;
    }

    /// <summary>
    /// Loads all existing encrypted DEKs from disk into memory cache
    /// </summary>
    private void LoadKeysFromDisk()
    {
        if (!Directory.Exists(_keyStorePath))
        {
            _logger.LogWarning(
                "Key storage directory does not exist, will be created: {KeyStorePath}",
                _keyStorePath);
            return;
        }

        var keyFiles = Directory.GetFiles(_keyStorePath, "*.key");
        var loadedCount = 0;

        foreach (var keyFile in keyFiles)
        {
            try
            {
                var keyId = Path.GetFileNameWithoutExtension(keyFile);
                var encryptedDek = File.ReadAllBytes(keyFile);
                var entropy = Encoding.UTF8.GetBytes($"sorcha-wallet-{keyId}");
                var dek = ProtectedData.Unprotect(encryptedDek, entropy, _scope);

                _keyCache[keyId] = dek;
                loadedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to load encryption key from file: {KeyFile}",
                    keyFile);
            }
        }

        _auditLogger.LogKeysLoaded(loadedCount, _keyStorePath);
    }

    /// <summary>
    /// Gets file path for encrypted DEK storage
    /// </summary>
    private string GetKeyFilePath(string keyId)
    {
        // Sanitize key ID for file system (remove invalid characters)
        var safeKeyId = string.Join("_", keyId.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_keyStorePath, $"{safeKeyId}.key");
    }
}
