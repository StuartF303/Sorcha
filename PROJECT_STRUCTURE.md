# Sorcha Project Structure

This document describes the modernized Sorcha project structure, created from the SiccarV3 codebase.

## Migration Summary

**Source**: SiccarV3 (.NET 8/9, legacy architecture)
**Target**: Sorcha (.NET 10, modern cloud-native architecture)

### Key Modernizations

1. **.NET 10**: Upgraded to the latest .NET framework
2. **.NET Aspire**: Cloud-native orchestration and service discovery
3. **Minimal APIs**: Modern, lightweight API design
4. **Simplified Architecture**: Focused on core blueprint execution
5. **Open Source Ready**: Complete with CI/CD, documentation, and contribution guidelines

## Project Structure

```
Sorcha/
├── .github/
│   └── workflows/
│       ├── build.yml           # Build and test workflow
│       ├── release.yml         # Release and deployment
│       └── codeql.yml          # Security analysis
│
├── docs/
│   ├── README.md               # Documentation index
│   ├── architecture.md         # System architecture
│   ├── getting-started.md      # Quick start guide
│   └── blueprint-schema.md     # Blueprint format specification
│
├── src/
│   ├── Sorcha.AppHost/
│   │   ├── Sorcha.AppHost.csproj
│   │   └── AppHost.cs          # Aspire orchestration
│   │
│   ├── Sorcha.ServiceDefaults/
│   │   ├── Sorcha.ServiceDefaults.csproj
│   │   └── Extensions.cs       # Shared configurations
│   │
│   ├── Sorcha.Blueprint.Engine/
│   │   ├── Sorcha.Blueprint.Engine.csproj
│   │   └── Program.cs          # Blueprint execution API
│   │
│   └── Sorcha.Blueprint.Designer/
│       ├── Sorcha.Blueprint.Designer.csproj
│       ├── Program.cs          # Web UI entry point
│       └── Components/         # Blazor components
│
├── tests/                      # Test projects (to be added)
│
├── .gitignore                  # Git ignore rules
├── .gitattributes              # Git attributes
├── CONTRIBUTING.md             # Contribution guidelines
├── LICENSE                     # MIT License
├── README.md                   # Main documentation
├── Sorcha.sln                  # Solution file
└── nuget.config                # NuGet configuration
```

## Core Components

### 1. Sorcha.AppHost
- **Purpose**: .NET Aspire orchestration host
- **Technology**: .NET Aspire 13.0.0
- **Target Framework**: net10.0
- **Role**: Manages service lifecycle, discovery, and configuration

### 2. Sorcha.ServiceDefaults
- **Purpose**: Shared service configurations
- **Features**:
  - OpenTelemetry integration
  - Health checks
  - Service discovery
  - Resilience patterns
  - HTTP client configuration
- **Target Framework**: net10.0

### 3. Sorcha.Blueprint.Engine
- **Purpose**: Blueprint execution engine
- **API Style**: Minimal APIs
- **Responsibilities**:
  - Blueprint validation
  - Workflow execution
  - State management
  - Action orchestration
- **Target Framework**: net10.0
- **Endpoints** (to be implemented):
  - POST /blueprints/execute
  - GET /blueprints/{id}/status
  - POST /blueprints/validate
  - GET /health

### 4. Sorcha.Blueprint.Designer
- **Purpose**: Visual blueprint designer
- **Technology**: Blazor Server
- **Features** (to be implemented):
  - Visual workflow editor
  - Real-time execution monitoring
  - Blueprint templates
  - Validation feedback
- **Target Framework**: net10.0

## Technology Stack

### Runtime & Frameworks
- .NET 10 (10.0.100)
- ASP.NET Core 10
- C# 13 with nullable reference types
- Implicit usings enabled

### Orchestration
- .NET Aspire 13.0.0
- Aspire.Hosting.AppHost
- Service discovery
- Configuration management

### Observability
- OpenTelemetry 1.12.0
  - OTLP Exporter
  - ASP.NET Core instrumentation
  - HTTP instrumentation
  - Runtime instrumentation
- Microsoft.Extensions.ServiceDiscovery 9.5.2
- Health checks

### API
- ASP.NET Core Minimal APIs
- OpenAPI/Swagger (Microsoft.AspNetCore.OpenApi 9.0.9)
- JSON serialization

### UI
- Blazor Server
- Interactive server components
- SignalR (built-in)

### Resilience
- Microsoft.Extensions.Http.Resilience 9.9.0
  - Retry policies
  - Circuit breakers
  - Timeout policies

## Configuration

### Build Configuration
- **Debug**: Development builds with symbols
- **Release**: Optimized production builds

### Target Framework
All projects target `net10.0`

### Nullable Reference Types
Enabled across all projects for improved null safety

## CI/CD Pipelines

### Build Workflow (.github/workflows/build.yml)
- Multi-platform build (Ubuntu, Windows, macOS)
- Automated testing
- Code quality checks
- Security scanning
- Code coverage reports

### Release Workflow (.github/workflows/release.yml)
- Triggered on version tags (v*.*.*)
- NuGet package creation
- Docker image builds
- GitHub release creation
- Optional NuGet.org publishing

### Security Workflow (.github/workflows/codeql.yml)
- CodeQL security analysis
- Multi-language scanning
- Scheduled weekly scans
- Security alerts

## Documentation

### User Documentation
- README.md - Project overview
- docs/getting-started.md - Quick start guide
- docs/architecture.md - System architecture
- docs/blueprint-schema.md - Blueprint format

### Developer Documentation
- CONTRIBUTING.md - Contribution guidelines
- PROJECT_STRUCTURE.md - This document
- Code comments and XML docs

## Next Steps

### Immediate (Foundation)
1. Create shared libraries:
   - Sorcha.Abstractions (interfaces, base classes)
   - Sorcha.Models (domain models)
2. Set up test projects:
   - Sorcha.Blueprint.Engine.Tests
   - Sorcha.Blueprint.Designer.Tests
   - Sorcha.Integration.Tests

### Phase 1 (Core Engine)
1. Implement blueprint schema models
2. Create blueprint validation engine
3. Build execution engine
4. Add action framework
5. Implement state management

### Phase 2 (Designer)
1. Create blueprint editor UI
2. Implement visual designer
3. Add real-time execution monitoring
4. Build template system

### Phase 3 (Advanced Features)
1. Plugin system
2. Multi-tenancy
3. Advanced scheduling
4. Distributed execution
5. Cloud deployment templates

## Migration from SiccarV3

### What Was Kept
- Core blueprint/workflow concept
- Multi-service architecture philosophy
- Observability-first approach

### What Was Modernized
- Upgraded to .NET 10
- Adopted .NET Aspire for orchestration
- Simplified to minimal APIs
- Removed legacy dependencies (IdentityServer4, Dapr)
- Streamlined service count
- Modern Blazor for UI

### What Was Removed (For Now)
- Tenant service (will be re-added as library)
- Wallet service (domain-specific)
- Peer-to-peer service (can be re-added as plugin)
- Register/ledger service (can be re-added as plugin)
- Validator service (integrated into engine)
- MongoDB (will use EF Core with flexible providers)
- RabbitMQ (Aspire provides alternatives)

## Design Principles

1. **Cloud-Native First**: Built for containerization and orchestration
2. **API-First**: RESTful APIs with OpenAPI documentation
3. **Observability**: Comprehensive telemetry from day one
4. **Modularity**: Clear separation of concerns
5. **Extensibility**: Plugin architecture for custom actions
6. **Developer Experience**: Easy local development with Aspire
7. **Production Ready**: CI/CD, monitoring, security built-in

## Build Status

✅ Solution builds successfully
✅ All projects target .NET 10
✅ NuGet packages restored
✅ Project structure organized
✅ CI/CD workflows configured
✅ Documentation created
✅ Open source ready

## Getting Started

```bash
# Clone repository
git clone https://github.com/yourusername/sorcha.git
cd sorcha

# Restore and build
dotnet restore
dotnet build

# Run with Aspire
dotnet run --project src/Sorcha.AppHost
```

See [docs/getting-started.md](docs/getting-started.md) for detailed instructions.

## License

MIT License - See [LICENSE](LICENSE) file for details.

## Acknowledgments

This project modernizes concepts from [SiccarV3](https://github.com/stuartf303/siccarv3) by Stuart Ferguson.

---

Last Updated: 2025-10-31
