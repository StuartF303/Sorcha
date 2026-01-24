# Playwright Workflows Reference

## Contents
- Test Project Setup
- Running Tests
- CI/CD Integration
- Debugging
- Test Organization

---

## Test Project Setup

### Project Configuration

```xml
<!-- Sorcha.UI.E2E.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.Testing" Version="13.1.0" />
    <PackageReference Include="Microsoft.Playwright.NUnit" Version="1.57.0" />
    <PackageReference Include="NUnit" Version="4.4.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Apps\Sorcha.AppHost\Sorcha.AppHost.csproj" />
  </ItemGroup>
</Project>
```

### Browser Installation

```bash
# Build first to generate playwright scripts
dotnet build tests/Sorcha.UI.E2E.Tests

# Install browsers (Windows PowerShell)
pwsh tests/Sorcha.UI.E2E.Tests/bin/Debug/net10.0/playwright.ps1 install

# Unix/Mac
tests/Sorcha.UI.E2E.Tests/bin/Debug/net10.0/playwright.sh install
```

Copy this checklist for new E2E test projects:
- [ ] Add Aspire.Hosting.Testing package
- [ ] Add Microsoft.Playwright.NUnit package
- [ ] Reference AppHost project
- [ ] Build project
- [ ] Install Playwright browsers
- [ ] Verify Docker Desktop running

---

## Running Tests

### Basic Execution

```bash
# All E2E tests
dotnet test tests/Sorcha.UI.E2E.Tests

# Single test
dotnet test tests/Sorcha.UI.E2E.Tests --filter "FullyQualifiedName~HomePage_LoadsSuccessfully"

# With verbose output
dotnet test tests/Sorcha.UI.E2E.Tests --logger "console;verbosity=detailed"
```

### Headed Mode (Visible Browser)

```powershell
# PowerShell
$env:HEADED="1"
dotnet test tests/Sorcha.UI.E2E.Tests
```

```bash
# Unix/Mac
HEADED=1 dotnet test tests/Sorcha.UI.E2E.Tests
```

### Debug with Playwright Inspector

```powershell
$env:PWDEBUG="1"
dotnet test tests/Sorcha.UI.E2E.Tests --filter "FullyQualifiedName~YourTest"
```

---

## CI/CD Integration

### GitHub Actions Workflow

```yaml
- name: Install Playwright Browsers
  run: pwsh tests/Sorcha.UI.E2E.Tests/bin/Debug/net10.0/playwright.ps1 install --with-deps

- name: Run E2E Tests
  run: dotnet test tests/Sorcha.UI.E2E.Tests --logger "trx"
  env:
    HEADED: false
```

### Test Result Upload

```yaml
- name: Upload test results
  uses: actions/upload-artifact@v4
  with:
    name: e2e-test-results
    path: '**/*.trx'
```

CI/CD validation loop:
1. Install browsers with `playwright.ps1 install --with-deps`
2. Run tests: `dotnet test tests/Sorcha.UI.E2E.Tests`
3. If tests fail, check Docker resource limits and port conflicts
4. Only proceed when all tests pass

---

## Debugging

### Common Failures

**Playwright browsers not installed:**
```
Error: Playwright executable doesn't exist
```

Fix:
```bash
cd tests/Sorcha.UI.E2E.Tests
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install
```

**Timeout during page load:**
```
TimeoutException: page.goto: Timeout 30000ms exceeded
```

Causes and fixes:
1. Services not started → Ensure Docker Desktop running
2. Port conflicts → Stop other services
3. Slow machine → Increase timeout in test

**JavaScript errors in console:**
Filter Blazor/WASM expected errors:
```csharp
var criticalErrors = errors.Where(e =>
    !e.Contains("WASM") &&
    !e.Contains("Blazor") &&
    !e.Contains("__webpack")).ToList();
```

### Trace Recording

```csharp
public override async Task InitializeAsync()
{
    await base.InitializeAsync();
    await Context.Tracing.StartAsync(new()
    {
        Title = TestContext.CurrentContext.Test.Name,
        Screenshots = true,
        Snapshots = true,
        Sources = true
    });
}

public override async Task DisposeAsync()
{
    await Context.Tracing.StopAsync(new()
    {
        Path = $"playwright-traces/{TestContext.CurrentContext.Test.Name}.zip"
    });
    await base.DisposeAsync();
}
```

---

## Test Organization

### Aspire Integration Pattern

```csharp
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class BlazorUITests : PageTest
{
    private DistributedApplication? _app;
    private string? _blazorUrl;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // Start full Aspire stack once for all tests
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Sorcha_AppHost>();
        _app = await appHost.BuildAsync();
        await _app.StartAsync();
        _blazorUrl = _app.GetEndpoint("blazor-client").ToString();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_app != null) await _app.DisposeAsync();
    }
}
```

### Test Categories

```csharp
// Navigation tests
[Test] public async Task HomePage_LoadsSuccessfully() { }
[Test] public async Task Navigation_WorksBetweenPages() { }

// Page-specific tests
[Test] public async Task BlueprintLibrary_Loads() { }
[Test] public async Task SchemaExplorer_Opens() { }
[Test] public async Task AdminPage_ShowsServices() { }

// UI/UX tests
[Test] public async Task MudBlazorComponents_Load() { }
[Test] public async Task NoJavaScriptErrors() { }
[Test] public async Task ResponsiveDesign_Works() { }
```

### Adding New Tests

Copy this template:
```csharp
[Test]
public async Task Feature_Scenario_ExpectedBehavior()
{
    // Arrange - Navigate to page
    await Page.GotoAsync($"{_blazorUrl}/mypage");
    await Page.WaitForLoadStateAsync();

    // Act - Interact with elements
    var button = Page.Locator("[data-testid='my-button']");
    if (await button.CountAsync() > 0)
    {
        await button.ClickAsync();
    }

    // Assert - Verify outcome
    await Expect(Page).ToHaveURLAsync(new Regex("/expected-path"));
}
```

New test checklist:
- [ ] Use descriptive test name: `Feature_Scenario_ExpectedBehavior`
- [ ] Wait for page load state
- [ ] Check element existence before interaction
- [ ] Use stable locators (test IDs or roles)
- [ ] Assert meaningful outcomes