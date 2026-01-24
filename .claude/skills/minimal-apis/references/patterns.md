# Minimal APIs Patterns Reference

## Contents
- Endpoint Organization
- Request/Response DTOs
- Error Handling
- Output Caching
- Authorization Policies
- Anti-Patterns

---

## Endpoint Organization

### Extension Method Pattern

Every service exposes endpoints via extension methods registered in `Program.cs`:

```csharp
// Program.cs - registration
app.MapWalletEndpoints();
app.MapDelegationEndpoints();
app.MapAuthEndpoints();
```

```csharp
// Endpoints/WalletEndpoints.cs - definition
public static class WalletEndpoints
{
    public static IEndpointRouteBuilder MapWalletEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/wallets")
            .WithTags("Wallets")
            .RequireAuthorization("CanManageWallets");

        group.MapPost("/", CreateWallet).WithName("CreateWallet")...;
        group.MapGet("/{address}", GetWallet).WithName("GetWallet")...;

        return app;
    }
}
```

### Route Naming Convention

| HTTP Method | Route Pattern | Example |
|-------------|---------------|---------|
| GET (list) | `/` | `GET /api/v1/wallets` |
| GET (single) | `/{id}` | `GET /api/v1/wallets/{address}` |
| POST (create) | `/` | `POST /api/v1/wallets` |
| PUT (replace) | `/{id}` | `PUT /api/v1/blueprints/{id}` |
| PATCH (update) | `/{id}` | `PATCH /api/v1/wallets/{address}` |
| DELETE | `/{id}` | `DELETE /api/v1/wallets/{address}` |
| POST (action) | `/{id}/{action}` | `POST /api/v1/wallets/{address}/sign` |

---

## Request/Response DTOs

### DO: Use Records for Immutability

```csharp
// Models/CreateWalletRequest.cs
public record CreateWalletRequest
{
    public required string Name { get; init; }
    public string Algorithm { get; init; } = "Ed25519";
    public int WordCount { get; init; } = 24;
    public string? Passphrase { get; init; }
}

// Models/CreateWalletResponse.cs
public record CreateWalletResponse
{
    public required WalletDto Wallet { get; init; }
    public required string[] MnemonicWords { get; init; }
}
```

### DON'T: Use Mutable Classes

```csharp
// BAD - mutable, verbose
public class CreateWalletRequest
{
    public string Name { get; set; }  // Mutable
    public string Algorithm { get; set; }
}
```

**Why:** Records are immutable by default, provide value equality, and generate `ToString()`, `GetHashCode()`, and `Equals()` automatically.

---

## Error Handling

### ProblemDetails for Structured Errors

```csharp
private static async Task<IResult> CreateWallet(
    [FromBody] CreateWalletRequest request,
    WalletManager walletManager,
    ILogger<Program> logger)
{
    try
    {
        var (wallet, mnemonic) = await walletManager.CreateWalletAsync(...);
        return Results.Created($"/api/v1/wallets/{wallet.Address}", response);
    }
    catch (ArgumentException ex)
    {
        logger.LogWarning(ex, "Invalid wallet creation request");
        return Results.BadRequest(new ProblemDetails
        {
            Title = "Invalid Request",
            Detail = ex.Message,
            Status = StatusCodes.Status400BadRequest
        });
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
    {
        return Results.Conflict(new ProblemDetails
        {
            Title = "Wallet Already Exists",
            Detail = ex.Message,
            Status = StatusCodes.Status409Conflict
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to create wallet");
        return Results.Problem(
            title: "Wallet Creation Failed",
            detail: "An error occurred while creating the wallet",
            statusCode: StatusCodes.Status500InternalServerError);
    }
}
```

### ValidationProblem for Input Errors

```csharp
if (string.IsNullOrWhiteSpace(request.Email))
{
    return TypedResults.ValidationProblem(new Dictionary<string, string[]>
    {
        ["email"] = ["Email is required"]
    });
}
```

---

## Output Caching

### Cache Read Endpoints with Tags

```csharp
// Blueprint Service - caching patterns
blueprintGroup.MapGet("/", GetAllBlueprints)
    .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)).Tag("blueprints"));

// Immutable data - cache longer
blueprintGroup.MapGet("/{id}/versions/{version}", GetPublishedVersion)
    .CacheOutput(policy => policy.Expire(TimeSpan.FromDays(365)).Tag("published"));
```

### Invalidate Cache on Mutations

```csharp
blueprintGroup.MapPost("/", async (BlueprintModel blueprint, IBlueprintService service, IOutputCacheStore cache) =>
{
    var created = await service.CreateAsync(blueprint);
    await cache.EvictByTagAsync("blueprints", default);  // Invalidate list cache
    return Results.Created($"/api/blueprints/{created.Id}", created);
});
```

---

## Authorization Policies

### Define Policies in Extensions

```csharp
// Extensions/AuthenticationExtensions.cs
public static IServiceCollection AddWalletAuthorization(this IServiceCollection services)
{
    services.AddAuthorization(options =>
    {
        options.AddPolicy("CanManageWallets", policy =>
            policy.RequireAssertion(context =>
            {
                var hasOrgId = context.User.Claims.Any(c => c.Type == "org_id");
                var isService = context.User.Claims.Any(c => c.Type == "token_type" && c.Value == "service");
                return hasOrgId || isService;
            }));

        options.AddPolicy("RequireService", policy =>
            policy.RequireClaim("token_type", "service"));
    });
    return services;
}
```

### Apply to Groups or Endpoints

```csharp
// Group-level - all endpoints require auth
var walletGroup = app.MapGroup("/api/v1/wallets")
    .RequireAuthorization("CanManageWallets");

// Endpoint-level override
group.MapPost("/login", Login)
    .AllowAnonymous();

// Stricter policy on specific endpoint
group.MapPost("/token/revoke-user", RevokeUserTokens)
    .RequireAuthorization("RequireAdministrator");
```

---

## Anti-Patterns

### WARNING: Business Logic in Endpoints

**The Problem:**

```csharp
// BAD - business logic in endpoint handler
walletGroup.MapPost("/", async (CreateWalletRequest request, IWalletRepository repo) =>
{
    // Validation logic
    if (request.WordCount != 12 && request.WordCount != 24)
        return Results.BadRequest("Invalid word count");

    // Business logic
    var mnemonic = new Mnemonic(Wordlist.English, (WordCount)request.WordCount);
    var seed = mnemonic.DeriveSeed();
    var masterKey = ExtKey.CreateFromSeed(seed);

    // Persistence logic
    var wallet = new Wallet { Address = masterKey.GetPublicKey().GetAddress() };
    await repo.AddAsync(wallet);

    return Results.Created(...);
});
```

**Why This Breaks:**
1. Endpoint handlers become untestable without HTTP infrastructure
2. Business logic scattered across endpoint files
3. Violates Single Responsibility Principle

**The Fix:**

```csharp
// GOOD - delegate to service layer
walletGroup.MapPost("/", async (CreateWalletRequest request, WalletManager walletManager) =>
{
    var (wallet, mnemonic) = await walletManager.CreateWalletAsync(
        request.Name, request.Algorithm, request.WordCount);
    return Results.Created($"/api/v1/wallets/{wallet.Address}", response);
});
```

---

### WARNING: Missing OpenAPI Metadata

**The Problem:**

```csharp
// BAD - no documentation
group.MapPost("/", CreateWallet);
```

**Why This Breaks:**
1. Scalar UI shows unhelpful auto-generated names
2. API consumers can't understand endpoint purpose
3. Violates project documentation policy

**The Fix:**

```csharp
// GOOD - full metadata
group.MapPost("/", CreateWallet)
    .WithName("CreateWallet")
    .WithSummary("Create a new wallet")
    .WithDescription("Creates a new HD wallet with the specified algorithm")
    .Produces<CreateWalletResponse>(StatusCodes.Status201Created)
    .ProducesValidationProblem()
    .Produces(StatusCodes.Status409Conflict);
```

---

### WARNING: Using Swagger/Swashbuckle

**The Problem:**

```csharp
// BAD - wrong library
builder.Services.AddSwaggerGen();
app.UseSwagger();
app.UseSwaggerUI();
```

**Why This Breaks:**
1. Project standard is Scalar, not Swagger
2. Inconsistent documentation across services
3. Build will fail linting checks

**The Fix:**

```csharp
// GOOD - use Scalar
builder.Services.AddOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("Wallet Service").WithTheme(ScalarTheme.Purple);
});