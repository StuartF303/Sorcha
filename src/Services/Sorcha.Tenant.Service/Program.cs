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
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info.Title = "Sorcha Tenant Service API";
            document.Info.Version = "1.0.0";
            document.Info.Description = """
                # Tenant Service API

                ## Overview

                The Tenant Service provides **multi-tenant organization management** and **authentication/authorization** capabilities for the Sorcha distributed ledger platform. It serves as the central identity and access management (IAM) system, enabling secure, isolated workspaces for different organizations.

                ## Primary Use Cases

                - **Organization Management**: Create and manage tenant organizations with isolated data boundaries
                - **User Management**: Manage users, roles, and permissions within organizations
                - **Service Principal Authentication**: Issue JWT tokens for service-to-service authentication
                - **Access Control**: Role-based access control (RBAC) for all platform services

                ## Key Concepts

                ### Organizations (Tenants)
                Organizations are the top-level isolation boundary in Sorcha. Each organization has:
                - Unique organization ID
                - Isolated users and resources
                - Independent billing and quotas
                - Custom branding and configuration

                ### Users
                Users belong to one or more organizations and have:
                - Email-based authentication
                - Role assignments per organization
                - JWT tokens for API access
                - Activity audit trails

                ### Service Principals
                Service principals enable machine-to-machine authentication:
                - Client ID and client secret credentials
                - Scoped permissions per service
                - Token-based authentication (JWT)
                - Used by CLI tools, backend services, and integrations

                ### Authentication Model
                - **User Authentication**: Email/password with JWT tokens
                - **Service Authentication**: Client credentials flow (OAuth2-style)
                - **Token Management**: Refresh tokens, revocation, expiry
                - **Security**: BCrypt password hashing, secure token storage

                ## Getting Started

                ### 1. Authenticate as a Service Principal
                ```http
                POST /api/service-auth/token
                Content-Type: application/json

                {
                  "clientId": "your-client-id",
                  "clientSecret": "your-client-secret"
                }
                ```

                ### 2. Create an Organization
                ```http
                POST /api/organizations
                Authorization: Bearer {token}
                Content-Type: application/json

                {
                  "name": "Acme Corporation",
                  "displayName": "Acme Corp"
                }
                ```

                ### 3. Create Users
                ```http
                POST /api/organizations/{orgId}/users
                Authorization: Bearer {token}
                Content-Type: application/json

                {
                  "email": "admin@acme.com",
                  "firstName": "Admin",
                  "lastName": "User"
                }
                ```

                ## Security Features

                - ✅ JWT-based authentication with configurable expiry
                - ✅ BCrypt password hashing (work factor: 12)
                - ✅ Token revocation and refresh
                - ✅ Rate limiting per IP address
                - ✅ OWASP security headers
                - ✅ Audit logging for all operations
                - ✅ CORS configuration for web clients

                ## Target Audience

                - **System Administrators**: Managing organizations and users
                - **DevOps Engineers**: Configuring service principals for automation
                - **Integration Developers**: Building applications that authenticate with Sorcha
                - **CLI Users**: Authenticating command-line tools

                ## Related Services

                - **Blueprint Service**: Uses Tenant Service for user authentication
                - **Wallet Service**: Associates wallets with tenant organizations
                - **Register Service**: Isolates transaction registers by tenant
                - **Peer Service**: Network access control via tenant validation
                """;
            if (document.Info.Contact == null)
            {
                document.Info.Contact = new() { };
            }
            document.Info.Contact.Name = "Sorcha Platform Team";
            document.Info.Contact.Url = new Uri("https://github.com/siccar-platform/sorcha");

            if (document.Info.License == null)
            {
                document.Info.License = new() { };
            }
            document.Info.License.Name = "MIT License";
            document.Info.License.Url = new Uri("https://opensource.org/licenses/MIT");

            return Task.CompletedTask;
        });
    });

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

    // Add JWT authentication (shared across all services with auto-key generation)
    builder.AddJwtAuthentication();

    // Configure JwtConfiguration for token issuance (used by TokenService)
    builder.Services.ConfigureJwtForTokenIssuance(builder.Configuration);

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
    app.MapBootstrapEndpoints();
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
