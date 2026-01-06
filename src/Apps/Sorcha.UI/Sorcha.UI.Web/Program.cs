using Microsoft.AspNetCore.DataProtection;
using Sorcha.UI.Web.Client.Pages;
using Sorcha.UI.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Configure Data Protection to use shared volume in Docker
var dataProtectionPath = Path.Combine("/home/app/.aspnet/DataProtection-Keys");
if (Directory.Exists("/home/app/.aspnet"))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Add authorization services
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Add CSP headers for Blazor WebAssembly
app.Use(async (context, next) =>
{
    // Content Security Policy for Blazor WASM
    // Note: 'unsafe-eval' is required for WebAssembly, 'unsafe-inline' for Blazor framework
    var csp = string.Join("; ", new[]
    {
        "default-src 'self'",
        "script-src 'self' 'unsafe-eval' 'wasm-unsafe-eval'", // WASM requires unsafe-eval
        "style-src 'self' 'unsafe-inline'", // Blazor uses inline styles
        "img-src 'self' data: https:",
        "font-src 'self' data:",
        "connect-src 'self' https://localhost:* http://localhost:* wss://localhost:* ws://localhost:*", // Allow SignalR and API calls
        "worker-src 'self' blob:", // For Blazor WASM workers
        "frame-ancestors 'none'", // Prevent clickjacking
        "base-uri 'self'",
        "form-action 'self'"
    });

    context.Response.Headers["Content-Security-Policy"] = csp;

    // Other security headers
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    await next();
});

// Serve static files and Blazor WebAssembly framework files FIRST
// This must be before routing to prevent Blazor from intercepting static file requests
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseRouting();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Sorcha.UI.Web.Client._Imports).Assembly);

app.Run();
