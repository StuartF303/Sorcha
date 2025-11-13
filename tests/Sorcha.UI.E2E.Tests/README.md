# Sorcha UI End-to-End Tests

Playwright-based end-to-end tests for the Blazor WebAssembly UI.

## Setup

### Install Playwright Browsers
```bash
# Required before first run
pwsh tests/Sorcha.UI.E2E.Tests/bin/Debug/net10.0/playwright.ps1 install

# Or on Unix/Mac
tests/Sorcha.UI.E2E.Tests/bin/Debug/net10.0/playwright.sh install
```

### Prerequisites
- Playwright browsers installed (Chromium, Firefox, WebKit)
- Docker Desktop running (for Aspire app)
- No other services running on ports (tests start their own)

## Running Tests

### Run All E2E Tests
```bash
dotnet test tests/Sorcha.UI.E2E.Tests
```

### Run with UI (Headed Mode)
```bash
# Set environment variable
$env:HEADED="1"
dotnet test tests/Sorcha.UI.E2E.Tests

# Or in launchSettings.json
```

### Run Single Test
```bash
dotnet test tests/Sorcha.UI.E2E.Tests --filter "FullyQualifiedName~HomePage_LoadsSuccessfully"
```

### Debug with Playwright Inspector
```bash
$env:PWDEBUG="1"
dotnet test tests/Sorcha.UI.E2E.Tests --filter "FullyQualifiedName~YourTest"
```

## Test Coverage

### Navigation Tests
- ✅ Home page loads
- ✅ Navigation between pages works
- ✅ All major pages accessible

### Page-Specific Tests
- ✅ Blueprint Library page
- ✅ Schema Explorer
- ✅ Admin/Service dashboard
- ✅ Designer canvas
- ✅ Event log

### UI/UX Tests
- ✅ MudBlazor components load correctly
- ✅ No JavaScript errors on page load
- ✅ Responsive design (mobile/desktop viewports)

### Interaction Tests
- ✅ Can create new blueprint
- ✅ Service status displays correctly

## Test Architecture

### E2E Test Base
Tests extend `PageTest` from Playwright and use Aspire test host:

```csharp
[TestFixture]
public class BlazorUITests : PageTest
{
    // Starts Aspire app with all services
    // Gets Blazor URL automatically
    // Provides browser Page object
}
```

### Test Pattern
```csharp
[Test]
public async Task MyTest()
{
    // Navigate to page
    await Page.GotoAsync($"{_blazorUrl}/mypage");
    await Page.WaitForLoadStateAsync();

    // Interact with elements
    await Page.ClickAsync("button");

    // Assert
    await Expect(Page).ToHaveURLAsync(new Regex("/expected"));
}
```

## Playwright Selectors

### Common Patterns
```csharp
// By text
Page.Locator("button:has-text('Click Me')")

// By role
Page.Locator("button[role='button']")

// By test ID
Page.Locator("[data-testid='my-element']")

// MudBlazor components
Page.Locator(".mud-button")
Page.Locator(".mud-table")
```

## Troubleshooting

### Playwright Browsers Not Installed
```
Error: Playwright executable doesn't exist
```

**Solution:**
```bash
cd tests/Sorcha.UI.E2E.Tests
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install
```

### Tests Timeout
```
TimeoutException: page.goto: Timeout 30000ms exceeded
```

**Solutions:**
- Increase timeout in playwright config
- Check if services started successfully
- Verify Docker has enough resources

### Service Startup Failures
**Solutions:**
- Stop all running services before tests
- Ensure Docker Desktop is running
- Check ports aren't in use

### Tests Pass Locally but Fail in CI
- Install Playwright browsers in CI
- Use headless mode in CI
- Increase timeouts for slower CI environments

## CI/CD Integration

### GitHub Actions Example
```yaml
- name: Install Playwright Browsers
  run: pwsh tests/Sorcha.UI.E2E.Tests/bin/Debug/net10.0/playwright.ps1 install --with-deps

- name: Run E2E Tests
  run: dotnet test tests/Sorcha.UI.E2E.Tests --logger "trx"
  env:
    HEADED: false
```

### Docker Support
Tests work in Docker if:
- Playwright browsers are installed
- X11/Xvfb configured for headed mode (or use headless)

## Best Practices

1. **Use Data Test IDs**: Add `data-testid` attributes to elements
2. **Wait for Load States**: Always wait for page/network to be ready
3. **Avoid Fixed Waits**: Use `WaitForSelectorAsync` instead of `WaitForTimeoutAsync`
4. **Test User Flows**: Focus on complete user journeys, not every button
5. **Keep Tests Independent**: Each test should work in isolation

## Performance

- Tests run in parallel by default
- Each test gets its own browser context
- App starts once for all tests (OneTimeSetUp)
- Typical run time: 2-3 minutes for full suite

## Adding New Tests

1. Create test method with `[Test]` attribute
2. Navigate to page: `await Page.GotoAsync(_blazorUrl + "/page")`
3. Interact: Use `Page.ClickAsync`, `Page.FillAsync`, etc.
4. Assert: Use `Expect(Page).To...` or NUnit assertions
5. Document what the test validates

## Resources

- [Playwright .NET](https://playwright.dev/dotnet/)
- [Playwright Selectors](https://playwright.dev/dotnet/docs/selectors)
- [Aspire Testing](https://learn.microsoft.com/dotnet/aspire/testing/)
- [NUnit Documentation](https://docs.nunit.org/)
