// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Wallet.Service.Extensions;
using Sorcha.Wallet.Service.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (health checks, OpenTelemetry, service discovery)
builder.AddServiceDefaults();

// Add Wallet Service infrastructure and domain services
builder.Services.AddWalletService(builder.Configuration);

// Add OpenAPI services (built-in .NET 10)
builder.Services.AddOpenApi();

// Add Wallet Service health checks
builder.Services.AddHealthChecks()
    .AddWalletServiceHealthChecks(builder.Configuration);

// Configure CORS (for development)
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Apply database migrations automatically (only if PostgreSQL is configured)
await app.Services.ApplyWalletDatabaseMigrationsAsync();

// Map default Aspire endpoints (/health, /alive)
app.MapDefaultEndpoints();

// Add OWASP security headers (SEC-004)
app.UseApiSecurityHeaders();

// Configure OpenAPI (available in all environments for API consumers)
app.MapOpenApi();

// Enable CORS policy in development
if (app.Environment.IsDevelopment())
{
    app.UseCors("DevelopmentPolicy");
}

app.UseHttpsRedirection();

// TODO: Add authentication middleware when ready
// app.UseAuthentication();
// app.UseAuthorization();

// Map Wallet API endpoints
app.MapWalletEndpoints();
app.MapDelegationEndpoints();

app.Run();

// Make the implicit Program class public for integration tests
public partial class Program { }
