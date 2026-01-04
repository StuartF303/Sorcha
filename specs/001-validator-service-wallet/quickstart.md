# Quick Start: Validator Service Wallet Integration

**Feature**: 001-validator-service-wallet
**Audience**: Developers implementing or testing wallet integration
**Last Updated**: 2026-01-04

## Overview

This guide provides step-by-step instructions for implementing, configuring, and testing the Validator Service wallet integration. Follow this guide to enable the Validator Service to sign dockets and consensus votes using a dedicated wallet from the Wallet Service.

---

## Prerequisites

Before starting, ensure you have:

- ✅ **.NET 10 SDK** installed (`dotnet --version` should show 10.x)
- ✅ **Sorcha.Wallet.Service** running and accessible via gRPC
- ✅ **Sorcha.Tenant.Service** running (or environment variable `VALIDATOR_WALLET_ID` set)
- ✅ **A wallet created** in the Wallet Service for the validator
- ✅ **System organization configured** in Tenant Service (organization ID: `00000000-0000-0000-0000-000000000001`)

---

## Part 1: Configuration

### Option A: Environment Variable (Development/Testing)

Fastest way to get started - no Tenant Service configuration needed.

**Step 1**: Create a wallet in the Wallet Service

```bash
# Using curl or Postman, create a wallet
curl -X POST https://localhost:7084/api/wallets \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "name": "Validator Primary Wallet",
    "algorithm": "ED25519",
    "wordCount": 24,
    "passphrase": ""
  }'

# Response will include the wallet_id (e.g., "f8e3a2b1-...")
```

**Step 2**: Set environment variable

```bash
# Linux/macOS
export VALIDATOR_WALLET_ID="f8e3a2b1-4c5d-6e7f-8a9b-0c1d2e3f4a5b"

# Windows PowerShell
$env:VALIDATOR_WALLET_ID = "f8e3a2b1-4c5d-6e7f-8a9b-0c1d2e3f4a5b"

# Windows CMD
set VALIDATOR_WALLET_ID=f8e3a2b1-4c5d-6e7f-8a9b-0c1d2e3f4a5b
```

**Step 3**: Verify configuration

```bash
# Check environment variable is set
echo $VALIDATOR_WALLET_ID  # Linux/macOS
echo %VALIDATOR_WALLET_ID%  # Windows CMD
```

---

### Option B: Tenant Service Configuration (Production)

Recommended for production deployments with centralized configuration.

**Step 1**: Update system organization in Tenant Service

```bash
# Call Tenant Service gRPC or REST API to update system org config
curl -X PATCH https://localhost:7081/api/organizations/system/config \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer ADMIN_JWT_TOKEN" \
  -d '{
    "validatorWalletId": "f8e3a2b1-4c5d-6e7f-8a9b-0c1d2e3f4a5b"
  }'
```

**Step 2**: Verify configuration

```bash
# Get system organization config
curl -X GET https://localhost:7081/api/organizations/system/config \
  -H "Authorization: Bearer ADMIN_JWT_TOKEN"

# Response should include:
# {
#   "organizationId": "00000000-0000-0000-0000-000000000001",
#   "validatorWalletId": "f8e3a2b1-4c5d-6e7f-8a9b-0c1d2e3f4a5b"
# }
```

---

### appsettings.json Configuration

Add Wallet Service endpoint and retry configuration to `appsettings.json`:

```json
{
  "WalletService": {
    "Endpoint": "https://localhost:7084",
    "RetryPolicy": {
      "MaxRetries": 3,
      "BackoffMultiplier": 2.0,
      "InitialDelaySeconds": 1
    }
  },
  "TenantService": {
    "Endpoint": "https://localhost:7081"
  },
  "SystemOrganization": {
    "OrganizationId": "00000000-0000-0000-0000-000000000001"
  }
}
```

---

## Part 2: Implementation Checklist

### Step 1: Add NuGet Packages

Edit `Sorcha.Validator.Service.csproj`:

```xml
<ItemGroup>
  <!-- gRPC Client -->
  <PackageReference Include="Grpc.Net.Client" Version="2.71.0" />
  <PackageReference Include="Grpc.Tools" Version="2.71.0" PrivateAssets="All" />

  <!-- Retry policies -->
  <PackageReference Include="Polly" Version="8.5.0" />

  <!-- Project references -->
  <ProjectReference Include="..\..\Common\Sorcha.Cryptography\Sorcha.Cryptography.csproj" />
  <ProjectReference Include="..\..\Common\Sorcha.ServiceClients\Sorcha.ServiceClients.csproj" />
</ItemGroup>

<!-- gRPC proto files -->
<ItemGroup>
  <Protobuf Include="Protos\wallet_service.proto" GrpcServices="Client" />
</ItemGroup>
```

### Step 2: Copy Proto Files

Copy `contracts/wallet_service.proto` to `src/Services/Sorcha.Validator.Service/Protos/`:

```bash
cp specs/001-validator-service-wallet/contracts/wallet_service.proto \
   src/Services/Sorcha.Validator.Service/Protos/
```

### Step 3: Create WalletConfiguration.cs

File: `src/Services/Sorcha.Validator.Service/Configuration/WalletConfiguration.cs`

```csharp
namespace Sorcha.Validator.Service.Configuration;

public class WalletConfiguration
{
    public required string WalletId { get; init; }
    public required Uri Endpoint { get; init; }
    public required RetryPolicyConfiguration RetryPolicy { get; init; }
}

public class RetryPolicyConfiguration
{
    public int MaxRetries { get; init; } = 3;
    public double BackoffMultiplier { get; init; } = 2.0;
    public int InitialDelaySeconds { get; init; } = 1;
}
```

### Step 4: Create WalletDetails.cs Model

File: `src/Services/Sorcha.Validator.Service/Models/WalletDetails.cs`

```csharp
namespace Sorcha.Validator.Service.Models;

public class WalletDetails
{
    public required string WalletId { get; init; }
    public required string Address { get; init; }
    public required byte[] PublicKey { get; init; }
    public required WalletAlgorithm Algorithm { get; init; }
    public required int Version { get; init; }
    public required DateTimeOffset CachedAt { get; init; }
}

public enum WalletAlgorithm : byte
{
    ED25519 = 1,
    NISTP256 = 2,
    RSA4096 = 3
}
```

### Step 5: Create IWalletIntegrationService Interface

File: `src/Services/Sorcha.Validator.Service/Services/IWalletIntegrationService.cs`

```csharp
using Sorcha.Validator.Service.Models;

namespace Sorcha.Validator.Service.Services;

public interface IWalletIntegrationService
{
    Task<WalletDetails> GetWalletDetailsAsync(CancellationToken ct = default);
    Task<Signature> SignDocketAsync(byte[] docketHash, CancellationToken ct = default);
    Task<Signature> SignVoteAsync(byte[] voteHash, CancellationToken ct = default);
    Task<bool> VerifySignatureAsync(byte[] signature, byte[] hash, byte[] publicKey, WalletAlgorithm algorithm, CancellationToken ct = default);
}
```

### Step 6: Implement WalletIntegrationService

File: `src/Services/Sorcha.Validator.Service/Services/WalletIntegrationService.cs`

```csharp
using Sorcha.Cryptography.Interfaces;
using Sorcha.Wallet.Service.Protos;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Models;
using Grpc.Net.Client;
using Polly;

namespace Sorcha.Validator.Service.Services;

public class WalletIntegrationService : IWalletIntegrationService, IDisposable
{
    private readonly ILogger<WalletIntegrationService> _logger;
    private readonly ICryptoModule _cryptoModule;
    private readonly WalletService.WalletServiceClient _walletClient;
    private readonly WalletConfiguration _config;
    private readonly IAsyncPolicy _retryPolicy;

    private readonly SemaphoreSlim _walletCacheLock = new(1, 1);
    private WalletDetails? _cachedWallet;

    private readonly SemaphoreSlim _derivedKeyCacheLock = new(1, 1);
    private readonly Dictionary<string, byte[]> _derivedKeyCache = new();

    public WalletIntegrationService(
        ILogger<WalletIntegrationService> logger,
        ICryptoModule cryptoModule,
        WalletConfiguration config,
        GrpcChannel walletServiceChannel)
    {
        _logger = logger;
        _cryptoModule = cryptoModule;
        _config = config;
        _walletClient = new WalletService.WalletServiceClient(walletServiceChannel);

        // Configure retry policy with exponential backoff
        _retryPolicy = Policy
            .Handle<RpcException>(ex => ex.StatusCode == StatusCode.Unavailable)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: config.RetryPolicy.MaxRetries,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(
                    config.RetryPolicy.InitialDelaySeconds * Math.Pow(config.RetryPolicy.BackoffMultiplier, attempt - 1)),
                onRetry: (exception, timeSpan, attemptNumber, context) =>
                {
                    _logger.LogWarning(exception,
                        "Wallet Service call failed on attempt {AttemptNumber}. Retrying after {RetryDelayMs}ms",
                        attemptNumber, timeSpan.TotalMilliseconds);
                });
    }

    public async Task<WalletDetails> GetWalletDetailsAsync(CancellationToken ct = default)
    {
        if (_cachedWallet != null)
            return _cachedWallet;

        await _walletCacheLock.WaitAsync(ct);
        try
        {
            if (_cachedWallet != null)
                return _cachedWallet;

            var response = await _retryPolicy.ExecuteAsync(async () =>
                await _walletClient.GetWalletDetailsAsync(
                    new GetWalletDetailsRequest { WalletId = _config.WalletId },
                    cancellationToken: ct));

            _cachedWallet = new WalletDetails
            {
                WalletId = response.WalletId,
                Address = response.Address,
                PublicKey = response.PublicKey.ToByteArray(),
                Algorithm = (WalletAlgorithm)response.Algorithm,
                Version = response.Version,
                CachedAt = DateTimeOffset.UtcNow
            };

            _logger.LogInformation(
                "Cached wallet details: {WalletId}, Algorithm: {Algorithm}, Version: {Version}",
                _cachedWallet.WalletId, _cachedWallet.Algorithm, _cachedWallet.Version);

            return _cachedWallet;
        }
        finally
        {
            _walletCacheLock.Release();
        }
    }

    public async Task<Signature> SignDocketAsync(byte[] docketHash, CancellationToken ct = default)
    {
        // Get derived key for docket signing (BIP44 path: m/44'/0'/0'/0/0)
        var derivedKey = await GetDerivedPrivateKeyAsync("m/44'/0'/0'/0/0", ct);

        // Sign locally using Sorcha.Cryptography
        var wallet = await GetWalletDetailsAsync(ct);
        var signResult = await _cryptoModule.SignAsync(
            hash: docketHash,
            network: (byte)wallet.Algorithm,
            privateKey: derivedKey,
            cancellationToken: ct);

        if (!signResult.IsSuccess)
            throw new CryptographicException($"Docket signing failed: {signResult.ErrorMessage}");

        return new Signature
        {
            PublicKey = wallet.PublicKey,
            SignatureValue = signResult.Value!,
            Algorithm = wallet.Algorithm.ToString(),
            SignedAt = DateTimeOffset.UtcNow,
            SignedBy = wallet.Address
        };
    }

    private async Task<byte[]> GetDerivedPrivateKeyAsync(string derivationPath, CancellationToken ct)
    {
        if (_derivedKeyCache.TryGetValue(derivationPath, out var cachedKey))
            return cachedKey;

        await _derivedKeyCacheLock.WaitAsync(ct);
        try
        {
            if (_derivedKeyCache.TryGetValue(derivationPath, out cachedKey))
                return cachedKey;

            var response = await _retryPolicy.ExecuteAsync(async () =>
                await _walletClient.GetDerivedKeyAsync(
                    new GetDerivedKeyRequest
                    {
                        WalletId = _config.WalletId,
                        DerivationPath = derivationPath
                    },
                    cancellationToken: ct));

            _derivedKeyCache[derivationPath] = response.PrivateKey.ToByteArray();

            _logger.LogInformation("Cached derived key for path: {DerivationPath}", derivationPath);

            return response.PrivateKey.ToByteArray();
        }
        finally
        {
            _derivedKeyCacheLock.Release();
        }
    }

    public void Dispose()
    {
        // Zero out cached private keys
        foreach (var key in _derivedKeyCache.Values)
        {
            Array.Clear(key, 0, key.Length);
        }
        _derivedKeyCache.Clear();

        _walletCacheLock.Dispose();
        _derivedKeyCacheLock.Dispose();
    }
}
```

### Step 7: Register Services in Program.cs

Edit `src/Services/Sorcha.Validator.Service/Program.cs`:

```csharp
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Services;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Interfaces;
using Grpc.Net.Client;

var builder = WebApplication.CreateBuilder(args);

// ... existing service registrations ...

// Register Wallet configuration
var walletConfig = builder.Configuration
    .GetSection("WalletService")
    .Get<WalletConfiguration>()
    ?? throw new InvalidOperationException("WalletService configuration is missing");

// Override wallet ID from environment variable if present
var envWalletId = Environment.GetEnvironmentVariable("VALIDATOR_WALLET_ID");
if (!string.IsNullOrEmpty(envWalletId))
{
    walletConfig = walletConfig with { WalletId = envWalletId };
}

builder.Services.AddSingleton(walletConfig);

// Register gRPC channel for Wallet Service
builder.Services.AddSingleton(services =>
{
    var config = services.GetRequiredService<WalletConfiguration>();
    return GrpcChannel.ForAddress(config.Endpoint);
});

// Register Sorcha.Cryptography
builder.Services.AddSingleton<ICryptoModule, CryptoModule>();

// Register Wallet Integration Service
builder.Services.AddSingleton<IWalletIntegrationService, WalletIntegrationService>();

var app = builder.Build();

// ... existing middleware ...

app.Run();
```

---

## Part 3: Testing

### Unit Tests

Create test file: `tests/Sorcha.Validator.Service.Tests/Services/WalletIntegrationServiceTests.cs`

```csharp
using Xunit;
using Moq;
using Sorcha.Validator.Service.Services;
using Sorcha.Cryptography.Interfaces;
using Sorcha.Wallet.Service.Protos;

public class WalletIntegrationServiceTests
{
    [Fact]
    public async Task GetWalletDetailsAsync_CachesWalletDetails()
    {
        // Arrange
        var mockClient = new Mock<WalletService.WalletServiceClient>();
        var mockCrypto = new Mock<ICryptoModule>();

        // Act
        var service = new WalletIntegrationService(...);
        var wallet1 = await service.GetWalletDetailsAsync();
        var wallet2 = await service.GetWalletDetailsAsync();

        // Assert
        Assert.Same(wallet1, wallet2); // Should return same cached instance
        mockClient.Verify(c => c.GetWalletDetailsAsync(...), Times.Once); // Only called once
    }

    [Fact]
    public async Task SignDocketAsync_UsesLocalCryptography()
    {
        // Arrange
        var mockCrypto = new Mock<ICryptoModule>();
        mockCrypto
            .Setup(c => c.SignAsync(It.IsAny<byte[]>(), It.IsAny<byte>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CryptoResult<byte[]>.Success(new byte[] { 0x01, 0x02, 0x03 }));

        // Act
        var service = new WalletIntegrationService(...);
        var signature = await service.SignDocketAsync(new byte[32]);

        // Assert
        Assert.NotNull(signature);
        Assert.Equal(3, signature.SignatureValue.Length);
        mockCrypto.Verify(c => c.SignAsync(...), Times.Once);
    }
}
```

### Integration Tests

Create test file: `tests/Sorcha.Validator.Service.IntegrationTests/WalletIntegrationTests.cs`

```csharp
using Xunit;
using Testcontainers.PostgreSql;
using Microsoft.Extensions.DependencyInjection;

public class WalletIntegrationTests : IAsyncLifetime
{
    [Fact]
    public async Task CanRetrieveWalletDetailsFromWalletService()
    {
        // Arrange
        var serviceProvider = ...; // Build with real Wallet Service connection

        // Act
        var walletService = serviceProvider.GetRequiredService<IWalletIntegrationService>();
        var wallet = await walletService.GetWalletDetailsAsync();

        // Assert
        Assert.NotNull(wallet);
        Assert.NotEmpty(wallet.WalletId);
        Assert.NotEmpty(wallet.Address);
        Assert.StartsWith("ws11", wallet.Address);
    }

    [Fact]
    public async Task CanSignAndVerifyDocketSignature()
    {
        // Arrange
        var walletService = ...; // Build with real services
        var docketHash = SHA256.HashData("test docket data"u8.ToArray());

        // Act
        var signature = await walletService.SignDocketAsync(docketHash);
        var isValid = await walletService.VerifySignatureAsync(
            signature.SignatureValue,
            docketHash,
            signature.PublicKey,
            WalletAlgorithm.ED25519);

        // Assert
        Assert.NotNull(signature);
        Assert.True(isValid);
    }
}
```

### Manual Testing with .NET Aspire

**Step 1**: Start all services

```bash
# From repository root
dotnet run --project src/Apps/Sorcha.AppHost
```

**Step 2**: Verify Validator Service started successfully

Check logs in Aspire Dashboard (`http://localhost:15888`):

```
[Validator Service] Cached wallet details: f8e3a2b1-..., Algorithm: ED25519, Version: 1
[Validator Service] Validator Service initialized successfully
```

**Step 3**: Trigger docket building

```bash
# Post a test transaction to trigger docket build
curl -X POST https://localhost:7082/api/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "blueprintId": "test-blueprint",
    "data": {"test": "data"}
  }'
```

**Step 4**: Verify docket was signed

Check Register Service for signed docket:

```bash
curl -X GET https://localhost:7082/api/registers/default/dockets/latest

# Response should include ProposerSignature:
# {
#   "docketNumber": 1,
#   "proposerSignature": {
#     "publicKey": "0x...",
#     "signatureValue": "0x...",
#     "algorithm": "ED25519"
#   },
#   "proposerValidatorId": "ws11q..."
# }
```

---

## Part 4: Troubleshooting

### Issue: Validator Service fails to start with "Wallet ID not configured"

**Cause**: Neither `VALIDATOR_WALLET_ID` environment variable nor Tenant Service configuration is set.

**Solution**:
1. Set environment variable: `export VALIDATOR_WALLET_ID="your-wallet-id"`
2. OR configure in Tenant Service (see Option B above)

### Issue: "Wallet Service unavailable" error during signing

**Cause**: Wallet Service is not running or endpoint is incorrect.

**Solution**:
1. Verify Wallet Service is running: `curl https://localhost:7084/health`
2. Check `appsettings.json` WalletService.Endpoint configuration
3. Check Aspire dashboard for service status

### Issue: Signature verification fails for peer votes

**Cause**: Algorithm mismatch or public key doesn't match signing wallet.

**Solution**:
1. Log signature details: `_logger.LogDebug("Verifying signature: Algo={Algo}, PublicKey={Key}", ...)`
2. Verify peer validator is using same algorithm
3. Check public key in signature matches wallet address

### Issue: Performance is slower than expected (< 10 dockets/sec)

**Cause**: Local signing with derived keys may not be working (falling back to Wallet Service calls).

**Solution**:
1. Check logs for "Cached derived key" messages
2. Verify `GetDerivedKeyAsync` is being called and cached
3. Profile signing operations with `Stopwatch` to measure timing

---

## Part 5: Best Practices

### Security

- ✅ **Never log private keys**: Avoid `_logger.LogDebug($"Key: {privateKey}")` - log only derivation paths
- ✅ **Zero memory on dispose**: Implement `IDisposable` and call `Array.Clear()` on cached keys
- ✅ **Use HTTPS for gRPC**: Always use TLS for Wallet Service communication in production
- ✅ **Environment variables for secrets**: Store wallet IDs in secrets management (Azure Key Vault, AWS Secrets Manager)

### Performance

- ✅ **Cache derived keys**: Retrieve once per derivation path, cache in memory
- ✅ **Use local signing**: Prefer `ICryptoModule.SignAsync` over Wallet Service gRPC calls
- ✅ **Minimize cache invalidation**: Only invalidate wallet cache when version changes

### Observability

- ✅ **Log all wallet operations**: Initialization, signing, verification, rotation detection
- ✅ **Add OpenTelemetry spans**: Instrument `SignDocketAsync`, `GetWalletDetailsAsync` with `Activity`
- ✅ **Monitor wallet health**: Check wallet connectivity in health check endpoint

---

## Next Steps

- ✅ Implement wallet integration in Validator Service
- ✅ Add comprehensive unit and integration tests
- ⏳ Implement wallet rotation detection logic
- ⏳ Add wallet deletion detection and graceful shutdown
- ⏳ Update Tenant Service with system organization configuration endpoint
- ⏳ Update Wallet Service with gRPC endpoints (if not already implemented)

---

**Need Help?**

- Review [spec.md](spec.md) for requirements and acceptance criteria
- Review [research.md](research.md) for technical decisions and best practices
- Review [data-model.md](data-model.md) for entity definitions

**Status**: ✅ Implementation guide complete. Ready for development.
