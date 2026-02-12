# .NET 10 Workflows Reference

## Contents
- Build and Test Workflows
- Service Startup Pattern
- Project Creation Checklist
- Debugging Workflows

---

## Build and Test Workflows

### Solution Build

```bash
# Restore and build entire solution
dotnet restore && dotnet build

# Build specific project
dotnet build src/Services/Sorcha.Blueprint.Service

# Build with warnings as errors (CI)
dotnet build --warnaserror
```

### Test Execution

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Sorcha.Blueprint.Service.Tests

# Filter tests by name pattern
dotnet test --filter "FullyQualifiedName~Blueprint"

# With code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Validation Checklist

Copy this checklist and track progress:
- [ ] Step 1: `dotnet restore` - Restore NuGet packages
- [ ] Step 2: `dotnet build` - Compile all projects
- [ ] Step 3: `dotnet test` - Run unit tests
- [ ] Step 4: If tests fail, fix and repeat step 3

---

## Service Startup Pattern

Every service follows this exact pattern in `Program.cs`:

```csharp
// 1. Create builder
var builder = WebApplication.CreateBuilder(args);

// 2. Add Aspire service defaults FIRST
builder.AddServiceDefaults();

// 3. Add infrastructure (Redis, databases via Aspire)
builder.AddRedisOutputCache("redis");
builder.AddRedisDistributedCache("redis");

// 4. Add OpenAPI (built-in .NET 10)
builder.Services.AddOpenApi();

// 5. Register domain services
builder.Services.AddScoped<IMyService, MyService>();
builder.Services.AddSingleton<IMyStore, InMemoryStore>();

// 6. Add service clients (shared HTTP clients)
builder.Services.AddServiceClients(builder.Configuration);

// 7. Add authentication
builder.AddJwtAuthentication();
builder.Services.AddMyServiceAuthorization();

// 8. Build app
var app = builder.Build();

// 9. Map Aspire health endpoints
app.MapDefaultEndpoints();

// 10. Add security headers
app.UseApiSecurityHeaders();

// 11. Configure OpenAPI/Scalar
app.MapOpenApi();
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
}

// 12. Enable auth middleware
app.UseAuthentication();
app.UseAuthorization();

// 13. Map endpoints
app.MapMyEndpoints();

// 14. Run
app.Run();

// 15. Expose for integration tests
public partial class Program { }
```

---

## Project Creation Checklist

### New Library Project

Copy this checklist and track progress:
- [ ] Create project: `dotnet new classlib -n Sorcha.NewProject`
- [ ] Set `<TargetFramework>net10.0</TargetFramework>`
- [ ] Enable `<ImplicitUsings>enable</ImplicitUsings>`
- [ ] Enable `<Nullable>enable</Nullable>`
- [ ] Add `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- [ ] Add license header to all .cs files
- [ ] Reference `Sorcha.ServiceDefaults` if needed
- [ ] Create corresponding test project

### New Service Project

Copy this checklist and track progress:
- [ ] Create project: `dotnet new webapi -n Sorcha.NewService`
- [ ] Use `Microsoft.NET.Sdk.Web` SDK
- [ ] Reference `Sorcha.ServiceDefaults`
- [ ] Reference `Sorcha.ServiceClients`
- [ ] Add to `Sorcha.AppHost` project references
- [ ] Register in AppHost `Program.cs`
- [ ] Configure in `docker-compose.yml`
- [ ] Add health check configuration
- [ ] Create `Endpoints/` folder with endpoint groups

### License Header (Required)

```csharp
// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors
```

---

## Debugging Workflows

### Run with Aspire (Recommended for Debugging)

```bash
# Start with Aspire orchestrator - enables breakpoints
dotnet run --project src/Apps/Sorcha.AppHost

# Aspire Dashboard: http://localhost:18888
# Services on HTTPS ports 7000-7290
```

### Run with Docker (Production-like)

```bash
# Start all services
docker-compose up -d

# View service logs
docker-compose logs -f blueprint-service

# Rebuild single service
docker-compose build blueprint-service && \
docker-compose up -d --force-recreate blueprint-service
```

### Test Naming Convention

```csharp
// Pattern: MethodName_Scenario_ExpectedBehavior
[Fact]
public async Task ValidateAsync_ValidData_ReturnsValid() { }

[Fact]
public void Build_WithoutTitle_ThrowsInvalidOperationException() { }

[Theory]
[InlineData(12)]
[InlineData(24)]
public void Generate_WordCount_ReturnsCorrectLength(int wordCount) { }
```

### Integration Test Setup

```csharp
public class WalletEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public WalletEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateWallet_ValidRequest_ReturnsCreated()
    {
        var request = new { Name = "Test", Algorithm = "ED25519" };

        var response = await _client.PostAsJsonAsync("/api/v1/wallets", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
```

---

## Code Quality Workflow

### Format Check

```bash
# Check formatting (CI)
dotnet format --verify-no-changes

# Auto-format code
dotnet format
```

### Import Order (Enforced)

```csharp
using System.Text.Json;           // 1. System
using Microsoft.Extensions.DI;    // 2. Microsoft
using FluentAssertions;           // 3. Third-party
using Sorcha.Blueprint.Models;    // 4. Sorcha
```

### Build Validation Loop

1. Make changes
2. Run: `dotnet build`
3. If build fails, fix compiler errors and repeat step 2
4. Run: `dotnet test`
5. If tests fail, fix failing tests and repeat step 4
6. Only commit when both build and tests pass