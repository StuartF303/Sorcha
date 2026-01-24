# Scalar Workflows Reference

## Contents
- Adding Scalar to a New Service
- Documenting Endpoints
- Setting Up OpenAPI Aggregation
- Troubleshooting

---

## Adding Scalar to a New Service

### Checklist

Copy this checklist and track progress:
- [ ] Step 1: Add NuGet package reference
- [ ] Step 2: Configure OpenAPI services
- [ ] Step 3: Map OpenAPI endpoint
- [ ] Step 4: Configure Scalar (development only)
- [ ] Step 5: Test at `/scalar`

### Step-by-Step Implementation

**Step 1: Package Reference**

The `Scalar.AspNetCore` package is included via `Sorcha.ServiceDefaults`. If creating a standalone service:

```xml
<PackageReference Include="Scalar.AspNetCore" Version="2.*" />
```

**Step 2: Configure Services**

```csharp
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Option A: Minimal (most services)
builder.Services.AddOpenApi();

// Option B: With document transformer (rich docs)
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info.Title = "My Service API";
        document.Info.Version = "1.0.0";
        return Task.CompletedTask;
    });
});
```

**Step 3: Map OpenAPI Endpoint**

```csharp
var app = builder.Build();
app.MapOpenApi();  // Exposes /openapi/v1.json
```

**Step 4: Configure Scalar**

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("My Service")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}
```

**Step 5: Verify**

```bash
# Start service
dotnet run --project src/Services/Sorcha.MyService

# Verify endpoints
curl http://localhost:5000/openapi/v1.json  # Should return JSON
# Open http://localhost:5000/scalar in browser
```

**Iterate-until-pass:**
1. Start service
2. Navigate to `/scalar`
3. If 404, check `MapScalarApiReference()` is called after `MapOpenApi()`
4. If theme wrong, verify `ScalarTheme.Purple`
5. Only proceed when Scalar UI loads correctly

---

## Documenting Endpoints

### Minimal API Documentation Workflow

**Step 1: Create Endpoint Group**

```csharp
var walletsGroup = app.MapGroup("/api/wallets")
    .WithTags("Wallets")
    .RequireAuthorization("CanManageWallets");
```

**Step 2: Document Each Endpoint**

```csharp
walletsGroup.MapGet("/", handler)
    .WithName("ListWallets")
    .WithSummary("List wallets for current user")
    .WithDescription("Retrieve all wallets owned by the current user in the current tenant");

walletsGroup.MapPost("/", handler)
    .WithName("CreateWallet")
    .WithSummary("Create a new wallet")
    .WithDescription("Creates a new HD wallet with the specified algorithm and returns the mnemonic phrase for backup");
```

**Documentation Method Reference:**

| Method | Purpose | Scalar Display |
|--------|---------|----------------|
| `.WithName("OpId")` | Operation ID | Used in code generation |
| `.WithSummary("...")` | Short description | Endpoint title in list |
| `.WithDescription("...")` | Detailed docs | Expanded documentation |
| `.WithTags("Tag")` | Group endpoints | Sidebar organization |
| `.ExcludeFromDescription()` | Hide from Scalar | Internal endpoints |

### Documentation Validation Checklist

- [ ] Every public endpoint has `.WithName()`
- [ ] Every endpoint has `.WithSummary()`
- [ ] Complex endpoints have `.WithDescription()` with markdown
- [ ] Endpoints are grouped with `.WithTags()`
- [ ] Internal endpoints use `.ExcludeFromDescription()`

---

## Setting Up OpenAPI Aggregation

For API Gateways that need to aggregate multiple service specs:

### Step 1: Create Aggregation Service

```csharp
public class OpenApiAggregationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Dictionary<string, ServiceOpenApiConfig> _serviceConfigs;

    public OpenApiAggregationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _serviceConfigs = new Dictionary<string, ServiceOpenApiConfig>
        {
            { "blueprint", new ServiceOpenApiConfig
                {
                    Name = "Blueprint API",
                    BaseUrl = configuration["Services:Blueprint:Url"] 
                              ?? "http://blueprint-api",
                    OpenApiPath = "/openapi/v1.json",
                    PathPrefix = "/api/blueprint"
                }
            }
            // Add other services...
        };
    }

    public async Task<JsonObject> GetAggregatedOpenApiAsync()
    {
        var aggregated = CreateBaseDocument();
        
        foreach (var (_, config) in _serviceConfigs)
        {
            var serviceSpec = await FetchServiceOpenApiAsync(config);
            if (serviceSpec != null)
            {
                MergeServiceOpenApi(serviceSpec, aggregated, config);
            }
        }
        
        return aggregated;
    }
}
```

### Step 2: Register and Configure

```csharp
builder.Services.AddSingleton<OpenApiAggregationService>();

app.MapGet("/openapi/aggregated.json", async (OpenApiAggregationService service) =>
{
    var spec = await service.GetAggregatedOpenApiAsync();
    return Results.Json(spec);
})
.ExcludeFromDescription();

app.MapScalarApiReference(options =>
{
    options
        .WithTitle("API Gateway - All Services")
        .WithOpenApiRoutePattern("/openapi/aggregated.json");
});
```

---

## Troubleshooting

### Scalar UI Not Loading

**Symptom:** 404 at `/scalar`

**Check:**
1. `MapScalarApiReference()` called in development environment?
2. Called AFTER `app.Build()`?
3. Is `ASPNETCORE_ENVIRONMENT=Development`?

**Fix:**
```bash
# Set environment
export ASPNETCORE_ENVIRONMENT=Development
dotnet run
```

### OpenAPI JSON Returns Empty

**Symptom:** `/openapi/v1.json` returns `{}`

**Check:**
1. `AddOpenApi()` called in service registration?
2. `MapOpenApi()` called in app configuration?
3. Are endpoints defined before `app.Run()`?

**Fix:**
```csharp
// Ensure correct order
builder.Services.AddOpenApi();  // Step 1
var app = builder.Build();
app.MapOpenApi();  // Step 2
// ... map endpoints ...
app.Run();  // Last
```

### CSP Blocking Scalar Assets

**Symptom:** Scalar loads but UI broken, console shows CSP errors

**Check:** Security headers middleware may be blocking inline scripts.

**Fix (from ServiceDefaults):**
```csharp
// Relax CSP for /scalar path
if (path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase))
{
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' blob:; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data: https://fonts.gstatic.com; " +
        "connect-src 'self' ws: wss:;";
}
```

### Aggregation Missing Services

**Symptom:** Gateway Scalar only shows some services

**Check:**
1. Service running and healthy?
2. Service OpenAPI endpoint accessible?
3. Configuration correct in aggregation service?

**Debug:**
```bash
# Test each service's OpenAPI endpoint
curl http://localhost:5000/openapi/v1.json  # Blueprint
curl http://localhost:5290/openapi/v1.json  # Register
curl http://localhost:7001/openapi/v1.json  # Wallet
```

**Iterate-until-pass:**
1. Start all services
2. Verify each service's `/openapi/v1.json` returns valid JSON
3. Check aggregation service logs for fetch errors
4. Refresh gateway's `/scalar`
5. Repeat until all services appear