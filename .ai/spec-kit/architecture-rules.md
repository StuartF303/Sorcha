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
│  - CORS (currently permissive)                      │
│  - ⚠️ Authentication/Authorization NOT YET IMPL     │
│  - ⚠️ Rate limiting NOT YET IMPLEMENTED             │
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

### ✅ RULES
1. **Dependency Direction**: Dependencies flow downward only (no upward dependencies)
2. **No Layer Skipping**: Each layer may only depend on the layer immediately below
3. **Core Independence**: Core/Domain layer MUST NOT depend on Application or Infrastructure layers
4. **Common Sharing**: Common layer provides shared types across all layers

### ❌ VIOLATIONS
```csharp
// ❌ WRONG: Core depending on Application layer
namespace Sorcha.Blueprint.Engine;
using Sorcha.Blueprint.Api; // VIOLATION

// ❌ WRONG: Common depending on Core
namespace Sorcha.Blueprint.Models;
using Sorcha.Blueprint.Engine; // VIOLATION

// ❌ WRONG: Skipping layers
namespace Sorcha.Blueprint.Designer;
using Sorcha.Blueprint.Engine; // Should go through API layer
```

### ✅ CORRECT
```csharp
// ✅ CORRECT: Application depending on Core
namespace Sorcha.Blueprint.Api;
using Sorcha.Blueprint.Engine;
using Sorcha.Blueprint.Models;

// ✅ CORRECT: Core depending on Common
namespace Sorcha.Blueprint.Engine;
using Sorcha.Blueprint.Models;

// ✅ CORRECT: UI calling through API Gateway
namespace Sorcha.Blueprint.Designer;
using HttpClient; // Call via API, not direct Core dependency
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

| Service | Port(s) | Protocol | Responsibility | Storage | Location |
|---------|---------|----------|----------------|---------|----------|
| **AppHost** | 15000-15100 | HTTP | .NET Aspire orchestration & service discovery | N/A | `src/Apps/Sorcha.AppHost/` |
| **Blueprint.Service** | 8050, 8051 | HTTP/REST | Blueprint CRUD, publishing, version control | In-memory (InMemoryBlueprintStore) | `src/Services/Sorcha.Blueprint.Service/` |
| **Peer.Service** | 8070, 8071 | gRPC | P2P peer registration, transaction streaming, metrics | In-memory (InMemoryPeerRepository) | `src/Services/Sorcha.Peer.Service/` |
| **ApiGateway** | 8060, 8061 | HTTP/REST, YARP | Reverse proxy, OpenAPI aggregation, health aggregation | Stateless | `src/Services/Sorcha.ApiGateway/` |
| **Blueprint.Designer.Client** | 8080, 8081 | HTTP/Blazor WASM | Client-side Blazor application | Blazored.LocalStorage | `src/Apps/UI/Sorcha.Blueprint.Designer.Client/` |
| **Redis** | 6379 | Redis Protocol | Distributed output caching | Persistent | External dependency |

### ✅ RULES

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
   - MUST implement retry policies
   - MUST implement circuit breakers
   - MUST implement timeout policies
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
- **409 Conflict**: State conflict
- **422 Unprocessable Entity**: Validation failed
- **500 Internal Server Error**: Server error

#### Endpoint Naming
```csharp
// ✅ CORRECT: RESTful resource naming
GET    /api/blueprints              // List all
GET    /api/blueprints/{id}         // Get one
POST   /api/blueprints              // Create
PUT    /api/blueprints/{id}         // Full update
PATCH  /api/blueprints/{id}         // Partial update
DELETE /api/blueprints/{id}         // Delete

GET    /api/blueprints/{id}/actions // Nested resources

// ❌ WRONG: Verb-based URLs
GET    /api/getBlueprints
POST   /api/createBlueprint
GET    /api/blueprints/getById/{id}
```

### OpenAPI/Scalar Documentation

#### ✅ MANDATORY RULES for All API Services

1. **OpenAPI Specification REQUIRED**
   - ALL services exposing HTTP REST APIs MUST generate OpenAPI specifications
   - OpenAPI spec MUST be exposed at `/openapi/v1.json` endpoint
   - Specification MUST include all endpoints, schemas, and response types

2. **Scalar UI Documentation REQUIRED**
   - ALL API services MUST provide Scalar API documentation UI
   - Scalar UI MUST be accessible in development environments
   - Use Scalar (NOT Swagger UI or Redoc)

3. **Service Identification**
   - Each service MUST have a unique, descriptive API title
   - API title MUST match service name (e.g., "Blueprint Service API", "Peer Service API")
   - Version information MUST be included in the OpenAPI document

4. **API Gateway Aggregation**
   - ApiGateway MUST aggregate OpenAPI specs from ALL backend services
   - Combined spec MUST present a complete, unified API surface
   - Aggregated spec MUST be accessible via `/openapi/v1.json` at gateway level
   - Scalar UI at gateway MUST display all available endpoints across services

#### Implementation

```csharp
// REQUIRED in ALL API services
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// REQUIRED: OpenAPI generation
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // REQUIRED: Set service-specific metadata
        document.Info = new()
        {
            Title = "Blueprint Service API",  // MUST be unique per service
            Version = "v1",
            Description = "Blueprint CRUD, publishing, and version control operations",
            Contact = new()
            {
                Name = "Sorcha Development Team"
            }
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// REQUIRED: Expose OpenAPI spec
app.MapOpenApi();  // Exposes /openapi/v1.json (always, not just dev)

// REQUIRED: Scalar API documentation UI
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Blueprint Service API")  // MUST match OpenAPI title
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.Run();
```

#### API Gateway OpenAPI Aggregation

```csharp
// REQUIRED in ApiGateway service
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer(async (document, context, cancellationToken) =>
    {
        // REQUIRED: Set gateway metadata
        document.Info = new()
        {
            Title = "Sorcha API Gateway",
            Version = "v1",
            Description = "Unified API surface for all Sorcha services"
        };

        // REQUIRED: Aggregate OpenAPI specs from backend services
        var httpClient = context.RequestServices.GetRequiredService<IHttpClientFactory>()
            .CreateClient();

        var services = new[]
        {
            ("http://blueprint-service", "/openapi/v1.json"),
            ("http://peer-service", "/openapi/v1.json")
            // Add all backend services here
        };

        foreach (var (serviceUrl, specPath) in services)
        {
            try
            {
                var spec = await httpClient.GetFromJsonAsync<OpenApiDocument>(
                    $"{serviceUrl}{specPath}", cancellationToken);

                if (spec?.Paths != null)
                {
                    // Merge paths from backend service
                    foreach (var path in spec.Paths)
                    {
                        document.Paths.Add(path.Key, path.Value);
                    }
                }

                if (spec?.Components?.Schemas != null)
                {
                    // Merge schemas from backend service
                    foreach (var schema in spec.Components.Schemas)
                    {
                        document.Components ??= new();
                        document.Components.Schemas ??= new Dictionary<string, OpenApiSchema>();
                        document.Components.Schemas.TryAdd(schema.Key, schema.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail - allow partial API surface
                context.RequestServices.GetRequiredService<ILogger<Program>>()
                    .LogWarning(ex, "Failed to fetch OpenAPI spec from {Service}", serviceUrl);
            }
        }

        return Task.CompletedTask;
    });
});

var app = builder.Build();

// REQUIRED: Expose aggregated OpenAPI spec
app.MapOpenApi();

// REQUIRED: Scalar UI showing complete API surface
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Sorcha Complete API")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}
```

#### Endpoint Documentation Standards

```csharp
// REQUIRED: Document all endpoints with summaries and descriptions
app.MapGet("/api/blueprints/{id}", async (string id, IBlueprintStore store) =>
{
    var blueprint = await store.GetByIdAsync(id);
    return blueprint is not null ? Results.Ok(blueprint) : Results.NotFound();
})
.WithName("GetBlueprint")
.WithSummary("Get a blueprint by ID")
.WithDescription("Retrieves a specific blueprint by its unique identifier")
.WithOpenApi(operation =>
{
    operation.Parameters[0].Description = "The unique identifier of the blueprint";
    return operation;
})
.Produces<Blueprint>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.WithTags("Blueprints");
```

#### Validation Checklist

Before deploying any API service:

- [ ] OpenAPI spec is generated and accessible at `/openapi/v1.json`
- [ ] Scalar UI is configured and accessible in development
- [ ] API title is unique and descriptive
- [ ] All endpoints have summaries and descriptions
- [ ] All response types are documented with `.Produces<T>()`
- [ ] All parameters have descriptions
- [ ] All endpoints are tagged appropriately
- [ ] ApiGateway successfully aggregates the service's OpenAPI spec
- [ ] Complete API surface is visible through gateway's Scalar UI

**Note**: Sorcha uses **Scalar** (modern API documentation UI), not Swagger UI

### API Versioning

```csharp
// REQUIRED for public APIs
GET /api/v1/blueprints
GET /api/v2/blueprints

// Version in URL path (preferred)
// OR version in Accept header
Accept: application/vnd.sorcha.v1+json
```

### Request Validation

```csharp
// REQUIRED: Validate all inputs
app.MapPost("/api/blueprints", async (
    BlueprintRequest request,
    IValidator<BlueprintRequest> validator,
    IBlueprintService service) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }

    var blueprint = await service.CreateAsync(request);
    return Results.Created($"/api/blueprints/{blueprint.Id}", blueprint);
});
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
// ✅ Blueprint is an Aggregate Root
public class Blueprint
{
    public required string Id { get; init; } // Aggregate ID
    public required string Title { get; set; }

    // Collection managed by aggregate root
    private readonly List<Action> _actions = new();
    public IReadOnlyList<Action> Actions => _actions.AsReadOnly();

    // ONLY aggregate root can modify children
    public void AddAction(Action action)
    {
        ValidateAction(action);
        _actions.Add(action);
    }

    // Business rule enforcement
    private void ValidateAction(Action action)
    {
        if (_actions.Any(a => a.Index == action.Index))
            throw new DomainException("Duplicate action index");
    }
}
```

### Value Objects

```csharp
// ✅ Immutable value object
public record Participant
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Organization { get; init; }
    public string? Did { get; init; }

    // Value equality by structure
}
```

### Domain Services

```csharp
// Core domain logic that doesn't belong to a single entity
public interface IBlueprintExecutionService
{
    Task<ExecutionResult> ExecuteAsync(
        Blueprint blueprint,
        ExecutionContext context);
}
```

### Repository Pattern

```csharp
// ✅ Abstract data access
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

### ✅ RULES
1. **Constructor Injection ONLY**: No property or method injection
2. **Interface Dependencies**: Depend on abstractions, not implementations
3. **Explicit Registration**: No auto-registration magic
4. **Validate Services**: Use `builder.Services.AddHealthChecks()` to validate DI

```csharp
// ✅ CORRECT
public class BlueprintService
{
    private readonly IBlueprintRepository _repository;
    private readonly IValidator<Blueprint> _validator;

    public BlueprintService(
        IBlueprintRepository repository,
        IValidator<Blueprint> validator)
    {
        _repository = repository;
        _validator = validator;
    }
}

// ❌ WRONG: Service Locator anti-pattern
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
// REQUIRED in every service
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
// ✅ CORRECT: Structured logging
_logger.LogInformation(
    "Blueprint {BlueprintId} executed by {ParticipantId}",
    blueprintId,
    participantId);

// ❌ WRONG: String interpolation
_logger.LogInformation($"Blueprint {blueprintId} executed"); // VIOLATION
```

### Custom Metrics

```csharp
// REQUIRED for business metrics
var meter = new Meter("Sorcha.Blueprint.Engine");
var executionCounter = meter.CreateCounter<long>("blueprint_executions_total");
var executionDuration = meter.CreateHistogram<double>("blueprint_execution_duration_ms");

executionCounter.Add(1, new KeyValuePair<string, object?>("status", "success"));
```

### Activity/Span Tracing

```csharp
// REQUIRED for operation tracking
using var activity = Activity.Current?.Source.StartActivity("ExecuteBlueprint");
activity?.SetTag("blueprint.id", blueprintId);
activity?.SetTag("participant.id", participantId);
```

---

## 7. Error Handling

### Exception Hierarchy

```csharp
// Domain exceptions
public class DomainException : Exception { }
public class BlueprintValidationException : DomainException { }
public class ExecutionException : DomainException { }

// Infrastructure exceptions
public class InfrastructureException : Exception { }
public class ServiceUnavailableException : InfrastructureException { }
```

### Global Exception Handler

```csharp
// REQUIRED in all APIs
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

### Distributed Caching (Redis + Output Caching)

```csharp
// REQUIRED: Use Redis for distributed output caching
builder.AddRedisOutputCache("redis");  // Aspire.StackExchange.Redis.OutputCaching

app.UseOutputCache();  // Enable output caching middleware

// Cache with tags for selective invalidation
var blueprintGroup = app.MapGroup("/api/blueprints");

blueprintGroup.MapGet("/", async (IBlueprintStore store) =>
{
    return await store.GetAllAsync();
})
.CacheOutput(policy => policy
    .Expire(TimeSpan.FromMinutes(5))
    .Tag("blueprints"));  // Tag for cache invalidation

blueprintGroup.MapGet("/{id}", async (string id, IBlueprintStore store) =>
{
    var blueprint = await store.GetByIdAsync(id);
    return blueprint is not null ? Results.Ok(blueprint) : Results.NotFound();
})
.CacheOutput(policy => policy
    .Expire(TimeSpan.FromMinutes(5))
    .Tag("blueprints"));

// Cache invalidation on mutation
blueprintGroup.MapPost("/", async (Blueprint blueprint, IBlueprintStore store, IOutputCacheStore cache) =>
{
    await store.AddAsync(blueprint);
    await cache.EvictByTagAsync("blueprints", CancellationToken.None);  // Invalidate cache
    return Results.Created($"/api/blueprints/{blueprint.Id}", blueprint);
});

// Published blueprints (immutable, long TTL)
blueprintGroup.MapGet("/{id}/versions/{version}", async (string id, int version, IPublishedBlueprintStore store) =>
{
    var published = await store.GetVersionAsync(id, version);
    return published is not null ? Results.Ok(published) : Results.NotFound();
})
.CacheOutput(policy => policy
    .Expire(TimeSpan.FromDays(365))  // Long cache for immutable versions
    .Tag("published"));
```

### Local Caching

```csharp
// In-memory for hot paths
builder.Services.AddMemoryCache();

var cache = serviceProvider.GetRequiredService<IMemoryCache>();
var schema = await cache.GetOrCreateAsync($"schema:{id}", async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
    return await schemaRepo.GetByIdAsync(id);
});
```

---

## 9. Configuration Management

### Configuration Sources (Priority Order)

1. Command-line arguments
2. Environment variables
3. User secrets (development only)
4. appsettings.{Environment}.json
5. appsettings.json

### ✅ RULES

```csharp
// ✅ CORRECT: Strongly-typed configuration
public class BlueprintSettings
{
    public int MaxActionsPerBlueprint { get; set; } = 100;
    public int ExecutionTimeoutSeconds { get; set; } = 300;
}

builder.Services.Configure<BlueprintSettings>(
    builder.Configuration.GetSection("Blueprint"));

// ❌ WRONG: Magic strings
var timeout = builder.Configuration["Blueprint:ExecutionTimeoutSeconds"]; // VIOLATION
```

### Secrets Management

```csharp
// Development
builder.Configuration.AddUserSecrets<Program>();

// Production
// MUST use Azure Key Vault or environment variables
// NEVER commit secrets to source control
```

---

## 10. Deployment Architecture

### Container Requirements

```dockerfile
# REQUIRED: Multi-stage builds
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release --no-restore
RUN dotnet publish -c Release --no-build -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Sorcha.Blueprint.Api.dll"]
```

### Azure Container Apps

All services MUST:
- Use managed identity (no passwords)
- Implement health checks
- Support horizontal scaling
- Export logs to Azure Monitor
- Use Azure Key Vault for secrets

---

## 11. Architectural Decision Records (ADRs)

### When Required

Create ADR for:
- Choosing technology or framework
- Changing architectural pattern
- Adding new layer or service
- Changing communication pattern

### Location

`docs/adr/YYYYMMDD-title.md`

### Template

```markdown
# ADR-001: Use .NET Aspire for Service Orchestration

## Status
Accepted

## Context
We need to orchestrate multiple microservices...

## Decision
Use .NET Aspire for local development and Azure Container Apps for production.

## Consequences
- Simplified local development
- Built-in observability
- Dependency on Microsoft ecosystem
```

---

## 12. Anti-Patterns to Avoid

### ❌ Service Locator
```csharp
// WRONG
var service = serviceProvider.GetService<IMyService>();
```

### ❌ Anemic Domain Model
```csharp
// WRONG: Just getters/setters
public class Blueprint
{
    public string Id { get; set; }
    public List<Action> Actions { get; set; }
}

// CORRECT: Rich domain model
public class Blueprint
{
    private readonly List<Action> _actions = new();
    public void AddAction(Action action)
    {
        ValidateAction(action);
        _actions.Add(action);
    }
}
```

### ❌ Leaky Abstractions
```csharp
// WRONG: Repository returning DbContext types
Task<DbSet<Blueprint>> GetBlueprintsDbSet(); // VIOLATION

// CORRECT: Repository returning domain models
Task<IEnumerable<Blueprint>> GetBlueprintsAsync();
```

### ❌ God Objects
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

Before committing code, verify:

- [ ] Follows layered architecture (no upward dependencies)
- [ ] Service is independently deployable
- [ ] Health checks implemented
- [ ] OpenTelemetry integrated
- [ ] APIs follow REST standards
- [ ] **OpenAPI specification generated and exposed at `/openapi/v1.json`**
- [ ] **Scalar UI configured for API documentation**
- [ ] **All endpoints documented with summaries and descriptions**
- [ ] **API Gateway aggregates service OpenAPI specs (if applicable)**
- [ ] All inputs validated
- [ ] Errors handled globally
- [ ] Configuration strongly-typed
- [ ] Caching strategy applied
- [ ] No architectural anti-patterns

---

## References

- [Spec-Kit Main](./spec-kit.md)
- [Architecture Documentation](../../docs/architecture.md)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
