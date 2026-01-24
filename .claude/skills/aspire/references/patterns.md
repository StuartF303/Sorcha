# Aspire Patterns Reference

## Contents
- AppHost Configuration
- Service Defaults
- Resource Patterns
- Service Discovery
- Environment Configuration
- Anti-Patterns

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