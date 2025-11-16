// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.WalletService.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (health checks, OpenTelemetry, service discovery)
builder.AddServiceDefaults();

// Add Wallet Service infrastructure and domain services
builder.Services.AddWalletService();

// Add API services
builder.Services.AddControllers();

// Configure OpenAPI (built-in .NET 10)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Add Wallet Service health checks
builder.Services.AddHealthChecks()
    .AddWalletServiceHealthChecks();

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

// Map default Aspire endpoints (/health, /alive)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("DevelopmentPolicy");
}

app.UseHttpsRedirection();

// TODO: Add authentication middleware
// app.UseAuthentication();
// app.UseAuthorization();

app.MapControllers();

app.Run();

// Make the implicit Program class public for integration tests
public partial class Program { }
