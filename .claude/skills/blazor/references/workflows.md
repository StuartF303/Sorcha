# Blazor Workflows Reference

## Contents
- Service Registration
- Adding New Pages
- Creating Components
- Testing Components
- Debugging Workflows

---

## Service Registration

### Server-Side (Sorcha.Admin/Program.cs)

```csharp
// src/Apps/Sorcha.Admin/Program.cs:24-91
// 1. Razor Components with hybrid rendering
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// 2. Authentication (cookie-based for Blazor Server)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Cookies";
})
.AddCookie("Cookies", options =>
{
    options.LoginPath = "/login";
    options.Cookie.Name = "Sorcha.Auth";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

// 3. Auth state provider
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => 
    sp.GetRequiredService<CustomAuthenticationStateProvider>());

// 4. HttpClient for API Gateway
builder.Services.AddHttpClient("SorchaAPI", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var gatewayUrl = config["services:api-gateway:https:0"]
                     ?? "https://localhost:8061";
    client.BaseAddress = new Uri(gatewayUrl);
});

// 5. MudBlazor
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
});
```

### Client-Side (Sorcha.Admin.Client/Program.cs)

```csharp
// src/Apps/Sorcha.Admin/Sorcha.Admin.Client/Program.cs:8-48
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Authorization only (no authentication - read from server)
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, 
    PersistentAuthenticationStateProvider>();

// HttpClient uses host base address
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();

await builder.Build().RunAsync();
```

### Middleware Order (Critical)

```csharp
// src/Apps/Sorcha.Admin/Program.cs:132-164
app.UseForwardedHeaders();      // 1. FIRST - proxy headers
app.MapStaticAssets();          // 2. Static files
app.UseSession();               // 3. Session BEFORE auth
app.UseAuthentication();        // 4. Auth
app.UseAuthorization();         // 5. Authz
app.UseAntiforgery();           // 6. CSRF protection

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Sorcha.Admin.Client._Imports).Assembly);
```

---

## Adding New Pages

### Checklist: Add Authenticated Page

Copy this checklist and track progress:
- [ ] Create `.razor` file in `Pages/` folder
- [ ] Add `@page "/route"` directive
- [ ] Choose render mode based on requirements
- [ ] Add `@attribute [Authorize]` if auth required
- [ ] Add `@attribute [Authorize(Roles = "...")]` for role-based access
- [ ] Inject required services
- [ ] Add to navigation (`NavMenu.razor`)

### Page Template

```razor
@page "/my-feature"
@rendermode @(new InteractiveServerRenderMode(prerender: false))
@attribute [Authorize]
@inject HttpClient Http
@inject ISnackbar Snackbar

<PageTitle>My Feature - Sorcha Platform</PageTitle>

<MudText Typo="Typo.h4" Class="mb-4">My Feature</MudText>

@if (_isLoading)
{
    <MudProgressLinear Indeterminate="true" />
}
else if (_error != null)
{
    <MudAlert Severity="Severity.Error">@_error</MudAlert>
}
else
{
    @* Content here *@
}

@code {
    private bool _isLoading = true;
    private string? _error;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                // Load data
            }
            catch (Exception ex)
            {
                _error = ex.Message;
            }
            finally
            {
                _isLoading = false;
                StateHasChanged();
            }
        }
    }
}
```

---

## Creating Components

### Checklist: Create Reusable Component

Copy this checklist and track progress:
- [ ] Create `.razor` file in `Components/` folder
- [ ] Define `[Parameter]` properties for inputs
- [ ] Define `[Parameter] EventCallback<T>` for outputs
- [ ] Handle null/empty states
- [ ] Add loading state if async
- [ ] Document with XML comments

### Component Template

```razor
@* Components/MyComponent.razor *@
<MudPaper Elevation="2" Class="pa-4">
    @if (Data == null)
    {
        <MudText Color="Color.Secondary">No data available</MudText>
    }
    else
    {
        <MudText Typo="Typo.h6">@Data.Title</MudText>
        <MudButton OnClick="HandleClick">Action</MudButton>
    }
</MudPaper>

@code {
    /// <summary>The data to display.</summary>
    [Parameter] public DataModel? Data { get; set; }

    /// <summary>Invoked when action button clicked.</summary>
    [Parameter] public EventCallback<DataModel> OnAction { get; set; }

    private async Task HandleClick()
    {
        if (Data != null)
            await OnAction.InvokeAsync(Data);
    }
}
```

---

## Testing Components

### Unit Test Pattern with bUnit

```csharp
// tests/Sorcha.Admin.Tests/Components/MyComponentTests.cs
public class MyComponentTests : TestContext
{
    [Fact]
    public void Render_WithData_DisplaysTitle()
    {
        // Arrange
        var data = new DataModel { Title = "Test Title" };
        
        // Act
        var cut = RenderComponent<MyComponent>(parameters => parameters
            .Add(p => p.Data, data));
        
        // Assert
        cut.Find("h6").TextContent.Should().Be("Test Title");
    }

    [Fact]
    public void Render_WithoutData_ShowsEmptyMessage()
    {
        // Act
        var cut = RenderComponent<MyComponent>();
        
        // Assert
        cut.Markup.Should().Contain("No data available");
    }

    [Fact]
    public async Task Click_ActionButton_InvokesCallback()
    {
        // Arrange
        var data = new DataModel { Title = "Test" };
        DataModel? captured = null;
        
        var cut = RenderComponent<MyComponent>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.OnAction, EventCallback.Factory.Create<DataModel>(this, d => captured = d)));
        
        // Act
        await cut.Find("button").ClickAsync(new MouseEventArgs());
        
        // Assert
        captured.Should().Be(data);
    }
}
```

---

## Debugging Workflows

### Validate Auth State

1. Open browser DevTools (F12)
2. Check Console for auth state logs:
   ```
   [CustomAuthStateProvider] GetAuthenticationStateAsync called
   [CustomAuthStateProvider] Active profile: docker
   [CustomAuthStateProvider] Access token retrieved (length: 1234)
   ```

### Debug Render Mode Issues

1. Check render mode in browser:
   - Server: `<script src="_framework/blazor.server.js">`
   - WASM: `<script src="_framework/blazor.webassembly.js">`
2. Verify component location matches render mode
3. Check `_Imports.razor` for correct usings

### Debug HTTP Calls

```csharp
// Add logging to HttpClient
builder.Services.AddHttpClient("SorchaAPI", client => { ... })
    .AddHttpMessageHandler<LoggingHandler>();

public class LoggingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        Console.WriteLine($"[HTTP] {request.Method} {request.RequestUri}");
        var response = await base.SendAsync(request, ct);
        Console.WriteLine($"[HTTP] Response: {response.StatusCode}");
        return response;
    }
}
```

### Iterate-Until-Pass: Build Validation

1. Make changes to `.razor` files
2. Validate: `dotnet build src/Apps/Sorcha.Admin/`
3. If build fails, check:
   - Missing `@using` directives
   - Incorrect render mode for component location
   - Missing service registrations
4. Only proceed when build passes

### Iterate-Until-Pass: Runtime Testing

1. Start services: `docker-compose up -d`
2. Run admin: `dotnet run --project src/Apps/Sorcha.Admin`
3. Navigate to page in browser
4. Check browser console for errors
5. If errors occur:
   - Check network tab for failed requests
   - Verify auth state in console logs
   - Check for JSInterop errors during prerender
6. Repeat until page works correctly