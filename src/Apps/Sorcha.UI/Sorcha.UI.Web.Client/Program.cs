using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sorcha.UI.Core.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register core services (authentication, encryption, configuration) with base address
builder.Services.AddCoreServices(builder.HostEnvironment.BaseAddress);

// Register authorization
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
