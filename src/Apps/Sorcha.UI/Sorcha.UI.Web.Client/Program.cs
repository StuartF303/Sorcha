using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Sorcha.Blueprint.Schemas;
using Sorcha.UI.Core.Extensions;
using Sorcha.UI.Core.Services.Authentication;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register core services (authentication, encryption, configuration) with base address
builder.Services.AddCoreServices(builder.HostEnvironment.BaseAddress);

// Register authorization
builder.Services.AddAuthorizationCore();

// Add MudBlazor services
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.ShowTransitionDuration = 300;
    config.SnackbarConfiguration.HideTransitionDuration = 300;
});

// Add local storage for WASM pages (blueprints, schemas, user preferences)
builder.Services.AddBlazoredLocalStorage();

// Add schema library service with caching (for designer and schema library pages)
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

var host = builder.Build();

// Sync auth state from server cookie to WASM LocalStorage
// This picks up tokens from the server-side login flow
var authStateSync = host.Services.GetRequiredService<AuthStateSync>();
await authStateSync.SyncAuthStateAsync();

await host.RunAsync();
