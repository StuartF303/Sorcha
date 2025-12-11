// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using Sorcha.Tenant.Service.Endpoints;
using Sorcha.Tenant.Service.Extensions;

// Configure Serilog first (before building the application)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Application", "Sorcha.Tenant.Service")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Sorcha Tenant Service");

    var builder = WebApplication.CreateBuilder(args);

    // Add .NET Aspire service defaults (includes service discovery, health checks, and default observability)
    builder.AddServiceDefaults();

    // Replace bootstrap Serilog with full configuration from appsettings.json
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("Application", "Sorcha.Tenant.Service"));

    // Add OpenAPI and Scalar API documentation
    builder.Services.AddOpenApi();

    // Add controllers and minimal API support
    builder.Services.AddEndpointsApiExplorer();

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    // Add Tenant Service dependencies (database, repositories, Redis, token revocation)
    builder.Services.AddTenantServices(builder.Configuration);

    // Add JWT authentication
    builder.Services.AddTenantAuthentication(builder.Configuration);

    // Add authorization policies
    builder.Services.AddTenantAuthorization();

    // Add rate limiting
    builder.Services.AddRateLimiter(options =>
    {
        var rateLimitingEnabled = builder.Configuration.GetValue<bool>("RateLimiting:EnableRateLimiting");
        if (rateLimitingEnabled)
        {
            var permitLimit = builder.Configuration.GetValue<int>("RateLimiting:PermitLimit");
            var window = builder.Configuration.GetValue<int>("RateLimiting:Window");

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromSeconds(window),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = builder.Configuration.GetValue<int>("RateLimiting:QueueLimit")
                    }));
        }
    });

    // Add health checks (PostgreSQL and Redis when configured)
    builder.Services.AddTenantHealthChecks(builder.Configuration);

    // Add database initializer for automatic migration and seeding
    // Creates default organization (sorcha.local) and admin user on startup
    builder.Services.AddDatabaseInitializer();

    var app = builder.Build();

    // Map default endpoints (OpenAPI, health checks)
    app.MapDefaultEndpoints();

    // Add OWASP security headers (SEC-004)
    app.UseApiSecurityHeaders();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();

        // Add Scalar API documentation UI
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("Sorcha Tenant Service API")
                .WithTheme(ScalarTheme.Purple)
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });
    }

    // Use Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress);
        };
    });

    app.UseCors();

    app.UseHttpsRedirection();

    // Authentication and authorization middleware
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseRateLimiter();

    // Map API endpoint groups
    app.MapOrganizationEndpoints();
    app.MapAuthEndpoints();
    app.MapServiceAuthEndpoints();

    // Health check is provided by MapDefaultEndpoints() which maps /health and /alive
    // The standard Aspire health endpoint returns plain text "Healthy" or "Unhealthy"

    Log.Information("Sorcha Tenant Service started successfully");
    Log.Information("Scalar API documentation available at: https://localhost:7080/scalar");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Sorcha Tenant Service failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
