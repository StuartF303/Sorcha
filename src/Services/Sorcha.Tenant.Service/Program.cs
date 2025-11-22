// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

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

    // TODO: Add database context (EF Core)
    // builder.Services.AddDbContext<TenantDbContext>(options =>
    // {
    //     var connectionString = builder.Configuration.GetConnectionString("TenantDatabase");
    //     var password = builder.Configuration["ConnectionStrings:Password"];
    //     // Inject password into connection string
    //     connectionString = connectionString?.Replace("Password=placeholder", $"Password={password}");
    //     options.UseNpgsql(connectionString);
    // });

    // TODO: Add Redis distributed cache
    // var redisConnection = builder.Configuration.GetValue<string>("Redis:ConnectionString");
    // builder.Services.AddStackExchangeRedisCache(options =>
    // {
    //     options.Configuration = redisConnection;
    //     options.InstanceName = builder.Configuration.GetValue<string>("Redis:InstanceName");
    // });

    // TODO: Add JWT authentication and validation services
    // builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    //     .AddJwtBearer(options => { ... });

    // TODO: Add authorization policies
    // builder.Services.AddAuthorization(options => { ... });

    // TODO: Add FIDO2/WebAuthn services
    // builder.Services.AddFido2(...);

    // TODO: Add application services
    // builder.Services.AddScoped<IOrganizationService, OrganizationService>();
    // builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
    // builder.Services.AddScoped<ITokenService, TokenService>();
    // builder.Services.AddScoped<IPassKeyService, PassKeyService>();

    // TODO: Add repositories
    // builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
    // builder.Services.AddScoped<IUserRepository, UserRepository>();

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

    // Add health checks
    builder.Services.AddHealthChecks();
        // TODO: Add database health check
        // .AddNpgSql(builder.Configuration.GetConnectionString("TenantDatabase"))
        // TODO: Add Redis health check
        // .AddRedis(builder.Configuration.GetValue<string>("Redis:ConnectionString"));

    var app = builder.Build();

    // Map default endpoints (OpenAPI, health checks)
    app.MapDefaultEndpoints();

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

    // TODO: Add authentication middleware
    // app.UseAuthentication();
    // app.UseAuthorization();

    app.UseRateLimiter();

    // TODO: Map endpoint groups
    // app.MapAuthEndpoints();
    // app.MapAdminEndpoints();
    // app.MapAuditEndpoints();

    // Temporary health check endpoint
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "Healthy",
        service = "Sorcha.Tenant.Service",
        version = "1.0.0",
        timestamp = DateTime.UtcNow
    }))
    .WithName("HealthCheck")
    .WithOpenApi(operation => new(operation)
    {
        Summary = "Service health check",
        Description = "Returns the health status of the Tenant Service"
    });

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
