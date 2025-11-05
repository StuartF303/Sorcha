// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Scalar.AspNetCore;
using Sorcha.ApiGateway.Models;
using Sorcha.ApiGateway.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add YARP reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add health aggregation service
builder.Services.AddHttpClient();
builder.Services.AddSingleton<HealthAggregationService>();

// Add OpenAPI documentation
builder.Services.AddOpenApi();

// Add CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.MapDefaultEndpoints();

// Enable CORS
app.UseCors();

// ===========================
// Aggregated Health Endpoint
// ===========================

app.MapGet("/api/health", async (HealthAggregationService healthService) =>
{
    var health = await healthService.GetAggregatedHealthAsync();
    var statusCode = health.Status switch
    {
        "healthy" => 200,
        "degraded" => 200,
        _ => 503
    };

    return Results.Json(health, statusCode: statusCode);
})
.WithName("AggregatedHealth")
.WithSummary("Get aggregated health status from all services")
.WithTags("Health")
.WithOpenApi();

// ===========================
// System Statistics Endpoint
// ===========================

app.MapGet("/api/stats", async (HealthAggregationService healthService) =>
{
    var stats = await healthService.GetSystemStatisticsAsync();
    return Results.Ok(stats);
})
.WithName("SystemStatistics")
.WithSummary("Get system-wide statistics from all services")
.WithTags("System")
.WithOpenApi();

// ===========================
// Landing Page
// ===========================

app.MapGet("/", async (HealthAggregationService healthService, HttpContext context) =>
{
    var stats = await healthService.GetSystemStatisticsAsync();
    var health = await healthService.GetAggregatedHealthAsync();

    var html = $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Sorcha API Gateway</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }
        .container {
            background: white;
            border-radius: 16px;
            box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
            max-width: 1200px;
            width: 100%;
            overflow: hidden;
        }
        .header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 40px;
            text-align: center;
        }
        .header h1 {
            font-size: 2.5rem;
            margin-bottom: 10px;
        }
        .header p {
            font-size: 1.1rem;
            opacity: 0.9;
        }
        .content {
            padding: 40px;
        }
        .stats-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 20px;
            margin-bottom: 40px;
        }
        .stat-card {
            background: #f8f9fa;
            padding: 24px;
            border-radius: 12px;
            border-left: 4px solid #667eea;
        }
        .stat-card h3 {
            color: #667eea;
            font-size: 0.875rem;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            margin-bottom: 8px;
        }
        .stat-card .value {
            font-size: 2rem;
            font-weight: bold;
            color: #2d3748;
        }
        .services-section {
            margin-bottom: 40px;
        }
        .services-section h2 {
            color: #2d3748;
            margin-bottom: 20px;
            font-size: 1.5rem;
        }
        .service-list {
            display: grid;
            gap: 12px;
        }
        .service-item {
            display: flex;
            align-items: center;
            justify-content: space-between;
            padding: 16px 20px;
            background: #f8f9fa;
            border-radius: 8px;
            border-left: 4px solid #e2e8f0;
        }
        .service-item.healthy {
            border-left-color: #48bb78;
        }
        .service-item.unhealthy {
            border-left-color: #f56565;
        }
        .service-name {
            font-weight: 600;
            color: #2d3748;
            text-transform: capitalize;
        }
        .service-status {
            display: inline-block;
            padding: 4px 12px;
            border-radius: 12px;
            font-size: 0.875rem;
            font-weight: 600;
        }
        .service-status.healthy {
            background: #c6f6d5;
            color: #22543d;
        }
        .service-status.unhealthy {
            background: #fed7d7;
            color: #742a2a;
        }
        .actions {
            display: flex;
            gap: 16px;
            flex-wrap: wrap;
        }
        .btn {
            display: inline-block;
            padding: 12px 24px;
            border-radius: 8px;
            text-decoration: none;
            font-weight: 600;
            transition: all 0.2s;
        }
        .btn-primary {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
        }
        .btn-primary:hover {
            transform: translateY(-2px);
            box-shadow: 0 10px 20px rgba(102, 126, 234, 0.4);
        }
        .btn-secondary {
            background: white;
            color: #667eea;
            border: 2px solid #667eea;
        }
        .btn-secondary:hover {
            background: #667eea;
            color: white;
        }
        .footer {
            text-align: center;
            padding: 20px;
            color: #718096;
            font-size: 0.875rem;
            border-top: 1px solid #e2e8f0;
        }
        .timestamp {
            color: #718096;
            font-size: 0.875rem;
            margin-top: 20px;
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>🚀 Sorcha API Gateway</h1>
            <p>Unified API endpoint for Sorcha blockchain services</p>
        </div>

        <div class="content">
            <div class="stats-grid">
                <div class="stat-card">
                    <h3>Total Services</h3>
                    <div class="value">{{stats.TotalServices}}</div>
                </div>
                <div class="stat-card">
                    <h3>Healthy Services</h3>
                    <div class="value">{{stats.HealthyServices}}</div>
                </div>
                <div class="stat-card">
                    <h3>System Status</h3>
                    <div class="value">{{health.Status}}</div>
                </div>
            </div>

            <div class="services-section">
                <h2>Service Status</h2>
                <div class="service-list">
{{string.Join("\n", health.Services.Select(s => $@"                    <div class=""service-item {s.Value.Status}"">
                        <span class=""service-name"">{s.Key}</span>
                        <span class=""service-status {s.Value.Status}"">{s.Value.Status}</span>
                    </div>"))}}
                </div>
            </div>

            <div class="actions">
                <a href="/scalar/v1" class="btn btn-primary">📚 API Documentation</a>
                <a href="/api/health" class="btn btn-secondary">🏥 Health Check</a>
                <a href="/api/stats" class="btn btn-secondary">📊 System Stats</a>
            </div>

            <div class="timestamp">
                Last updated: {{stats.Timestamp:yyyy-MM-dd HH:mm:ss}} UTC
            </div>
        </div>

        <div class="footer">
            <p>Sorcha Blockchain Platform &copy; 2025 | Licensed under MIT</p>
        </div>
    </div>
</body>
</html>
""";

    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(html);
})
.ExcludeFromDescription();

// Configure OpenAPI and Scalar
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Sorcha API Gateway")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// Map YARP reverse proxy (must be last)
app.MapReverseProxy();

app.Run();
