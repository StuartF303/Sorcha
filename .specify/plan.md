# SORCHA Implementation Plan

**Version:** 1.0
**Date:** 2025-11-11
**Status:** Active
**Spec Reference:** [spec.md](.specify/spec.md)
**Constitution Reference:** [constitution.md](.specify/constitution.md)

## Summary

This implementation plan describes the technical approach, architecture, and development strategy for the SORCHA distributed ledger platform. The platform is built as a microservices architecture using .NET 10, Docker, Kubernetes, and .Net Aspire, providing enterprise-ready blockchain capabilities.

## Technical Context

### Language and Framework
- **Primary Language:** C# 13+
- **Framework:** .NET 10.0
- **API Style:** RESTful HTTP APIs (ASP.NET Core)
- **Testing Framework:** xUnit
- **Mocking:** Moq, NSubstitute

### Dependencies

#### Core Infrastructure
- **Dapr 1.11+:** Microservices runtime for state, pub/sub, and secrets
- **Docker:** Container runtime
- **Docker Compose:** Local orchestration
- **Kubernetes:** Production orchestration

#### Data Storage
- **MongoDB 5.0+:** Document storage (Register, Blueprint)
  - Driver: MongoDB.Driver
  - Storage: RegisterCoreMongoDBStorage
- **MySQL 8.0+:** Relational storage (Wallet)
  - Provider: Pomelo.EntityFrameworkCore.MySql
  - Azure SQL for cloud deployments
- **Redis 7.0+:** Distributed cache and state
  - Client: StackExchange.Redis

#### Messaging
- **RabbitMQ 3.11+:** Message broker
  - Client: RabbitMQ.Client
  - Patterns: Pub/sub, work queues

#### Observability
- **Serilog:** Structured logging
  - Sinks: Console, Seq, Application Insights
- **Seq:** Log aggregation and search
- **Zipkin:** Distributed tracing
- **Application Insights:** Azure monitoring

#### Security
- **Microsoft.AspNetCore.Authentication.JwtBearer:** JWT authentication
- **Microsoft.Identity.Web:** Azure AD integration
- **Azure Key Vault:** Secret management
- **SiccarPlatformCryptography:** Custom crypto library

#### Build and CI/CD
- **Azure DevOps:** Source control and pipelines
- **Azure Bicep:** Infrastructure as Code
- **NuGet:** Package management
  - Private feed: projectbob.pkgs.visualstudio.com/SORCHA

### Target Platform
- **Development:** Windows, Linux, macOS with Docker Desktop
- **Production:** Azure Kubernetes Service (AKS)
- **Container Registry:** Azure Container Registry (ACR)

### Project Type
Microservices solution with multiple deployable services:
- 7 core microservices
- 1 proxy/gateway service
- 1 admin UI application
- 1 CLI tool
- Multiple shared libraries
- Comprehensive test suite

### Performance Goals
- **Transaction Throughput:** 1000+ TPS per service instance
- **API Latency:** p95 < 500ms (reads), p95 < 2s (writes)
- **Database Performance:** Optimized queries with indexing
- **Cache Hit Rate:** > 80% for frequently accessed data

### Constraints
- Must maintain backward compatibility for APIs
- All services must be independently deployable
- Configuration must support multiple environments
- Secrets never committed to source control
- .NET 10 framework requirement 

### Scale and Scope
- **Microservices:** 8 independent services
- **Source Files:** 100+ C# projects
- **LOC:** Estimated 100,000+ lines of code
- **Team Size:** Small to medium development team
- **Deployment Environments:** Local, Development, Staging, Production

## Constitution Check

✅ **Constitution Compliance:** This plan adheres to all principles defined in [constitution.md](.specify/constitution.md)

### Key Compliance Areas:
- ✅ Microservices-first architecture maintained
- ✅ Cloud-native design with containerization
- ✅ Zero trust security model implemented
- ✅ Comprehensive testing strategy
- ✅ Infrastructure as Code (Bicep)
- ✅ Proper observability (logging, tracing, monitoring)
- ✅ Dependency management via NuGet

## Project Structure

### Documentation
```
.specify/
├── constitution.md          # Project principles and standards
├── spec.md                 # Requirements and goals
├── plan.md                 # This file - implementation plan
└── tasks/                  # Task management (future use)

docs/
├── DOTNET9_PHASE1_PROGRESS.md

deploy/
├── bicep/                  # Azure infrastructure templates
│   ├── main.bicep
│   ├── aks.bicep
│   ├── acr.bicep
│   ├── key-vault.bicep
│   └── ...
├── k8s/                    # Kubernetes manifests
│   ├── deployment-microservice-*.yaml
│   ├── service-microservice-*.yaml
│   ├── ingress-microservice-*.yaml
│   └── component-*.yaml
└── docs/                   # Deployment documentation
```

### Source Code Structure

```
src/
├── Common/                     # Shared libraries
│   ├── SiccarCommon/          # Common utilities
│   ├── SiccarPlatform/        # Core platform models
│   ├── SiccarPlatformCryptography/  # Crypto operations
│   ├── SiccarApplication/     # Application abstractions
│   ├── SiccarCommonServiceClients/  # HTTP clients
│   └── *Tests/                # Unit tests for common libs
│
├── Services/                   # Microservices
│   ├── Action/
│   │   ├── ActionService/     # Main service project
│   │   ├── ActionUnitTests/
│   │   └── ActionService.IntegrationTests/
│   ├── Blueprint/
│   │   ├── BlueprintService/
│   │   ├── BlueprintTests/
│   │   └── BlueprintService.IntegrationTests/
│   ├── Peer/
│   │   ├── PeerCore/          # Business logic
│   │   ├── PeerService/       # API service
│   │   ├── PeerUtilities/     # Helper utilities
│   │   ├── Router/            # Routing logic
│   │   ├── PeerUnitTests/
│   │   └── PeerService.IntegrationTests/
│   ├── Register/
│   │   ├── RegisterCore/      # Business logic
│   │   ├── RegisterService/   # API service
│   │   ├── RegisterCoreMongoDBStorage/  # MongoDB implementation
│   │   ├── RegisterTests/
│   │   └── RegisterService.IntegrationTests/
│   ├── Tenant/
│   │   ├── TenantCore/        # Business logic
│   │   ├── TenantRepository/  # Data access
│   │   ├── TenantService/     # API service
│   │   ├── TenantUnitTests/
│   │   └── TenantService.IntegrationTests/
│   ├── Validator/
│   │   ├── ValidatorCore/     # Business logic
│   │   ├── ValidatorService/  # API service
│   │   ├── ValidationEngine/  # Validation rules
│   │   └── ValidatorTests/
│   ├── Wallet/
│   │   ├── WalletServiceCore/ # Business logic
│   │   ├── WalletService/     # API service
│   │   ├── WalletSQLRepository/  # EF Core implementation
│   │   ├── WalletUnitTests/
│   │   └── WalletService.IntegrationTests/
│   └── Proxy/                 # NGINX reverse proxy
│       ├── nginx.conf
│       ├── Dockerfile
│       └── deploy/
│
├── SDK/                        # Client libraries
│   ├── SiccarApplicationClient/
│   ├── SiccarApplicationClientTests/
│   └── Siccar.SDK.Fluent/
│
├── UI/                         # User interfaces
│   ├── AdminUI/
│   │   ├── Server/            # Blazor backend
│   │   ├── Client/            # Blazor WASM frontend
│   │   └── AdminUiTest/
│   └── siccarcmd/             # CLI tool
│
└── Siccar.EndToEndTests/       # End-to-end test suite
```

### Test Organization

```
Tests Structure:
├── Unit Tests (per project)
│   └── *UnitTests/*.csproj
├── Integration Tests (per service)
│   └── *.IntegrationTests/*.csproj
└── End-to-End Tests
    └── Siccar.EndToEndTests/
```

## Architecture Design

### Microservices Communication Patterns

#### Synchronous Communication (HTTP)
- Service-to-service via Dapr service invocation
- External clients via API gateway (Proxy service)
- Authentication via JWT tokens
- Rate limiting and throttling

#### Asynchronous Communication (Messaging)
- Event-driven via RabbitMQ through Dapr pub/sub
- Service independence and loose coupling
- Event sourcing patterns where applicable
- Dead letter queues for failed messages

### Data Architecture

#### Per-Service Databases (Database per Service Pattern)

**Wallet Service → MySQL**
- Wallet entities and metadata
- Encrypted private keys
- Access control lists
- EF Core with migrations

**Register Service → MongoDB**
- Blocks and transactions
- Merkle trees
- Historical ledger data
- RegisterCoreMongoDBStorage implementation

**Blueprint Service → MongoDB**
- Blueprint definitions
- Workflow templates
- Blueprint versions

**Action Service → Redis**
- Transient action state
- Action processing queues
- Dapr state store

**Tenant Service → SQL**
- Tenant configuration
- Identity provider mappings
- Policies and quotas

**Validator Service → Redis**
- Validation state
- Validation results cache

**Peer Service → Redis**
- Peer registry
- Network topology
- Connection state

#### Shared Data Stores

**Redis (Distributed Cache)**
- Session state
- Configuration cache
- Distributed locks
- Rate limiting counters

**RabbitMQ (Message Broker)**
- Event bus for inter-service communication
- Work queues for background processing
- Topic exchanges for event routing

### Security Architecture

#### Identity and Authentication Flow

1. **User Authentication**
   - User authenticates with identity provider (Azure AD/B2C)
   - Identity provider issues JWT token
   - Token includes user claims and tenant information

2. **Service Authentication**
   - Client presents JWT to API Gateway (Proxy)
   - Gateway validates token and routes to service
   - Services validate Dapr app tokens for service-to-service calls

3. **Authorization**
   - Role-based access control (RBAC)
   - Tenant isolation enforced at service level
   - Resource-level permissions

#### Secret Management

**Local Development:**
- `components/secretsFile.json` (gitignored)
- `localsecretstore.yaml` Dapr component
- Secrets: keyVaultConnectionString, SORCHAClientId, SORCHAClientSecret, walletEncryptionKey

**Production:**
- Azure Key Vault for secret storage
- Kubernetes secrets for service configuration
- Managed identities for Azure resource access
- Dapr secret store component referencing Key Vault

#### Cryptography

- **SiccarPlatformCryptography library** for all crypto operations
- Industry-standard algorithms (RSA, ECDSA, AES)
- Secure random number generation
- Key derivation functions (PBKDF2)
- Hardware security module (HSM) support for production

### Observability Architecture

#### Logging Strategy

**Structured Logging (Serilog)**
```
Output Template:
{Timestamp:o} [{Level:u3}][{MachineName}/{Application}/{SourceContext}] {Message:lj}{NewLine}{Exception}
```

**Log Levels:**
- Debug: Development diagnostics
- Information: Service lifecycle, business events
- Warning: Recoverable errors, degraded performance
- Error: Unhandled exceptions, service failures

**Log Sinks:**
- Console: All environments (Docker logs)
- Seq: Centralized log aggregation
- Application Insights: Azure production monitoring

#### Distributed Tracing (Zipkin)
- Automatic trace context propagation via Dapr
- Trace ID in all log entries
- Span creation for external calls
- Performance profiling

#### Metrics and Monitoring
- Health check endpoints (`/health`, `/ready`)
- Prometheus metrics export
- Application Insights custom metrics
- Performance counters
- Business KPIs

### Deployment Architecture

#### Local Development (Docker Compose)

```yaml
Services:
- Infrastructure: redis, mongodb, mysql, rabbitmq, zipkin, seq
- Microservices: action, blueprint, peer, register, tenant, validator, wallet, adminui
- Sidecars: *-dapr (Dapr sidecar for each service)
- Proxy: nginx reverse proxy
```

**Access:** http://localhost:8080

#### Kubernetes Deployment

**Namespace:** siccar (configurable)

**Resources per Service:**
- Deployment (service + Dapr sidecar)
- Service (ClusterIP)
- Ingress (external access)
- ConfigMap (configuration)
- Secret (sensitive config)

**Dapr Components:**
- State stores (per service)
- Pub/sub (RabbitMQ)
- Secret store (Azure Key Vault)
- Configuration (Kubernetes ConfigMap)

#### Azure Infrastructure (Bicep)

**Core Resources:**
- Azure Kubernetes Service (AKS)
- Azure Container Registry (ACR)
- Azure Key Vault
- Azure Cosmos DB (MongoDB API)
- Azure Database for MySQL
- Azure Cache for Redis
- Azure Service Bus (or RabbitMQ on AKS)
- Application Insights
- Log Analytics Workspace
- Virtual Network (VNet)

**Infrastructure as Code:**
- `main.bicep` - Master orchestration
- `aks.bicep` - Kubernetes cluster
- `acr.bicep` - Container registry
- `key-vault.bicep` - Secret management
- `cosmosdb-mongo.bicep` - MongoDB
- `storage-account.bicep` - Storage
- `vnet.bicep` - Networking

### CI/CD Pipeline

#### Build Pipeline (Azure DevOps)

1. **Source Control Trigger**
   - Commit to feature branch or main
   - Branch naming: `claude/*` for automated pushes

2. **Restore Dependencies**
   - Authenticate to private NuGet feed (FEED_ACCESSTOKEN)
   - `dotnet restore`

3. **Build**
   - `dotnet build --configuration Release`
   - Build all projects in SORCHA.sln

4. **Test**
   - Unit tests: `dotnet test --filter "Category=Unit"`
   - Integration tests: `dotnet test --filter "Category=Integration"`
   - Code coverage collection

5. **Docker Build**
   - Build Docker images for each service
   - Tag with build number and git commit SHA

6. **Push Images**
   - Push to Azure Container Registry
   - Update Kubernetes manifests with new image tags

#### Deployment Pipeline

1. **Development Environment**
   - Automatic deployment on main branch merge
   - Kubernetes namespace: `siccar-dev`
   - Lower resource limits

2. **Staging Environment**
   - Manual approval required
   - Full production configuration
   - Smoke tests after deployment

3. **Production Environment**
   - Manual approval required
   - Blue-green deployment strategy
   - Health checks before traffic switch
   - Rollback capability

## Development Workflow

### Local Setup

1. **Prerequisites**
   - Docker Desktop installed and running
   - Visual Studio 2022 or VS Code with C# extension
   - .NET 9 SDK
   - Git
   - Personal Access Token (PAT) for NuGet feed

2. **Configuration**
   - Clone repository
   - Create `.env` file in root: `FEED_ACCESSTOKEN=<your-pat>`
   - Create `components/secretsFile.json` with required secrets
   - Copy service-specific `appsettings.Development.json` files

3. **Start Services**
   - Set `docker-compose` as startup project
   - Run in Visual Studio, or
   - `docker-compose up -d` from command line

4. **Verify**
   - Access http://localhost:8080
   - Check service health endpoints
   - View logs in Seq: http://localhost:5341

### Making Changes

1. **Create Feature Branch**
   - Branch from `main`
   - Naming: `feature/description` or `bugfix/description`
   - Automated branches: `claude/*` for AI-assisted changes

2. **Implement Changes**
   - Follow constitutional principles
   - Write tests first (TDD encouraged)
   - Maintain test coverage > 80%

3. **Test Locally**
   - Run unit tests: `dotnet test`
   - Run integration tests with Docker services running
   - Manual testing via AdminUI or siccarcmd

4. **Code Review**
   - Create pull request
   - Ensure CI builds pass
   - Address review feedback
   - Squash and merge

### Testing Strategy

#### Unit Testing
- Test business logic in isolation
- Mock external dependencies
- Fast execution (< 1s per test)
- High coverage for core libraries (> 80%)

#### Integration Testing
- Test service APIs with real dependencies
- Use TestContainers for database setup
- Clean state between tests
- Verify service contracts

#### End-to-End Testing
- Test complete user workflows
- All services running in test environment
- Realistic data and scenarios
- Performance verification

### Dependency Management

#### NuGet Packages

**Adding Package:**
```bash
dotnet add package <PackageName> --version <Version>
```

**Updating Packages:**
- Follow [DEPENDENCY_UPGRADE_PLAN.md](../DEPENDENCY_UPGRADE_PLAN.md)
- Test thoroughly after upgrades
- Document breaking changes

**Internal Packages:**
- Published to private NuGet feed
- Versioned semantically (SemVer)
- Common libraries shared across services

#### .NET Framework Upgrade

**Recent Upgrade: .NET 8 → .NET 9**
- Phase 1: 45 projects upgraded
- See [docs/DOTNET9_PHASE1_PROGRESS.md](../docs/DOTNET9_PHASE1_PROGRESS.md)
- Ongoing testing and validation

## Risk Management

### Technical Risks

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Service coupling increases | High | Enforce API contracts, use versioning |
| Performance degradation | High | Regular performance testing, monitoring |
| Data consistency issues | High | Event sourcing, saga patterns |
| Security vulnerabilities | Critical | Regular security audits, dependency scanning |
| Dependency conflicts | Medium | Version pinning, compatibility testing |

### Operational Risks

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Service downtime | High | High availability setup, monitoring |
| Data loss | Critical | Regular backups, disaster recovery plan |
| Scalability limits | Medium | Load testing, auto-scaling configuration |
| Configuration errors | Medium | Configuration validation, gitops |
| Deployment failures | Medium | Automated rollback, blue-green deployment |

## Future Enhancements

### Near-Term (Next 3-6 months)
- Complete .NET 9 migration for remaining projects
- Enhanced monitoring dashboards
- Performance optimization
- Additional integration tests
- Security hardening

### Medium-Term (6-12 months)
- Advanced consensus mechanisms
- GraphQL API support
- Real-time event streaming
- Enhanced analytics capabilities
- Multi-region deployment

### Long-Term (12+ months)
- Smart contract execution environment
- Advanced query capabilities
- AI-powered fraud detection
- Blockchain interoperability
- Mobile SDK

## Success Criteria

### Technical Success
- ✅ All services independently deployable
- ✅ Test coverage > 80% for core libraries
- ✅ API response times meet SLAs
- ✅ Zero critical security vulnerabilities
- ✅ Successful CI/CD pipeline execution

### Operational Success
- ✅ System uptime > 99.9%
- ✅ Mean time to recovery < 15 minutes
- ✅ Successful deployments per week > 3
- ✅ Zero data loss incidents
- ✅ Monitoring provides full visibility

### Business Success
- ✅ Multiple tenants onboarded successfully
- ✅ Transaction volume growing month-over-month
- ✅ Developer adoption (SDK usage)
- ✅ Positive customer feedback
- ✅ Regulatory compliance maintained

## References

- [Project Specification](.specify/spec.md)
- [Project Constitution](.specify/constitution.md)
- [Project README](../README.md)
- [Deployment Guide](../deploy/bicep/README.md)
- [Troubleshooting Guide](../TROUBLESHOOTING.md)
- [Dependency Upgrade Plan](../DEPENDENCY_UPGRADE_PLAN.md)
- [Release Notes](../RELEASENOTES.md)

---

**Document Control**
- **Plan Owner:** SORCHA Architecture Team
- **Last Updated:** 2025-11-11
- **Review Schedule:** Monthly
- **Next Review:** 2025-12-11
- **Status:** Active - Living Document
