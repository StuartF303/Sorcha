# Blueprint API Implementation Plan

## Architecture Overview

### Technology Stack
- **.NET 10** with **Minimal APIs**
- **Aspire 9.5** for orchestration
- **Redis** for distributed caching and output caching
- **Scalar** for OpenAPI documentation (better than Swagger)
- **Docker** Linux containers
- **OpenAPI 3.1** specification

### Project Structure
```
src/
├── Apps/
│   ├── Orchestration/
│   │   └── Sorcha.AppHost/           # Aspire orchestration
│   └── Services/
│       └── Sorcha.Blueprint.Api/      # Blueprint REST API
├── Common/
│   ├── Sorcha.Blueprint.Models/       # Shared models
│   └── Sorcha.ServiceDefaults/        # Aspire service defaults
└── Core/
    └── Sorcha.Blueprint.Schemas/      # Schema library
```

## API Endpoints

### Blueprint CRUD Operations

#### GET /api/blueprints
- List all blueprints (with pagination)
- Query params: `page`, `pageSize`, `search`, `status`
- Response: `PagedResult<BlueprintSummary>`
- Cache: Output cache (5 minutes)

#### GET /api/blueprints/{id}
- Get blueprint by ID
- Response: `Blueprint`
- Cache: Output cache (5 minutes)

#### POST /api/blueprints
- Create new blueprint
- Request: `CreateBlueprintRequest`
- Response: `Blueprint`
- Invalidates: Blueprint list cache

#### PUT /api/blueprints/{id}
- Update existing blueprint
- Request: `UpdateBlueprintRequest`
- Response: `Blueprint`
- Invalidates: Blueprint caches

#### DELETE /api/blueprints/{id}
- Delete blueprint (soft delete)
- Response: `204 No Content`
- Invalidates: Blueprint caches

### Blueprint Publishing

#### POST /api/blueprints/{id}/publish
- Publish blueprint to make it available
- Validates blueprint structure
- Creates immutable published version
- Response: `PublishedBlueprint`
- Invalidates: Blueprint + published caches

#### GET /api/blueprints/{id}/versions
- Get all published versions
- Response: `List<PublishedBlueprint>`
- Cache: Output cache (10 minutes)

#### GET /api/blueprints/{id}/versions/{version}
- Get specific published version
- Response: `PublishedBlueprint`
- Cache: Output cache (permanent - immutable)

### Schema Operations

#### GET /api/schemas
- List available schemas
- Query params: `category`, `source`, `search`
- Response: `List<SchemaDocument>`
- Cache: Output cache (15 minutes)

#### GET /api/schemas/{id}
- Get schema by ID
- Response: `SchemaDocument`
- Cache: Output cache (30 minutes)

#### POST /api/schemas/validate
- Validate data against schema
- Request: `ValidateSchemaRequest`
- Response: `ValidationResult`

## Storage Strategy

### In-Memory Storage (for now)
- `ConcurrentDictionary<string, Blueprint>` for blueprints
- `ConcurrentDictionary<string, PublishedBlueprint>` for published versions
- Later: Replace with Entity Framework + PostgreSQL/SQL Server

### Redis Caching Strategy
1. **Output Caching**: Cache HTTP responses
   - GET requests cached at edge
   - Automatic cache invalidation on mutations

2. **Distributed Cache**: Cross-instance data
   - Schema cache (shared across instances)
   - Published blueprints (immutable, long cache)

## OpenAPI Documentation

### Scalar Configuration
- Interactive API explorer at `/scalar/v1`
- Better UX than Swagger
- Dark mode support
- Request/response examples
- Authentication testing

### Documentation Features
- Full XML comments on all endpoints
- Request/response examples
- Error response documentation
- Authentication schemes
- Rate limiting info

## Aspire Integration

### AppHost Configuration
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Redis for caching
var redis = builder.AddRedis("redis")
    .WithRedisCommander();

// Blueprint API
var api = builder.AddProject<Projects.Sorcha_Blueprint_Api>("blueprint-api")
    .WithReference(redis)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

### Service Defaults
- Health checks (Redis, API)
- Telemetry (OpenTelemetry)
- Resilience (Polly)
- Service discovery

## Docker Support

### Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY . .
RUN dotnet restore "Sorcha.Blueprint.Api.csproj"
RUN dotnet build "Sorcha.Blueprint.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sorcha.Blueprint.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sorcha.Blueprint.Api.dll"]
```

## Security Considerations

### Authentication (Future)
- JWT Bearer tokens
- API Keys
- OAuth 2.0 / OpenID Connect

### Authorization (Future)
- Role-based access control
- Blueprint ownership
- Publish permissions

### Rate Limiting
- Per-IP rate limits
- Per-user rate limits (when auth added)
- Configured in Aspire

## Validation

### Blueprint Validation Rules
1. Must have at least 2 participants
2. Must have at least 1 action
3. All action references must be valid
4. All participant references must exist
5. All schema references must be valid
6. No circular action dependencies
7. All required fields present

### Schema Validation
- JSON Schema Draft 7 validation
- Schema registry integration
- Schema versioning

## Monitoring & Observability

### OpenTelemetry Integration
- Traces: HTTP requests, cache hits/misses
- Metrics: Request rates, cache efficiency, error rates
- Logs: Structured logging with Serilog

### Health Checks
- `/health`: Basic health
- `/health/ready`: Readiness probe
- `/health/live`: Liveness probe

## Next Steps

1. ✅ Create Aspire projects
2. ⏳ Implement Program.cs with all endpoints
3. ⏳ Add Blueprint storage service
4. ⏳ Implement publish mechanism
5. ⏳ Configure Redis caching
6. ⏳ Add OpenAPI documentation
7. ⏳ Configure AppHost
8. ⏳ Add Dockerfile
9. ⏳ Test all endpoints
10. ⏳ Add comprehensive error handling
