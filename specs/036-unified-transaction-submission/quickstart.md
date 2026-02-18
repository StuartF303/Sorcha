# Quickstart: Unified Transaction Submission

## Overview

This feature unifies all transaction submission paths through a single generic validator endpoint and introduces a secure system wallet signing service. After this change:

- **All transactions** (genesis, control, action, governance) submit to `POST /api/v1/transactions/validate`
- **System-level signing** is handled by `ISystemWalletSigningService` with audit logging, rate limiting, and operation whitelist
- **The legacy genesis endpoint** (`POST /api/validator/genesis`) is removed

## Build & Test

```bash
# Build affected projects
dotnet build src/Common/Sorcha.ServiceClients/
dotnet build src/Services/Sorcha.Validator.Service/
dotnet build src/Services/Sorcha.Register.Service/

# Run tests
dotnet test tests/Sorcha.ServiceClients.Tests/
dotnet test tests/Sorcha.Validator.Service.Tests/
dotnet test tests/Sorcha.Register.Core.Tests/

# Docker rebuild (both services changed)
docker compose build --no-cache register-service validator-service
docker compose up -d register-service validator-service
```

## Key Changes

### 1. New: ISystemWalletSigningService
- **Location**: `src/Common/Sorcha.ServiceClients/SystemWallet/`
- **Registration**: `builder.Services.AddSystemWalletSigning(builder.Configuration)` — opt-in only
- **Used by**: Register Service (genesis, blueprint publish), Validator Service (docket signing)

### 2. Modified: RegisterCreationOrchestrator
- Signs genesis transaction locally via `ISystemWalletSigningService`
- Submits via `IValidatorServiceClient.SubmitTransactionAsync()` (generic endpoint)
- No longer calls `SubmitGenesisTransactionAsync()`

### 3. Modified: Register Service Blueprint Publish
- Signs control transaction locally via `ISystemWalletSigningService`
- Submits via `IValidatorServiceClient.SubmitTransactionAsync()` (generic endpoint)
- No longer calls `SubmitGenesisTransactionAsync()`

### 4. Modified: ValidationEngine
- Signature verification no longer skipped for genesis/control transactions
- Schema validation and blueprint conformance skips remain (correct behaviour)

### 5. Removed: Legacy Genesis Endpoint
- `POST /api/validator/genesis` — removed
- `GenesisTransactionRequest` model — removed
- `GenesisTransactionSubmission` model — removed
- `SubmitGenesisTransactionAsync()` on IValidatorServiceClient — removed

### 6. Audited: Direct Write Paths
- Blueprint Service Program.cs endpoints that bypass validator — identified and flagged

## Configuration

```json
{
  "SystemWalletSigning": {
    "ValidatorId": "validator-001",
    "AllowedDerivationPaths": ["sorcha:register-control", "sorcha:docket-signing"],
    "MaxSignsPerRegisterPerMinute": 10
  }
}
```

## End-to-End Verification

1. Create a new register → genesis transaction flows through generic endpoint → docket 0 created
2. Publish a blueprint → control transaction flows through generic endpoint → docket N+1 created
3. Execute a blueprint action → action transaction flows through generic endpoint (unchanged)
4. Check audit logs → every system sign operation has a structured log entry
