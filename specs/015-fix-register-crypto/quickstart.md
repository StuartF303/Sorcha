# Quickstart: Fix Register Creation - Fully Functional Cryptographic Register Flow

**Branch**: `015-fix-register-crypto` | **Date**: 2026-01-27

## Prerequisites

- .NET 10 SDK
- Docker Desktop (for running services)
- Git checkout of `015-fix-register-crypto` branch

## Implementation Order

Changes must be applied in this order due to dependencies:

### Layer 1: Foundation (no dependencies between these)

1. **Add `IsPreHashed` to Wallet Service** -- `TransactionService`, `WalletManager`, `WalletEndpoints`, `SignTransactionRequest`
2. **Create `ServiceAuthClient`** -- New files in `Sorcha.ServiceClients/Auth/`
3. **Update `PendingRegistration` model** -- Add `AttestationHashes` dictionary

### Layer 2: Service Clients (depends on Layer 1)

4. **De-stub `WalletServiceClient`** -- Depends on `ServiceAuthClient` (Layer 1.2) and `IsPreHashed` support (Layer 1.1)
5. **Update `IWalletServiceClient` interface** -- New `WalletSignResult` return type, `isPreHashed` parameter

### Layer 3: Core Flow (depends on Layer 2)

6. **Update `RegisterCreationOrchestrator`** -- Return hex hash, verify from stored hash, atomic genesis-then-persist
7. **Update `ValidationEndpoints` genesis handler** -- Use real wallet signing via de-stubbed client
8. **Update `GenesisManager`** -- Use real wallet signing for docket signatures

### Layer 4: Cleanup and Routing (depends on Layer 3)

9. **Remove simple CRUD POST endpoint** -- In Register Service `Program.cs`
10. **Fix API Gateway routing** -- Add genesis-specific route in `appsettings.json`

### Layer 5: Verification

11. **Update walkthrough scripts** -- Use hex hash and `isPreHashed: true`
12. **Run existing tests** -- Ensure no regressions
13. **Add new unit tests** -- Pre-hashed signing, orchestrator hash verification, WalletServiceClient HTTP

## Build & Test

```bash
# Build solution
dotnet restore && dotnet build

# Run all tests
dotnet test

# Run specific test projects
dotnet test tests/Sorcha.Wallet.Core.Tests/
dotnet test tests/Sorcha.Register.Service.Tests/

# Docker end-to-end
docker-compose up -d
# Then run walkthrough:
pwsh walkthroughs/RegisterCreationFlow/test-register-creation-with-real-signing.ps1
```

## Configuration Required

### Validator Service (appsettings.json / environment)

```json
{
  "ServiceAuth": {
    "ClientId": "service-validator",
    "ClientSecret": "<from service principal registration>",
    "TenantServiceUrl": "http://tenant-service:8080"
  }
}
```

### Register Service (appsettings.json / environment)

```json
{
  "ServiceAuth": {
    "ClientId": "service-register",
    "ClientSecret": "<from service principal registration>",
    "TenantServiceUrl": "http://tenant-service:8080"
  }
}
```

### Service Principal Setup

Before register creation works end-to-end, service principals must be registered in the Tenant Service:

```bash
# Register Validator service principal (requires admin token)
curl -X POST http://localhost/api/service-principals/ \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"serviceName": "validator", "allowedScopes": ["wallet:sign"]}'

# Register Register service principal
curl -X POST http://localhost/api/service-principals/ \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"serviceName": "register", "allowedScopes": ["wallet:sign", "validator:genesis"]}'
```

Store the returned `clientId` and `clientSecret` in each service's configuration.

## Key Files Changed

| File | Change Summary |
|------|---------------|
| `RegisterCreationOrchestrator.cs` | Return hex hash, verify from stored hash, atomic create |
| `RegisterCreationModels.cs` | Add `AttestationHashes` to `PendingRegistration`, update `DataToSign` doc |
| `TransactionService.cs` | Add `isPreHashed` conditional |
| `ITransactionService.cs` | Add `isPreHashed` parameter |
| `WalletManager.cs` | Pass `isPreHashed` through |
| `WalletEndpoints.cs` | Pass `IsPreHashed` from request |
| `SignTransactionRequest.cs` | Add `IsPreHashed` property |
| `IWalletServiceClient.cs` | New `WalletSignResult`, updated signature |
| `WalletServiceClient.cs` | Full rewrite: real HTTP + JWT auth |
| `IServiceAuthClient.cs` | NEW: token acquisition interface |
| `ServiceAuthClient.cs` | NEW: OAuth2 client_credentials implementation |
| `ServiceCollectionExtensions.cs` | Register HttpClient for wallet, register ServiceAuthClient |
| `ValidationEndpoints.cs` | Use real wallet signing, compute PayloadHash |
| `GenesisManager.cs` | Use real wallet signing |
| `Program.cs` (Register) | Remove simple POST endpoint |
| `Program.cs` (Validator) | Configure ServiceAuth |
| `appsettings.json` (Gateway) | Add genesis route |
| Walkthrough scripts | Use hex hash + isPreHashed |
