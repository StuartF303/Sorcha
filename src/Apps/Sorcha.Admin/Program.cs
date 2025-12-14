using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Sorcha.Admin;
using Sorcha.Admin.Services;
using Sorcha.Admin.Services.Authentication;
using Sorcha.Admin.Services.Configuration;
using Sorcha.Admin.Services.Encryption;
using Sorcha.Admin.Services.Http;
using Sorcha.Blueprint.Schemas;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Authentication & Authorization
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(s =>
    s.GetRequiredService<CustomAuthenticationStateProvider>());

// Configuration Services
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();

// Authentication Services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<BrowserTokenCache>();

// Encryption Services
builder.Services.AddScoped<IEncryptionProvider, BrowserEncryptionProvider>();

// HTTP Services
builder.Services.AddScoped<AuthenticatedHttpMessageHandler>();

// Configure HttpClient with authentication
builder.Services.AddHttpClient("SorchaAPI")
    .AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();

// Default HttpClient (for Blazor components) uses authenticated client
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("SorchaAPI"));

// Add MudBlazor services
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.VisibleStateDuration = 5000; // 5 seconds
    config.SnackbarConfiguration.ShowTransitionDuration = 300;
    config.SnackbarConfiguration.HideTransitionDuration = 300;
});

// Add local storage for saving blueprints
builder.Services.AddBlazoredLocalStorage();

// Add event log service (singleton to persist events across navigation)
builder.Services.AddSingleton<EventLogService>();

// Add schema cache service (scoped to match ILocalStorageService)
builder.Services.AddScoped<ISchemaCacheService, LocalStorageSchemaCacheService>();

// Add schema library service with caching (scoped to match cache service)
builder.Services.AddScoped<SchemaLibraryService>(sp =>
{
    var cacheService = sp.GetRequiredService<ISchemaCacheService>();
    var schemaLibrary = new SchemaLibraryService(cacheService);

    // Add SchemaStore repository with HttpClient
    var httpClient = new HttpClient();
    schemaLibrary.AddRepository(new SchemaStoreRepository(httpClient));

    return schemaLibrary;
});

await builder.Build().RunAsync();
