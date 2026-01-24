# Minimal APIs Workflows Reference

## Contents
- Creating a New Endpoint
- Adding a New Route Group
- Adding Authorization to Endpoints
- Implementing Output Caching
- Testing Endpoints

---

## Creating a New Endpoint

### Workflow Checklist

Copy this checklist and track progress:
- [ ] Step 1: Create request/response DTOs in `Models/`
- [ ] Step 2: Add endpoint method to appropriate `*Endpoints.cs` file
- [ ] Step 3: Add route mapping with full OpenAPI metadata
- [ ] Step 4: Add authorization if required
- [ ] Step 5: Handle all error cases with ProblemDetails
- [ ] Step 6: Register endpoint mapper in `Program.cs` (if new file)
- [ ] Step 7: Write integration tests

### Step-by-Step Example

**1. Create DTOs:**

```csharp
// Models/SignTransactionRequest.cs
public record SignTransactionRequest
{
    public required string TransactionData { get; init; }
    public string? DerivationPath { get; init; }
}

// Models/SignTransactionResponse.cs
public record SignTransactionResponse
{
    public required string Signature { get; init; }
    public required string SignedBy { get; init; }
    public required DateTime SignedAt { get; init; }
    public required string PublicKey { get; init; }
}
```

**2. Add endpoint handler:**

```csharp
// Endpoints/WalletEndpoints.cs
private static async Task<IResult> SignTransaction(
    string address,
    [FromBody] SignTransactionRequest request,
    WalletManager walletManager,
    ILogger<Program> logger,
    CancellationToken cancellationToken = default)
{
    try
    {
        var data = Convert.FromBase64String(request.TransactionData);
        var (signature, publicKey) = await walletManager.SignTransactionAsync(
            address, data, request.DerivationPath, cancellationToken);

        return Results.Ok(new SignTransactionResponse
        {
            Signature = Convert.ToBase64String(signature),
            SignedBy = address,
            SignedAt = DateTime.UtcNow,
            PublicKey = Convert.ToBase64String(publicKey)
        });
    }
    catch (FormatException ex)
    {
        return Results.BadRequest(new ProblemDetails
        {
            Title = "Invalid Request",
            Detail = "TransactionData must be valid base64"
        });
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
    {
        return Results.NotFound();
    }
}
```

**3. Add route mapping:**

```csharp
// In MapWalletEndpoints()
walletGroup.MapPost("/{address}/sign", SignTransaction)
    .WithName("SignTransaction")
    .WithSummary("Sign a transaction")
    .WithDescription("Sign transaction data with the wallet's private key")
    .Produces<SignTransactionResponse>()
    .ProducesValidationProblem()
    .Produces(StatusCodes.Status404NotFound);
```

---

## Adding a New Route Group

### When to Create a New Group

- Endpoints share a common base path (`/api/v1/wallets`, `/api/v1/addresses`)
- Endpoints share authentication requirements
- Endpoints logically belong together (same resource)

### Workflow

```csharp
// 1. Create new file: Endpoints/DelegationEndpoints.cs
public static class DelegationEndpoints
{
    public static IEndpointRouteBuilder MapDelegationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/delegations")
            .WithTags("Delegations")
            .RequireAuthorization("CanManageWallets");

        group.MapPost("/", CreateDelegation)
            .WithName("CreateDelegation")
            .WithSummary("Create a wallet delegation");

        group.MapGet("/", ListDelegations)
            .WithName("ListDelegations")
            .WithSummary("List active delegations");

        group.MapDelete("/{id:guid}", RevokeDelegation)
            .WithName("RevokeDelegation")
            .WithSummary("Revoke a delegation");

        return app;
    }

    // ... handler methods
}

// 2. Register in Program.cs
app.MapWalletEndpoints();
app.MapDelegationEndpoints();  // Add this line
```

---

## Adding Authorization to Endpoints

### Public Endpoints (AllowAnonymous)

```csharp
group.MapPost("/login", Login)
    .WithName("Login")
    .AllowAnonymous()  // No authentication required
    .Produces<TokenResponse>()
    .Produces(StatusCodes.Status401Unauthorized);
```

### User Authentication

```csharp
group.MapGet("/me", GetCurrentUser)
    .WithName("GetCurrentUser")
    .RequireAuthorization()  // Any authenticated user
    .Produces<CurrentUserResponse>();
```

### Policy-Based Authorization

```csharp
// Group-level policy
var walletGroup = app.MapGroup("/api/v1/wallets")
    .RequireAuthorization("CanManageWallets");

// Endpoint-level stricter policy
group.MapPost("/token/revoke-user", RevokeUserTokens)
    .RequireAuthorization("RequireAdministrator");
```

### Creating Custom Policies

See the **jwt** skill for detailed authorization policy configuration.

```csharp
// Extensions/AuthenticationExtensions.cs
services.AddAuthorization(options =>
{
    options.AddPolicy("CanPublishBlueprints", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("role", "publisher") ||
            context.User.HasClaim("token_type", "service")));
});
```

---

## Implementing Output Caching

### Setup (Once per Service)

```csharp
// Program.cs
builder.AddRedisOutputCache("redis");  // From Aspire

// Middleware order matters
app.UseOutputCache();  // After auth, before endpoints
```

### Cache Read Endpoints

```csharp
// Cache list endpoint for 5 minutes
blueprintGroup.MapGet("/", GetAllBlueprints)
    .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)).Tag("blueprints"));

// Cache individual item longer
blueprintGroup.MapGet("/{id}", GetBlueprint)
    .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(10)).Tag("blueprints"));

// Cache immutable data for a year
blueprintGroup.MapGet("/{id}/versions/{version}", GetPublishedVersion)
    .CacheOutput(policy => policy.Expire(TimeSpan.FromDays(365)).Tag("published"));
```

### Invalidate on Mutations

```csharp
blueprintGroup.MapPost("/", async (
    BlueprintModel blueprint,
    IBlueprintService service,
    IOutputCacheStore cache) =>
{
    var created = await service.CreateAsync(blueprint);

    // CRITICAL: Invalidate cache after mutation
    await cache.EvictByTagAsync("blueprints", default);

    return Results.Created($"/api/blueprints/{created.Id}", created);
});

blueprintGroup.MapDelete("/{id}", async (
    string id,
    IBlueprintService service,
    IOutputCacheStore cache) =>
{
    var deleted = await service.DeleteAsync(id);
    if (!deleted) return Results.NotFound();

    await cache.EvictByTagAsync("blueprints", default);
    return Results.NoContent();
});
```

### Feedback Loop

1. Add caching to endpoint
2. Test: `curl -v http://localhost:5000/api/blueprints` - check for cache headers
3. If no `Age` header, verify middleware order and Redis connection
4. Only proceed when caching is verified working

---

## Testing Endpoints

### Integration Test Pattern

```csharp
public class WalletEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public WalletEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real services with mocks
                services.AddScoped<IWalletRepository, InMemoryWalletRepository>();
            });
        }).CreateClient();
    }

    [Fact]
    public async Task CreateWallet_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateWalletRequest { Name = "Test Wallet" };
        var content = JsonContent.Create(request);

        // Act
        var response = await _client.PostAsync("/api/v1/wallets", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWallet_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/v1/wallets/nonexistent");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

### Test Naming Convention

```csharp
// Pattern: EndpointName_Scenario_ExpectedResult
public async Task CreateWallet_ValidRequest_ReturnsCreated() { }
public async Task CreateWallet_DuplicateName_ReturnsConflict() { }
public async Task GetWallet_Unauthorized_Returns401() { }
public async Task ListWallets_WithPagination_ReturnsPagedResults() { }
```

See the **xunit** skill for testing framework details.