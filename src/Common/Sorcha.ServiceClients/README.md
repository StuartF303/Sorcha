# Sorcha.ServiceClients

**Consolidated gRPC/HTTP clients for inter-service communication**

## Purpose

This library provides unified client implementations for all Sorcha microservices, eliminating duplication across service projects. All services should reference this library instead of creating their own client implementations.

## Architecture

Each service has:
- **Interface** (`I{Service}Client`) - Defines all methods needed by ANY consumer
- **Implementation** (`{Service}Client`) - gRPC or HTTP client implementation
- **Models** - Shared DTOs and response types

## Supported Services

| Service | Protocol | Status | Interface |
|---------|----------|--------|-----------|
| Wallet Service | gRPC (planned) / HTTP (current) | Stub | `IWalletServiceClient` |
| Register Service | gRPC (planned) / HTTP (current) | Stub | `IRegisterServiceClient` |
| Blueprint Service | gRPC (planned) / HTTP (current) | Stub | `IBlueprintServiceClient` |
| Peer Service | gRPC | Partial | `IPeerServiceClient` |
| Validator Service | gRPC | Complete | Via proto |
| Tenant Service | HTTP | Planned | `ITenantServiceClient` |

## Usage

### 1. Add Package Reference

```xml
<ProjectReference Include="..\..\Common\Sorcha.ServiceClients\Sorcha.ServiceClients.csproj" />
```

### 2. Register Clients in DI

```csharp
// Program.cs
builder.Services.AddServiceClients(builder.Configuration);
```

### 3. Inject and Use

```csharp
public class MyService
{
    private readonly IWalletServiceClient _walletClient;

    public MyService(IWalletServiceClient walletClient)
    {
        _walletClient = walletClient;
    }

    public async Task DoWork()
    {
        var wallet = await _walletClient.CreateWalletAsync(...);
    }
}
```

## Configuration

Configure service endpoints in `appsettings.json`:

```json
{
  "ServiceClients": {
    "WalletService": {
      "Address": "https://localhost:7001",
      "UseGrpc": false
    },
    "RegisterService": {
      "Address": "https://localhost:7002",
      "UseGrpc": false
    },
    "BlueprintService": {
      "Address": "https://localhost:7003",
      "UseGrpc": false
    },
    "PeerService": {
      "Address": "https://localhost:7004",
      "UseGrpc": true
    }
  }
}
```

## Design Principles

1. **Single Source of Truth** - One client implementation per service
2. **Comprehensive Interfaces** - Include ALL methods needed by ANY consumer
3. **Service Discovery** - Use .NET Aspire service discovery when available
4. **Resilience** - Built-in retry policies and circuit breakers
5. **Protocol Agnostic** - Support both gRPC and HTTP where needed

## Migration from Service-Specific Clients

### Before (Duplicated)
```
src/Services/Sorcha.Validator.Service/Clients/WalletServiceClient.cs
src/Services/Sorcha.Blueprint.Service/Clients/WalletServiceClient.cs
```

### After (Consolidated)
```
src/Common/Sorcha.ServiceClients/Wallet/WalletServiceClient.cs
```

All services now reference `Sorcha.ServiceClients` and inject `IWalletServiceClient`.

## Contributing

When adding new methods:
1. Add method to the interface (`I{Service}Client`)
2. Implement in the client class (`{Service}Client`)
3. Update this README with the new functionality
4. Add integration tests in `tests/Sorcha.ServiceClients.Tests`

## Status

**Current Phase:** Consolidation in progress
**Target Completion:** Sprint 11 (US3 - Consensus implementation)
