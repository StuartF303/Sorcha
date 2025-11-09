# Sorcha Gateway Integration Tests

Comprehensive integration tests for the API Gateway using Aspire Test Host.

## Test Suites

### 1. GatewayRoutingTests
Tests YARP reverse proxy functionality:
- Blueprint API routing (`/api/blueprint/*`)
- Peer Service routing (`/api/peer/*`)
- Status endpoint mappings
- CORS headers
- 404 handling for invalid routes

### 2. HealthAggregationTests
Tests health monitoring and aggregation:
- Aggregated health endpoint (`/api/health`)
- System statistics (`/api/stats`)
- Service status checks (Blueprint, Peer)
- Landing page rendering

### 3. ClientDownloadTests
Tests client distribution functionality:
- Client metadata API (`/api/client/info`)
- ZIP package download (`/api/client/download`)
- ZIP content validation (includes .csproj, excludes bin/obj)
- Installation instructions (`/api/client/instructions`)

### 4. OpenApiAggregationTests
Tests OpenAPI documentation aggregation:
- Aggregated OpenAPI spec (`/openapi/aggregated.json`)
- Blueprint paths inclusion
- Peer paths inclusion
- Schema components
- Scalar UI accessibility

## Running the Tests

### Prerequisites
- Ensure all services are **stopped** before running tests
- Tests will start their own Aspire app host
- Tests use the Aspire.Hosting.Testing package

### Run All Tests
```bash
dotnet test tests/Sorcha.Gateway.Integration.Tests
```

### Run Specific Test Suite
```bash
# Health aggregation only
dotnet test --filter "FullyQualifiedName~HealthAggregationTests"

# Routing only
dotnet test --filter "FullyQualifiedName~GatewayRoutingTests"

# Client download only
dotnet test --filter "FullyQualifiedName~ClientDownloadTests"

# OpenAPI only
dotnet test --filter "FullyQualifiedName~OpenApiAggregationTests"
```

### Run Single Test
```bash
dotnet test --filter "FullyQualifiedName~GetAggregatedHealth_ReturnsHealthStatus"
```

## Test Architecture

### GatewayIntegrationTestBase
Base class for all integration tests:
- Creates Aspire DistributedApplication from AppHost
- Starts all services (Redis, Blueprint API, Peer Service, Gateway)
- Provides HTTP client for gateway
- Implements IAsyncLifetime for proper setup/teardown

### Test Pattern
```csharp
public class MyTests : GatewayIntegrationTestBase
{
    [Fact]
    public async Task MyTest()
    {
        // Arrange - services are already running via base class

        // Act - use GatewayClient to call API
        var response = await GatewayClient!.GetAsync("/api/endpoint");

        // Assert - use FluentAssertions
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

## Troubleshooting

### Tests Fail with File Locks
- Stop all running Sorcha services
- Close Visual Studio if running
- Kill any dotnet processes: `taskkill /f /im dotnet.exe`

### Tests Timeout
- Increase test timeout (default: 5 minutes)
- Check Docker is running (for Redis)
- Check services can start individually

### Port Conflicts
- Tests use dynamic port allocation
- If conflicts occur, check no other services are using ports 7080-7082

## CI/CD Integration

These tests are designed to run in CI/CD pipelines:
- No manual setup required
- Self-contained (starts own services)
- Clean up after execution
- Fail fast on errors

Example GitHub Actions:
```yaml
- name: Run Integration Tests
  run: dotnet test tests/Sorcha.Gateway.Integration.Tests --logger "trx"
```
