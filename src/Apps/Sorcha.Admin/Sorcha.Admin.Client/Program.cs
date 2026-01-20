using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Sorcha.Blueprint.Schemas;
using Sorcha.Admin.Client.Services;
using Sorcha.Admin.Services.Authentication;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Authentication & Authorization (Client-side for WASM pages)
builder.Services.AddAuthorizationCore();

// Use PersistentAuthenticationStateProvider to read auth state from server
builder.Services.AddScoped<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();

// HttpClient configuration
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// SignalR Actions Hub connection for real-time action notifications
// Uses the same base address as the HTTP client (goes through API Gateway)
builder.Services.AddScoped<ActionsHubConnection>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ActionsHubConnection>>();
    // Use the host environment base address which routes through API Gateway
    var baseUrl = builder.HostEnvironment.BaseAddress.TrimEnd('/');
    return new ActionsHubConnection(baseUrl, logger);
});

// Add MudBlazor services
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.ShowTransitionDuration = 300;
    config.SnackbarConfiguration.HideTransitionDuration = 300;
});

// Add local storage for WASM pages (offline capability)
builder.Services.AddBlazoredLocalStorage();

// Add schema library service with caching (for designer)
builder.Services.AddScoped<ISchemaCacheService, LocalStorageSchemaCacheService>();
builder.Services.AddScoped<SchemaLibraryService>(sp =>
{
    var cacheService = sp.GetRequiredService<ISchemaCacheService>();
    var schemaLibrary = new SchemaLibraryService(cacheService);

    // Add SchemaStore repository with HttpClient
    var httpClient = sp.GetRequiredService<HttpClient>();
    schemaLibrary.AddRepository(new SchemaStoreRepository(httpClient));

    return schemaLibrary;
});

await builder.Build().RunAsync();
