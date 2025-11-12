# Sorcha Architecture

## Overview

Sorcha is a modern .NET 10 platform for defining, designing, and executing multi-participant data flow orchestration workflows (called "Blueprints"). Built on .NET Aspire for cloud-native orchestration, Sorcha provides a flexible and scalable solution for workflow automation with selective data disclosure and conditional routing.

**Last Updated:** 2025-01-04
**Version:** 1.0.0
**Status:** Active Development

## High-Level Architecture

```
┌───────────────────────────────────────────────────────────────────────────────┐
│                            Sorcha Platform                                     │
├───────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌────────────────────────┐              ┌──────────────────────────┐         │
│  │  Blueprint Designer    │              │  Blueprint Engine        │         │
│  │  (Blazor Server)       │─────────────▶│  (REST API)              │         │
│  │  + Designer.Client     │   HTTP       │  Minimal APIs            │         │
│  │  (Blazor WASM)         │              │  (In Development)        │         │
│  └────────────────────────┘              └──────────────────────────┘         │
│           │                                         │                          │
│           │              ┌──────────────────────────┼─────────────────┐       │
│           │              │                          │                  │       │
│           ▼              ▼                          ▼                  ▼       │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │                   Common Libraries Layer                                 │ │
│  ├─────────────────────────────────────────────────────────────────────────┤ │
│  │  • Blueprint.Models (Data Models)                                       │ │
│  │  • Blueprint.Fluent (Fluent API Builders)                              │ │
│  │  • Blueprint.Schemas (Schema Management & Caching)                     │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
│                              │                                                 │
│           ┌──────────────────┼──────────────────┐                            │
│           │                  │                  │                            │
│           ▼                  ▼                  ▼                            │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────────────────┐    │
│  │  Peer Service  │  │  Hosting Layer │  │  Blueprint Services        │    │
│  │  (P2P Network) │  │  - AppHost     │  │  - Engine                  │    │
│  │  - Discovery   │  │  - Service     │  │  - Designer                │    │
│  │  - Distribution│  │    Defaults    │  └────────────────────────────┘    │
│  │  - Gossip      │  └────────────────┘                                     │
│  │  (Planned)     │                                                          │
│  └────────────────┘                                                          │
│           │                                                                   │
│           └──────────────────────┐                                          │
│                                  │                                          │
└──────────────────────────────────┼──────────────────────────────────────────┘
                                   │
         ┌─────────────────────────┼─────────────────────────┐
         │                         │                         │
         ▼                         ▼                         ▼
┌─────────────────┐    ┌─────────────────┐    ┌───────────────────────┐
│  Storage        │    │  External       │    │  Peer Network         │
│  (Planned:      │    │  Schema Sources │    │  (Other Sorcha Nodes) │
│   EF Core)      │    │  (SchemaStore)  │    │  - gRPC/REST          │
└─────────────────┘    └─────────────────┘    │  - Transaction Sync   │
                                               └───────────────────────┘
```

## Solution Structure

Sorcha follows a clean 3-folder architecture pattern for maximum maintainability:

```
Sorcha/
├── src/
│   ├── Common/                           # Shared models and contracts
│   │   └── Sorcha.Blueprint.Models       # Blueprint data models
│   ├── Core/                             # Core business logic
│   │   ├── Sorcha.Blueprint.Engine       # Execution engine (REST API)
│   │   ├── Sorcha.Blueprint.Fluent       # Fluent API builders
│   │   └── Sorcha.Blueprint.Schemas      # Schema management
│   ├── Services/                         # Background services (PLANNED)
│   │   └── Sorcha.Peer.Service          # P2P networking service
│   └── Apps/                             # Applications
│       ├── Hosting/
│       │   ├── Sorcha.AppHost            # .NET Aspire orchestration
│       │   └── Sorcha.ServiceDefaults    # Shared service configs
│       └── UI/
│           ├── Sorcha.Blueprint.Designer         # Blazor Server
│           └── Sorcha.Blueprint.Designer.Client  # Blazor WASM
├── tests/                                # Test projects
│   ├── Sorcha.Blueprint.Models.Tests
│   ├── Sorcha.Blueprint.Fluent.Tests
│   ├── Sorcha.Blueprint.Schemas.Tests
│   ├── Sorcha.Blueprint.Engine.Tests
│   ├── Sorcha.Blueprint.Designer.Tests
│   ├── Sorcha.Peer.Service.Tests        # PLANNED
│   └── Sorcha.Integration.Tests
└── docs/                                 # Documentation
```

## Core Components

### 1. Common Layer

#### Sorcha.Blueprint.Models
Core domain models representing the Blueprint data structure.

**Key Models:**
- `Blueprint` - Root workflow definition (title, description, version, participants, actions)
- `Action` - Workflow step (data schemas, routing, disclosures, calculations, form)
- `Participant` - Workflow party (ID, name, organization, wallet, DID)
- `Disclosure` - Data visibility rule (participant, JSON Pointers)
- `Condition` - Routing logic (participant, JSON Logic)
- `Control` - UI form definition (hierarchical layout)
- `Calculation` - Computed field (JSON Logic expressions)

**Features:**
- JSON serialization/deserialization
- Data validation with `DataAnnotations`
- JSON Schema generation support
- Equality comparison (`IEquatable<T>`)
- Audit timestamps (createdAt, updatedAt)

**Technology:**
- System.Text.Json for serialization
- DataAnnotations for validation
- JsonSchema.Net.Generation for schema generation

### 2. Core Layer

#### Sorcha.Blueprint.Fluent
Fluent API for programmatically building blueprints with compile-time safety.

**Key Builders:**
- `BlueprintBuilder` - Main workflow builder
- `ParticipantBuilder` - Participant configuration
- `ActionBuilder` - Action/step configuration
- `DisclosureBuilder` - Data visibility rules
- `ConditionBuilder` - Routing conditions
- `CalculationBuilder` - Computed fields
- `FormBuilder` / `ControlBuilder` - UI form layout
- `DataSchemaBuilder` - JSON Schema definitions

**Example Usage:**
```csharp
var blueprint = BlueprintBuilder.Create()
    .WithTitle("Purchase Order")
    .WithDescription("Two-party purchase workflow")
    .AddParticipant("buyer", p => p
        .Named("Buyer Organization")
        .FromOrganisation("ORG-123"))
    .AddParticipant("seller", p => p
        .Named("Seller Organization"))
    .AddAction(0, a => a
        .WithTitle("Submit Order")
        .SentBy("buyer")
        .RequiresData(d => d
            .WithTitle("Order Details")
            .AddProperty("itemName", "string")
            .AddProperty("quantity", "integer"))
        .Disclose("seller", d => d.Field("/itemName").Field("/quantity"))
        .RouteToNext("seller"))
    .Build();
```

**Features:**
- Type-safe blueprint construction
- Validation at build time
- Fluent method chaining
- Participant reference validation
- Draft mode for incomplete blueprints

**Technology:**
- Modern C# patterns (method chaining, delegates)
- Strong typing with generics
- Integrated validation

#### Sorcha.Blueprint.Schemas
Schema management with pluggable repositories and client-side caching.

**Key Components:**
- `SchemaLibraryService` - Unified schema access across repositories
- `ISchemaRepository` - Repository interface for schema sources
- `BuiltInSchemaRepository` - Embedded schemas (person, address, document, payment)
- `SchemaStoreRepository` - External schema.org integration
- `ISchemaCacheService` - Caching interface
- `LocalStorageSchemaCacheService` - Browser LocalStorage cache

**Built-in Schemas:**
- `person.json` - Contact information, dates, social media
- `address.json` - Physical location data
- `document.json` - Document metadata
- `payment.json` - Payment information

**Features:**
- Multi-source schema aggregation
- Search and filtering (by category, source, keywords)
- Favorites management
- Usage tracking
- Client-side caching with statistics
- Extensible repository pattern

**Technology:**
- JsonSchema.Net for validation (Draft 2020-12)
- Blazored.LocalStorage for caching
- Async/await patterns throughout

#### Sorcha.Blueprint.Engine
REST API for blueprint execution and management (IN DEVELOPMENT).

**Current Status:** Template code only - not yet implemented

**Planned Responsibilities:**
- Blueprint CRUD operations
- Blueprint validation
- Execution orchestration
- State management
- Action execution
- Error handling and retry logic

**Planned API Endpoints:**
- `GET /api/blueprints` - List all blueprints
- `GET /api/blueprints/{id}` - Get blueprint by ID
- `POST /api/blueprints` - Create new blueprint
- `PUT /api/blueprints/{id}` - Update blueprint
- `DELETE /api/blueprints/{id}` - Delete blueprint
- `POST /api/blueprints/{id}/execute` - Execute blueprint
- `GET /api/blueprints/{id}/status` - Get execution status
- `POST /api/blueprints/validate` - Validate blueprint
- `GET /health` - Health check endpoint
- `GET /alive` - Liveness check endpoint

**Technology:**
- ASP.NET Core Minimal APIs
- OpenAPI/Swagger documentation
- Dependency Injection
- Background services for long-running tasks (planned)

### 3. Apps Layer

#### Sorcha.Blueprint.Designer (Blazor Server)
Web-based application host for the Blueprint designer.

**Responsibilities:**
- Host Blazor Server application
- HTTP client for Engine API
- Service discovery integration
- Health checks

**Features:**
- Interactive server-side rendering
- SignalR for real-time updates
- Output caching
- Anti-forgery protection

**Technology:**
- Blazor Server
- MudBlazor components
- SignalR

#### Sorcha.Blueprint.Designer.Client (Blazor WASM)
Client-side Blazor components and logic.

**Responsibilities:**
- Visual blueprint editor UI
- Blueprint validation
- Schema browser
- Form designer
- Client-side state management

**Features:**
- Visual workflow designer (Z.Blazor.Diagrams)
- MudBlazor component library
- LocalStorage caching for schemas
- JSON Schema validation

**Technology:**
- Blazor WebAssembly
- MudBlazor 8.0.0
- Z.Blazor.Diagrams 3.0.3
- Blazored.LocalStorage 4.5.0

### 4. Hosting Layer

#### Sorcha.AppHost
.NET Aspire orchestration host managing service lifecycle.

**Responsibilities:**
- Service orchestration and discovery
- Configuration management
- Resource allocation
- Developer dashboard
- Health check aggregation

**Configuration:**
```csharp
var blueprintEngine = builder.AddProject<Projects.Sorcha_Blueprint_Engine>("blueprint-engine")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Sorcha_Blueprint_Designer>("blueprint-designer")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(blueprintEngine)
    .WaitFor(blueprintEngine);
```

**Technology:**
- .NET Aspire 9.5.2
- Service discovery
- Health checks

#### Sorcha.ServiceDefaults
Shared service configurations and cross-cutting concerns.

**Extension Methods:**
- `AddServiceDefaults<TBuilder>()` - Complete service setup
- `ConfigureOpenTelemetry<TBuilder>()` - Observability setup
- `AddDefaultHealthChecks<TBuilder>()` - Health/liveness checks
- `MapDefaultEndpoints(WebApplication)` - Health endpoints

**Features:**
- OpenTelemetry integration (logs, metrics, traces)
- Health check endpoints (`/health`, `/alive`)
- Service discovery configuration
- Resilience patterns (retry, circuit breaker, timeout)
- HTTP client defaults with resilience

**Technology:**
- OpenTelemetry 1.12.0
- Microsoft.Extensions.Http.Resilience 9.9.0
- Microsoft.Extensions.ServiceDiscovery 9.5.2

### 5. Services Layer (Planned)

#### Sorcha.Peer.Service
Peer-to-peer networking service for decentralized transaction distribution.

**Status:** Design Phase - See [Peer Service Design](peer-service-design.md) and [Implementation Plan](peer-service-implementation-plan.md)

**Purpose:**
Enable decentralized, peer-to-peer communication and transaction distribution across a network of Sorcha nodes without reliance on centralized infrastructure.

**Key Components:**
- **Peer Discovery Service** - Bootstrap connection, recursive discovery, health monitoring
- **Communication Manager** - Protocol negotiation (gRPC Stream → gRPC → REST), connection pooling
- **Transaction Distributor** - Gossip protocol for efficient distribution, bandwidth optimization
- **Network Address Discovery** - STUN/TURN for NAT traversal, external address detection
- **Offline/Online Mode** - Transaction queuing, automatic flush when connectivity restored

**Features:**
- Multi-protocol support (gRPC streaming preferred, REST fallback)
- Gossip-based transaction distribution (O(log N) complexity)
- Automatic peer discovery and health monitoring
- NAT traversal with STUN/TURN
- Offline transaction queuing
- Bloom filter for duplicate detection
- Streaming support for large transactions
- Bandwidth optimization with compression
- Real-time metrics and monitoring dashboard

**Communication Flow:**
```
New Transaction → Local Queue → Gossip Protocol → Select Peers (fanout) →
Notify Peers (hash only) → Peers Request Full Transaction (if unknown) →
Peers Repeat Gossip → 90% Network Coverage in < 1 minute
```

**Planned Configuration:**
```json
{
  "PeerService": {
    "Enabled": true,
    "BootstrapNodes": ["https://peer.sorcha.org:5001"],
    "RefreshIntervalMinutes": 15,
    "GossipProtocol": {
      "FanoutFactor": 3,
      "GossipRounds": 3
    }
  }
}
```

**Technology:**
- gRPC with bidirectional streaming
- Protocol Buffers for efficient serialization
- SQLite for peer list persistence
- STUN protocol for NAT traversal
- Bloom filter for duplicate detection
- Circuit breaker pattern for resilience

**Implementation Timeline:** 20 weeks (10 sprints) - See [Implementation Plan](peer-service-implementation-plan.md)

**Benefits:**
- Decentralization - No single point of failure
- Scalability - Distributed load across all peers
- Resilience - Automatic failover and recovery
- Bandwidth Efficiency - Only transmit to subset of peers
- Privacy - Direct peer-to-peer communication

**Use Cases:**
- Distribute new blueprint transactions across the network
- Synchronize transaction history between nodes
- Enable offline-first operation with automatic sync
- Build decentralized blueprint registry
- Support multi-region deployment without centralized coordination

## Testing Architecture

Sorcha follows a comprehensive testing strategy with multiple levels of testing:

### Test Projects Structure

```
tests/
├── Sorcha.Blueprint.Models.Tests      # Unit tests for domain models
├── Sorcha.Blueprint.Fluent.Tests      # Unit tests for fluent builders
├── Sorcha.Blueprint.Schemas.Tests     # Unit tests for schema management
├── Sorcha.Blueprint.Engine.Tests      # Unit tests for API endpoints
├── Sorcha.Blueprint.Designer.Tests    # Component tests for Blazor UI
└── Sorcha.Integration.Tests           # End-to-end integration tests
```

### Testing Layers

**1. Unit Tests**
- Test individual classes and methods in isolation
- Mock external dependencies
- Fast execution (< 100ms per test)
- High code coverage target (80%+)
- Technologies: xUnit, Moq, FluentAssertions

**2. Component Tests**
- Test Blazor components in isolation
- Verify UI rendering and interactions
- Test client-side logic
- Technologies: bUnit, xUnit

**3. Integration Tests**
- Test service-to-service communication
- Test Aspire orchestration
- Test database operations (when implemented)
- Test API endpoints end-to-end
- Technologies: xUnit, WebApplicationFactory, Testcontainers

**4. Contract Tests**
- Verify API contracts match OpenAPI specs
- Ensure backward compatibility
- Validate request/response schemas

### Test Naming Convention

```csharp
// Pattern: MethodName_Scenario_ExpectedBehavior
[Fact]
public void Build_WithoutTitle_ThrowsInvalidOperationException() { }

[Fact]
public void Build_WithValidData_ReturnsBlueprint() { }

[Theory]
[InlineData("", "short")]
[InlineData("a", "short")]
public void WithTitle_TooShort_ThrowsException(string title, string reason) { }
```

### CI/CD Testing Phases

```
Build Pipeline:
1. Restore dependencies
2. Build solution
3. Run unit tests (parallel)
4. Run component tests
5. Run integration tests
6. Collect code coverage
7. Generate coverage reports
8. Upload to Codecov
```

## Design Principles

### 1. Cloud-Native

- Containerized deployments
- Horizontal scalability
- Service discovery
- Resilience patterns (retry, circuit breaker, timeout)
- Configuration as code
- Stateless service design

### 2. Observability-First

- Distributed tracing with OpenTelemetry
- Structured logging with correlation IDs
- Health checks (`/health`, `/alive`)
- Metrics collection (request rate, duration, errors)
- Real-time monitoring via Aspire dashboard

### 3. API-First

- RESTful APIs using minimal API pattern
- OpenAPI/Swagger documentation
- Versioned APIs (planned)
- Standard HTTP status codes
- JSON request/response
- Consistent error responses (ProblemDetails)

### 4. Modular Architecture

- Loose coupling between services
- Clear separation of concerns (Common/Core/Apps)
- Extensible plugin system (planned)
- Shared abstractions via interfaces
- Dependency injection throughout

### 5. Domain-Driven Design

- Rich domain models with behavior
- Ubiquitous language (Blueprint, Action, Participant, Disclosure)
- Value objects for immutable data
- Fluent builders for complex construction
- Validation at domain boundaries

### 6. Security by Design

- Input validation at all entry points
- JSON Schema validation for blueprints
- DataAnnotations for model validation
- Planned: Authentication and authorization
- Planned: Encryption at rest and in transit
- Audit logging for compliance

## Data Flow

### Blueprint Execution Flow

```
1. User creates/edits blueprint in Designer
2. Designer validates blueprint schema
3. User triggers execution via API
4. Engine validates blueprint
5. Engine creates execution context
6. Engine executes actions in order
7. Engine reports status updates
8. Designer displays real-time progress
9. Engine completes and returns results
```

### Monitoring Flow

```
1. Service emits telemetry
2. OpenTelemetry collector receives data
3. Data exported to monitoring backend
4. Dashboards visualize metrics/traces
5. Alerts triggered on anomalies
```

## Technology Stack

### Runtime
- .NET 10 (10.0.100)
- C# 13
- ASP.NET Core

### Frameworks
- .NET Aspire for orchestration
- Minimal APIs for REST endpoints
- Blazor Server for UI
- Entity Framework Core (planned)

### Observability
- OpenTelemetry
- Application Insights (optional)
- Prometheus (optional)
- Grafana (optional)

### Storage
- SQL database for metadata (planned)
- Blob storage for blueprints (planned)
- Cache for performance (planned)

### Message Queue
- Azure Service Bus (optional)
- RabbitMQ (optional)
- For async processing and event-driven patterns

## Deployment Models

### Development
- Local development with Aspire
- In-process services
- File-based storage

### Production - Single Instance
- Docker containers
- Reverse proxy (nginx/Kestrel)
- External database
- Centralized logging

### Production - Distributed
- Kubernetes cluster
- Service mesh (optional)
- Managed databases
- External monitoring
- Load balancing
- Auto-scaling

## Security Architecture

### Authentication & Authorization
- JWT token-based auth (planned)
- Role-based access control (planned)
- API key support (planned)

### Data Protection
- Encryption at rest (planned)
- Encryption in transit (TLS)
- Secrets management
- Audit logging (planned)

### Network Security
- HTTPS enforcement
- CORS configuration
- Rate limiting (planned)
- IP filtering (optional)

## Scalability Considerations

### Horizontal Scaling
- Stateless service design
- External state management
- Load balancing
- Service discovery

### Performance
- Async/await throughout
- Connection pooling
- Response caching
- Background processing

### Reliability
- Health checks
- Graceful degradation
- Circuit breakers
- Retry policies
- Bulkhead isolation

## Future Architecture Plans

- [ ] Distributed execution across multiple nodes
- [ ] Plugin system for custom actions
- [ ] Multi-tenancy support
- [ ] Advanced scheduling and cron support
- [ ] Workflow versioning and rollback
- [ ] GraphQL API (in addition to REST)
- [ ] Real-time collaboration in Designer
- [ ] Blueprint marketplace

## Related Documentation

- [Execution Model](execution-model.md)
- [Blueprint Schema](blueprint-schema.md)
- [API Reference](api-reference.md)
- [Deployment Guide](deployment.md)
