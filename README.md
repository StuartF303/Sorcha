# Sorcha

A modern .NET 10 blueprint execution engine and designer for data flow orchestration.

## Development Status

**Current Stage:** Active Development - MVD Phase (97% Complete) | [View Detailed Status Report](docs/development-status.md)

| Component | Status | Completion |
|-----------|--------|------------|
| Core Libraries | Production Ready | 97% |
| **⭐ Execution Engine (Portable)** | **✅ COMPLETE** | **100%** |
| **⭐ Blueprint Service** | **✅ COMPLETE** | **100%** |
| **⭐ Wallet Service** | **✅ Core Complete** | **90%** |
| **⭐ Register Service** | **✅ COMPLETE** | **100%** |
| Services & APIs | Enhanced | 97% |
| Testing & CI/CD | Production Ready | 95% |

> **⚠️ Production Readiness: 10%** - Core functionality complete, but authentication, database persistence, and security hardening are pending. See [MASTER-PLAN.md](.specify/MASTER-PLAN.md) for details.

**Recent Updates (2025-12-07):**
- ✅ **Blueprint Service Orchestration (Sprint 10) complete** - Delegation tokens, state reconstruction, instance management
- ✅ **Project cleanup and rationalization** - Archived superseded documents, removed orphaned files

**Previous Updates (2025-11-18):**
- ✅ **Performance baseline established** - Mean latency 1.16ms, P99 5.08ms, 55+ RPS sustained
- ✅ **Performance test infrastructure fixed** - Resolved HttpClient disposal bug, NBomber 6.1.2 compatible
- ✅ **Blueprint Validation Test Plan created** - 10 test categories, ~70 test cases, graph cycle detection
- ✅ **Wallet Service integration complete** - Real cryptographic operations in Blueprint Service
- ✅ **Transaction signing implemented** - All actions cryptographically signed via Wallet Service
- ✅ **Wallet Service comprehensive status report** - 90% feature complete, 111 tests, 14 REST endpoints ([View Status](docs/wallet-service-status.md))
- ✅ **Register Service 100% complete with comprehensive testing** (112 tests, ~2,459 LOC)
- ✅ Blueprint-Action Service Sprints 3-7 completed (96% Phase 1 complete)
- ✅ SignalR real-time notifications with Redis backplane operational

**Key Milestones:**
- ✅ Blueprint modeling and fluent API
- ✅ REST API for blueprint management
- ✅ Cryptography and transaction handling
- ✅ Production-grade CI/CD pipeline
- ✅ Portable execution engine complete (client + server side)
- ✅ Comprehensive unit and integration test coverage (102+ tests for engine alone)
- ✅ **Unified Blueprint-Action service with SignalR**
- ✅ **Wallet Service core implementation and API endpoints**
- ✅ **Execution helper endpoints for client-side validation**
- ✅ **Register Service full implementation with comprehensive testing (100%)**
- ✅ **Register Service Phase 5 API with 20 REST endpoints, OData, and SignalR**
- 🚧 End-to-end integration (Blueprint → Wallet → Register flow)
- 🚧 Wallet Service EF Core repository and production deployment
- 🚧 Transaction processing in P2P service

See the [detailed development status](docs/development-status.md) for complete information on modules, testing coverage, and infrastructure.

## Overview

Sorcha is a modernized, cloud-native platform for defining, designing, and executing data flow blueprints. Built on .NET 10 and leveraging .NET Aspire for cloud-native orchestration, Sorcha provides a flexible and scalable solution for workflow automation and data processing pipelines.

## Specification & Planning

This project uses [Spec-Kit](https://github.com/github/spec-kit) for specification-driven development. All project specifications, architectural plans, and task tracking are maintained in the [.specify/](.specify/README.md) directory.

**Key Documents:**
- **[Constitution](.specify/constitution.md)** - Project principles and development standards
- **[Specification](.specify/spec.md)** - Requirements, architecture, and user scenarios
- **[Master Plan](.specify/MASTER-PLAN.md)** - Unified implementation strategy and phases
- **[Master Tasks](.specify/MASTER-TASKS.md)** - Consolidated task list with priorities
- **[Service Specs](.specify/specs/)** - Detailed specifications for each service

**For Developers:**
- Start with the [.specify README](.specify/README.md) to understand the specification structure
- Check [MASTER-PLAN.md](.specify/MASTER-PLAN.md) for current development phase and priorities
- Find tasks in [MASTER-TASKS.md](.specify/MASTER-TASKS.md) (P0 = MVD blockers, P1 = Core, P2 = Nice-to-have, P3 = Post-MVD)
- Follow [constitution.md](.specify/constitution.md) for architectural principles and coding standards

**For AI Agents:**
All specifications are designed to provide context for AI-assisted development. Consult the constitution for guardrails, the spec for requirements, and the master plan for implementation priorities.

## Features

### Core Capabilities
- **✅ Portable Blueprint Execution Engine** (COMPLETE): Stateless engine that runs client-side (Blazor WASM) and server-side
  - ✅ JSON Schema validation (Draft 2020-12)
  - ✅ JSON Logic evaluation for calculations and conditions
  - ✅ Selective data disclosure using JSON Pointers (RFC 6901)
  - ✅ Conditional routing between participants
  - ✅ Thread-safe, immutable design pattern
  - ✅ Comprehensive test coverage: 93 unit tests + 9 integration tests
  - ✅ Real-world scenarios tested: loan applications, purchase orders, multi-step surveys

- **✅ Unified Blueprint-Action Service** (Sprints 3-5 COMPLETE): Complete workflow management
  - ✅ Blueprint CRUD operations and versioning
  - ✅ Action retrieval, submission, and rejection (Sprint 4)
  - ✅ Real-time notifications via SignalR with Redis backplane (Sprint 5)
  - ✅ Execution helper endpoints (validate, calculate, route, disclose) (Sprint 5)
  - ✅ File upload/download support
  - ✅ Integration with Wallet Service (encryption/decryption) (Sprint 3)
  - ✅ Integration with Register Service (blockchain transactions) (Sprint 3)

- **✅ Wallet Service** (Core 90% COMPLETE): Secure cryptographic wallet management ([View Detailed Status](docs/wallet-service-status.md))
  - ✅ HD wallet support with BIP32/BIP39/BIP44 standards (NBitcoin)
  - ✅ Multi-algorithm support (ED25519, NISTP256, RSA-4096)
  - ✅ Transaction signing and verification
  - ✅ Payload encryption/decryption
  - ✅ Access delegation and control (Owner/ReadWrite/ReadOnly)
  - ✅ 14 REST API endpoints with comprehensive OpenAPI docs
  - ✅ 111 unit tests (~75-80% coverage)
  - ✅ In-memory repository implementation
  - 🚧 EF Core repository (pending - P1)
  - 🚧 Azure Key Vault integration (pending - P1)
  - 🚧 Authentication & authorization (pending - P1)
  - 🚧 HD address generation (not implemented - design needed)

- **✅ Register Service** (100% COMPLETE): Distributed ledger for transaction storage
  - ✅ Complete domain models (Register, TransactionModel, Docket, PayloadModel)
  - ✅ RegisterManager, TransactionManager, DocketManager, QueryManager (~3,500 LOC)
  - ✅ 20 REST endpoints (registers, transactions, dockets, query API)
  - ✅ Real-time notifications via SignalR with RegisterHub
  - ✅ OData V4 support for flexible queries
  - ✅ Comprehensive testing (112 tests, ~2,459 LOC)
  - ✅ Chain validation and block sealing
  - ✅ DID URI support: `did:sorcha:register:{id}/tx:{txId}`
  - 🚧 MongoDB repository (InMemory implementation complete)

- **Blueprint Designer**: Visual designer for creating and managing workflows
  - Blazor WASM client with offline capabilities
  - Client-side validation using portable execution engine
  - Real-time blueprint testing mode
  - Schema browser and form designer

### Platform Features
- **.NET 10**: Built on the latest .NET platform for maximum performance
- **.NET Aspire**: Cloud-native orchestration and service discovery
- **Minimal APIs**: Modern, lightweight API design
- **SignalR**: Real-time notifications with Redis backplane
- **Observability**: Built-in OpenTelemetry support for monitoring and tracing
- **Security**: JWT authentication, rate limiting, audit logging

## Project Structure

```
Sorcha/
├── src/
│   ├── Apps/                        # Application layer
│   │   ├── Sorcha.AppHost/         # .NET Aspire orchestration host
│   │   ├── Sorcha.Demo/            # Blueprint workflow demo CLI
│   │   └── UI/
│   │       └── Sorcha.Blueprint.Designer.Client/  # Blazor WASM UI
│   ├── Common/                      # Cross-cutting concerns
│   │   ├── Sorcha.Blueprint.Models/ # Domain models
│   │   ├── Sorcha.Cryptography/    # Cryptographic operations
│   │   └── Sorcha.ServiceDefaults/ # Shared service configurations
│   ├── Core/                        # Business logic
│   │   ├── Sorcha.Blueprint.Engine/ # Blueprint execution engine
│   │   ├── Sorcha.Blueprint.Fluent/ # Fluent API builders
│   │   └── Sorcha.Blueprint.Schemas/ # Schema management
│   └── Services/                    # Service layer
│       ├── Sorcha.ApiGateway/      # YARP API Gateway
│       ├── Sorcha.Blueprint.Service/ # Blueprint REST API
│       ├── Sorcha.Register.Service/ # Distributed ledger service
│       ├── Sorcha.Wallet.Service/  # Wallet management service
│       ├── Sorcha.Tenant.Service/  # Multi-tenancy service
│       └── Sorcha.Peer.Service/    # P2P networking service
├── tests/                           # Test projects
├── docs/                            # Documentation
└── .github/                         # GitHub workflows
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later (version 10.0.100+)
- [Git](https://git-scm.com/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (required for integration tests and Redis)
- A code editor:
  - [Visual Studio 2025](https://visualstudio.microsoft.com/) (recommended for Windows)
  - [Visual Studio Code](https://code.visualstudio.com/) with C# extension
  - [JetBrains Rider](https://www.jetbrains.com/rider/)

### Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/StuartF303/Sorcha.git
   cd Sorcha
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run all tests**
   ```bash
   dotnet test
   ```

5. **Start the application**
   ```bash
   # Using Aspire (recommended)
   dotnet run --project src/Apps/Sorcha.AppHost

   # Or run services individually
   dotnet run --project src/Services/Sorcha.ApiGateway
   ```

### Running in Development

#### Option 1: Using .NET Aspire (Recommended)

The easiest way to run all services with orchestration:

```bash
dotnet run --project src/Apps/Sorcha.AppHost
```

This will:
- Start all services (Gateway, Blueprint Service, Peer Service, Blazor Client)
- Launch the Aspire dashboard at `http://localhost:15888`
- Configure service discovery and health checks automatically
- Start Redis container via Docker

Access points:
- **Aspire Dashboard**: `http://localhost:15888`
- **API Gateway**: `https://localhost:7082`
- **Blueprint Designer**: `https://localhost:7083`
- **Health Checks**: `https://localhost:7082/api/health`

#### Option 2: Running Individual Services

**API Gateway:**
```bash
dotnet run --project src/Services/Sorcha.ApiGateway
# Available at https://localhost:7082
```

**Blueprint Service:**
```bash
dotnet run --project src/Services/Sorcha.Blueprint.Service
# Available at https://localhost:7080
```

**Peer Service:**
```bash
dotnet run --project src/Services/Sorcha.Peer.Service
# Available at https://localhost:7081
```

**Blueprint Designer (Blazor WebAssembly):**
```bash
dotnet run --project src/Apps/UI/Sorcha.Blueprint.Designer.Client
# Available at https://localhost:7083
```

### Development Workflow

1. **Make code changes** in your preferred editor

2. **Run tests** to verify changes
   ```bash
   dotnet test
   ```

3. **Hot reload** - Many changes reload automatically without restart when using `dotnet watch`
   ```bash
   dotnet watch --project src/Services/Sorcha.Blueprint.Service
   ```

4. **Format code** before committing
   ```bash
   dotnet format
   ```

5. **Check for issues**
   ```bash
   # Check for vulnerable packages
   dotnet list package --vulnerable

   # Check for outdated packages
   dotnet list package --outdated
   ```

## Testing

Sorcha includes comprehensive test coverage across multiple layers.

### Test Projects

- **Sorcha.Blueprint.Api.Tests** - API endpoint tests
- **Sorcha.Blueprint.Engine.Tests** - Blueprint engine and workflow demo tests
- **Sorcha.Blueprint.Fluent.Tests** - Fluent builder pattern tests
- **Sorcha.Cryptography.Tests** - Cryptography library tests
- **Sorcha.Gateway.Integration.Tests** - Gateway routing and integration tests
- **Sorcha.Performance.Tests** - NBomber load/performance tests
- **Sorcha.UI.E2E.Tests** - End-to-end Playwright tests

### Running Tests

**Run all tests:**
```bash
dotnet test
```

**Run specific test project:**
```bash
dotnet test tests/Sorcha.Blueprint.Api.Tests
dotnet test tests/Sorcha.Cryptography.Tests
```

**Run with code coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
```

**Run tests in watch mode (auto-rerun on changes):**
```bash
dotnet watch test --project tests/Sorcha.Blueprint.Api.Tests
```

**Filter tests by name:**
```bash
dotnet test --filter "FullyQualifiedName~CryptoModule"
```

### Integration Tests

Integration tests require Docker for Redis.

**Prerequisites:**
```bash
# Ensure Docker Desktop is running
docker ps

# Run integration tests
dotnet test tests/Sorcha.Gateway.Integration.Tests
```

**What they test:**
- Full Aspire AppHost with all services
- YARP gateway routing
- Service-to-service communication
- Health check aggregation
- Redis caching

### Workflow Demo Tests

The Expense Approval Workflow demonstrates Blueprint functionality with JSON Logic routing:

**Run the workflow demo tests:**
```bash
dotnet test tests/Sorcha.Blueprint.Engine.Tests --filter "FullyQualifiedName~ExpenseApprovalWorkflowDemoTests"
```

**What it demonstrates:**
- **JSON Logic Routing**: Dynamic workflow paths based on expense amount:
  - < $100 → Instant system approval
  - $100-$1000 → Route to manager for review
  - ≥ $1000 → Route to finance director for approval
- **Participant Role Fulfillment**: Employee, Manager, Finance Director, and System roles
- **Data Flow Visualization**: CLI output shows the complete approval workflow
- **Workflow Execution**: Real-time routing decisions with formatted console output

**Example output:**
```
╔═══════════════════════════════════════════════════════════╗
║ Expense Approval - Manager Review ($100-$1000)            ║
╚═══════════════════════════════════════════════════════════╝

📝 Expense Claim Submitted:
   Employee: Alice Johnson (employee)
   Amount: $450
   Description: Client dinner and entertainment
   Category: Entertainment

🔀 Routing Decision:
   Next Action: 2 - Manager Review
   Assigned To: Bob Smith (manager)

👔 Manager: Bob Smith
   Decision: APPROVED ✅
   Comments: Approved - valid client entertainment expense

✅ Result: APPROVED BY MANAGER
   Amount: $450
════════════════════════════════════════════════════════════
```

### Performance Tests

Load test the application using NBomber:

```bash
# Run performance tests (30s duration, 50 RPS target)
dotnet run --project tests/Sorcha.Performance.Tests --configuration Release -- http://localhost:5000 30 50

# Quick test (10s duration, 10 RPS)
dotnet run --project tests/Sorcha.Performance.Tests --configuration Release -- http://localhost:5000 10 10
```

**Baseline Performance Metrics (Sprint 7 - 2025-11-18):**

```
Environment: .NET 10.0, Aspire 13.0.0, Windows 11
Test Load: 13,065 requests over 30 seconds, 50 RPS target

Average Latency:
├─ Mean:    1.16 ms  ⚡ Excellent
├─ P50:     0.84 ms  ⚡ Excellent
├─ P95:     2.85 ms  ⚡ Excellent
└─ P99:     5.08 ms  ✅ Very Good

Throughput:
├─ Peak RPS: 55.5 req/sec (stress test)
└─ Average:  435.5 req/sec across all scenarios

Top Performing Scenarios:
├─ Stress Test (ramping):  0.98ms mean, 55.5 RPS
├─ Health Check:           1.09ms mean, 50.0 RPS
└─ Execution Helpers:      1.10ms mean, 50.0 RPS
```

**Test Scenarios:**
- Health endpoint load test (50 RPS)
- Blueprint CRUD operations (25 RPS)
- Action submission workflow (20 RPS)
- Wallet signing operations (30 RPS)
- Register transaction queries (25 RPS)
- Mixed workload with concurrent operations
- Stress test with ramping load (up to 100 RPS)

**Performance Tracking:**
- Baseline metrics: `tests/Sorcha.Performance.Tests/PERFORMANCE-BASELINE.md`
- Historical data: `tests/Sorcha.Performance.Tests/baseline-metrics.csv`
- Reports generated in: `tests/Sorcha.Performance.Tests/performance-reports/`

**Regression Detection:**
- Mean latency >20% worse: **investigate**
- P95 latency >20% worse: **investigate**
- Throughput >20% lower: **investigate**

### Cryptography Library Tests

Test the cryptography library with multiple key types:

```bash
dotnet test tests/Sorcha.Cryptography.Tests
```

**Example: Performance testing different key types**
```bash
# Run specific crypto tests
dotnet test tests/Sorcha.Cryptography.Tests --filter "FullyQualifiedName~ED25519"
dotnet test tests/Sorcha.Cryptography.Tests --filter "FullyQualifiedName~NISTP256"
dotnet test tests/Sorcha.Cryptography.Tests --filter "FullyQualifiedName~RSA4096"
```

**Benchmarking crypto operations:**
```csharp
// Example: Load test key generation
for (int i = 0; i < 1000; i++)
{
    var result = await cryptoModule.GenerateKeySetAsync(WalletNetworks.ED25519);
}

// Example: Load test signing
var keySet = await cryptoModule.GenerateKeySetAsync(WalletNetworks.ED25519);
byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes("test data"));

for (int i = 0; i < 10000; i++)
{
    await cryptoModule.SignAsync(hash, (byte)WalletNetworks.ED25519, keySet.Value!.PrivateKey.Key!);
}
```

### Code Coverage Reports

Generate HTML coverage reports:

```bash
# Install report generator (one time)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:Html

# Open report (Windows)
start coverage-report/index.html

# Open report (Mac/Linux)
open coverage-report/index.html
```

### E2E Tests (Playwright)

End-to-end browser tests require Playwright setup:

```bash
# First-time setup
cd tests/Sorcha.UI.E2E.Tests
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install --with-deps

# Run E2E tests
dotnet test tests/Sorcha.UI.E2E.Tests

# Run in headed mode (see browser)
dotnet test tests/Sorcha.UI.E2E.Tests -- NUnit.Headless=false
```

### Continuous Testing

Watch tests and auto-run on file changes:

```bash
# Watch all tests
dotnet watch test

# Watch specific project
dotnet watch test --project tests/Sorcha.Cryptography.Tests
```

### Test Best Practices

See [docs/testing.md](docs/testing.md) for comprehensive testing guidelines including:
- Test naming conventions
- AAA pattern (Arrange-Act-Assert)
- Mocking with Moq
- FluentAssertions usage
- Test data builders
- Coverage targets

## Development

### Solution Structure

- **Sorcha.AppHost**: The .NET Aspire orchestration project that manages all services
- **Sorcha.ServiceDefaults**: Shared configurations including OpenTelemetry, health checks, and service discovery
- **Sorcha.Blueprint.Api**: The core API for blueprint management via minimal APIs
- **Sorcha.Blueprint.Designer.Client**: Blazor WebAssembly application for designing and managing blueprints
- **Sorcha.Cryptography**: Standalone cryptography library for key management and digital signatures

### Architecture

Sorcha follows a microservices architecture with:

- **Service-oriented design**: Each component is independently deployable
- **Cloud-native patterns**: Built-in support for service discovery, health checks, and distributed tracing
- **Modern APIs**: RESTful APIs using minimal API patterns
- **WebAssembly UI**: Blazor WebAssembly for responsive, offline-capable user interfaces
- **Gateway Pattern**: YARP-based API gateway for routing and aggregation

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details on:

- Code of conduct
- Development workflow
- Submitting pull requests
- Reporting issues

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Roadmap

- [x] Core blueprint execution engine (100% - Portable, client + server)
- [x] Blueprint validation and testing framework (100%)
- [x] Unified Blueprint-Action Service with SignalR (100%)
- [x] Wallet Service core implementation (90% - Features complete, production infra pending)
- [x] Register Service with distributed ledger (100%)
- [ ] Wallet Service production readiness (40% - Auth, storage, key mgmt needed)
- [ ] End-to-end integration (Blueprint → Wallet → Register)
- [ ] Visual blueprint designer (85% - functional, needs polish)
- [ ] Production storage (EF Core repositories for Wallet/Register)
- [ ] Plugin system for custom actions
- [ ] Multi-tenant support
- [ ] Cloud deployment templates (Azure, AWS, GCP)
- [ ] Advanced consensus mechanisms
- [ ] Real-time monitoring dashboard

## Documentation

Full documentation is available in the [docs](docs/) directory:

- [Architecture Overview](docs/architecture.md)
- [Getting Started Guide](docs/getting-started.md)
- [Blueprint Schema](docs/blueprint-schema.md)
- [Development Status](docs/development-status.md)
- [Wallet Service Status](docs/wallet-service-status.md) ⭐ NEW
- [API Reference](docs/api-reference.md)
- [Deployment Guide](docs/deployment.md)

## Support

- Documentation: [docs/](docs/)
- Issues: [GitHub Issues](https://github.com/yourusername/sorcha/issues)
- Discussions: [GitHub Discussions](https://github.com/yourusername/sorcha/discussions)

## Acknowledgments

This project is inspired by and modernizes concepts from the [SiccarV3](https://github.com/stuartf303/siccarv3) project.

---

Built with ❤️ using .NET 10 and .NET Aspire
