using Blazored.LocalStorage;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor.Services;
using Sorcha.Admin.Components;
using Sorcha.Admin.Services;
using Sorcha.Admin.Services.Authentication;
using Sorcha.Admin.Services.Configuration;
using Sorcha.Blueprint.Schemas;

var builder = WebApplication.CreateBuilder(args);

// Configure forwarded headers for running behind a reverse proxy (API Gateway)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    // Trust all proxies (we're behind API Gateway in Docker)
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add Razor Components with Interactive Server and WebAssembly support
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Authentication & Authorization (Server-side)
// Use cookie authentication for Blazor Server pages
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Cookies";
})
.AddCookie("Cookies", options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
    options.Cookie.Name = "Sorcha.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Configuration Services (Server-side)
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();

// Server-side authentication state provider
// Register both the concrete type (for components that need NotifyAuthenticationStateChanged)
// and as the interface (for the framework)
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthenticationStateProvider>());

// Encryption Services (Server-side)
builder.Services.AddScoped<Sorcha.Admin.Services.Encryption.IEncryptionProvider,
    Sorcha.Admin.Services.Encryption.BrowserEncryptionProvider>();

// Authentication Services (Server-side)
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Token Cache - Use server-side session storage instead of browser localStorage
// This avoids Blazor Server circuit recreation issues where localStorage writes
// are queued in the old circuit's JavaScript and never complete before navigation
builder.Services.AddScoped<ITokenCache, ServerSideTokenCache>();

// HTTP Services - Configure HttpClient to use API Gateway
builder.Services.AddHttpClient("SorchaAPI", (sp, client) =>
{
    // Use service discovery to find API Gateway URL (provided by Aspire)
    // Falls back to configuration or localhost for non-Aspire scenarios
    var configuration = sp.GetRequiredService<IConfiguration>();
    var gatewayUrl = configuration["services:api-gateway:https:0"]
                     ?? configuration["services:api-gateway:http:0"]
                     ?? configuration["ApiGateway:BaseUrl"]
                     ?? "https://localhost:8061"; // Fallback for local dev

    client.BaseAddress = new Uri(gatewayUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Default HttpClient (for Server components)
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("SorchaAPI"));

// Add MudBlazor services
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.ShowTransitionDuration = 300;
    config.SnackbarConfiguration.HideTransitionDuration = 300;
});

// Add Blazored LocalStorage for server-side components (uses JSInterop)
builder.Services.AddBlazoredLocalStorage();

// Add event log service (singleton to persist events across requests)
builder.Services.AddSingleton<EventLogService>();

// Configure ASP.NET Core Session for server-side token caching
// This is required for ServerSideTokenCache to work across Blazor circuit recreation
builder.Services.AddDistributedMemoryCache(); // In-memory session storage (can be replaced with Redis for production)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8); // Match authentication cookie timeout
    options.Cookie.Name = "Sorcha.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.IsEssential = true; // Required for GDPR compliance
});

// Add Data Protection for encrypting session data
builder.Services.AddDataProtection();

// Add HttpContextAccessor for ServerSideTokenCache
builder.Services.AddHttpContextAccessor();

// Note: SchemaLibraryService is only needed for designer pages which run in WASM mode
// Client project will register it with LocalStorage cache

var app = builder.Build();

// Configure the HTTP request pipeline

// IMPORTANT: UseForwardedHeaders must be FIRST to properly handle proxy headers
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Map static assets BEFORE MapRazorComponents (required for Blazor framework files)
app.MapStaticAssets();

// Session middleware MUST come before authentication
app.UseSession();

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Map Razor components with both Server and WASM render modes
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Sorcha.Admin.Client._Imports).Assembly);

app.Run();
