using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sorcha.UI.Core.Extensions;
using Sorcha.UI.Core.Services.Authentication;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register core services (authentication, encryption, configuration) with base address
builder.Services.AddCoreServices(builder.HostEnvironment.BaseAddress);

// Register authorization
builder.Services.AddAuthorizationCore();

var host = builder.Build();

// Sync auth state from server cookie to WASM LocalStorage
// This picks up tokens from the server-side login flow
var authStateSync = host.Services.GetRequiredService<AuthStateSync>();
await authStateSync.SyncAuthStateAsync();

await host.RunAsync();
