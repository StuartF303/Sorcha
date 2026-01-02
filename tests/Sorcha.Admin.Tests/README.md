# Sorcha.Admin.Tests

Comprehensive test suite for the Sorcha Admin Blazor WebAssembly application.

## Test Coverage

### Configuration Management
- **ConfigurationServiceTests** - Tests for LocalStorage-based configuration management
  - Default configuration creation
  - Profile management (get, set, list)
  - Active profile switching
  - LocalStorage key consistency (`sorcha:config`)
  - Docker profile as default

### Authentication & Token Management
- **BrowserTokenCacheTests** - Tests for encrypted token storage
  - Token encryption and storage
  - Token retrieval and decryption
  - Expired token cleanup
  - LocalStorage key format (`sorcha:tokens:{profile}`)
  - Clear all tokens functionality

### Profile Defaults
- **ProfileDefaultsTests** - Tests for default profile configurations
  - Docker as default active profile
  - Correct authentication URLs
  - HTTP/HTTPS protocol validation
  - Required field validation

## Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~ConfigurationServiceTests"

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"
```

## Test Frameworks

- **xUnit** - Test framework
- **bUnit** - Blazor component testing
- **FluentAssertions** - Assertion library
- **Moq** - Mocking framework

## Key Test Scenarios

### Regression Prevention

1. **Docker Default Profile** - Ensures "docker" is the default active profile
2. **Authentication URL** - Verifies `/api/service-auth/token` (not `/api/tenant/service-auth/token`)
3. **LocalStorage Keys** - Validates consistent key naming (`sorcha:config`, `sorcha:tokens:{profile}`)
4. **Token Expiration** - Ensures expired tokens are automatically cleaned up

### LocalStorage Structure

```javascript
// Configuration (sorcha:config)
{
  "ActiveProfile": "docker",
  "Profiles": {
    "docker": { ... },
    "local": { ... },
    "production": { ... }
  },
  "VerboseLogging": false
}

// Token Cache (sorcha:tokens:docker)
// Base64-encoded encrypted JSON of TokenCacheEntry
{
  "AccessToken": "...",
  "RefreshToken": "...",
  "ExpiresAt": "2026-01-03T12:00:00Z",
  "Profile": "docker",
  "Subject": "admin@sorcha.local"
}
```

## Browser Testing

To manually verify LocalStorage in browser:

1. Open browser DevTools (F12)
2. Navigate to Application → Local Storage → http://localhost
3. Look for keys:
   - `sorcha:config` - Configuration JSON
   - `sorcha:tokens:docker` - Encrypted token (Base64)

### Clearing LocalStorage

```javascript
// In browser console
localStorage.removeItem('sorcha:config');
localStorage.removeItem('sorcha:tokens:docker');
// Or clear all
localStorage.clear();
```

## CI/CD Integration

Tests are designed to run in CI/CD pipelines:

```yaml
# Example GitHub Actions
- name: Run Tests
  run: dotnet test --logger trx --results-directory ./test-results

- name: Publish Test Results
  uses: dorny/test-reporter@v1
  if: always()
  with:
    name: Test Results
    path: ./test-results/*.trx
    reporter: dotnet-trx
```

## Adding New Tests

1. Create test class in appropriate namespace:
   - `Services/` - Service layer tests
   - `Models/` - Model and configuration tests
   - `Components/` - Blazor component tests (using bUnit)

2. Follow naming convention:
   - Test class: `{ClassUnderTest}Tests`
   - Test method: `{MethodUnderTest}_{Scenario}_{ExpectedResult}`

3. Use FluentAssertions for readable assertions:
   ```csharp
   result.Should().NotBeNull();
   result.ActiveProfile.Should().Be("docker");
   ```

4. Document regression prevention tests clearly

## Known Issues

- Token encryption tests require mocking `IEncryptionProvider` as browser crypto APIs aren't available in test context
- bUnit tests for components may require additional setup for MudBlazor components

## Future Enhancements

- [ ] End-to-end authentication flow tests
- [ ] Component rendering tests with bUnit
- [ ] Integration tests with TestContainers
- [ ] Performance benchmarks for token encryption/decryption
