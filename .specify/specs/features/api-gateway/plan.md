# Implementation Plan: API Gateway

**Feature Branch**: `api-gateway`
**Created**: 2025-12-03
**Status**: 95% Complete

## Summary

The API Gateway provides a unified entry point for all Sorcha platform services, implementing reverse proxy routing via YARP, health aggregation, and OpenAPI documentation aggregation. It serves as the primary interface for client applications.

## Design Decisions

### Decision 1: YARP for Reverse Proxy

**Approach**: Use YARP (Yet Another Reverse Proxy) for request routing.

**Rationale**:
- Native .NET integration
- High performance
- Configuration-driven routing
- Built-in load balancing and health checks

**Alternatives Considered**:
- Ocelot - Less actively maintained
- Nginx - External dependency, less .NET integration

### Decision 2: Health Aggregation

**Approach**: Custom HealthAggregationService that queries all backend services.

**Rationale**:
- Provides single health endpoint for operations
- Customizable health status logic
- Integrates with Aspire service discovery

### Decision 3: Scalar for API Docs

**Approach**: Use Scalar.AspNetCore for interactive API documentation.

**Rationale**:
- Modern, clean UI
- .NET 10 native OpenAPI integration
- Supports OpenAPI spec aggregation

## Technical Approach

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│                  Sorcha.ApiGateway                       │
│                  (ASP.NET Core 10)                       │
├─────────────────────────────────────────────────────────┤
│  Routing Layer (YARP)                                    │
│  ├── /api/blueprints/*  → Blueprint Service             │
│  ├── /api/wallets/*     → Wallet Service                │
│  ├── /api/registers/*   → Register Service              │
│  └── /api/tenants/*     → Tenant Service                │
├─────────────────────────────────────────────────────────┤
│  Aggregation Services                                    │
│  ├── HealthAggregationService.cs                        │
│  ├── OpenApiAggregationService.cs                       │
│  └── ClientDownloadService.cs                           │
├─────────────────────────────────────────────────────────┤
│  Gateway Endpoints                                       │
│  ├── GET /api/health          (Aggregated health)       │
│  ├── GET /api/stats           (System statistics)       │
│  ├── GET /api/client/download (Client ZIP)              │
│  ├── GET /scalar/v1           (API documentation)       │
│  └── GET /                    (Landing page)            │
└─────────────────────────────────────────────────────────┘
```

### Component Status

| Component | Status | Notes |
|-----------|--------|-------|
| YARP Configuration | 100% | Route configuration complete |
| HealthAggregationService | 100% | Health checks for all services |
| OpenApiAggregationService | 100% | OpenAPI spec aggregation |
| ClientDownloadService | 100% | ZIP package generation |
| Scalar UI | 100% | Interactive documentation |
| Landing Page | 100% | HTML dashboard |
| CORS | 100% | Cross-origin support |

### API Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| GET | `/` | Landing page with dashboard | Done |
| GET | `/api/health` | Aggregated health status | Done |
| GET | `/api/stats` | System statistics | Done |
| GET | `/api/client/info` | Client information | Done |
| GET | `/api/client/download` | Download client ZIP | Done |
| GET | `/api/client/instructions` | Installation guide | Done |
| GET | `/scalar/v1` | API documentation UI | Done |
| GET | `/openapi/aggregated.json` | Aggregated OpenAPI spec | Done |

## Dependencies

### Internal Dependencies

- `Sorcha.ServiceDefaults` - .NET Aspire configuration

### External Dependencies

- `Yarp.ReverseProxy` - Reverse proxy
- `Scalar.AspNetCore` - API documentation UI

### Service Dependencies

- Blueprint Service - Backend routing
- Wallet Service - Backend routing
- Register Service - Backend routing
- Tenant Service - Backend routing (when available)

## Migration/Integration Notes

### YARP Configuration

```json
{
  "ReverseProxy": {
    "Routes": {
      "blueprint-route": {
        "ClusterId": "blueprint-cluster",
        "Match": { "Path": "/api/blueprints/{**catch-all}" }
      },
      "wallet-route": {
        "ClusterId": "wallet-cluster",
        "Match": { "Path": "/api/wallets/{**catch-all}" }
      },
      "register-route": {
        "ClusterId": "register-cluster",
        "Match": { "Path": "/api/registers/{**catch-all}" }
      }
    },
    "Clusters": {
      "blueprint-cluster": {
        "Destinations": {
          "destination1": { "Address": "http://blueprint-service" }
        }
      }
    }
  }
}
```

### Breaking Changes

- None for MVD phase

## Open Questions

1. Should we implement rate limiting at the gateway level?
2. How to handle authentication propagation to backend services?
3. Should we add request caching for frequently accessed resources?
