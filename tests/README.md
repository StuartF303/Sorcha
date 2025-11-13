# Sorcha Tests

This directory contains all test projects for the Sorcha application.

## Test Projects

### Unit Tests
- **Sorcha.Blueprint.Api.Tests** - API endpoint tests
- **Sorcha.Blueprint.Fluent.Tests** - Fluent builder pattern tests
- **Sorcha.Cryptography.Tests** - Cryptography library tests ([detailed README](Sorcha.Cryptography.Tests/README.md))
- **Sorcha.Performance.Tests** - NBomber performance and load tests

### Integration Tests
- **Sorcha.Gateway.Integration.Tests** - Gateway routing and YARP proxy tests (uses Aspire TestHost)
- **Sorcha.UI.E2E.Tests** - End-to-end Playwright tests

## Running Tests

### Run All Tests Locally

```bash
# From repository root
dotnet test
```

### Run Specific Test Project

```bash
# Unit tests
dotnet test tests/Sorcha.Blueprint.Api.Tests
dotnet test tests/Sorcha.Blueprint.Fluent.Tests
dotnet test tests/Sorcha.Cryptography.Tests

# Cryptography - run specific algorithm tests
dotnet test tests/Sorcha.Cryptography.Tests --filter "FullyQualifiedName~ED25519"
dotnet test tests/Sorcha.Cryptography.Tests --filter "FullyQualifiedName~NISTP256"
dotnet test tests/Sorcha.Cryptography.Tests --filter "FullyQualifiedName~RSA4096"

# Integration tests (requires Docker for Redis)
dotnet test tests/Sorcha.Gateway.Integration.Tests

# E2E tests (requires Playwright installation)
dotnet test tests/Sorcha.UI.E2E.Tests
```

### Run with Code Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Integration Tests Setup

Integration tests use **Aspire.Hosting.Testing** to start a full application host with all services.

### Prerequisites

1. **Docker Desktop** - Required for Redis
2. **.NET 10 SDK** - Required for Aspire

### Running Integration Tests

```bash
# Start Docker Desktop first

# Run integration tests
dotnet test tests/Sorcha.Gateway.Integration.Tests

# Run with verbose output
dotnet test tests/Sorcha.Gateway.Integration.Tests --logger "console;verbosity=detailed"
```

### What Integration Tests Do

1. **Start Aspire AppHost** - Launches all services (API Gateway, Blueprint API, Peer Service, Blazor Client)
2. **Start Redis** - Uses Docker to run Redis container
3. **Test Routing** - Verifies YARP gateway routes requests correctly
4. **Test Health Checks** - Verifies health endpoint aggregation
5. **Cleanup** - Stops all services after tests complete

### Troubleshooting Integration Tests

If integration tests fail:

```bash
# Check Docker is running
docker ps

# Check Redis is accessible
docker run -d -p 6379:6379 redis:8.2

# Run tests with detailed logging
dotnet test tests/Sorcha.Gateway.Integration.Tests \
  --logger "console;verbosity=detailed" \
  -- NUnit.Verbosity=5
```

## E2E Tests Setup

End-to-end tests use **Playwright** to test the Blazor UI.

### Prerequisites

```bash
# Install Playwright browsers
cd tests/Sorcha.UI.E2E.Tests
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install --with-deps
```

### Running E2E Tests

```bash
# Run E2E tests
dotnet test tests/Sorcha.UI.E2E.Tests

# Run in headed mode (see browser)
dotnet test tests/Sorcha.UI.E2E.Tests -- NUnit.Headless=false

# Run specific test
dotnet test tests/Sorcha.UI.E2E.Tests --filter "FullyQualifiedName~HomePage_LoadsSuccessfully"
```

## Cryptography Tests

The cryptography test suite includes comprehensive tests for key generation, signing, encryption, and performance benchmarking.

### Quick Start

```bash
# Run all crypto tests
dotnet test tests/Sorcha.Cryptography.Tests

# Run specific algorithm tests
dotnet test tests/Sorcha.Cryptography.Tests --filter "FullyQualifiedName~ED25519"

# Run with detailed output for performance metrics
dotnet test tests/Sorcha.Cryptography.Tests --logger "console;verbosity=detailed"
```

### Performance Testing Cryptography

The crypto tests include built-in performance benchmarks:

```bash
# Test key generation performance
dotnet test tests/Sorcha.Cryptography.Tests --filter "FullyQualifiedName~KeyGeneration"

# Test signing performance
dotnet test tests/Sorcha.Cryptography.Tests --filter "FullyQualifiedName~Signing"

# Test verification performance
dotnet test tests/Sorcha.Cryptography.Tests --filter "FullyQualifiedName~Verify"
```

**Example performance output:**
```
ED25519 Key Generation: 1000 iterations in 245ms
Average: 0.25ms per key pair
Throughput: 4081 keys/second

ED25519 Signing: 10000 iterations in 487ms
Average: 0.05ms per signature
Throughput: 20534 signatures/second
```

### Load Testing Cryptography

```bash
# Run load tests (parallel operations)
dotnet test tests/Sorcha.Cryptography.Tests --filter "Category=LoadTest"

# Memory leak tests
dotnet test tests/Sorcha.Cryptography.Tests --filter "FullyQualifiedName~MemoryLeak"
```

For detailed examples and advanced scenarios, see [Sorcha.Cryptography.Tests/README.md](Sorcha.Cryptography.Tests/README.md).

## Performance Tests

Performance tests use **NBomber** for load testing.

```bash
# Run performance tests
dotnet test tests/Sorcha.Performance.Tests

# Or run the console app directly
dotnet run --project tests/Sorcha.Performance.Tests

# Target custom URL
dotnet run --project tests/Sorcha.Performance.Tests https://your-api-url
```

## CI/CD Pipeline

### GitHub Actions

The CI/CD pipeline runs:
- ✅ **Unit tests** - Always run
- ❌ **Integration tests** - Skipped (too complex for CI, run locally)
- ❌ **E2E tests** - Only on PRs (requires browser setup)

### Why Integration Tests Are Skipped in CI

Integration tests require:
1. Full Aspire AppHost with all services
2. Redis container
3. Extended timeout for service startup
4. More complex environment setup

These are better suited for local development and pre-deployment testing.

### Running All Tests Locally Before Push

```bash
# 1. Ensure Docker is running
docker ps

# 2. Run all unit tests
dotnet test tests/Sorcha.Blueprint.Api.Tests
dotnet test tests/Sorcha.Blueprint.Fluent.Tests

# 3. Run integration tests
dotnet test tests/Sorcha.Gateway.Integration.Tests

# 4. Build solution to verify no errors
dotnet build --configuration Release

# 5. Push to trigger CI/CD
git push origin main
```

## Test Coverage

To generate and view test coverage:

```bash
# Install report generator
dotnet tool install -g dotnet-reportgenerator-globaltool

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:Html

# Open report
open coverage-report/index.html  # Mac
start coverage-report/index.html # Windows
```

## Writing New Tests

### Unit Test Example

```csharp
using FluentAssertions;
using Xunit;

namespace Sorcha.MyFeature.Tests;

public class MyFeatureTests
{
    [Fact]
    public void MyTest_Should_DoSomething()
    {
        // Arrange
        var sut = new MyFeature();

        // Act
        var result = sut.DoSomething();

        // Assert
        result.Should().Be("expected");
    }
}
```

### Integration Test Example

```csharp
using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace Sorcha.Integration.Tests;

public class MyIntegrationTests : IAsyncLifetime
{
    private DistributedApplication? _app;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Sorcha_AppHost>();

        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        _client = _app.CreateHttpClient("api-gateway");
    }

    [Fact]
    public async Task MyTest()
    {
        var response = await _client!.GetAsync("/api/test");
        response.Should().BeSuccessful();
    }

    public async Task DisposeAsync()
    {
        if (_app != null) await _app.DisposeAsync();
        _client?.Dispose();
    }
}
```

## Best Practices

1. **Unit tests** - Fast, isolated, no external dependencies
2. **Integration tests** - Test service interactions, use TestHost
3. **E2E tests** - Test user workflows, use Playwright
4. **Performance tests** - Measure throughput and latency

## Test Organization

```
tests/
├── Sorcha.Blueprint.Api.Tests/          # API unit tests
├── Sorcha.Blueprint.Fluent.Tests/       # Fluent API tests
├── Sorcha.Cryptography.Tests/           # Cryptography tests with perf benchmarks
│   ├── Unit/                            # Unit tests for crypto operations
│   └── README.md                        # Detailed crypto testing guide
├── Sorcha.Gateway.Integration.Tests/    # Gateway integration tests
├── Sorcha.UI.E2E.Tests/                 # End-to-end tests
├── Sorcha.Performance.Tests/            # NBomber performance tests
└── README.md                            # This file
```

## Continuous Testing

For local continuous testing:

```bash
# Install dotnet watch
dotnet tool install -g dotnet-watch

# Run tests on file changes
dotnet watch test --project tests/Sorcha.Blueprint.Api.Tests
```

## Resources

- [xUnit Documentation](https://xunit.net/)
- [NUnit Documentation](https://nunit.org/)
- [FluentAssertions](https://fluentassertions.com/)
- [Aspire Testing](https://learn.microsoft.com/dotnet/aspire/testing/)
- [Playwright .NET](https://playwright.dev/dotnet/)
- [NBomber](https://nbomber.com/)
