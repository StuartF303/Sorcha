# Implementation Plan: Sorcha AppHost

**Feature Branch**: `apphost`
**Created**: 2025-12-03
**Status**: 95% Complete

## Summary

The Sorcha AppHost is the .NET Aspire orchestration project that manages all platform services, infrastructure resources, and development tools. It provides service discovery, dependency management, and external endpoint configuration for the entire Sorcha platform.

## Design Decisions

### Decision 1: .NET Aspire for Orchestration

**Approach**: Use .NET Aspire as the service orchestration framework.

**Rationale**:
- Native .NET integration
- Built-in service discovery
- Container orchestration support
- Development dashboard included
- Azure deployment ready

### Decision 2: External Endpoint Strategy

**Approach**: Only API Gateway and Blazor Client have external endpoints.

**Rationale**:
- Security by default - internal services not exposed
- Single entry point for API traffic
- Simplified firewall configuration
- Clear separation of public/private services

### Decision 3: Infrastructure as Resources

**Approach**: Define PostgreSQL and Redis as Aspire resources.

**Rationale**:
- Automatic container management
- Connection string injection
- Development tools included (pgAdmin, Redis Commander)
- Consistent across environments

### Decision 4: Service Dependency Graph

**Approach**: Explicit service references define startup order.

**Rationale**:
- Clear dependency visualization
- Automatic wait for dependencies
- Health check integration
- Prevents race conditions

## Technical Approach

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Sorcha.AppHost                            │
│                  (.NET Aspire Host)                          │
├─────────────────────────────────────────────────────────────┤
│  Infrastructure Resources                                    │
│  ├── PostgreSQL (tenant-db)                                 │
│  │   └── pgAdmin (development UI)                           │
│  └── Redis (distributed cache)                              │
│      └── Redis Commander (development UI)                   │
├─────────────────────────────────────────────────────────────┤
│  Internal Services (Not Externally Accessible)               │
│  ├── tenant-service ──→ postgres, redis                     │
│  ├── blueprint-service ──→ redis                            │
│  ├── wallet-service ──→ redis                               │
│  ├── register-service ──→ redis                             │
│  └── peer-service ──→ redis                                 │
├─────────────────────────────────────────────────────────────┤
│  External Services (Publicly Accessible)                     │
│  ├── api-gateway ──→ all services, redis                    │
│  │   └── WithExternalHttpEndpoints()                        │
│  └── blazor-client                                          │
│      └── WithExternalHttpEndpoints()                        │
└─────────────────────────────────────────────────────────────┘
```

### Component Status

| Component | Status | Notes |
|-----------|--------|-------|
| PostgreSQL Resource | 100% | With pgAdmin |
| Redis Resource | 100% | With Redis Commander |
| Tenant Service | 100% | References postgres, redis |
| Blueprint Service | 100% | References redis |
| Wallet Service | 100% | References redis |
| Register Service | 100% | References redis |
| Peer Service | 100% | References redis |
| API Gateway | 100% | External endpoints, all refs |
| Blazor Client | 100% | External endpoints |
| Validator Service | 0% | Not yet added |

### Service Configuration

| Service | References | External | Notes |
|---------|-----------|----------|-------|
| tenant-service | postgres, redis | No | Authentication |
| blueprint-service | redis | No | Workflow engine |
| wallet-service | redis | No | Crypto wallets |
| register-service | redis | No | Ledger storage |
| peer-service | redis | No | P2P networking |
| api-gateway | all services, redis | Yes | Single entry point |
| blazor-client | none | Yes | Browser UI |

## Dependencies

### Framework Dependencies

- `Aspire.Hosting` - .NET Aspire hosting framework
- `Aspire.Hosting.PostgreSQL` - PostgreSQL resource support
- `Aspire.Hosting.Redis` - Redis resource support

### Service Projects

- `Sorcha.Tenant.Service` - Authentication/authorization
- `Sorcha.Blueprint.Service` - Blueprint management
- `Sorcha.Wallet.Service` - Wallet management
- `Sorcha.Register.Service` - Ledger operations
- `Sorcha.Peer.Service` - P2P networking
- `Sorcha.ApiGateway` - YARP reverse proxy
- `Sorcha.Blueprint.Designer.Client` - Blazor WebAssembly UI

## Migration/Integration Notes

### Running the AppHost

```bash
# Start all services
dotnet run --project src/Apps/Sorcha.AppHost

# Access points
# Aspire Dashboard: http://localhost:15888
# API Gateway: https://localhost:7082
# Blueprint Designer: https://localhost:7083
```

### Environment Variables

```json
{
  "ConnectionStrings": {
    "postgres": "Host=localhost;Database=sorcha_tenant;...",
    "redis": "localhost:6379"
  }
}
```

### Breaking Changes

- None (foundational project)

## Open Questions

1. Should Validator Service be added to AppHost when implemented?
2. How to handle production secrets vs development configuration?
3. Should we add MongoDB for Register Service?
4. How to configure HTTPS certificates for production?
