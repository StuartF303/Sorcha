# Sorcha Architecture

## Overview

Sorcha is built on a modern, cloud-native architecture using .NET 10 and .NET Aspire. The system is designed for scalability, reliability, and observability.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Sorcha Platform                          │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────────┐         ┌─────────────────────┐       │
│  │  Blueprint       │         │  Blueprint          │       │
│  │  Designer        │────────▶│  Engine             │       │
│  │  (Web UI)        │         │  (Execution)        │       │
│  └──────────────────┘         └─────────────────────┘       │
│          │                             │                     │
│          │                             │                     │
│          ▼                             ▼                     │
│  ┌──────────────────────────────────────────────┐          │
│  │         Service Defaults                     │          │
│  │  (OpenTelemetry, Health, Discovery)         │          │
│  └──────────────────────────────────────────────┘          │
│                                                               │
└─────────────────────────────────────────────────────────────┘
         │                                    │
         ▼                                    ▼
┌─────────────────┐                 ┌─────────────────┐
│  Storage        │                 │  Message Queue  │
│  (Database)     │                 │  (Optional)     │
└─────────────────┘                 └─────────────────┘
```

## Core Components

### 1. Sorcha.AppHost

The .NET Aspire orchestration host that manages the lifecycle of all services.

**Responsibilities:**
- Service orchestration and discovery
- Configuration management
- Resource allocation
- Developer dashboard

**Technology:**
- .NET Aspire 9.5.2
- Service discovery
- Health checks

### 2. Sorcha.ServiceDefaults

Shared service configurations and cross-cutting concerns.

**Features:**
- OpenTelemetry integration
- Health check endpoints
- Service discovery configuration
- Resilience patterns (retry, circuit breaker)
- Logging and tracing

**Technology:**
- OpenTelemetry for observability
- Microsoft.Extensions.Http.Resilience for resilience
- Serilog for structured logging

### 3. Sorcha.Blueprint.Engine

The core execution engine for running blueprints.

**Responsibilities:**
- Blueprint validation
- Execution orchestration
- State management
- Action execution
- Error handling and retry logic

**API Endpoints:**
- `POST /blueprints/execute` - Execute a blueprint
- `GET /blueprints/{id}/status` - Get execution status
- `POST /blueprints/validate` - Validate a blueprint
- `GET /health` - Health check endpoint

**Technology:**
- ASP.NET Core Minimal APIs
- Dependency Injection
- Background services for long-running tasks

### 4. Sorcha.Blueprint.Designer

Web-based visual designer for creating and managing blueprints.

**Features:**
- Visual blueprint editor
- Blueprint validation
- Real-time execution monitoring
- Blueprint templates
- Version control

**Technology:**
- Blazor Server
- Interactive UI components
- SignalR for real-time updates

## Design Principles

### 1. Cloud-Native

- Containerized deployments
- Horizontal scalability
- Service discovery
- Resilience patterns
- Configuration as code

### 2. Observability-First

- Distributed tracing with OpenTelemetry
- Structured logging
- Health checks
- Metrics collection
- Real-time monitoring

### 3. API-First

- RESTful APIs using minimal API pattern
- OpenAPI/Swagger documentation
- Versioned APIs
- Standard HTTP status codes
- JSON request/response

### 4. Modular Architecture

- Loose coupling between services
- Clear separation of concerns
- Extensible plugin system
- Shared abstractions

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
- .NET 10 (RC2)
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
