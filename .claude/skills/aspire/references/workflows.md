# Aspire 13.x Workflows Reference

## Contents
- Adding a New Service
- Aspire CLI Commands (v13+)
- MCP Setup for AI Assistants (v13+)
- Debugging with Aspire
- Running Without Aspire (Docker Compose)
- Testing with Aspire.Hosting.Testing
- Deployment Preparation
- Pipeline System (v13+ Experimental)
- Troubleshooting

---

## Adding a New Service

### Workflow Checklist

Copy this checklist and track progress:
- [ ] Create service project with Aspire reference
- [ ] Add `AddServiceDefaults()` in Program.cs
- [ ] Add `MapDefaultEndpoints()` after build
- [ ] Register in AppHost with dependencies
- [ ] Configure JWT if service needs auth
- [ ] Test health endpoints respond

### Step 1: Create Service Project

```bash
dotnet new webapi -n Sorcha.MyService -o src/Services/Sorcha.MyService
dotnet add src/Services/Sorcha.MyService reference src/Common/Sorcha.ServiceDefaults
```

### Step 2: Configure Program.cs

```csharp
// src/Services/Sorcha.MyService/Program.cs
var builder = WebApplication.CreateBuilder(args);

// CRITICAL: Add service defaults first
builder.AddServiceDefaults();

// Add any Aspire resources by name (must match AppHost)
builder.AddRedisDistributedCache("redis");

// Add services, authentication, etc.
builder.Services.AddOpenApi();

var app = builder.Build();

// CRITICAL: Map health endpoints
app.MapDefaultEndpoints();

app.MapOpenApi();
app.Run();
```

### Step 3: Register in AppHost

```csharp
// src/Apps/Sorcha.AppHost/AppHost.cs
var myService = builder.AddProject<Projects.Sorcha_MyService>("my-service")
    .WithReference(redis)
    .WithEnvironment("JwtSettings__SigningKey", jwtSigningKey);
```

### Step 4: Verify Health

```bash
# Start Aspire
dotnet run --project src/Apps/Sorcha.AppHost

# Check service health
curl http://localhost:7xxx/health
curl http://localhost:7xxx/alive
```

---

## Aspire CLI Commands (v13+)

### Installation and Updates

```bash
# Install Aspire CLI (if not installed)
dotnet tool install -g aspire

# Update the CLI itself
aspire update --self

# Check installed version
aspire --version
```

### Solution Initialization

```bash
# Interactive initialization — discovers projects, configures service defaults
aspire init

# This will:
# 1. Scan for .NET projects
# 2. Create/update AppHost if needed
# 3. Add ServiceDefaults references
# 4. Configure NuGet packages
```

### Package Updates

```bash
# Update all Aspire packages in the solution
aspire update

# Preview what would change
aspire update --dry-run

# Update to specific version
aspire update --version 13.1.0
```

### Channel Selection

```bash
# Set preferred release channel (persisted in ~/.aspire/globalsettings.json)
aspire config set channel stable    # Stable releases
aspire config set channel preview   # Preview releases
```

### Running the AppHost

```bash
# Start with Aspire orchestration
dotnet run --project src/Apps/Sorcha.AppHost

# Dashboard: http://localhost:18888
# Services: HTTPS ports 7000-7290
```

---

## MCP Setup for AI Assistants (v13+)

### Aspire Dashboard MCP

```bash
# Configure MCP endpoints for AI coding agents
aspire mcp init

# This creates ~/.aspire/globalsettings.json with MCP configuration
# AI assistants can then query:
# - Resource status and health
# - Structured logs and traces
# - Service configuration
# - Telemetry data
```

### Sorcha MCP Server (Complementary)

Sorcha has its own MCP server at `src/Apps/Sorcha.McpServer/`:

```bash
# Run Sorcha MCP server with JWT auth
docker-compose run mcp-server --jwt-token <token>

# Or via environment variable
SORCHA_JWT_TOKEN=<token> docker-compose run mcp-server
```

**Aspire MCP** = infrastructure visibility (containers, health, traces)
**Sorcha MCP** = domain operations (blueprints, registers, wallets)

---

## Debugging with Aspire

### Starting the Dashboard

```bash
# Start all services with Aspire orchestration
dotnet run --project src/Apps/Sorcha.AppHost

# Dashboard available at:
# http://localhost:18888
```

### Dashboard Features (v13.1)

| Tab | Purpose |
|-----|---------|
| Resources | Service status, health, **Parameters (v13.1)** |
| Logs | Structured logs per service |
| Traces | Distributed trace visualization |
| Metrics | OpenTelemetry metrics |

### Viewing Traces

1. Open Aspire Dashboard at `http://localhost:18888`
2. Click "Traces" in sidebar
3. Filter by service name or trace ID
4. Expand spans to see timing

### Attaching Debugger

1. Start AppHost: `dotnet run --project src/Apps/Sorcha.AppHost`
2. In VS Code/Visual Studio: Debug -> Attach to Process
3. Select the service process (e.g., `Sorcha.Blueprint.Service`)
4. Set breakpoints and trigger requests

### Viewing Service Logs

```bash
# In Aspire dashboard: Click service -> Logs tab
# Or via CLI during development:
dotnet run --project src/Services/Sorcha.Blueprint.Service
```

---

## Running Without Aspire (Docker Compose)

### Docker Compose Mode

```bash
# Start infrastructure and services
docker-compose up -d

# Access points:
# - API Gateway:      http://localhost:80
# - Main UI:          http://localhost/app
# - Aspire Dashboard: http://localhost:18888 (standalone container)
```

### Configuration Differences

| Setting | Aspire Mode | Docker Mode |
|---------|-------------|-------------|
| Redis | `builder.AddRedisDistributedCache("redis")` | `appsettings.json` connection string |
| Service URLs | Auto-discovered | Hardcoded in `appsettings.json` |
| JWT Key | Generated by AppHost | Must provide `JWTSETTINGS__SIGNINGKEY` env var |
| Ports | 7000-7290 | 5000-5800 (as configured in docker-compose) |
| Dashboard | Built into Aspire | Standalone container at :18888 |

### Standalone Service Testing

```bash
# Run single service without Aspire
cd src/Services/Sorcha.Blueprint.Service
dotnet run

# Requires local Redis and configuration
# ConnectionStrings__redis=localhost:6379
```

---

## Testing with Aspire.Hosting.Testing

### Integration Test Setup

```csharp
// Used in: Gateway Integration, Wallet Integration, UI E2E tests
// Package: Aspire.Hosting.Testing 13.1.0

public class IntegrationTestFixture : IAsyncLifetime
{
    private DistributedApplication? _app;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Sorcha_AppHost>();

        _app = await builder.BuildAsync();
        await _app.StartAsync();
    }

    public HttpClient CreateHttpClient(string resourceName)
    {
        return _app!.CreateHttpClient(resourceName);
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
            await _app.DisposeAsync();
    }
}
```

### Test Example

```csharp
public class BlueprintServiceIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public BlueprintServiceIntegrationTests(IntegrationTestFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        var client = _fixture.CreateHttpClient("blueprint-service");
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

### Sorcha Test Projects Using Aspire.Hosting.Testing

| Project | Path |
|---------|------|
| Gateway Integration | `tests/Sorcha.Gateway.Integration.Tests` |
| Wallet Integration | `tests/Sorcha.Wallet.Service.IntegrationTests` |
| UI E2E | `tests/Sorcha.UI.E2E.Tests` |

---

## Deployment Preparation

### Production Configuration

```csharp
// Remove development UIs in production AppHost
#if DEBUG
var postgres = builder.AddPostgres("postgres").WithPgAdmin();
var redis = builder.AddRedis("redis").WithRedisCommander();
#else
var postgres = builder.AddPostgres("postgres");
var redis = builder.AddRedis("redis");
#endif
```

### Kubernetes Deployment

Aspire generates Kubernetes manifests:

```bash
# Generate manifests
dotnet aspire publish -o ./k8s-manifests

# Apply to cluster
kubectl apply -f ./k8s-manifests/
```

### Health Check Requirements

```csharp
// Production health checks should verify dependencies
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
    .AddRedis(connectionString)
    .AddNpgSql(connectionString)
    .AddMongoDb(connectionString);
```

---

## Pipeline System (v13+ Experimental)

### aspire do

The `aspire do` command coordinates build, publish, and deployment steps with dependency tracking and parallel execution.

```bash
# Run the default pipeline
aspire do

# Preview pipeline steps
aspire do --dry-run

# Run specific step
aspire do build
aspire do publish
aspire do deploy
```

### Pipeline Features
- **Dependency tracking**: Steps execute in correct order
- **Parallel execution**: Independent steps run concurrently
- **Artifact management**: Build outputs flow between steps
- **Environment targeting**: Deploy to different environments

**Note:** Pipeline system is experimental in v13 and may change in future releases.

---

## Troubleshooting

### Service Shows Unhealthy

**Symptoms:** Aspire dashboard shows red status

**Diagnose:**
```bash
# Check health endpoint directly
curl http://localhost:7000/health

# Check logs
dotnet run --project src/Services/Sorcha.Blueprint.Service 2>&1
```

**Common Causes:**
1. Missing `app.MapDefaultEndpoints()` in Program.cs
2. Database connection failing
3. Redis not available

### Connection String Not Found

**Symptoms:** `InvalidOperationException: No connection string named 'redis'`

**Diagnose:**
1. Verify AppHost has `.WithReference(redis)` for the service
2. Check resource name matches: `AddRedis("redis")` -> `AddRedisDistributedCache("redis")`

**Fix:**
```csharp
// AppHost - ensure reference exists
var myService = builder.AddProject<Projects.MyService>("my-service")
    .WithReference(redis);  // This injects ConnectionStrings__redis
```

### Service Can't Find Another Service

**Symptoms:** `HttpRequestException` when calling other services

**Diagnose:**
```csharp
// Check if service discovery is configured
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddServiceDiscovery();  // Required!
});
```

**Fix:**
1. Ensure `AddServiceDefaults()` is called
2. Add `.WithReference(targetService)` in AppHost
3. Use service name as hostname: `http://wallet-service/api/wallets`

### Startup Race Conditions (Docker)

**Symptoms:** `Connection refused` errors in early logs, then resolves

**Example from Sorcha:** Validator service fails to reach tenant-service during first ~8s because tenant-service is still applying migrations.

**This is normal** — Polly retry policies in `AddStandardResilienceHandler()` handle this automatically. The validator service retries 3 times and succeeds.

**If retries are exhausted:**
1. Increase retry count in resilience handler config
2. Add `depends_on` with health checks in docker-compose
3. Add a startup delay or readiness probe

### Port Already in Use

**Symptoms:** `System.IO.IOException: Failed to bind to address`

**Fix:**
```bash
# Find process using port (Windows)
netstat -ano | findstr :7000

# Kill process or change port in launchSettings.json
```

### JWT Authentication Failing Between Services

**Symptoms:** 401 Unauthorized on service-to-service calls

**Diagnose:**
1. Check all services have same `JwtSettings__SigningKey`
2. Verify `JwtSettings__Issuer` matches token issuer
3. Check `JwtSettings__Audience` includes calling service

**Fix:**
```csharp
// AppHost - ensure consistent JWT config
.WithEnvironment("JwtSettings__SigningKey", jwtSigningKey)  // Same key everywhere
.WithEnvironment("JwtSettings__Issuer", "https://localhost:7110")
.WithEnvironment("JwtSettings__Audience", "https://sorcha.local")
```

### DataProtection Key Errors in Docker

**Symptoms:**
```
System.UnauthorizedAccessException: Access to the path
'/home/app/.aspnet/DataProtection-Keys/key-*.xml' is denied.
```

**Root Cause:** Docker volume sharing creates files with wrong ownership for the `app` user.

**Impact:** Non-blocking. Services fall back to ephemeral keys. Auth works but tokens don't survive container restarts.

**Fix Options:**
1. Persist keys to Redis: `builder.Services.AddDataProtection().PersistKeysToStackExchangeRedis()`
2. Fix volume permissions in Dockerfile: `RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R app:app /home/app/.aspnet`
3. Use a dedicated named volume with correct ownership

### MongoDB Health Check Timeout

**Symptoms:** `docker-compose ps` shows MongoDB as `unhealthy`

**Root Cause:** `mongosh` cold start exceeds the 3s health check timeout.

**Fix:** Increase timeout and add start_period in docker-compose:
```yaml
healthcheck:
  test: ["CMD", "mongosh", "--eval", "db.adminCommand('ping')"]
  interval: 10s
  timeout: 10s        # Increased from 3s
  retries: 5
  start_period: 30s   # Grace period for cold start
```

---

## Iterate-Until-Pass Pattern

For service registration validation:

1. Add service to AppHost
2. Run: `dotnet run --project src/Apps/Sorcha.AppHost`
3. Check dashboard at `http://localhost:18888`
4. If service shows unhealthy:
   - Check logs in dashboard
   - Fix configuration
   - Restart AppHost (Ctrl+C, re-run)
5. Repeat until all services show healthy
