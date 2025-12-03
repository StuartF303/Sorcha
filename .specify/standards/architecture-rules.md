# Architecture Rules

**Version:** 1.1.0
**Status:** MANDATORY
**Audience:** All developers and AI assistants

---

## Overview

This document defines the architectural rules and patterns that MUST be followed in the Sorcha project. These rules ensure system consistency, maintainability, scalability, and reliability.

---

## 1. Layered Architecture

### Layer Structure (Bottom-Up)

```
┌─────────────────────────────────────────────────────┐
│  Presentation Layer                                  │
│  - Blazor Server UI (Designer)                      │
│  - Blazor WASM Client                               │
│  - Scalar API Documentation UI                      │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│  API Gateway Layer (YARP Reverse Proxy)             │
│  - Request routing                                   │
│  - Service discovery (via .NET Aspire)              │
│  - OpenAPI aggregation                              │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│  Application Services Layer                          │
│  - Blueprint.Api (REST API)                          │
│  - Peer.Service (gRPC P2P)                          │
│  - SignalR Hubs                                      │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│  Core/Domain Layer                                   │
│  - Blueprint.Engine (execution logic)                │
│  - Blueprint.Fluent (builder API)                    │
│  - Blueprint.Schemas (validation)                    │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│  Common Layer                                        │
│  - Blueprint.Models (domain models)                  │
│  - ServiceDefaults (shared configs)                  │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│  Infrastructure Layer                                │
│  - AppHost (Aspire orchestration)                    │
│  - Redis (caching)                                   │
│  - OpenTelemetry (observability)                     │
└─────────────────────────────────────────────────────┘
```

### Rules

1. **Dependency Direction**: Dependencies flow downward only (no upward dependencies)
2. **No Layer Skipping**: Each layer may only depend on the layer immediately below
3. **Core Independence**: Core/Domain layer MUST NOT depend on Application or Infrastructure layers
4. **Common Sharing**: Common layer provides shared types across all layers

### Violations

```csharp
// WRONG: Core depending on Application layer
namespace Sorcha.Blueprint.Engine;
using Sorcha.Blueprint.Api; // VIOLATION

// WRONG: Common depending on Core
namespace Sorcha.Blueprint.Models;
using Sorcha.Blueprint.Engine; // VIOLATION
```

### Correct

```csharp
// CORRECT: Application depending on Core
namespace Sorcha.Blueprint.Api;
using Sorcha.Blueprint.Engine;
using Sorcha.Blueprint.Models;

// CORRECT: Core depending on Common
namespace Sorcha.Blueprint.Engine;
using Sorcha.Blueprint.Models;
```

---

## 2. Microservices Architecture

### Service Boundaries

Each service MUST be:
- **Independently Deployable**: Can be deployed without affecting other services
- **Single Responsibility**: Owns one business capability
- **Autonomous**: Has its own data and logic
- **Observable**: Exports metrics, traces, and logs

### Service Catalog

| Service | Protocol | Responsibility |
|---------|----------|----------------|
| **AppHost** | HTTP | .NET Aspire orchestration & service discovery |
| **Blueprint.Service** | HTTP/REST | Blueprint CRUD, publishing, version control |
| **Peer.Service** | gRPC | P2P peer registration, transaction streaming |
| **ApiGateway** | HTTP/REST, YARP | Reverse proxy, OpenAPI aggregation |
| **Blueprint.Designer.Client** | Blazor WASM | Client-side Blazor application |

### Rules

1. **Service Discovery**
   - MUST use .NET Aspire service discovery
   - NO hardcoded URLs or ports in production code
   - Use named services: `httpClient.GetFromJsonAsync("http://blueprint-api/...")`

2. **Inter-Service Communication**
   - REST for synchronous request/response
   - gRPC for high-performance RPC
   - Event-driven for asynchronous workflows (future)

3. **Data Ownership**
   - Each service owns its data
   - NO shared databases between services
   - Data sharing via APIs only

4. **Health Checks**
   ```csharp
   // REQUIRED in every service
   app.MapHealthChecks("/health");
   app.MapHealthChecks("/alive");
   ```

5. **Resilience Patterns**
   ```csharp
   builder.Services.AddHttpClient("blueprint-api")
       .AddStandardResilienceHandler(); // REQUIRED
   ```

---

## 3. API Design Rules

### REST API Standards

#### HTTP Methods
- **GET**: Read operations (idempotent, cacheable)
- **POST**: Create new resources
- **PUT**: Full resource replacement
- **PATCH**: Partial resource update
- **DELETE**: Resource deletion

#### Status Codes
- **200 OK**: Successful GET/PUT/PATCH
- **201 Created**: Successful POST
- **204 No Content**: Successful DELETE
- **400 Bad Request**: Invalid input
- **401 Unauthorized**: Authentication required
- **403 Forbidden**: Insufficient permissions
- **404 Not Found**: Resource not found
- **422 Unprocessable Entity**: Validation failed
- **500 Internal Server Error**: Server error

#### Endpoint Naming
```csharp
// CORRECT: RESTful resource naming
GET    /api/blueprints              // List all
GET    /api/blueprints/{id}         // Get one
POST   /api/blueprints              // Create
PUT    /api/blueprints/{id}         // Full update
DELETE /api/blueprints/{id}         // Delete

// WRONG: Verb-based URLs
GET    /api/getBlueprints
POST   /api/createBlueprint
```

### OpenAPI/Scalar Documentation

1. **OpenAPI Specification REQUIRED**
   - ALL services exposing HTTP REST APIs MUST generate OpenAPI specifications
   - OpenAPI spec MUST be exposed at `/openapi/v1.json` endpoint

2. **Scalar UI Documentation REQUIRED**
   - ALL API services MUST provide Scalar API documentation UI
   - Use Scalar (NOT Swagger UI or Redoc)

3. **Implementation**
   ```csharp
   builder.Services.AddOpenApi(options =>
   {
       options.AddDocumentTransformer((document, context, cancellationToken) =>
       {
           document.Info = new()
           {
               Title = "Blueprint Service API",
               Version = "v1",
               Description = "Blueprint CRUD operations"
           };
           return Task.CompletedTask;
       });
   });

   app.MapOpenApi();
   app.MapScalarApiReference();
   ```

---

## 4. Domain-Driven Design (DDD) Patterns

### Bounded Contexts

Each Core project represents a bounded context:
- **Blueprint.Models**: Core domain models (shared kernel)
- **Blueprint.Engine**: Blueprint execution context
- **Blueprint.Schemas**: Schema management context
- **Blueprint.Fluent**: Blueprint construction context

### Aggregates

```csharp
// Blueprint is an Aggregate Root
public class Blueprint
{
    public required string Id { get; init; }
    public required string Title { get; set; }

    private readonly List<Action> _actions = new();
    public IReadOnlyList<Action> Actions => _actions.AsReadOnly();

    public void AddAction(Action action)
    {
        ValidateAction(action);
        _actions.Add(action);
    }
}
```

### Value Objects

```csharp
// Immutable value object
public record Participant
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Organization { get; init; }
}
```

### Repository Pattern

```csharp
public interface IBlueprintRepository
{
    Task<Blueprint?> GetByIdAsync(string id);
    Task<IEnumerable<Blueprint>> GetAllAsync();
    Task<Blueprint> AddAsync(Blueprint blueprint);
    Task UpdateAsync(Blueprint blueprint);
    Task DeleteAsync(string id);
}
```

---

## 5. Dependency Injection

### Service Lifetimes

```csharp
// Transient: Created each time (stateless services)
builder.Services.AddTransient<IBlueprintValidator, BlueprintValidator>();

// Scoped: One per HTTP request (repositories, DB contexts)
builder.Services.AddScoped<IBlueprintRepository, BlueprintRepository>();

// Singleton: Single instance (caches, configurations)
builder.Services.AddSingleton<ISchemaCache, SchemaCache>();
```

### Rules

1. **Constructor Injection ONLY**: No property or method injection
2. **Interface Dependencies**: Depend on abstractions, not implementations
3. **Explicit Registration**: No auto-registration magic

```csharp
// CORRECT
public class BlueprintService
{
    private readonly IBlueprintRepository _repository;

    public BlueprintService(IBlueprintRepository repository)
    {
        _repository = repository;
    }
}

// WRONG: Service Locator anti-pattern
public class BlueprintService
{
    public void DoSomething(IServiceProvider provider)
    {
        var repo = provider.GetService<IBlueprintRepository>(); // VIOLATION
    }
}
```

---

## 6. Observability Requirements

### OpenTelemetry Integration

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation())
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .UseOtlpExporter();
```

### Structured Logging

```csharp
// CORRECT: Structured logging
_logger.LogInformation(
    "Blueprint {BlueprintId} executed by {ParticipantId}",
    blueprintId,
    participantId);

// WRONG: String interpolation
_logger.LogInformation($"Blueprint {blueprintId} executed"); // VIOLATION
```

---

## 7. Error Handling

### Exception Hierarchy

```csharp
public class DomainException : Exception { }
public class BlueprintValidationException : DomainException { }
public class ExecutionException : DomainException { }

public class InfrastructureException : Exception { }
public class ServiceUnavailableException : InfrastructureException { }
```

### Global Exception Handler

```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features
            .Get<IExceptionHandlerFeature>()?.Error;

        var problemDetails = exception switch
        {
            ValidationException => new ProblemDetails
            {
                Status = 422,
                Title = "Validation failed"
            },
            DomainException => new ProblemDetails
            {
                Status = 400,
                Title = "Business rule violation"
            },
            _ => new ProblemDetails
            {
                Status = 500,
                Title = "An error occurred"
            }
        };

        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});
```

---

## 8. Caching Strategy

### Distributed Caching (Redis)

```csharp
builder.AddRedisOutputCache("redis");
app.UseOutputCache();

app.MapGet("/api/blueprints", async (IBlueprintStore store) =>
{
    return await store.GetAllAsync();
})
.CacheOutput(policy => policy
    .Expire(TimeSpan.FromMinutes(5))
    .Tag("blueprints"));
```

---

## 9. Configuration Management

### Strongly-typed Configuration

```csharp
public class BlueprintSettings
{
    public int MaxActionsPerBlueprint { get; set; } = 100;
    public int ExecutionTimeoutSeconds { get; set; } = 300;
}

builder.Services.Configure<BlueprintSettings>(
    builder.Configuration.GetSection("Blueprint"));

// WRONG: Magic strings
var timeout = builder.Configuration["Blueprint:ExecutionTimeoutSeconds"]; // VIOLATION
```

---

## 10. Anti-Patterns to Avoid

### Service Locator
```csharp
// WRONG
var service = serviceProvider.GetService<IMyService>();
```

### Anemic Domain Model
```csharp
// WRONG: Just getters/setters
public class Blueprint
{
    public string Id { get; set; }
    public List<Action> Actions { get; set; }
}
```

### Leaky Abstractions
```csharp
// WRONG: Repository returning DbContext types
Task<DbSet<Blueprint>> GetBlueprintsDbSet(); // VIOLATION
```

### God Objects
```csharp
// WRONG: Single service doing everything
public class BlueprintManager
{
    void Validate() { }
    void Execute() { }
    void Store() { }
    void SendNotifications() { }
} // VIOLATION: Too many responsibilities
```

---

## Compliance Checklist

Before committing code:

- [ ] Follows layered architecture (no upward dependencies)
- [ ] Service is independently deployable
- [ ] Health checks implemented
- [ ] OpenTelemetry integrated
- [ ] APIs follow REST standards
- [ ] OpenAPI specification exposed at `/openapi/v1.json`
- [ ] Scalar UI configured for API documentation
- [ ] All inputs validated
- [ ] Errors handled globally
- [ ] Configuration strongly-typed
- [ ] No architectural anti-patterns

---

## References

- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
