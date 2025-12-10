# Tenant Service Integration Tests

## Overview

This test suite provides comprehensive integration testing for the Tenant Service with **configurable database modes**:

- **InMemory** (default): Fast, isolated, no Docker required - ideal for CI/CD and rapid development
- **PostgreSQL**: Realistic database tests using Testcontainers - ideal for pre-production validation

## Running Tests

### Quick Start (InMemory Mode)

```bash
# Run all tests with default InMemory database
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~AuthApiTests"

# Run specific test
dotnet test --filter "FullyQualifiedName~Login_ShouldReturnTokens_WithValidCredentials"
```

### PostgreSQL Mode (Testcontainers)

**Requirements:**
- Docker Desktop installed and running
- At least 2GB RAM available for containers

```bash
# Set environment variable to use PostgreSQL
$env:TEST_DATABASE_MODE="PostgreSQL"  # PowerShell
# or
export TEST_DATABASE_MODE=PostgreSQL  # Bash

# Run tests with PostgreSQL
dotnet test
```

### CI/CD Integration

**GitHub Actions Example:**

```yaml
- name: Run Integration Tests (InMemory)
  run: dotnet test tests/Sorcha.Tenant.Service.IntegrationTests

- name: Run Integration Tests (PostgreSQL)
  run: dotnet test tests/Sorcha.Tenant.Service.IntegrationTests
  env:
    TEST_DATABASE_MODE: PostgreSQL
```

**Azure DevOps Example:**

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run Integration Tests (InMemory)'
  inputs:
    command: 'test'
    projects: 'tests/Sorcha.Tenant.Service.IntegrationTests'

- task: DotNetCoreCLI@2
  displayName: 'Run Integration Tests (PostgreSQL)'
  inputs:
    command: 'test'
    projects: 'tests/Sorcha.Tenant.Service.IntegrationTests'
  env:
    TEST_DATABASE_MODE: 'PostgreSQL'
```

## Test Structure

```
tests/Sorcha.Tenant.Service.IntegrationTests/
├── Configuration/
│   ├── TestConfiguration.cs      # Environment-based configuration
│   └── TestDatabaseMode.cs       # Database mode enum
├── Fixtures/
│   ├── TenantServiceWebApplicationFactory.cs  # Main test factory
│   ├── TestAuthHandler.cs        # Mock authentication
│   └── TestDataSeeder.cs         # Test data seeding
├── AuthApiTests.cs               # Authentication endpoint tests
└── OrganizationApiTests.cs       # Organization endpoint tests
```

## Test Data

All tests use well-known GUIDs and credentials for reproducibility:

### Test Organization
- **ID:** `00000000-0000-0000-0000-000000000001`
- **Name:** "Test Organization"
- **Subdomain:** "test-org"

### Test Users (External IDP)
- **Admin:** admin@test-org.sorcha.io (00000000-0000-0000-0001-000000000001)
- **Member:** member@test-org.sorcha.io (00000000-0000-0000-0001-000000000002)
- **Auditor:** auditor@test-org.sorcha.io (00000000-0000-0000-0001-000000000003)

### Test Users (Local Auth)
- **Local Admin:** local-admin@test-org.sorcha.io (password: TestPassword123!)
- **Local Member:** local-member@test-org.sorcha.io (password: MemberPass456!)
- **Inactive User:** inactive@test-org.sorcha.io (suspended status)

## Database Modes Comparison

| Feature | InMemory | PostgreSQL |
|---------|----------|------------|
| **Speed** | ⚡ Very Fast (ms) | 🐢 Slower (seconds) |
| **Docker Required** | ❌ No | ✅ Yes |
| **Database Fidelity** | ⚠️ Approximate | ✅ Exact |
| **Migrations Tested** | ❌ No | ✅ Yes |
| **Isolation** | ✅ Perfect | ✅ Perfect (containers) |
| **CI/CD Friendly** | ✅ Yes | ⚠️ Requires Docker |
| **Best For** | Unit/Integration tests | Pre-production validation |

## When to Use Each Mode

### Use InMemory Mode For:
- ✅ Rapid development and debugging
- ✅ CI/CD pipelines without Docker
- ✅ Unit tests and integration tests
- ✅ Quick validation of business logic
- ✅ TDD workflows

### Use PostgreSQL Mode For:
- ✅ Pre-production validation
- ✅ Testing database-specific features (JSONB, indexes, constraints)
- ✅ Migration testing
- ✅ Performance testing with realistic data
- ✅ Debugging PostgreSQL-specific issues

## Troubleshooting

### Tests Fail with "Docker not found"
- **Solution:** Install Docker Desktop or switch to InMemory mode
- **Quick Fix:** `unset TEST_DATABASE_MODE` (removes PostgreSQL requirement)

### Tests are slow
- **Check:** Are you in PostgreSQL mode unintentionally?
- **Solution:** Ensure `TEST_DATABASE_MODE` is not set (defaults to InMemory)

### Database seeding fails
- **Check:** Is the factory's `InitializeAsync()` being called?
- **Solution:** Ensure test class implements `IAsyncLifetime` and calls `await _factory.EnsureSeededAsync()`

### InMemory tests fail with "user not found"
- **Issue:** DbContext scoping problem
- **Solution:** Verify all DbContext instances use the same database name (handled automatically)

## Performance Benchmarks

**Typical test execution times:**

```
InMemory Mode:
- Single test: ~50-150ms
- Full suite: ~2-5 seconds

PostgreSQL Mode:
- Container startup: ~5-10 seconds (one-time)
- Single test: ~200-500ms
- Full suite: ~15-30 seconds
```

## Contributing

When adding new tests:

1. ✅ Test should work in BOTH InMemory and PostgreSQL modes
2. ✅ Use well-known test data from `TestDataSeeder`
3. ✅ Clean up resources in `DisposeAsync()` if needed
4. ✅ Add test to both modes in CI/CD pipeline

## See Also

- [Tenant Service README](../../src/Services/Sorcha.Tenant.Service/README.md)
- [Testcontainers Documentation](https://dotnet.testcontainers.org/)
- [xUnit Documentation](https://xunit.net/)
