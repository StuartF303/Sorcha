# Implementation Plan: Azure Deployment

**Feature Branch**: `deployment-azure`
**Created**: 2025-12-03
**Status**: Planning (0%)

## Summary

Azure deployment leverages .NET Aspire's native Azure integration to deploy the Sorcha platform to Azure Container Apps with managed services for databases, caching, secrets, and monitoring. The deployment uses Azure Developer CLI (azd) for streamlined infrastructure provisioning.

## Design Decisions

### Decision 1: Azure Container Apps

**Approach**: Deploy services to Azure Container Apps (ACA).

**Rationale**:
- Serverless container platform
- Native .NET Aspire integration
- Built-in DAPR support (optional)
- Cost-effective scaling to zero
- Simplified networking

### Decision 2: Azure Developer CLI (azd)

**Approach**: Use azd for deployment orchestration.

**Rationale**:
- Native Aspire manifest support
- Infrastructure as code generation
- Environment management
- CI/CD integration

### Decision 3: Managed Azure Services

**Approach**: Use Azure managed services for infrastructure.

**Rationale**:
- No infrastructure management
- Built-in HA and backups
- Automatic patching
- SLA guarantees

### Decision 4: Managed Identities

**Approach**: Use managed identities for all Azure resource access.

**Rationale**:
- No credential management
- Automatic token rotation
- Audit trail via Azure AD
- Zero secrets in code

## Technical Approach

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Azure Subscription                        │
├─────────────────────────────────────────────────────────────┤
│  Resource Group: rg-sorcha-production                        │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  Azure Front Door (Optional - Global Load Balancing)   │  │
│  └───────────────────────────────────────────────────────┘  │
│                          │                                   │
│                          ▼                                   │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  Container App Environment: cae-sorcha-prod            │  │
│  │  ┌─────────────────────────────────────────────────┐  │  │
│  │  │  Virtual Network (internal communication)        │  │  │
│  │  └─────────────────────────────────────────────────┘  │  │
│  │                                                        │  │
│  │  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐     │  │
│  │  │ API     │ │Blueprint│ │ Wallet  │ │Register │     │  │
│  │  │ Gateway │ │ Service │ │ Service │ │ Service │     │  │
│  │  │ (ext)   │ │ (int)   │ │ (int)   │ │ (int)   │     │  │
│  │  └─────────┘ └─────────┘ └─────────┘ └─────────┘     │  │
│  │                                                        │  │
│  │  ┌─────────┐ ┌─────────┐ ┌─────────┐                 │  │
│  │  │ Peer    │ │ Tenant  │ │ Blazor  │                 │  │
│  │  │ Service │ │ Service │ │ Client  │                 │  │
│  │  │ (int)   │ │ (int)   │ │ (ext)   │                 │  │
│  │  └─────────┘ └─────────┘ └─────────┘                 │  │
│  └───────────────────────────────────────────────────────┘  │
│                          │                                   │
│         ┌────────────────┼────────────────┐                 │
│         ▼                ▼                ▼                 │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │   Azure     │  │   Azure     │  │   Azure     │         │
│  │  Database   │  │  Cache for  │  │  Key Vault  │         │
│  │ PostgreSQL  │  │   Redis     │  │  (secrets)  │         │
│  └─────────────┘  └─────────────┘  └─────────────┘         │
│                                                              │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  Azure Monitor                                         │  │
│  │  ├── Application Insights (telemetry)                 │  │
│  │  └── Log Analytics Workspace (logs)                   │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Component Status

| Component | Status | Notes |
|-----------|--------|-------|
| Aspire Azure Support | 0% | Need Aspire.Hosting.Azure |
| Container App Environment | 0% | Not created |
| Service Container Apps | 0% | Not created |
| Azure PostgreSQL | 0% | Not provisioned |
| Azure Redis | 0% | Not provisioned |
| Azure Key Vault | 0% | Not provisioned |
| Azure AD Integration | 0% | Not configured |
| Application Insights | 0% | Not configured |
| Bicep Templates | 0% | Not created |
| azd Configuration | 0% | Not created |

### Azure Resources

| Resource | Type | Purpose |
|----------|------|---------|
| cae-sorcha-* | Container App Environment | Container hosting |
| ca-api-gateway | Container App | External API entry |
| ca-blazor-client | Container App | UI hosting |
| ca-blueprint-svc | Container App | Blueprint Service |
| ca-wallet-svc | Container App | Wallet Service |
| ca-register-svc | Container App | Register Service |
| ca-peer-svc | Container App | Peer Service |
| ca-tenant-svc | Container App | Tenant Service |
| psql-sorcha-* | Azure PostgreSQL | Relational data |
| redis-sorcha-* | Azure Cache Redis | Distributed cache |
| kv-sorcha-* | Key Vault | Secrets |
| appi-sorcha-* | Application Insights | Telemetry |
| log-sorcha-* | Log Analytics | Centralized logs |

## Dependencies

### Azure SDK Packages

```xml
<PackageReference Include="Aspire.Hosting.Azure.ContainerApps" />
<PackageReference Include="Aspire.Hosting.Azure.PostgreSQL" />
<PackageReference Include="Aspire.Hosting.Azure.Redis" />
<PackageReference Include="Azure.Identity" />
```

### Azure Services

- Azure Container Apps
- Azure Database for PostgreSQL Flexible Server
- Azure Cache for Redis
- Azure Key Vault
- Azure Active Directory
- Azure Monitor (Application Insights, Log Analytics)
- Azure Front Door (optional)

## Migration/Integration Notes

### Deployment Commands

```bash
# Initialize Azure Developer CLI
azd init

# Provision infrastructure
azd provision

# Deploy application
azd deploy

# Full deployment
azd up

# View logs
azd monitor
```

### Environment Configuration

```bash
# azure.yaml
name: sorcha
services:
  api-gateway:
    project: ./src/Services/Sorcha.ApiGateway
    host: containerapp
  blueprint-service:
    project: ./src/Services/Sorcha.Blueprint.Service
    host: containerapp
  # ... other services
```

### Bicep Parameters

```bicep
param location string = 'eastus2'
param environmentName string = 'production'
param postgresSkuName string = 'Standard_D2s_v3'
param redisCacheSkuName string = 'Standard'
param containerAppsCpuCores string = '0.5'
param containerAppsMemory string = '1.0Gi'
```

## Open Questions

1. Which Azure region(s) for deployment?
2. Single vs multi-region for HA?
3. Azure AD B2C vs Azure AD for consumer auth?
4. Azure Confidential Computing for Validator Service?
