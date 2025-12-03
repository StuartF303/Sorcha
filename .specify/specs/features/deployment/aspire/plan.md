# Implementation Plan: .NET Aspire Deployment

**Feature Branch**: `deployment-aspire`
**Created**: 2025-12-03
**Status**: 95% Complete

## Summary

.NET Aspire deployment is the primary development and testing environment for the Sorcha platform. It provides service orchestration, discovery, configuration management, and observability through the Aspire Dashboard.

## Design Decisions

### Decision 1: Aspire as Primary Development Environment

**Approach**: Use .NET Aspire for all local development and testing.

**Rationale**:
- Native .NET integration
- Simplified service orchestration
- Built-in observability
- Consistent developer experience

### Decision 2: Service Dependencies via References

**Approach**: Explicit service references define dependency graph.

**Rationale**:
- Clear startup ordering
- Automatic wait for dependencies
- Health check integration
- Visual dependency graph in dashboard

### Decision 3: Infrastructure as Resources

**Approach**: Define PostgreSQL and Redis as Aspire resources.

**Rationale**:
- Automatic container provisioning
- Connection string injection
- Development tools included
- Consistent across team members

### Decision 4: External Endpoints Strategy

**Approach**: Only API Gateway and Blazor Client exposed externally.

**Rationale**:
- Security by design
- Single API entry point
- Simplified firewall rules
- Production-like topology

## Technical Approach

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    .NET Aspire Host                          │
│                   (Sorcha.AppHost)                           │
├─────────────────────────────────────────────────────────────┤
│  Aspire Dashboard                                            │
│  ├── Logs Aggregation                                       │
│  ├── Distributed Tracing                                    │
│  ├── Metrics Collection                                     │
│  └── Resource Health                                        │
├─────────────────────────────────────────────────────────────┤
│  Infrastructure Resources                                    │
│  ├── PostgreSQL (sorcha_tenant)                             │
│  │   └── pgAdmin (dev tool)                                 │
│  └── Redis (distributed cache)                              │
│      └── Redis Commander (dev tool)                         │
├─────────────────────────────────────────────────────────────┤
│  Service Resources                                           │
│  ├── tenant-service → postgres, redis                       │
│  ├── blueprint-service → redis                              │
│  ├── wallet-service → redis                                 │
│  ├── register-service → redis                               │
│  ├── peer-service → redis                                   │
│  ├── api-gateway → all services, redis (EXTERNAL)           │
│  └── blazor-client (EXTERNAL)                               │
└─────────────────────────────────────────────────────────────┘
```

### Component Status

| Component | Status | Notes |
|-----------|--------|-------|
| AppHost Project | 100% | Fully configured |
| PostgreSQL Resource | 100% | With pgAdmin |
| Redis Resource | 100% | With Redis Commander |
| Service Registration | 100% | All services registered |
| Service Discovery | 100% | Name-based resolution |
| Aspire Dashboard | 100% | Logs, traces, metrics |
| External Endpoints | 100% | Gateway and Client |
| OpenTelemetry | 90% | Tracing enabled |
| Health Checks | 90% | Basic health probes |

### Default Ports

| Service | Port | External |
|---------|------|----------|
| Aspire Dashboard | 15888 | Yes (dev) |
| API Gateway | 7082 | Yes |
| Blazor Client | 7083 | Yes |
| PostgreSQL | 5432 | No |
| Redis | 6379 | No |
| pgAdmin | Dynamic | Yes (dev) |
| Redis Commander | Dynamic | Yes (dev) |

## Dependencies

### Framework Dependencies

- `Aspire.Hosting` (9.0+)
- `Aspire.Hosting.PostgreSQL`
- `Aspire.Hosting.Redis`

### Service Defaults

All services reference `Sorcha.ServiceDefaults` which configures:
- OpenTelemetry exporters
- Health check endpoints
- Resilience policies
- Service discovery client

## Migration/Integration Notes

### Running Locally

```bash
# Start all services
cd src/Apps/Sorcha.AppHost
dotnet run

# Access points
# Dashboard: http://localhost:15888
# API Gateway: https://localhost:7082
# Designer: https://localhost:7083
```

### Environment Variables

```bash
# Optional overrides
DOTNET_ENVIRONMENT=Development
ASPIRE_DASHBOARD_PORT=15888
```

### Docker Requirements

- Docker Desktop or compatible runtime
- Minimum 8GB RAM allocated
- WSL2 on Windows

## Open Questions

1. Should we support podman as Docker alternative?
2. How to handle database migrations in Aspire?
3. Should development secrets use user-secrets or .env files?
4. How to configure SSL certificates for HTTPS?
