# Sorcha UI Patterns Reference

## Contents
- Page Implementation Patterns
- Page Object Patterns
- Test Patterns
- Selector Strategy
- Anti-Patterns

---

## Page Implementation Patterns

### Standard Authenticated Page

Every authenticated page follows this structure:

```razor
@* src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Example.razor *@
@page "/example"
@layout MainLayout
@rendermode @(new InteractiveWebAssemblyRenderMode(prerender: false))
@attribute [Authorize]
@using Microsoft.AspNetCore.Components.Authorization
@using Sorcha.UI.Web.Client.Components.Layout
@inject HttpClient Http

<PageTitle>Example - Sorcha</PageTitle>

<CascadingAuthenticationState>
    <AuthorizeView>
        <Authorized>
            @* Page content here *@
        </Authorized>
        <NotAuthorized>
            <Sorcha.UI.Web.Client.Components.RedirectToLogin />
        </NotAuthorized>
    </AuthorizeView>
</CascadingAuthenticationState>
```

Reference: `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Home.razor`

### Loading → Data → Empty State Pattern

Use for any page that fetches data from an API:

```razor
@if (_isLoading && !_hasLoadedOnce)
{
    <MudProgressCircular Indeterminate="true" data-testid="loading-spinner" />
}
else if (_errorMessage != null)
{
    <MudAlert Severity="Severity.Error" data-testid="error-alert">@_errorMessage</MudAlert>
}
else if (_items.Count == 0)
{
    <MudPaper Elevation="0" Class="pa-8 d-flex flex-column align-center" data-testid="empty-state">
        <MudIcon Icon="@Icons.Material.Filled.Inbox" Size="Size.Large" Class="mb-4" />
        <MudText Typo="Typo.h6">No items found</MudText>
        <MudText Typo="Typo.body2" Class="mud-text-secondary">
            Create your first item to get started.
        </MudText>
    </MudPaper>
}
else
{
    @foreach (var item in _items)
    {
        <MudCard data-testid="item-card-@item.Id" Class="mb-3">
            <MudCardContent>
                <MudText Typo="Typo.h6">@item.Name</MudText>
            </MudCardContent>
        </MudCard>
    }
}

@code {
    private List<ItemDto> _items = [];
    private string? _errorMessage;
    private bool _isLoading;
    private bool _hasLoadedOnce;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _isLoading = true;
        StateHasChanged();
        try
        {
            _items = await Http.GetFromJsonAsync<List<ItemDto>>("/api/items") ?? [];
            _hasLoadedOnce = true;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load items: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }
}
```

### data-testid Naming Convention

Apply `data-testid` attributes to every key element. Use these naming patterns:

| Element | Pattern | Example |
|---------|---------|---------|
| Page container | `{page}-container` | `data-testid="wallet-list-container"` |
| Cards (collection) | `{item}-card-{id}` | `data-testid="wallet-card-abc123"` |
| Loading spinner | `{page}-loading` | `data-testid="wallet-loading"` |
| Empty state | `{page}-empty-state` | `data-testid="wallet-empty-state"` |
| Error alert | `{page}-error` | `data-testid="wallet-error"` |
| Action buttons | `{action}-{item}-btn` | `data-testid="create-wallet-btn"` |
| Search input | `{page}-search` | `data-testid="wallet-search"` |
| Table | `{page}-table` | `data-testid="wallet-table"` |
| Stat cards | `stat-{metric}` | `data-testid="stat-blueprints"` |

---

## Page Object Patterns

### Standard Page Object Structure

```csharp
// tests/Sorcha.UI.E2E.Tests/PageObjects/{PageName}Page.cs
using Microsoft.Playwright;
using Sorcha.UI.E2E.Tests.Infrastructure;
using Sorcha.UI.E2E.Tests.PageObjects.Shared;

namespace Sorcha.UI.E2E.Tests.PageObjects;

public class ExamplePage
{
    private readonly IPage _page;
    public ExamplePage(IPage page) => _page = page;

    // 1. data-testid selectors (primary - most stable)
    public ILocator Container => MudBlazorHelpers.TestId(_page, "example-container");
    public ILocator ItemCards => MudBlazorHelpers.TestIdPrefix(_page, "item-card-");
    public ILocator EmptyState => MudBlazorHelpers.TestId(_page, "example-empty-state");
    public ILocator LoadingSpinner => MudBlazorHelpers.TestId(_page, "example-loading");
    public ILocator ErrorAlert => MudBlazorHelpers.TestId(_page, "example-error");
    public ILocator CreateButton => MudBlazorHelpers.TestId(_page, "create-item-btn");

    // 2. MudBlazor class selectors (fallback)
    public ILocator Table => MudBlazorHelpers.Table(_page);
    public ILocator Cards => MudBlazorHelpers.Cards(_page);
    public ILocator Dialog => MudBlazorHelpers.Dialog(_page);

    // 3. Navigation
    public async Task NavigateAsync()
    {
        await _page.GotoAsync($"{TestConstants.UiWebUrl}/app/example");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await MudBlazorHelpers.WaitForBlazorAsync(_page);
    }

    // 4. State queries
    public async Task<int> GetItemCountAsync() => await ItemCards.CountAsync();
    public async Task<bool> IsLoadingAsync() =>
        await LoadingSpinner.CountAsync() > 0 && await LoadingSpinner.IsVisibleAsync();
    public async Task<bool> IsEmptyAsync() =>
        await EmptyState.CountAsync() > 0 && await EmptyState.IsVisibleAsync();
    public async Task<bool> HasErrorAsync() =>
        await ErrorAlert.CountAsync() > 0 && await ErrorAlert.IsVisibleAsync();
    public async Task<string?> GetErrorMessageAsync() =>
        await HasErrorAsync() ? await ErrorAlert.TextContentAsync() : null;

    // 5. Actions
    public async Task ClickCreateAsync()
    {
        await CreateButton.ClickAsync();
        await _page.WaitForTimeoutAsync(TestConstants.ShortWait);
    }

    public async Task SearchAsync(string term)
    {
        var search = MudBlazorHelpers.TestId(_page, "example-search");
        await search.FillAsync(term);
        await _page.WaitForTimeoutAsync(TestConstants.ShortWait);
    }
}
```

### Page Object for existing pages (using LoginPage as reference)

Reference: `tests/Sorcha.UI.E2E.Tests/PageObjects/LoginPage.cs`
Reference: `tests/Sorcha.UI.E2E.Tests/PageObjects/DashboardPage.cs`
Reference: `tests/Sorcha.UI.E2E.Tests/PageObjects/NavigationComponent.cs`

---

## Test Patterns

### Standard Test Class (Authenticated)

```csharp
// tests/Sorcha.UI.E2E.Tests/Docker/{Feature}Tests.cs
using Sorcha.UI.E2E.Tests.Infrastructure;
using Sorcha.UI.E2E.Tests.PageObjects;

namespace Sorcha.UI.E2E.Tests.Docker;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("Docker")]
[Category("FeatureName")]      // Feature-specific category
[Category("Authenticated")]     // Requires login
public class FeatureTests : AuthenticatedDockerTestBase
{
    private FeaturePage _page = null!;

    [SetUp]
    public override async Task BaseSetUp()
    {
        await base.BaseSetUp();
        _page = new FeaturePage(Page);
    }

    // --- Smoke Tests (does it load?) ---

    [Test]
    [Retry(2)]
    public async Task Feature_LoadsWithoutErrors()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Feature);
        // DockerTestBase automatically checks: console errors, network 5xx, layout CSS
    }

    // --- Structure Tests (are elements present?) ---

    [Test]
    public async Task Feature_ShowsExpectedElements()
    {
        await _page.NavigateAsync();
        await Expect(_page.Container).ToBeVisibleAsync();
    }

    // --- Behavior Tests (do interactions work?) ---

    [Test]
    public async Task Feature_CreateButton_OpensDialog()
    {
        await _page.NavigateAsync();
        await _page.ClickCreateAsync();
        await Expect(_page.Dialog).ToBeVisibleAsync();
    }
}
```

### Standard Test Class (Unauthenticated)

```csharp
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("Docker")]
[Category("Auth")]
[Category("Smoke")]
public class PublicPageTests : DockerTestBase
{
    [Test]
    public async Task LandingPage_LoadsWithoutErrors()
    {
        await NavigateToAsync(TestConstants.PublicRoutes.Landing);
        var content = await Page.TextContentAsync("body");
        Assert.That(content, Is.Not.Null.And.Not.Empty);
    }
}
```

### Test Three Categories Per Page

Every page should have tests in these three categories:

| Category | Purpose | Example |
|----------|---------|---------|
| **Smoke** | Page loads, no JS errors, no crashes | `WalletList_LoadsWithoutErrors` |
| **Structure** | Expected elements present | `WalletList_ShowsCreateButton` |
| **Behavior** | Interactions produce expected results | `WalletList_CreateButton_NavigatesToCreatePage` |

### Responsive Design Tests

```csharp
[Test]
[TestCase(375, 667, "Mobile")]
[TestCase(768, 1024, "Tablet")]
[TestCase(1920, 1080, "Desktop")]
public async Task Feature_RendersAtViewport(int width, int height, string label)
{
    await Page.SetViewportSizeAsync(width, height);
    await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Feature);
    // Page content should render
    var content = await Page.TextContentAsync("body");
    Assert.That(content, Is.Not.Null.And.Not.Empty,
        $"Page should render at {label} ({width}x{height})");
}
```

---

## Selector Strategy

Priority order for element selection:

1. **`data-testid`** -- Most stable, explicit test contract
   ```csharp
   MudBlazorHelpers.TestId(page, "wallet-card-123")
   ```

2. **MudBlazor CSS classes** -- Stable across page changes, framework-provided
   ```csharp
   MudBlazorHelpers.Table(page)      // .mud-table
   MudBlazorHelpers.Cards(page)      // .mud-card
   MudBlazorHelpers.Dialog(page)     // .mud-dialog
   ```

3. **Semantic/ARIA selectors** -- Good for accessible elements
   ```csharp
   page.GetByRole(AriaRole.Button, new() { Name = "Create" })
   ```

4. **Text content** -- Last resort, fragile to label changes
   ```csharp
   page.Locator("button:has-text('Create')")
   ```

---

## Anti-Patterns

### WARNING: Repeating Login Boilerplate

**The Problem:**
```csharp
// BAD - Every test repeats 20+ lines of login code
[Test]
public async Task MyTest()
{
    await Page.GotoAsync(loginUrl);
    await Page.Locator("input[type='text']").FillAsync(email);
    await Page.Locator("input[type='password']").FillAsync(password);
    // ... 15 more lines ...
}
```

**The Fix:**
```csharp
// GOOD - Extend AuthenticatedDockerTestBase, login happens once
public class MyTests : AuthenticatedDockerTestBase
{
    [Test]
    public async Task MyTest()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.MyPage);
        // Already logged in
    }
}
```

### WARNING: Assert.Pass on Auth Redirects

**The Problem:**
```csharp
// BAD - Masks real failures by treating auth redirect as success
if (Page.Url.Contains("/login"))
{
    Assert.Pass("Requires auth - redirected");
    return;
}
```

**The Fix:**
```csharp
// GOOD - Use AuthenticatedDockerTestBase which handles auth
// Or for unauthenticated tests, assert the redirect explicitly:
Assert.That(Page.Url, Does.Contain("/auth/login"),
    "Protected page should redirect to login");
```

### WARNING: Hardcoded WaitForTimeoutAsync

**The Problem:**
```csharp
// BAD - Slow and flaky
await Page.WaitForTimeoutAsync(5000);
```

**The Fix:**
```csharp
// GOOD - Wait for specific condition
await MudBlazorHelpers.WaitForBlazorAsync(Page);
// or
await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
// or
await someLocator.WaitForAsync(new() { Timeout = TestConstants.ElementTimeout });
```

### WARNING: Missing data-testid Attributes

**The Problem:**
```razor
<!-- BAD - No test hook, tests use fragile selectors -->
<MudCard>
    <MudCardContent>@wallet.Name</MudCardContent>
</MudCard>
```

**The Fix:**
```razor
<!-- GOOD - Explicit test contract -->
<MudCard data-testid="wallet-card-@wallet.Id">
    <MudCardContent>@wallet.Name</MudCardContent>
</MudCard>
```
