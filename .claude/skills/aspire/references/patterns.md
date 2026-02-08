# Aspire 13.x Patterns Reference

## Contents
- SDK Format (v13 Migration)
- AppHost Configuration
- Service Defaults
- Resource Patterns
- Named References (v13+)
- Connection Properties (v13+)
- TLS Termination (v13.1+)
- Service Discovery
- Environment Configuration
- MCP Dashboard Integration (v13+)
- Container File Artifacts (v13+)
- Anti-Patterns

---

## SDK Format (v13 Migration)

### Current Sorcha Format (Legacy Dual-SDK)

```xml
<!-- src/Apps/Sorcha.AppHost/Sorcha.AppHost.csproj — CURRENT -->
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="Aspire.AppHost.Sdk" Version="13.0.0" />
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.AppHost" Version="13.1.0" />
  </ItemGroup>
</Project>
```

### New v13 Single-SDK Format

```xml
<!-- Simplified single-SDK format (optional migration) -->
<Project Sdk="Aspire.AppHost.Sdk/13.0.0">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <!-- Aspire.Hosting.AppHost is implicit in the SDK -->
  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.MongoDB" Version="13.1.0" />
    <PackageReference Include="Aspire.Hosting.PostgreSQL" Version="13.1.0" />
    <PackageReference Include="Aspire.Hosting.Redis" Version="13.1.0" />
  </ItemGroup>
</Project>
```

**Migration Notes:**
- The `Aspire.Hosting.AppHost` package reference becomes implicit in the SDK
- Both formats work — no forced migration required
- The dual-SDK format is still supported and widely used
- Migration command: `aspire update` can handle this automatically

### Single-File AppHosts (v13+)

```csharp
// AppHost.cs — no .csproj needed with #:sdk directives
#:sdk Aspire.AppHost.Sdk/13.0.0
#:package Aspire.Hosting.Redis@13.1.0

var builder = DistributedApplication.CreateBuilder(args);
var redis = builder.AddRedis("redis");
builder.Build().Run();
```

**Note:** Single-file format is for prototyping. Sorcha uses standard project format.

---

## AppHost Configuration

### Basic Service Registration

```csharp
// src/Apps/Sorcha.AppHost/AppHost.cs
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure first
var postgres = builder.AddPostgres("postgres").WithPgAdmin();
var mongodb = builder.AddMongoDB("mongodb").WithMongoExpress();
var redis = builder.AddRedis("redis").WithRedisCommander();

// Databases from infrastructure
var tenantDb = postgres.AddDatabase("tenant-db", "sorcha_tenant");
var walletDb = postgres.AddDatabase("wallet-db", "sorcha_wallet");
var registerDb = mongodb.AddDatabase("register-db", "sorcha_register");

// Services with dependencies
var tenantService = builder.AddProject<Projects.Sorcha_Tenant_Service>("tenant-service")
    .WithReference(tenantDb)
    .WithReference(redis);

builder.Build().Run();
```

### Shared Configuration Pattern

```csharp
// Generate shared JWT key once, distribute to all services
var jwtSigningKey = GetOrCreateJwtSigningKey();

var tenantService = builder.AddProject<Projects.Sorcha_Tenant_Service>("tenant-service")
    .WithEnvironment("JwtSettings__SigningKey", jwtSigningKey)
    .WithEnvironment("JwtSettings__Issuer", "https://localhost:7110")
    .WithEnvironment("JwtSettings__Audience__0", "https://sorcha.local");

var blueprintService = builder.AddProject<Projects.Sorcha_Blueprint_Service>("blueprint-service")
    .WithEnvironment("JwtSettings__SigningKey", jwtSigningKey)  // Same key
    .WithEnvironment("JwtSettings__Issuer", "https://localhost:7110");
```

---

## Service Defaults

### Standard Service Setup

```csharp
// src/Common/Sorcha.ServiceDefaults/Extensions.cs
public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
    where TBuilder : IHostApplicationBuilder
{
    builder.ConfigureOpenTelemetry();
    builder.AddDefaultHealthChecks();
    builder.Services.AddServiceDiscovery();

    builder.Services.ConfigureHttpClientDefaults(http =>
    {
        http.AddStandardResilienceHandler();  // Polly resilience
        http.AddServiceDiscovery();           // DNS-based discovery
    });

    return builder;
}
```

### OpenTelemetry Configuration

```csharp
// Tracing excludes health endpoints to reduce noise
tracing.AddAspNetCoreInstrumentation(tracing =>
    tracing.Filter = context =>
        !context.Request.Path.StartsWithSegments("/health")
        && !context.Request.Path.StartsWithSegments("/alive")
);

// Custom meters for specific services
metrics.AddMeter("Sorcha.Peer.Service");
tracing.AddSource("Sorcha.Peer.Service");
```

### Sorcha ServiceDefaults Extras

```csharp
// SecurityHeaders, HTTPS enforcement, rate limiting, input validation
app.UseApiSecurityHeaders();            // OWASP headers (SEC-004)
app.UseHttpsEnforcement();              // HSTS + redirect (SEC-001)
app.UseRateLimiting();                  // Rate limiter middleware (SEC-002)
app.UseInputValidation();               // Input sanitization (SEC-003)
```

**Rate Limit Policies (defined in ServiceDefaults):**
| Policy | Limit | Type |
|--------|-------|------|
| `RateLimitPolicies.Api` | 100/min per IP | Fixed window |
| `RateLimitPolicies.Authentication` | 10/min per IP | Sliding window |
| `RateLimitPolicies.Strict` | 5/min per IP | Token bucket |
| `RateLimitPolicies.HeavyOperations` | 10 concurrent global | Concurrency |
| `RateLimitPolicies.Relaxed` | 1000/min per IP | Fixed window |

---

## Resource Patterns

### DO: Use Typed Resource Methods

```csharp
// GOOD - Type-safe resource configuration
builder.AddRedisOutputCache("redis");       // For IOutputCacheStore
builder.AddRedisDistributedCache("redis");  // For IDistributedCache
builder.AddRedisClient("redis");            // For IConnectionMultiplexer
```

### DON'T: Hardcode Connection Strings

```csharp
// BAD - Bypasses service discovery
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";  // Hardcoded!
});

// GOOD - Uses Aspire resource name
builder.AddRedisDistributedCache("redis");
```

**Why:** Hardcoded strings break when running in Docker, Kubernetes, or different environments. Aspire injects the correct connection string automatically.

---

## Named References (v13+)

### Custom Connection String Names

```csharp
// Default: ConnectionStrings__register-db
var registerService = builder.AddProject<Projects.Sorcha_Register_Service>("register-service")
    .WithReference(registerDb);

// Named: ConnectionStrings__primary-store
var registerService = builder.AddProject<Projects.Sorcha_Register_Service>("register-service")
    .WithReference(registerDb, "primary-store");
```

### Use Case: Multiple Databases of Same Type

```csharp
var readDb = postgres.AddDatabase("read-db", "sorcha_read");
var writeDb = postgres.AddDatabase("write-db", "sorcha_write");

var myService = builder.AddProject<Projects.MyService>("my-service")
    .WithReference(readDb, "read-connection")     // ConnectionStrings__read-connection
    .WithReference(writeDb, "write-connection");   // ConnectionStrings__write-connection
```

```csharp
// In the service Program.cs
builder.Services.AddDbContext<ReadDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("read-connection")));
builder.Services.AddDbContext<WriteDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("write-connection")));
```

---

## Connection Properties (v13+)

### Polyglot Connection Access

```csharp
// v13+ exposes individual connection properties for non-.NET consumers
var postgres = builder.AddPostgres("postgres");

// Access individual fields (useful for polyglot services)
var host = postgres.GetConnectionProperty("HostName");
var port = postgres.GetConnectionProperty("Port");
var jdbc = postgres.GetConnectionProperty("JdbcConnectionString");
```

### Non-.NET Service Configuration

```csharp
// Python/JavaScript services get simple environment variables
var pythonApp = builder.AddPythonApp("analytics", "../python-analytics")
    .WithReference(postgres);
// Automatically receives: DB_HOST, DB_PORT, DB_USERNAME, DB_PASSWORD
// Instead of complex connection string formats
```

---

## TLS Termination (v13.1+)

### Container HTTPS Certificates

```csharp
// Auto-generate and trust development HTTPS certificates
var redis = builder.AddRedis("redis")
    .WithHttpsCertificate();

var gateway = builder.AddProject<Projects.Sorcha_ApiGateway>("api-gateway")
    .WithHttpsCertificate();
```

### Custom Certificate Configuration

```csharp
// Use a specific certificate file
var myService = builder.AddProject<Projects.MyService>("my-service")
    .WithHttpsCertificate(certPath: "/certs/service.pfx", certPassword: "password");
```

### Supported Resources
Built-in TLS support for: YARP, Redis, Keycloak, Uvicorn, Vite containers.

---

## Service Discovery

### Explicit Service References

```csharp
// API Gateway references all services it routes to
var apiGateway = builder.AddProject<Projects.Sorcha_ApiGateway>("api-gateway")
    .WithReference(tenantService)
    .WithReference(blueprintService)
    .WithReference(walletService)
    .WithReference(registerService)
    .WithReference(peerService)
    .WithReference(validatorService)
    .WithReference(redis)
    .WithExternalHttpEndpoints();
```

### External Endpoints

```csharp
// Mark services that need external access
var registerService = builder.AddProject<Projects.Sorcha_Register_Service>("register-service")
    .WithExternalHttpEndpoints();  // For walkthrough testing

var uiWeb = builder.AddProject<Projects.Sorcha_UI_Web>("ui-web")
    .WithExternalHttpEndpoints();  // Primary user entry point
```

### Network Identifiers (v13+)

```csharp
// Resolve endpoints contextually for host vs container networking
var endpoint = myService.GetEndpoint("https");
// Uses KnownNetworkIdentifiers internally to route correctly
```

---

## Environment Configuration

### Configuration Hierarchy

1. **Environment variables** (highest priority) - set via `.WithEnvironment()`
2. **AppHost injected** - connection strings from `WithReference()`
3. **appsettings.json** (lowest priority)

### Array Configuration

```csharp
// Single audience
.WithEnvironment("JwtSettings__Audience", "https://sorcha.local")

// Multiple audiences (use index)
.WithEnvironment("JwtSettings__Audience__0", "https://sorcha.local")
.WithEnvironment("JwtSettings__Audience__1", "https://api.sorcha.io")
```

---

## MCP Dashboard Integration (v13+)

### Dashboard MCP Endpoint

The Aspire dashboard exposes an MCP (Model Context Protocol) endpoint that AI assistants can use to:
- Query resource status and health
- Access structured logs and traces
- Inspect service configuration
- View telemetry data

### Setting Up MCP

```bash
# Initialize MCP configuration for AI assistants
aspire mcp init

# This creates configuration in ~/.aspire/globalsettings.json
# AI assistants can then connect to the MCP endpoint
```

### Sorcha MCP Server

Sorcha also has its own MCP server at `src/Apps/Sorcha.McpServer/` that provides:
- Blueprint CRUD operations
- Register query capabilities
- Wallet management
- Authenticated via JWT

These are complementary: Aspire MCP provides infrastructure visibility, Sorcha MCP provides domain operations.

---

## Container File Artifacts (v13+)

### Frontend-in-Backend Pattern

```csharp
// Extract build outputs from one container into another
var frontend = builder.AddNpmApp("frontend", "../frontend")
    .PublishWithContainerFiles("/app/dist");

var backend = builder.AddProject<Projects.Backend>("backend")
    .WithContainerFiles(frontend, "/app/wwwroot");
```

**Use Case:** Build a Vite/React frontend in a Node container and copy the static output into a .NET backend container for serving — eliminating a separate frontend container in production.

---

## Anti-Patterns

### WARNING: Missing AddServiceDefaults

**The Problem:**

```csharp
// BAD - Service without Aspire integration
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
var app = builder.Build();
app.Run();
```

**Why This Breaks:**
1. No health endpoints - Aspire can't monitor service
2. No OpenTelemetry - invisible in dashboard
3. No service discovery - HttpClients can't find other services
4. No resilience handlers - failures cascade

**The Fix:**

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();  // First line after builder creation
// ... rest of configuration
var app = builder.Build();
app.MapDefaultEndpoints();     // Before app.Run()
```

### WARNING: Forgetting MapDefaultEndpoints

**The Problem:**

```csharp
var app = builder.Build();
app.UseRouting();
app.MapControllers();
app.Run();  // No health endpoints!
```

**Why This Breaks:**
1. Aspire dashboard shows service as unhealthy
2. Kubernetes readiness probes fail
3. Load balancers can't health check

**The Fix:**

```csharp
var app = builder.Build();
app.MapDefaultEndpoints();  // Add health endpoints
app.UseRouting();
app.MapControllers();
app.Run();
```

### WARNING: Circular References

**The Problem:**

```csharp
// BAD - Creates startup deadlock
var serviceA = builder.AddProject<Projects.ServiceA>("a")
    .WithReference(serviceB);
var serviceB = builder.AddProject<Projects.ServiceB>("b")
    .WithReference(serviceA);
```

**Why This Breaks:**
1. Neither service can start - waiting for the other
2. Aspire dashboard shows both as unhealthy
3. Must manually kill and restructure

**The Fix:**

```csharp
// Use async communication or shared infrastructure
var serviceA = builder.AddProject<Projects.ServiceA>("a")
    .WithReference(redis);  // Communicate via Redis
var serviceB = builder.AddProject<Projects.ServiceB>("b")
    .WithReference(redis);
```

### WARNING: Mismatched Resource Names

**The Problem:**

```csharp
// In AppHost
var redis = builder.AddRedis("redis-cache");

// In service Program.cs
builder.AddRedisDistributedCache("redis");  // Wrong name!
```

**Why This Breaks:**
1. Connection string injection fails
2. Runtime error: "No service for type 'IDistributedCache'"
3. Works locally with fallback, fails in production

**The Fix:**

Use consistent names. The name in `AddRedis()` must match the name in consuming methods.

### WARNING: Using Old AddNpmApp (v13 Breaking)

**The Problem:**

```csharp
// BAD - Removed in v13
var frontend = builder.AddNpmApp("frontend", "../frontend");
```

**The Fix:**

```csharp
// GOOD - v13 replacement
var frontend = builder.AddJavaScriptApp("frontend", "../frontend");
// Or for Vite specifically:
var frontend = builder.AddViteApp("frontend", "../frontend");
```

### WARNING: DataProtection Key Permissions in Docker

**Known Issue:** When running in Docker, the DataProtection key file at `/home/app/.aspnet/DataProtection-Keys/` can have permission errors due to volume sharing between containers.

**Symptoms:**
```
System.UnauthorizedAccessException: Access to the path
'/home/app/.aspnet/DataProtection-Keys/key-*.xml' is denied.
```

**Impact:** Non-blocking — services fall back to ephemeral keys. Auth works but tokens don't survive container restarts.

**Fix Options:**
1. Configure DataProtection to use Redis: `builder.Services.AddDataProtection().PersistKeysToStackExchangeRedis()`
2. Set proper volume permissions in Dockerfile
3. Use a shared volume with correct ownership
