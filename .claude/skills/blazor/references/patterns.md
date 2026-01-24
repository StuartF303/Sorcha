# Blazor Patterns Reference

## Contents
- Render Mode Patterns
- Authentication Patterns
- State Management Patterns
- Component Communication
- Anti-Patterns

---

## Render Mode Patterns

### Choosing the Right Render Mode

| Page Type | Render Mode | Reason |
|-----------|-------------|--------|
| Complex interactive (Designer) | `InteractiveWebAssembly` | Full client-side state, no circuit overhead |
| Admin with real-time | `InteractiveServer(prerender: false)` | SignalR updates, secure server access |
| Public pages | Static (no directive) | Fast initial load, no interactivity needed |
| Auth-required with SSR | `InteractiveServer(prerender: false)` | Avoid auth state during prerender |

```razor
@* src/Apps/Sorcha.Admin/Pages/Designer.razor:12 *@
@page "/designer"
@rendermode InteractiveWebAssembly
@attribute [Authorize]

@* src/Apps/Sorcha.Admin/Pages/Admin/Audit.razor:2-3 *@
@page "/admin/audit"
@rendermode @(new InteractiveServerRenderMode(prerender: false))
@attribute [Authorize(Roles = "Administrator,SystemAdmin")]
```

### WARNING: Prerendering with Authentication

**The Problem:**

```razor
// BAD - Auth state unavailable during prerender
@page "/dashboard"
@rendermode InteractiveServer
@attribute [Authorize]
```

**Why This Breaks:**
1. During prerender, `AuthenticationState` is anonymous
2. JSInterop unavailable - cannot read tokens from storage
3. Component renders twice - once anonymous, once authenticated

**The Fix:**

```razor
// GOOD - Disable prerender for auth-required pages
@page "/dashboard"
@rendermode @(new InteractiveServerRenderMode(prerender: false))
@attribute [Authorize]
```

---

## Authentication Patterns

### Server-Side Auth State Provider

```csharp
// src/Apps/Sorcha.Admin/Services/Authentication/CustomAuthenticationStateProvider.cs:33-94
public override async Task<AuthenticationState> GetAuthenticationStateAsync()
{
    try
    {
        var profile = await _configService.GetActiveProfileAsync();
        if (profile == null)
            return CreateAnonymousState();

        var token = await _authService.GetAccessTokenAsync(profile.Name);
        if (string.IsNullOrEmpty(token))
            return CreateAnonymousState();

        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
    catch (InvalidOperationException)
    {
        // JSInterop not available during prerendering
        return CreateAnonymousState();
    }
}
```

### WASM Auth State (Persistent)

```csharp
// src/Apps/Sorcha.Admin/Sorcha.Admin.Client/Services/Authentication/PersistentAuthenticationStateProvider.cs:19-49
public PersistentAuthenticationStateProvider(PersistentComponentState persistentState)
{
    if (!persistentState.TryTakeFromJson<UserInfo>(nameof(UserInfo), out var userInfo))
    {
        _authenticationStateTask = _unauthenticatedTask;
        return;
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, userInfo.UserId),
        new(ClaimTypes.Name, userInfo.UserName),
        new(ClaimTypes.Email, userInfo.Email)
    };
    
    foreach (var role in userInfo.Roles)
        claims.Add(new Claim(ClaimTypes.Role, role));

    _authenticationStateTask = Task.FromResult(
        new AuthenticationState(new ClaimsPrincipal(
            new ClaimsIdentity(claims, nameof(PersistentAuthenticationStateProvider)))));
}
```

### Login Flow Pattern

```razor
@* src/Apps/Sorcha.Admin/Components/Authentication/LoginDialog.razor:104-152 *@
@code {
    private async Task Login()
    {
        _isLoading = true;
        _showError = false;
        StateHasChanged();

        try
        {
            var request = new LoginRequest
            {
                Username = _username,
                Password = _password,
                ClientId = "sorcha-admin"
            };

            await AuthService.LoginAsync(request, _selectedProfile);
            await ConfigService.SetActiveProfileAsync(_selectedProfile);
            
            // CRITICAL: Notify auth state changed
            AuthStateProvider.NotifyAuthenticationStateChanged();

            Snackbar.Add("Login successful!", Severity.Success);
            MudDialog?.Close(DialogResult.Ok(true));
        }
        catch (UnauthorizedAccessException)
        {
            _showError = true;
            _errorMessage = "Invalid username or password.";
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }
}
```

---

## State Management Patterns

### Loading State Pattern

```razor
@* src/Apps/Sorcha.Admin/Components/SystemStatusCard.razor:28-84 *@
@if (isLoading && !hasLoadedOnce)
{
    <MudProgressCircular Indeterminate="true" Size="Size.Small" />
    <MudText Typo="Typo.body2">Checking system status...</MudText>
}
else if (systemStatus != null)
{
    <MudAlert Severity="@GetOverallSeverity()">
        @GetOverallStatusText()
    </MudAlert>
}
else if (errorMessage != null)
{
    <MudAlert Severity="Severity.Warning">@errorMessage</MudAlert>
}

@code {
    private SystemStatusDto? systemStatus;
    private string? errorMessage;
    private bool isLoading;
    private bool hasLoadedOnce;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await RefreshStatus();
    }
}
```

### WARNING: Calling StateHasChanged in Loops

**The Problem:**

```csharp
// BAD - Excessive re-renders
foreach (var item in items)
{
    ProcessItem(item);
    StateHasChanged(); // Called 100 times!
}
```

**The Fix:**

```csharp
// GOOD - Single re-render after batch
foreach (var item in items)
{
    ProcessItem(item);
}
StateHasChanged(); // Called once
```

---

## Component Communication

### CascadingValue for Diagram Context

```razor
@* src/Apps/Sorcha.Admin/Pages/Designer.razor:88-92 *@
@if (Diagram != null)
{
    <CascadingValue Value="Diagram" IsFixed="true">
        <DiagramCanvas></DiagramCanvas>
    </CascadingValue>
}
```

### Event Callbacks

```csharp
// Parent passes callback to child
<PropertiesPanel Blueprint="CurrentBlueprint"
                 SelectedNode="SelectedNode"
                 OnBlueprintSaved="OnBlueprintPropertiesSaved"
                 OnActionSaved="OnActionPropertiesSaved" />

// Child invokes callback
[Parameter] public EventCallback<Blueprint> OnBlueprintSaved { get; set; }

private async Task Save()
{
    await OnBlueprintSaved.InvokeAsync(blueprint);
}
```

---

## Anti-Patterns

### WARNING: Direct Parameter Modification

**The Problem:**

```csharp
// BAD - Violates one-way data flow
[Parameter] public bool Expanded { get; set; }

private void Toggle()
{
    Expanded = !Expanded; // Direct modification!
}
```

**The Fix:**

```csharp
// GOOD - Use EventCallback for two-way binding
[Parameter] public bool Expanded { get; set; }
[Parameter] public EventCallback<bool> ExpandedChanged { get; set; }

private async Task ToggleAsync()
{
    await ExpandedChanged.InvokeAsync(!Expanded);
}
```

### WARNING: JSInterop During Prerendering

**When You Might Be Tempted:**
- Reading from localStorage in OnInitializedAsync
- Calling JavaScript functions for initial state

**The Fix:**

```csharp
// Use OnAfterRenderAsync for JSInterop
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        // Safe to use JSInterop here
        var value = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "key");
    }
}
```

### WARNING: Missing CascadingAuthenticationState

**The Problem:**

```razor
@* BAD - AuthorizeView won't work *@
<Router AppAssembly="@typeof(Program).Assembly">
    <AuthorizeRouteView ... />
</Router>
```

**The Fix:**

```razor
@* GOOD - Wrap in CascadingAuthenticationState *@
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(Program).Assembly">
        <AuthorizeRouteView ... />
    </Router>
</CascadingAuthenticationState>