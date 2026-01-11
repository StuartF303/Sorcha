# Wallet Service Encryption Implementation Plan

**Date:** 2026-01-11
**Status:** 📋 Planning
**Related Spec:** [sorcha-wallet-service.md](../../.specify/specs/sorcha-wallet-service.md)

---

## Executive Summary

This document outlines the implementation plan for production-ready encryption providers for the Sorcha Wallet Service. The current `LocalEncryptionProvider` stores encryption keys in-memory, which is unsuitable for production use. This plan details the implementation of:

1. **Azure Key Vault Provider** - Production encryption using Azure Key Vault (cloud)
2. **OS-Level Encryption Providers** - Platform-specific secure storage (Windows DPAPI, macOS Keychain, Linux Secret Service)
3. **Configuration-Based Provider Selection** - Runtime provider selection based on configuration

**Goal:** Maximize use of compiled infrastructure code and OS-provided cryptographic facilities, minimizing custom encryption implementation.

---

## Clarifications

### Session 2026-01-11

- Q: When Azure Key Vault is unavailable (network failure, service outage, rate limit), what should the wallet service do? → A: Read-through cache with stale tolerance - Use cached DEKs for decryption only (read operations continue), fail new wallet creation gracefully with retry
- Q: For Windows DPAPI provider, where should encrypted DEKs (Data Encryption Keys) be persisted? → A: File-based storage on persistent Docker volume
- Q: How should key rotation work when a new default encryption key is created? → A: Automatic rotation with backward compatibility (new wallets use new key, old wallets keep original key, optional background re-encryption)
- Q: What level of audit logging should be implemented for encryption operations (Encrypt/Decrypt/CreateKey)? → A: Structured logging with sanitization (log all operations with timestamp, keyId, operation type, success/failure, user context, sanitize sensitive data)
- Q: What implementation order should be used for the four encryption providers? → A: Local providers first (Windows DPAPI, Linux, macOS) for faster development and testing, then cloud (Azure Key Vault)

---

## Table of Contents

1. [Current State Analysis](#current-state-analysis)
2. [Requirements Summary](#requirements-summary)
3. [Proposed Architecture](#proposed-architecture)
4. [Provider Implementations](#provider-implementations)
5. [Configuration Strategy](#configuration-strategy)
6. [Testing Approach](#testing-approach)
7. [Migration Path](#migration-path)
8. [Security Considerations](#security-considerations)
9. [Implementation Tasks](#implementation-tasks)

---

## Current State Analysis

### Existing Interface: IEncryptionProvider

**Location:** `src/Common/Sorcha.Wallet.Core/Encryption/Interfaces/IEncryptionProvider.cs`

```csharp
public interface IEncryptionProvider
{
    /// <summary>
    /// Encrypts plaintext data using the specified encryption key
    /// </summary>
    Task<string> EncryptAsync(byte[] plaintext, string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts ciphertext data using the specified encryption key
    /// </summary>
    Task<byte[]> DecryptAsync(string ciphertext, string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default encryption key identifier
    /// </summary>
    string GetDefaultKeyId();

    /// <summary>
    /// Checks if a key exists in the provider
    /// </summary>
    Task<bool> KeyExistsAsync(string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new encryption key with the specified identifier
    /// </summary>
    Task CreateKeyAsync(string keyId, CancellationToken cancellationToken = default);
}
```

**Key Design Decisions:**
- **Key ID Concept:** Supports multiple keys and key rotation
- **Async Operations:** All encryption operations are asynchronous
- **Base64 Encoding:** Ciphertext returned as base64-encoded string for database storage
- **Byte Array Input/Output:** Plaintext input and decrypted output are byte arrays (flexibility)

### Current Implementation: LocalEncryptionProvider

**Location:** `src/Common/Sorcha.Wallet.Core/Encryption/Providers/LocalEncryptionProvider.cs`

**Current Behavior:**
```csharp
// ⚠️ WARNING: Development only - keys stored in-memory Dictionary
private readonly ConcurrentDictionary<string, byte[]> _keys = new();

// Encryption: AES-256-GCM
// Format: nonce (12 bytes) + tag (16 bytes) + ciphertext → base64
public async Task<string> EncryptAsync(byte[] plaintext, string keyId, ...)
{
    var key = _keys[keyId]; // 32-byte AES key
    using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);

    // Encrypt with random nonce
    var nonce = RandomNumberGenerator.GetBytes(12);
    var tag = new byte[16];
    var ciphertext = new byte[plaintext.Length];

    aes.Encrypt(nonce, plaintext, ciphertext, tag);

    // Return: nonce + tag + ciphertext (base64)
    return Convert.ToBase64String(combined);
}
```

**Issues:**
- ❌ Keys lost on service restart
- ❌ No key backup or recovery
- ❌ No protection against memory dumps
- ❌ Not suitable for production
- ✅ Correct encryption algorithm (AES-256-GCM)
- ✅ Correct format (nonce + tag + ciphertext)

### Usage in Wallet Service

**Location:** `src/Services/Sorcha.Wallet.Service/Extensions/WalletServiceExtensions.cs`

```csharp
// Line 41: Hardcoded to LocalEncryptionProvider
services.AddSingleton<IEncryptionProvider, LocalEncryptionProvider>();
```

**What Gets Encrypted:**
- **Wallet.EncryptedPrivateKey** - Main use case: private keys stored in PostgreSQL
- **Database Column:** `text` column storing base64-encoded ciphertext
- **Database Field:** `encryption_key_id` stores the key identifier used for encryption

---

## Requirements Summary

### Functional Requirements

1. **Encrypt Wallet Private Keys** - Primary use case
2. **Support Multiple Keys** - Key rotation and versioning via `keyId`
3. **Survive Service Restarts** - Keys must be persisted or retrievable
4. **Platform Independence** - Work across Windows, Linux, macOS
5. **Cloud Integration** - Azure Key Vault for production deployments
6. **Key Management** - Create, check existence, get default key
7. **Backward Compatibility** - Existing wallets encrypted with old provider must still decrypt

### Security Requirements

1. **Strong Encryption** - AES-256-GCM (authenticated encryption)
2. **Key Protection** - Keys must not be stored in plaintext
3. **Separation of Concerns** - Data Encryption Keys (DEK) vs Master Encryption Keys (MEK)
4. **Audit Logging** - Log all key operations (create, encrypt, decrypt)
5. **Key Rotation** - Support changing encryption keys without re-encrypting all data
6. **No Custom Crypto** - Use compiled OS/infrastructure implementations

### Non-Functional Requirements

1. **Performance** - <50ms encryption/decryption latency (p95)
2. **Availability** - Read-through cache with stale tolerance for Azure Key Vault outages (decryption continues with cached keys, new wallet creation fails gracefully with retry)
3. **Configuration** - Runtime provider selection via configuration
4. **Testing** - Unit tests, integration tests, performance tests
5. **Documentation** - Clear setup instructions for each provider
6. **Audit Logging** - Structured logging for all encryption operations (timestamp, keyId, operation type, success/failure, user context) with sensitive data sanitization

---

## Proposed Architecture

### Provider Hierarchy

```
IEncryptionProvider (interface)
├── LocalEncryptionProvider (development only, in-memory keys)
├── AzureKeyVaultEncryptionProvider (production, cloud)
├── WindowsDpapiEncryptionProvider (production, Windows servers)
├── MacOsKeychainEncryptionProvider (development, macOS)
└── LinuxSecretServiceEncryptionProvider (production, Linux servers)
```

### Encryption Pattern: Envelope Encryption

**Recommended Pattern:**
```
User Data (Private Key)
    ↓
Encrypt with DEK (Data Encryption Key)
    ↓
Encrypted Private Key + DEK ID
    ↓
DEK encrypted with MEK (Master Encryption Key)
    ↓
MEK stored in Azure Key Vault / OS keystore
```

**Simplified Pattern (Current):**
```
User Data (Private Key)
    ↓
Encrypt with MEK directly (keyId = MEK reference)
    ↓
Encrypted Private Key + MEK ID
    ↓
MEK stored in Azure Key Vault / OS keystore
```

**Decision:** Start with simplified pattern (encrypt directly with MEK), add envelope encryption later if needed for performance/cost.

### Provider Selection Flow

```
Configuration: EncryptionProvider__Type = "AzureKeyVault" | "WindowsDpapi" | "MacOsKeychain" | "LinuxSecretService" | "Local"
    ↓
Provider Factory reads configuration
    ↓
Validate provider available for current platform
    ↓
Register provider in DI container
    ↓
Service uses IEncryptionProvider (abstraction)
```

---

## Provider Implementations

### 1. Azure Key Vault Provider (Production - Cloud)

**Purpose:** Production encryption for cloud-hosted services

**Implementation Strategy:**
- **SDK:** `Azure.Security.KeyVault.Keys` (official Azure SDK)
- **Authentication:** Azure Managed Identity (production) or DefaultAzureCredential (development)
- **Key Storage:** AES-256 keys stored in Azure Key Vault
- **Encryption:** Use Key Vault's cryptographic operations (encrypt/decrypt)
- **Key ID Format:** `https://{vault-name}.vault.azure.net/keys/{key-name}/{version}`
- **Caching Strategy:** Read-through cache with stale tolerance - Cache DEKs in memory with extended TTL during outages, allow decryption to continue, fail new wallet creation gracefully with exponential backoff retry

**Code Outline:**
```csharp
public class AzureKeyVaultEncryptionProvider : IEncryptionProvider
{
    private readonly KeyClient _keyClient;
    private readonly CryptographyClient _cryptoClient;
    private readonly string _defaultKeyName;

    public AzureKeyVaultEncryptionProvider(string vaultUri, string defaultKeyName)
    {
        // Use DefaultAzureCredential (supports Managed Identity, CLI, Environment)
        _keyClient = new KeyClient(new Uri(vaultUri), new DefaultAzureCredential());
        _defaultKeyName = defaultKeyName;
    }

    public async Task<string> EncryptAsync(byte[] plaintext, string keyId, CancellationToken ct)
    {
        // Get key reference from Key Vault
        var key = await _keyClient.GetKeyAsync(keyId, cancellationToken: ct);
        var cryptoClient = new CryptographyClient(key.Value.Id, new DefaultAzureCredential());

        // Use Key Vault's encrypt operation (AES-GCM)
        var result = await cryptoClient.EncryptAsync(
            EncryptionAlgorithm.A256Gcm,
            plaintext,
            cancellationToken: ct);

        // Return ciphertext + authentication tag (base64)
        return Convert.ToBase64String(result.Ciphertext);
    }

    public async Task CreateKeyAsync(string keyId, CancellationToken ct)
    {
        // Create AES-256 key in Key Vault
        var keyOptions = new CreateKeyOptions(keyId, KeyType.Oct)
        {
            KeySize = 256, // AES-256
            KeyOperations = { KeyOperation.Encrypt, KeyOperation.Decrypt }
        };

        await _keyClient.CreateKeyAsync(keyOptions, ct);
    }

    // Implement other interface methods...
}
```

**Configuration:**
```json
{
  "EncryptionProvider": {
    "Type": "AzureKeyVault",
    "AzureKeyVault": {
      "VaultUri": "https://sorcha-keyvault.vault.azure.net/",
      "DefaultKeyName": "wallet-encryption-key-2025",
      "UseManagedIdentity": true
    }
  }
}
```

**Azure Setup:**
```bash
# Create Key Vault
az keyvault create --name sorcha-keyvault --resource-group sorcha-rg --location eastus

# Create encryption key
az keyvault key create --vault-name sorcha-keyvault --name wallet-encryption-key-2025 --kty oct --size 256

# Grant service identity access
az keyvault set-policy --name sorcha-keyvault --object-id <service-principal-id> --key-permissions encrypt decrypt get
```

**Pros:**
- ✅ Enterprise-grade key management
- ✅ Automatic key backup and recovery
- ✅ Audit logging built-in
- ✅ Supports key rotation
- ✅ HSM-backed keys optional (Premium tier)
- ✅ No custom crypto code

**Cons:**
- ❌ Requires Azure subscription
- ❌ Network dependency (latency ~10-50ms per operation)
- ❌ Cost per 10,000 operations (~$0.03)
- ❌ Requires Azure credentials

**Best For:** Production deployments on Azure, high-security requirements

---

### 2. Windows DPAPI Provider (Production - Windows)

**Purpose:** Production encryption for Windows servers without Azure dependency

**Implementation Strategy:**
- **API:** `System.Security.Cryptography.ProtectedData` (built-in .NET)
- **Key Storage:** Windows Data Protection API (DPAPI) using machine or user credentials
- **Scope:** `DataProtectionScope.LocalMachine` for service accounts
- **Encryption:** DPAPI encrypts DEK, we encrypt data with DEK using AES-GCM
- **Key Persistence:** File-based storage on persistent Docker volume (mounted to `/app/keys` or `C:\app\keys`), ensuring DEKs survive container restarts

**Code Outline:**
```csharp
public class WindowsDpapiEncryptionProvider : IEncryptionProvider
{
    private readonly ConcurrentDictionary<string, byte[]> _keys = new();
    private readonly string _keyStorePath;

    public bool IsAvailable => OperatingSystem.IsWindows();

    public WindowsDpapiEncryptionProvider(string keyStorePath)
    {
        if (!IsAvailable)
            throw new PlatformNotSupportedException("Windows DPAPI is only available on Windows.");

        _keyStorePath = keyStorePath;
        LoadKeys(); // Load DPAPI-encrypted keys from disk
    }

    public async Task<string> EncryptAsync(byte[] plaintext, string keyId, CancellationToken ct)
    {
        // Get DEK (decrypted from DPAPI-protected storage)
        var dek = await GetOrCreateKeyAsync(keyId, ct);

        // Encrypt data with DEK using AES-GCM (same as LocalEncryptionProvider)
        using var aes = new AesGcm(dek, AesGcm.TagByteSizes.MaxSize);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var ciphertext = new byte[plaintext.Length];

        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        // Return nonce + tag + ciphertext (base64)
        var combined = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, combined, nonce.Length + tag.Length, ciphertext.Length);

        return Convert.ToBase64String(combined);
    }

    public async Task CreateKeyAsync(string keyId, CancellationToken ct)
    {
        // Generate random DEK
        var dek = RandomNumberGenerator.GetBytes(32); // AES-256

        // Encrypt DEK with DPAPI (LocalMachine scope for service accounts)
        var encryptedDek = ProtectedData.Protect(
            dek,
            optionalEntropy: Encoding.UTF8.GetBytes($"sorcha-wallet-{keyId}"), // Additional entropy
            scope: DataProtectionScope.LocalMachine);

        // Store encrypted DEK to disk
        var keyFilePath = Path.Combine(_keyStorePath, $"{keyId}.key");
        await File.WriteAllBytesAsync(keyFilePath, encryptedDek, ct);

        // Cache decrypted DEK in memory
        _keys[keyId] = dek;
    }

    private void LoadKeys()
    {
        // Load all encrypted DEKs from disk and decrypt with DPAPI
        foreach (var keyFile in Directory.GetFiles(_keyStorePath, "*.key"))
        {
            var keyId = Path.GetFileNameWithoutExtension(keyFile);
            var encryptedDek = File.ReadAllBytes(keyFile);

            var dek = ProtectedData.Unprotect(
                encryptedDek,
                optionalEntropy: Encoding.UTF8.GetBytes($"sorcha-wallet-{keyId}"),
                scope: DataProtectionScope.LocalMachine);

            _keys[keyId] = dek;
        }
    }
}
```

**Configuration:**
```json
{
  "EncryptionProvider": {
    "Type": "WindowsDpapi",
    "WindowsDpapi": {
      "KeyStorePath": "C:\\app\\keys",  // Docker: Use volume mount path
      "DefaultKeyId": "wallet-key-2025"
    }
  }
}
```

**Docker Volume Configuration:**
```yaml
# docker-compose.yml
services:
  wallet-service:
    volumes:
      - wallet-encryption-keys:/app/keys  # Persistent volume for DEKs

volumes:
  wallet-encryption-keys:
```

**Setup:**
```powershell
# Create key storage directory with proper ACLs
$keyPath = "C:\ProgramData\Sorcha\WalletKeys"
New-Item -Path $keyPath -ItemType Directory -Force

# Grant service account read/write access
$acl = Get-Acl $keyPath
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("NT SERVICE\SorchaWalletService", "ReadWrite", "Allow")
$acl.SetAccessRule($rule)
Set-Acl $keyPath $acl
```

**Pros:**
- ✅ No external dependencies (built-in Windows API)
- ✅ No network latency
- ✅ Free (no Azure costs)
- ✅ Keys survive service restarts
- ✅ Machine/user-specific encryption
- ✅ No custom crypto for key protection

**Cons:**
- ❌ Windows-only
- ❌ Tied to machine (can't move encrypted data to another machine)
- ❌ No built-in key rotation
- ❌ Manual key backup required

**Best For:** Windows server deployments without Azure, on-premises installations

---

### 3. macOS Keychain Provider (Development - macOS)

**Purpose:** Development encryption for developers on macOS

**Implementation Strategy:**
- **API:** `security` command-line tool (macOS built-in)
- **Key Storage:** macOS Keychain (managed by OS)
- **Encryption:** Store DEKs in Keychain, encrypt data with AES-GCM
- **Access Control:** Keychain ACLs managed by OS

**Code Outline:**
```csharp
public class MacOsKeychainEncryptionProvider : IEncryptionProvider
{
    private const string ServiceName = "sorcha-wallet-service";

    public bool IsAvailable => OperatingSystem.IsMacOS();

    public async Task<string> EncryptAsync(byte[] plaintext, string keyId, CancellationToken ct)
    {
        // Retrieve DEK from Keychain
        var dek = await RetrieveDekFromKeychainAsync(keyId, ct);

        // Encrypt with AES-GCM (same as Windows DPAPI implementation)
        using var aes = new AesGcm(dek, AesGcm.TagByteSizes.MaxSize);
        // ... (same encryption logic)
    }

    public async Task CreateKeyAsync(string keyId, CancellationToken ct)
    {
        // Generate DEK
        var dek = RandomNumberGenerator.GetBytes(32);

        // Store DEK in Keychain using `security` command
        // security add-generic-password -s sorcha-wallet-service -a {keyId} -w {base64_dek} -U
        var dekBase64 = Convert.ToBase64String(dek);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "security",
                Arguments = $"add-generic-password -s {ServiceName} -a {keyId} -w \"{dekBase64}\" -U",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Failed to store key in Keychain: {error}");
        }
    }

    private async Task<byte[]> RetrieveDekFromKeychainAsync(string keyId, CancellationToken ct)
    {
        // security find-generic-password -s sorcha-wallet-service -a {keyId} -w
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "security",
                Arguments = $"find-generic-password -s {ServiceName} -a {keyId} -w",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Key not found in Keychain: {keyId}");

        return Convert.FromBase64String(output.Trim());
    }
}
```

**Pros:**
- ✅ OS-managed key storage
- ✅ Automatic backup (iCloud Keychain)
- ✅ User-friendly (integrates with macOS security)
- ✅ No custom key management code

**Cons:**
- ❌ macOS-only
- ❌ Requires `security` command (external dependency)
- ❌ User must grant Keychain access (interactive prompt first time)

**Best For:** Local development on macOS, developer workstations

---

### 4. Linux Secret Service Provider (Production - Linux)

**Purpose:** Production encryption for Linux servers using freedesktop.org Secret Service

**Implementation Strategy:**
- **API:** D-Bus Secret Service API (freedesktop.org standard)
- **Key Storage:** GNOME Keyring, KWallet, or other Secret Service implementations
- **Fallback:** File-based storage with machine-derived encryption (if Secret Service unavailable)
- **Encryption:** Store DEKs in Secret Service, encrypt data with AES-GCM

**Code Outline:**
```csharp
public class LinuxSecretServiceEncryptionProvider : IEncryptionProvider
{
    private readonly bool _secretServiceAvailable;
    private readonly string _fallbackKeyPath;

    public bool IsAvailable => OperatingSystem.IsLinux();

    public LinuxSecretServiceEncryptionProvider(string fallbackKeyPath)
    {
        if (!IsAvailable)
            throw new PlatformNotSupportedException("Linux Secret Service is only available on Linux.");

        _secretServiceAvailable = CheckSecretServiceAvailable();
        _fallbackKeyPath = fallbackKeyPath;
    }

    public async Task<string> EncryptAsync(byte[] plaintext, string keyId, CancellationToken ct)
    {
        byte[] dek;

        if (_secretServiceAvailable)
        {
            // Retrieve DEK from Secret Service via D-Bus
            dek = await RetrieveDekFromSecretServiceAsync(keyId, ct);
        }
        else
        {
            // Fallback: Retrieve DEK from file (encrypted with machine-derived key)
            dek = await RetrieveDekFromFileAsync(keyId, ct);
        }

        // Encrypt with AES-GCM
        // ... (same as other providers)
    }

    public async Task CreateKeyAsync(string keyId, CancellationToken ct)
    {
        var dek = RandomNumberGenerator.GetBytes(32);

        if (_secretServiceAvailable)
        {
            // Store in Secret Service
            await StoreDekInSecretServiceAsync(keyId, dek, ct);
        }
        else
        {
            // Fallback: Store in file encrypted with machine-derived key
            await StoreDekInFileAsync(keyId, dek, ct);
        }
    }

    private bool CheckSecretServiceAvailable()
    {
        // Check if D-Bus Secret Service is available
        // org.freedesktop.secrets interface
        // Implementation: use D-Bus libraries or `secret-tool` command
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "secret-tool",
                Arguments = "search --all service sorcha-wallet",
                RedirectStandardOutput = true,
                UseShellExecute = false
            });

            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    // Fallback implementation (machine-derived encryption, similar to CLI LinuxEncryption)
    private async Task StoreDekInFileAsync(string keyId, byte[] dek, CancellationToken ct)
    {
        // Derive machine key from username + machine-id + salt
        var machineKey = DeriveMachineKey();

        // Encrypt DEK with machine key using AES-GCM
        using var aes = new AesGcm(machineKey, AesGcm.TagByteSizes.MaxSize);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var encryptedDek = new byte[dek.Length];

        aes.Encrypt(nonce, dek, encryptedDek, tag);

        // Store nonce + tag + encryptedDek
        var keyFile = Path.Combine(_fallbackKeyPath, $"{keyId}.key");
        var combined = new byte[nonce.Length + tag.Length + encryptedDek.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
        Buffer.BlockCopy(encryptedDek, 0, combined, nonce.Length + tag.Length, encryptedDek.Length);

        await File.WriteAllBytesAsync(keyFile, combined, ct);
    }

    private byte[] DeriveMachineKey()
    {
        // Same logic as CLI LinuxEncryption
        var username = Environment.UserName;
        var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var machineId = GetMachineId(); // Read /etc/machine-id

        var keyMaterial = $"{username}:{homePath}:{machineId}:sorcha-wallet-v1";

        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(keyMaterial),
            Encoding.UTF8.GetBytes("sorcha-wallet-salt"),
            iterations: 100000,
            HashAlgorithmName.SHA256,
            outputLength: 32);
    }
}
```

**Pros:**
- ✅ OS-managed key storage (if Secret Service available)
- ✅ Fallback for environments without Secret Service
- ✅ No network dependency
- ✅ Free (no cloud costs)

**Cons:**
- ❌ Linux-only
- ❌ Requires Secret Service or fallback to file-based
- ❌ Fallback is less secure (machine-derived keys)

**Best For:** Linux server deployments, Docker containers with Secret Service

---

## Configuration Strategy

### Provider Selection Configuration

**Configuration File:** `appsettings.json`

```json
{
  "EncryptionProvider": {
    "Type": "AzureKeyVault",  // "AzureKeyVault" | "WindowsDpapi" | "MacOsKeychain" | "LinuxSecretService" | "Local"

    "AzureKeyVault": {
      "VaultUri": "https://sorcha-keyvault.vault.azure.net/",
      "DefaultKeyName": "wallet-encryption-key-2025",
      "UseManagedIdentity": true,
      "TenantId": null,  // Optional: Specify for multi-tenant Azure
      "ClientId": null,  // Optional: For service principal auth
      "ClientSecret": null  // Optional: For service principal auth (use Key Vault reference!)
    },

    "WindowsDpapi": {
      "KeyStorePath": "C:\\ProgramData\\Sorcha\\WalletKeys",
      "DefaultKeyId": "wallet-key-2025",
      "Scope": "LocalMachine"  // "CurrentUser" | "LocalMachine"
    },

    "MacOsKeychain": {
      "ServiceName": "sorcha-wallet-service",
      "DefaultKeyId": "wallet-key-2025"
    },

    "LinuxSecretService": {
      "ServiceName": "sorcha-wallet-service",
      "DefaultKeyId": "wallet-key-2025",
      "FallbackKeyStorePath": "/var/lib/sorcha/wallet-keys",
      "UseSecretService": true
    },

    "Local": {
      "DefaultKeyId": "default"
    }
  }
}
```

### Provider Registration (DI)

**Updated:** `src/Services/Sorcha.Wallet.Service/Extensions/WalletServiceExtensions.cs`

```csharp
public static IServiceCollection AddWalletService(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... existing code ...

    // Register encryption provider based on configuration
    services.AddEncryptionProvider(configuration);

    // ... rest of existing code ...
}

public static IServiceCollection AddEncryptionProvider(
    this IServiceCollection services,
    IConfiguration configuration)
{
    var providerType = configuration["EncryptionProvider:Type"] ?? "Local";

    switch (providerType)
    {
        case "AzureKeyVault":
            var vaultUri = configuration["EncryptionProvider:AzureKeyVault:VaultUri"]
                ?? throw new InvalidOperationException("Azure Key Vault URI not configured");
            var defaultKeyName = configuration["EncryptionProvider:AzureKeyVault:DefaultKeyName"] ?? "default-key";

            services.AddSingleton<IEncryptionProvider>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<AzureKeyVaultEncryptionProvider>>();
                return new AzureKeyVaultEncryptionProvider(vaultUri, defaultKeyName, logger);
            });
            break;

        case "WindowsDpapi":
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Windows DPAPI is only available on Windows");

            var keyStorePath = configuration["EncryptionProvider:WindowsDpapi:KeyStorePath"]
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Sorcha", "WalletKeys");

            services.AddSingleton<IEncryptionProvider>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<WindowsDpapiEncryptionProvider>>();
                return new WindowsDpapiEncryptionProvider(keyStorePath, logger);
            });
            break;

        case "MacOsKeychain":
            if (!OperatingSystem.IsMacOS())
                throw new PlatformNotSupportedException("macOS Keychain is only available on macOS");

            services.AddSingleton<IEncryptionProvider>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<MacOsKeychainEncryptionProvider>>();
                return new MacOsKeychainEncryptionProvider(logger);
            });
            break;

        case "LinuxSecretService":
            if (!OperatingSystem.IsLinux())
                throw new PlatformNotSupportedException("Linux Secret Service is only available on Linux");

            var fallbackPath = configuration["EncryptionProvider:LinuxSecretService:FallbackKeyStorePath"]
                ?? "/var/lib/sorcha/wallet-keys";

            services.AddSingleton<IEncryptionProvider>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<LinuxSecretServiceEncryptionProvider>>();
                return new LinuxSecretServiceEncryptionProvider(fallbackPath, logger);
            });
            break;

        case "Local":
        default:
            services.AddSingleton<IEncryptionProvider, LocalEncryptionProvider>();
            Console.WriteLine("⚠️  WARNING: Using LocalEncryptionProvider (development only, keys not persisted)");
            break;
    }

    return services;
}
```

---

## Testing Approach

### Unit Tests

**Test Project:** `tests/Sorcha.Wallet.Core.Tests/Encryption/`

**Test Cases (Per Provider):**
```csharp
[Fact]
public async Task EncryptAsync_WithValidData_ReturnsBase64String()
{
    // Arrange
    var provider = CreateProvider();
    var plaintext = "sensitive-private-key-data"u8.ToArray();
    var keyId = provider.GetDefaultKeyId();

    // Act
    var ciphertext = await provider.EncryptAsync(plaintext, keyId);

    // Assert
    Assert.False(string.IsNullOrEmpty(ciphertext));
    Assert.True(IsBase64String(ciphertext));
}

[Fact]
public async Task DecryptAsync_WithEncryptedData_ReturnsOriginalPlaintext()
{
    // Arrange
    var provider = CreateProvider();
    var plaintext = "sensitive-private-key-data"u8.ToArray();
    var keyId = provider.GetDefaultKeyId();

    // Act
    var ciphertext = await provider.EncryptAsync(plaintext, keyId);
    var decrypted = await provider.DecryptAsync(ciphertext, keyId);

    // Assert
    Assert.Equal(plaintext, decrypted);
}

[Fact]
public async Task CreateKeyAsync_CreatesNewKey()
{
    // Arrange
    var provider = CreateProvider();
    var keyId = $"test-key-{Guid.NewGuid()}";

    // Act
    await provider.CreateKeyAsync(keyId);

    // Assert
    var exists = await provider.KeyExistsAsync(keyId);
    Assert.True(exists);
}

[Fact]
public async Task EncryptAsync_SurvivesServiceRestart()
{
    // Arrange
    var provider1 = CreateProvider();
    var keyId = provider1.GetDefaultKeyId();
    var plaintext = "test-data"u8.ToArray();
    var ciphertext = await provider1.EncryptAsync(plaintext, keyId);

    // Simulate restart: Create new provider instance
    var provider2 = CreateProvider();

    // Act
    var decrypted = await provider2.DecryptAsync(ciphertext, keyId);

    // Assert
    Assert.Equal(plaintext, decrypted);
}
```

### Integration Tests

**Azure Key Vault Integration Tests:**
```csharp
[Fact]
[Trait("Category", "Integration")]
[Trait("Category", "Azure")]
public async Task AzureKeyVault_EncryptDecrypt_WorksEndToEnd()
{
    // Requires Azure Key Vault instance
    // Run with: dotnet test --filter "Category=Azure"
    // Skip if AZURE_KEYVAULT_URI not set

    var vaultUri = Environment.GetEnvironmentVariable("AZURE_KEYVAULT_URI");
    if (string.IsNullOrEmpty(vaultUri))
    {
        // Use Skip attribute or return early
        return;
    }

    var provider = new AzureKeyVaultEncryptionProvider(vaultUri, "test-key", logger);
    // ... test encryption/decryption ...
}
```

**Platform-Specific Tests:**
```csharp
[Fact]
[Trait("Platform", "Windows")]
public async Task WindowsDpapi_EncryptDecrypt_WorksOnWindows()
{
    // Only runs on Windows CI agents
    if (!OperatingSystem.IsWindows())
    {
        return; // Skip on other platforms
    }

    var provider = new WindowsDpapiEncryptionProvider(tempPath, logger);
    // ... test encryption/decryption ...
}
```

### Performance Tests

**Benchmark:** `tests/Sorcha.Wallet.Service.Benchmarks/EncryptionBenchmarks.cs`

```csharp
[MemoryDiagnoser]
public class EncryptionBenchmarks
{
    private IEncryptionProvider _provider;
    private byte[] _testData;
    private string _ciphertext;

    [GlobalSetup]
    public void Setup()
    {
        _provider = new WindowsDpapiEncryptionProvider("./keys", NullLogger.Instance);
        _testData = RandomNumberGenerator.GetBytes(256); // Typical private key size
    }

    [Benchmark]
    public async Task<string> Encrypt()
    {
        return await _provider.EncryptAsync(_testData, "default");
    }

    [Benchmark]
    public async Task<byte[]> Decrypt()
    {
        return await _provider.DecryptAsync(_ciphertext, "default");
    }
}
```

**Success Criteria:**
- **Encryption:** <50ms (p95) for Azure Key Vault, <10ms for local providers
- **Decryption:** <50ms (p95) for Azure Key Vault, <10ms for local providers
- **Memory:** <1 MB allocations per operation

---

## Migration Path

### Phase 1: Implement Providers (Week 1-2)

**Implementation Order:** Local providers first (faster development, no Azure dependencies), then cloud

**Week 1 - Local Providers (Priority 1-3):**
1. [ ] Create provider interfaces and base classes
2. [ ] Implement structured audit logging framework
3. [ ] Implement `WindowsDpapiEncryptionProvider` with Docker volume support
4. [ ] Implement `LinuxSecretServiceEncryptionProvider` with fallback
5. [ ] Implement `MacOsKeychainEncryptionProvider` (development)
6. [ ] Update configuration system for provider selection
7. [ ] Update `WalletServiceExtensions` for provider registration
8. [ ] Unit tests for each local provider

**Week 2 - Cloud Provider (Priority 4):**
9. [ ] Implement `AzureKeyVaultEncryptionProvider` with caching strategy
10. [ ] Integration tests with test Azure Key Vault instance
11. [ ] Test failover and caching behavior

### Phase 2: Testing (Week 1)

1. ✅ Write unit tests for each provider
2. ✅ Write integration tests (Azure, platform-specific)
3. ✅ Write performance benchmarks
4. ✅ Test provider switching (configuration changes)
5. ✅ Test key rotation scenarios

### Phase 3: Documentation (Week 1)

1. ✅ Update README with provider setup instructions
2. ✅ Create Azure Key Vault setup guide
3. ✅ Create Windows DPAPI setup guide
4. ✅ Create Linux setup guide
5. ✅ Update deployment documentation

### Phase 4: Deployment (Week 2)

1. ✅ Update Docker Compose with provider configuration
2. ✅ Update Azure deployment templates
3. ✅ Create migration script for existing wallets (if needed)
4. ✅ Update CI/CD pipelines
5. ✅ Conduct security review

---

## Security Considerations

### Key Management Best Practices

1. **Default Key Rotation**
   - **Strategy:** Automatic rotation with backward compatibility
   - Create new default key every 90 days
   - New wallets automatically use new key via `GetDefaultKeyId()`
   - Existing wallets continue using their original key (keyId stored per wallet in database)
   - Keep old keys for decryption indefinitely (never delete, required for backward compatibility)
   - Optional: Background re-encryption job to migrate old wallets to new key (performance optimization, not required for security)

2. **Key Separation**
   - Production keys: Azure Key Vault with HSM
   - Staging keys: Azure Key Vault (software)
   - Development keys: OS-specific providers
   - Never share keys across environments

3. **Access Control**
   - Azure Key Vault: Grant only encrypt/decrypt permissions to service identity
   - Windows DPAPI: Lock down key file directory with ACLs
   - Linux: Use Secret Service or file permissions (chmod 600)

4. **Audit Logging**
   - **Level:** Structured logging with sanitization for all encryption operations
   - **Log Format:** JSON-structured logs with consistent schema
   - **Fields Logged:**
     - Timestamp (ISO 8601 UTC)
     - Operation type (CreateKey, Encrypt, Decrypt, KeyExists)
     - Key ID (keyId parameter)
     - Success/failure status
     - User context (subject/tenant if available)
     - Error details (if failed)
     - Performance metrics (operation duration)
   - **Sanitization Rules:**
     - NEVER log plaintext data or decrypted keys
     - NEVER log ciphertext (only log that encryption occurred)
     - NEVER log key material (DEKs, MEKs)
     - Redact sensitive fields in exceptions
   - **Destination:** Send logs to centralized logging (Azure Monitor, Application Insights, ELK)
   - **Retention:** Minimum 90 days for compliance, 1 year recommended

5. **Backup and Recovery**
   - Azure Key Vault: Automatic backup enabled
   - Windows DPAPI: Include key directory in system backups
   - Linux: Backup key files or Secret Service store
   - Test recovery process quarterly

### Threat Model

**Threats Mitigated:**
- ✅ Memory dumps (keys not in plaintext memory for long)
- ✅ Database compromise (private keys encrypted at rest)
- ✅ Service restart (keys persisted outside service)
- ✅ Unauthorized decryption (requires access to key store)

**Threats Not Mitigated:**
- ❌ Compromised service account (can access keys)
- ❌ Side-channel attacks (timing, cache)
- ❌ Supply chain attacks (compromised dependencies)
- ❌ Physical server access (attacker with root/admin)

**Recommendations:**
- Use Azure Key Vault with Managed Identity (no credentials in code)
- Use HSM-backed keys for high-value wallets
- Implement rate limiting on encryption operations
- Monitor for unusual encryption activity

---

## Implementation Tasks

### Task Breakdown

**Implementation Order: Local → Cloud**

**Core Infrastructure - Local Providers (P0, Week 1):**
- [ ] Implement structured audit logging framework (base for all providers)
- [ ] Create `src/Common/Sorcha.Wallet.Core/Encryption/Providers/WindowsDpapiEncryptionProvider.cs` with Docker volume support
- [ ] Create `src/Common/Sorcha.Wallet.Core/Encryption/Providers/LinuxSecretServiceEncryptionProvider.cs` with fallback
- [ ] Create `src/Common/Sorcha.Wallet.Core/Encryption/Providers/MacOsKeychainEncryptionProvider.cs`
- [ ] Add configuration models for local providers
- [ ] Update `src/Services/Sorcha.Wallet.Service/Extensions/WalletServiceExtensions.cs` (local provider registration)

**Core Infrastructure - Azure Key Vault (P1, Week 2):**
- [ ] Create `src/Common/Sorcha.Wallet.Core/Encryption/Providers/AzureKeyVaultEncryptionProvider.cs` with caching
- [ ] Add configuration models for Azure Key Vault provider
- [ ] Update `WalletServiceExtensions.cs` (Azure provider registration)

**Testing (P0):**
- [ ] Unit tests for each provider (core encryption/decryption)
- [ ] Integration tests for Azure Key Vault (requires Azure resources)
- [ ] Platform-specific tests (Windows, macOS, Linux)
- [ ] Performance benchmarks (BenchmarkDotNet)

**Documentation (P1):**
- [ ] Update `README.md` with provider setup instructions
- [ ] Create `docs/AZURE-KEYVAULT-SETUP.md`
- [ ] Create `docs/WINDOWS-DPAPI-SETUP.md`
- [ ] Create `docs/LINUX-ENCRYPTION-SETUP.md`
- [ ] Update `.specify/specs/sorcha-wallet-service.md`

**Deployment (P1):**
- [ ] Update `docker-compose.yml` with provider configuration examples
- [ ] Update `src/Services/Sorcha.Wallet.Service/appsettings.json`
- [ ] Create Azure deployment template with Key Vault
- [ ] Update CI/CD to run platform-specific tests

**Optional Enhancements (P2):**
- [ ] Implement envelope encryption (DEK + MEK) for performance
- [ ] Add key rotation background job
- [ ] Add key versioning support
- [ ] Add HSM support (Azure Key Vault Premium)
- [ ] Add AWS KMS provider
- [ ] Add TPM 2.0 provider (Windows TPM)

---

## Next Steps

**Implementation Priority:** Local providers first (faster development, no Azure dependencies), then cloud

1. **Phase 1A - Local Providers (Week 1)**
   - Implement structured audit logging framework (base for all providers)
   - Implement `WindowsDpapiEncryptionProvider` with Docker volume persistence
   - Implement `LinuxSecretServiceEncryptionProvider` with file fallback
   - Implement `MacOsKeychainEncryptionProvider` for development
   - Update configuration system and DI registration
   - Write unit tests for each local provider
   - Test automatic key rotation with backward compatibility

2. **Phase 1B - Azure Key Vault (Week 2)**
   - Set up Azure Key Vault instance for development/testing
   - Implement `AzureKeyVaultEncryptionProvider` with read-through caching
   - Write unit and integration tests for Azure provider
   - Validate caching strategy under simulated outage conditions

3. **Phase 2 - Testing & Documentation (Week 3)**
   - Complete platform-specific integration tests
   - Run performance benchmarks (target <50ms p95 for Azure, <10ms for local)
   - Create setup guides (Windows, Linux, macOS, Azure)
   - Update deployment documentation (Docker, Azure templates)

4. **Phase 3 - Deployment (Week 4)**
   - Update docker-compose.yml with volume configuration
   - Create Azure deployment templates with Key Vault
   - Update CI/CD pipelines for platform-specific tests
   - Conduct security review and penetration testing

---

## References

- [Azure Key Vault Best Practices](https://learn.microsoft.com/azure/key-vault/general/best-practices)
- [Azure.Security.KeyVault.Keys SDK](https://learn.microsoft.com/dotnet/api/azure.security.keyvault.keys)
- [Windows Data Protection API (DPAPI)](https://learn.microsoft.com/windows/win32/api/dpapi/)
- [freedesktop.org Secret Service API](https://specifications.freedesktop.org/secret-service/)
- [macOS Keychain Services](https://developer.apple.com/documentation/security/keychain_services)
- [.NET Cryptography Documentation](https://learn.microsoft.com/dotnet/api/system.security.cryptography)

---

**Document Version:** 1.0
**Last Updated:** 2026-01-11
**Status:** Draft for Review
**Next Review:** After Phase 1 implementation
