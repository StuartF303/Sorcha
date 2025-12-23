# Quickstart: Hardware Cryptographic Storage and Execution Enclaves

**Feature**: 001-hardware-crypto-enclaves
**Date**: 2025-12-23
**Audience**: Developers implementing or integrating with the cryptographic backend abstraction

This guide provides quick-start examples for common scenarios using the hardware cryptographic storage system.

---

## Table of Contents

1. [Installation](#installation)
2. [Configuration](#configuration)
3. [Basic Usage](#basic-usage)
4. [Azure Key Vault (Production)](#azure-key-vault-production)
5. [AWS KMS (Production)](#aws-kms-production)
6. [Local Development (OS Storage)](#local-development-os-storage)
7. [Key Rotation](#key-rotation)
8. [Key Migration](#key-migration)
9. [Testing](#testing)
10. [Troubleshooting](#troubleshooting)

---

## Installation

Add the Sorcha.Cryptography NuGet package to your project:

```bash
dotnet add package Sorcha.Cryptography --version 1.0.0
```

For specific backend providers, add additional packages:

```bash
# Azure Key Vault support
dotnet add package Azure.Security.KeyVault.Keys --version 4.6.0
dotnet add package Azure.Identity --version 1.13.0

# AWS KMS support
dotnet add package AWSSDK.KeyManagementService --version 3.7.400

# GCP Cloud KMS support
dotnet add package Google.Cloud.Kms.V1 --version 3.19.0

# Kubernetes support
dotnet add package KubernetesClient --version 15.0.1
```

---

## Configuration

### appsettings.json

```json
{
  "Cryptography": {
    "Environment": {
      "Type": "Production",
      "RequiredSecurityLevel": "HSM",
      "AllowedBackends": ["Azure", "Aws", "Gcp"],
      "BackendPrecedence": ["Azure", "Aws", "Gcp"],
      "KeyRotation": {
        "AutomaticEnabled": true,
        "IntervalDays": 90,
        "RetainDeprecatedDays": 180
      },
      "FallbackPolicy": {
        "Enabled": false
      }
    },
    "Backends": {
      "Azure": {
        "KeyVaultUrl": "https://your-keyvault.vault.azure.net/",
        "UseManagedIdentity": true
      },
      "Aws": {
        "Region": "us-east-1",
        "UseIamRole": true
      },
      "Gcp": {
        "ProjectId": "your-project-id",
        "LocationId": "global",
        "KeyRingId": "sorcha-keys"
      }
    }
  }
}
```

### Program.cs (ASP.NET Core)

```csharp
using Sorcha.Cryptography.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register cryptographic backend services
builder.Services.AddCryptographicBackends(builder.Configuration);

// Optional: Register specific backend implementations
builder.Services.AddAzureKeyVaultBackend();
builder.Services.AddAwsKmsBackend();
builder.Services.AddOsSecureStorageBackend();

// Register key storage (uses DbContext or in-memory)
builder.Services.AddKeyStorage<SqlServerKeyStorage>();

// Register domain services
builder.Services.AddKeyRotationService();
builder.Services.AddKeyMigrationService();

var app = builder.Build();

// FR-016: Fail-fast on startup if production backend unavailable
if (app.Environment.IsProduction())
{
    await app.Services.ValidateCryptographicBackendAsync();
}

app.Run();
```

---

## Basic Usage

### 1. Generate a New Key

```csharp
using Sorcha.Cryptography.Abstractions;

public class WalletService
{
    private readonly IBackendSelector _backendSelector;
    private readonly EnvironmentConfiguration _config;

    public async Task<KeyHandle> CreateWalletKeyAsync(string walletId)
    {
        // Select the best available backend
        var backend = await _backendSelector.SelectBackendAsync(_config);

        // Define security properties (FR-003: non-exportable for HSM)
        var securityProps = new KeySecurityProperties
        {
            IsExportable = false,  // Non-exportable for production
            IsHsmBacked = true,    // Must be HSM-backed
            AllowedOperations = new[] { KeyOperation.Sign, KeyOperation.Verify }
        };

        // Generate key in HSM (FR-003: key never leaves enclave)
        var keyHandle = await backend.GenerateKeyAsync(
            algorithm: CryptoAlgorithm.ECDSA_P256,
            keyId: $"wallet-{walletId}",
            securityProperties: securityProps);

        return keyHandle;
    }
}
```

### 2. Sign Data

```csharp
public async Task<byte[]> SignTransactionAsync(string keyId, byte[] transactionData)
{
    // Get the active key version
    var keyHandle = await _keyStorage.GetActiveKeyHandleAsync(keyId);
    if (keyHandle == null)
        throw new InvalidOperationException($"Key {keyId} not found");

    // Get the backend where this key is stored
    var backend = await _backendSelector.SelectBackendAsync(_config);

    // Sign data (FR-004: signing occurs in HSM, key never exposed)
    var signature = await backend.SignAsync(keyHandle, transactionData);

    return signature;
}
```

### 3. Verify Signature

```csharp
public async Task<bool> VerifyTransactionAsync(
    string keyId,
    byte[] transactionData,
    byte[] signature)
{
    // Get all key versions (might be signed with deprecated key)
    var keyVersions = await _keyStorage.GetAllKeyVersionsAsync(keyId);

    var backend = await _backendSelector.SelectBackendAsync(_config);

    // Try verification with each version (active first, then deprecated)
    foreach (var keyHandle in keyVersions)
    {
        var isValid = await backend.VerifyAsync(keyHandle, transactionData, signature);
        if (isValid)
            return true;
    }

    return false;  // Signature invalid with all key versions
}
```

---

## Azure Key Vault (Production)

### Prerequisites

1. **Azure Key Vault**: Create a Key Vault with HSM tier
2. **Managed Identity**: Enable Managed Identity on your Azure VM/App Service
3. **Access Policy**: Grant Managed Identity permissions: `keys/get`, `keys/sign`, `keys/create`

### Configuration

```json
{
  "Cryptography": {
    "Environment": {
      "Type": "Production",
      "RequiredSecurityLevel": "HSM"
    },
    "Backends": {
      "Azure": {
        "KeyVaultUrl": "https://sorcha-prod-kv.vault.azure.net/",
        "UseManagedIdentity": true,
        "TenantId": null,  // Auto-detected from managed identity
        "ClientId": null   // Auto-detected from managed identity
      }
    }
  }
}
```

### Code Example

```csharp
using Azure.Identity;
using Sorcha.Cryptography.Backends.Azure;

public class AzureKeyVaultExample
{
    public async Task DemonstrateAsync()
    {
        // DefaultAzureCredential automatically uses Managed Identity in Azure
        var credential = new DefaultAzureCredential();

        var backend = new AzureKeyVaultBackend();
        await backend.InitializeAsync(new Dictionary<string, string>
        {
            ["KeyVaultUrl"] = "https://sorcha-prod-kv.vault.azure.net/",
            ["UseManagedIdentity"] = "true"
        });

        // Generate RSA-4096 key in Azure Key Vault HSM
        var keyHandle = await backend.GenerateKeyAsync(
            CryptoAlgorithm.RSA_4096,
            "wallet-production-001",
            new KeySecurityProperties
            {
                IsExportable = false,  // HSM keys are non-exportable
                IsHsmBacked = true,
                AllowedOperations = new[] { KeyOperation.Sign, KeyOperation.Verify }
            });

        Console.WriteLine($"Key created: {keyHandle.ProviderSpecificUri}");
        // Output: Key created: https://sorcha-prod-kv.vault.azure.net/keys/wallet-production-001/abc123...
    }
}
```

### Local Development with Azure

For local development, authenticate with Azure CLI:

```bash
az login
az account set --subscription "Your Subscription"
```

`DefaultAzureCredential` will automatically use Azure CLI credentials.

---

## AWS KMS (Production)

### Prerequisites

1. **AWS KMS**: Create a Customer Managed Key (CMK) with usage: `SIGN_VERIFY`
2. **IAM Role**: Attach IAM role to EC2/ECS with permissions: `kms:CreateKey`, `kms:Sign`, `kms:GetPublicKey`
3. **Tags**: Use tags for environment separation (e.g., `Environment=Production`)

### Configuration

```json
{
  "Cryptography": {
    "Backends": {
      "Aws": {
        "Region": "us-east-1",
        "UseIamRole": true,
        "KeyTags": {
          "Environment": "Production",
          "Application": "Sorcha"
        }
      }
    }
  }
}
```

### Code Example

```csharp
using Amazon.KeyManagementService;
using Sorcha.Cryptography.Backends.Aws;

public class AwsKmsExample
{
    public async Task DemonstrateAsync()
    {
        // AmazonKeyManagementServiceClient automatically uses IAM Role
        var backend = new AwsKmsBackend();
        await backend.InitializeAsync(new Dictionary<string, string>
        {
            ["Region"] = "us-east-1",
            ["UseIamRole"] = "true"
        });

        // Generate RSA-4096 key in AWS KMS
        var keyHandle = await backend.GenerateKeyAsync(
            CryptoAlgorithm.RSA_4096,
            "wallet-production-002",
            new KeySecurityProperties
            {
                IsExportable = false,
                IsHsmBacked = true,
                AllowedOperations = new[] { KeyOperation.Sign, KeyOperation.Verify }
            });

        Console.WriteLine($"Key created: {keyHandle.ProviderSpecificUri}");
        // Output: Key created: arn:aws:kms:us-east-1:123456789012:key/abc123...
    }
}
```

### Local Development with AWS

For local development, configure AWS credentials:

```bash
aws configure
# Enter AWS Access Key ID, Secret Access Key, Region
```

AWS SDK will automatically use credentials from `~/.aws/credentials`.

---

## Local Development (OS Storage)

### Windows DPAPI

```csharp
using Sorcha.Cryptography.Backends.Os;

public class WindowsDevelopmentExample
{
    public async Task DemonstrateAsync()
    {
        var backend = new WindowsDpapiBackend();
        await backend.InitializeAsync(new Dictionary<string, string>
        {
            ["Scope"] = "CurrentUser",  // User-specific encryption
            ["StoragePath"] = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Sorcha", "Keys")
        });

        // Generate key (stored encrypted with DPAPI)
        var keyHandle = await backend.GenerateKeyAsync(
            CryptoAlgorithm.ECDSA_P256,
            "wallet-dev-001",
            new KeySecurityProperties
            {
                IsExportable = true,   // Dev keys can be exportable
                IsHsmBacked = false,   // DPAPI is software encryption
                AllowedOperations = new[] { KeyOperation.Sign, KeyOperation.Verify }
            });

        Console.WriteLine($"Key stored at: {keyHandle.ProviderSpecificUri}");
        // Output: Key stored at: file://C:/Users/Developer/AppData/Roaming/Sorcha/Keys/wallet-dev-001.key
    }
}
```

### macOS Keychain

```csharp
public class MacOsDevelopmentExample
{
    public async Task DemonstrateAsync()
    {
        var backend = new MacOsKeychainBackend();
        await backend.InitializeAsync(new Dictionary<string, string>
        {
            ["KeychainPath"] = "login.keychain",  // User's login keychain
            ["AccessControl"] = "kSecAttrAccessibleWhenUnlocked"
        });

        var keyHandle = await backend.GenerateKeyAsync(
            CryptoAlgorithm.ED25519,
            "wallet-dev-002",
            new KeySecurityProperties
            {
                IsExportable = true,
                IsHsmBacked = false,
                AllowedOperations = new[] { KeyOperation.Sign, KeyOperation.Verify }
            });

        Console.WriteLine($"Key stored in keychain: {keyHandle.ProviderSpecificUri}");
        // Output: Key stored in keychain: keychain://login.keychain/wallet-dev-002
    }
}
```

---

## Key Rotation

### Automatic Rotation

Configure automatic rotation in `appsettings.json`:

```json
{
  "Cryptography": {
    "Environment": {
      "KeyRotation": {
        "AutomaticEnabled": true,
        "IntervalDays": 90,
        "RetainDeprecatedDays": 180,
        "ValidationPeriodHours": 24
      }
    }
  }
}
```

The rotation service automatically monitors key age and initiates rotation:

```csharp
// Hosted service (background task)
public class KeyRotationHostedService : BackgroundService
{
    private readonly IKeyRotationService _rotationService;
    private readonly IKeyStorage _keyStorage;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Find keys requiring rotation
            var keysToRotate = await _keyStorage.FindKeysRequiringRotationAsync(
                _rotationPolicy, ct);

            foreach (var keyHandle in keysToRotate)
            {
                // Initiate automatic rotation (FR-022)
                var result = await _rotationService.InitiateAutomaticRotationAsync(
                    keyHandle.KeyId, ct);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "Key {KeyId} rotated successfully. New version: {Version}",
                        keyHandle.KeyId, result.NewKeyHandle?.Version);
                }
                else
                {
                    _logger.LogError(
                        "Key {KeyId} rotation failed: {Error}",
                        keyHandle.KeyId, result.ErrorMessage);
                }
            }

            // Check every hour
            await Task.Delay(TimeSpan.FromHours(1), ct);
        }
    }
}
```

### Manual Rotation

Trigger immediate rotation (e.g., for security incidents):

```csharp
public async Task EmergencyRotationAsync(string keyId, string adminUser, string reason)
{
    var result = await _rotationService.InitiateManualRotationAsync(
        keyId,
        requestingPrincipal: adminUser,
        reason: reason);  // e.g., "Suspected key compromise"

    if (result.Success)
    {
        _logger.LogWarning(
            "Emergency rotation completed for key {KeyId}. " +
            "Old version: {OldVersion}, New version: {NewVersion}. " +
            "Reason: {Reason}",
            keyId,
            result.OldKeyHandle?.Version,
            result.NewKeyHandle?.Version,
            reason);
    }
}
```

---

## Key Migration

Migrate a key from AWS KMS to Azure Key Vault:

```csharp
public async Task MigrateKeyAsync(string keyId)
{
    var migrationService = _serviceProvider.GetRequiredService<IKeyMigrationService>();

    var approval = new ApprovalRecord(
        ApprovedBy: "admin@sorcha.com",
        ApprovedAt: DateTime.UtcNow,
        ApprovalJustification: "Migrating from AWS to Azure for unified infrastructure");

    var migrationRecord = await migrationService.MigrateKeyAsync(
        keyId,
        source: BackendType.Aws,
        destination: BackendType.Azure,
        approval: approval);

    if (migrationRecord.Status == MigrationStatus.Completed)
    {
        Console.WriteLine($"Migration successful!");
        Console.WriteLine($"Source Key: {migrationRecord.SourceKeyId}");
        Console.WriteLine($"Destination Key: {migrationRecord.DestinationKeyId}");
        Console.WriteLine($"Integrity Hash: {migrationRecord.IntegrityHash}");
    }
    else
    {
        Console.WriteLine($"Migration failed: {migrationRecord.ErrorMessage}");
    }
}
```

---

## Testing

### Unit Testing with Mocked Backend

```csharp
using Moq;
using Xunit;

public class CryptographicServiceTests
{
    [Fact]
    public async Task SignAsync_WithValidKey_ReturnsSignature()
    {
        // Arrange
        var mockBackend = new Mock<ICryptographicBackend>();
        var keyHandle = new KeyHandle
        {
            KeyId = "test-key-001",
            Version = 1,
            Algorithm = CryptoAlgorithm.ECDSA_P256,
            ProviderSpecificUri = "mock://test-key-001"
        };

        var expectedSignature = new byte[] { 0x01, 0x02, 0x03 };

        mockBackend
            .Setup(b => b.SignAsync(It.IsAny<KeyHandle>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSignature);

        // Act
        var signature = await mockBackend.Object.SignAsync(
            keyHandle, new byte[] { 0xAA, 0xBB }, CancellationToken.None);

        // Assert
        Assert.Equal(expectedSignature, signature);
    }
}
```

### Integration Testing with Testcontainers

```csharp
using Testcontainers.LocalStack;
using Xunit;

public class AwsKmsIntegrationTests : IAsyncLifetime
{
    private readonly LocalStackContainer _localStack = new LocalStackBuilder()
        .WithServices(LocalStackServices.KMS)
        .Build();

    public async Task InitializeAsync()
    {
        await _localStack.StartAsync();
    }

    [Fact]
    public async Task GenerateKey_InLocalStackKms_CreatesKey()
    {
        // Arrange
        var backend = new AwsKmsBackend();
        await backend.InitializeAsync(new Dictionary<string, string>
        {
            ["ServiceUrl"] = _localStack.GetConnectionString(),
            ["Region"] = "us-east-1",
            ["UseLocalStack"] = "true"
        });

        // Act
        var keyHandle = await backend.GenerateKeyAsync(
            CryptoAlgorithm.RSA_4096,
            "test-key-integration",
            new KeySecurityProperties
            {
                IsExportable = false,
                IsHsmBacked = true,
                AllowedOperations = new[] { KeyOperation.Sign }
            });

        // Assert
        Assert.NotNull(keyHandle);
        Assert.StartsWith("arn:aws:kms:", keyHandle.ProviderSpecificUri);
    }

    public async Task DisposeAsync()
    {
        await _localStack.DisposeAsync();
    }
}
```

---

## Troubleshooting

### Issue: Backend Selection Fails

**Error**: `BackendSelectionException: No suitable backend available`

**Solution**:
1. Check `appsettings.json` has correct backend configuration
2. Verify health check endpoints are reachable (Azure IMDS, AWS IMDS, etc.)
3. Check managed identity/IAM role permissions
4. Enable debug logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Sorcha.Cryptography": "Debug"
    }
  }
}
```

### Issue: Azure Key Vault Access Denied

**Error**: `CryptographicBackendException: Access denied to Key Vault`

**Solution**:
1. Verify Managed Identity is enabled on VM/App Service
2. Check Key Vault Access Policies: `az keyvault set-policy --name sorcha-kv --object-id <managed-identity-object-id> --key-permissions get create sign`
3. Ensure Key Vault firewall allows your IP (if restricted)

### Issue: AWS KMS Signing Timeout

**Error**: `OperationCanceledException: Operation timed out after 500ms`

**Solution**:
1. Check AWS region is correct in configuration
2. Verify IAM role has `kms:Sign` permission
3. Increase timeout in configuration:

```json
{
  "Cryptography": {
    "Backends": {
      "Aws": {
        "TimeoutSeconds": 10
      }
    }
  }
}
```

### Issue: Key Not Found During Signature Verification

**Error**: `KeyHandle not found for keyId: wallet-001`

**Solution**:
- Key may have been rotated. Ensure you're querying all versions:
  ```csharp
  var allVersions = await _keyStorage.GetAllKeyVersionsAsync(keyId);
  ```
- Check that key metadata was persisted after generation
- Verify KeyStorage database connection is healthy

---

## Next Steps

1. **Read the full specification**: [spec.md](./spec.md)
2. **Review API contracts**: [contracts/](./contracts/)
3. **See data model**: [data-model.md](./data-model.md)
4. **Implementation tasks**: [tasks.md](./tasks.md) (generated by `/speckit.tasks`)

---

**Document Version**: 1.0
**Last Updated**: 2025-12-23
**Feedback**: Report issues or suggestions in the GitHub repository
