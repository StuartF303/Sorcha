using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Blazored.LocalStorage;
using MudBlazor.Services;
using Sorcha.Blueprint.Schemas;
using Sorcha.UI.Web.Client.Pages;
using Sorcha.UI.Web.Components;
using Sorcha.UI.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Data Protection to use shared volume in Docker
var dataProtectionPath = Path.Combine("/home/app/.aspnet/DataProtection-Keys");
if (Directory.Exists("/home/app/.aspnet"))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));
}

// Add HttpContextAccessor for server-side auth state
builder.Services.AddHttpContextAccessor();

// Add HttpClient for backend API calls
builder.Services.AddHttpClient("BackendApi", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add MudBlazor services
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
});

// Add Blazored LocalStorage for browser storage
builder.Services.AddBlazoredLocalStorage();

// Add Schema Library services
builder.Services.AddScoped<ISchemaCacheService, LocalStorageSchemaCacheService>();
builder.Services.AddScoped<SchemaLibraryService>(sp =>
{
    var cacheService = sp.GetRequiredService<ISchemaCacheService>();
    var schemaLibrary = new SchemaLibraryService(cacheService);
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    schemaLibrary.AddRepository(new SchemaStoreRepository(httpClient));
    return schemaLibrary;
});

// Add cookie authentication (used for tracking login state and token handoff)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Sorcha.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Add server-side auth state provider for SSR nav awareness
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

// Add Razor components with both Server and WebAssembly support
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// Configure ForwardedHeaders for Docker/reverse proxy support
// This ensures HTTPS redirects use the external hostname (e.g., localhost:443)
// instead of internal Docker hostnames (e.g., sorcha-ui-web:8443)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                               ForwardedHeaders.XForwardedProto |
                               ForwardedHeaders.XForwardedHost;
    // Trust the API Gateway proxy - in Docker, we trust all proxies
    // For production, you should restrict this to known proxy IPs
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Handle forwarded headers from reverse proxy (API Gateway)
// MUST be called before UseHttpsRedirection() so redirects use external hostname
app.UseForwardedHeaders();

// Security headers
app.Use(async (context, next) =>
{
    var csp = string.Join("; ", new[]
    {
        "default-src 'self'",
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' 'wasm-unsafe-eval'",
        "style-src 'self' 'unsafe-inline'",
        "img-src 'self' data: https:",
        "font-src 'self' data:",
        "connect-src 'self' https://localhost:* http://localhost:* wss://localhost:* ws://localhost:*",
        "worker-src 'self' blob:",
        "frame-ancestors 'none'",
        "base-uri 'self'",
        "form-action 'self'"
    });

    context.Response.Headers["Content-Security-Policy"] = csp;
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    await next();
});

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// HTTPS redirection disabled for Docker development
// Enable only in production when certificates are properly configured
if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Sorcha.UI.Web.Client._Imports).Assembly);

app.Run();
