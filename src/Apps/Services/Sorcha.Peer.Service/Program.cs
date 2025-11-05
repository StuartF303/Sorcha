// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Scalar.AspNetCore;
using Sorcha.Peer.Service.Services;
using Sorcha.Peer.Service.Models;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add Redis for distributed caching
builder.AddRedisOutputCache("redis");

// Add gRPC services
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

// Add OpenAPI documentation
builder.Services.AddOpenApi();

// Register services
builder.Services.AddSingleton<IPeerRepository, InMemoryPeerRepository>();
builder.Services.AddSingleton<IMetricsService, MetricsService>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.MapDefaultEndpoints();

// Map gRPC services
app.MapGrpcService<PeerGrpcService>();

if (app.Environment.IsDevelopment())
{
    // Enable gRPC reflection for tools like grpcurl
    app.MapGrpcReflectionService();
}

// Configure OpenAPI and Scalar
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Peer Service API")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// REST API endpoints for peer management
var peerApi = app.MapGroup("/api/peers")
    .WithTags("Peer Management")
    .WithOpenApi();

peerApi.MapGet("/", async (IPeerRepository repo) =>
{
    var peers = await repo.GetAllPeersAsync();
    return Results.Ok(peers);
})
.WithName("GetAllPeers")
.WithSummary("Get all registered peer nodes");

peerApi.MapGet("/{id}", async (string id, IPeerRepository repo) =>
{
    var peer = await repo.GetPeerAsync(id);
    return peer is not null ? Results.Ok(peer) : Results.NotFound();
})
.WithName("GetPeer")
.WithSummary("Get a specific peer node by ID");

peerApi.MapPost("/", async (PeerNode peer, IPeerRepository repo) =>
{
    var peerId = await repo.RegisterPeerAsync(peer.PeerId, peer.Endpoint, peer.Metadata);
    var registered = await repo.GetPeerAsync(peerId);
    return Results.Created($"/api/peers/{peerId}", registered);
})
.WithName("RegisterPeer")
.WithSummary("Register a new peer node");

peerApi.MapDelete("/{id}", async (string id, IPeerRepository repo) =>
{
    var result = await repo.UnregisterPeerAsync(id);
    return result ? Results.NoContent() : Results.NotFound();
})
.WithName("UnregisterPeer")
.WithSummary("Unregister a peer node");

// Metrics endpoint
app.MapGet("/api/metrics", (IMetricsService metricsService) =>
{
    var metrics = metricsService.GetCurrentMetrics();
    return Results.Ok(metrics);
})
.WithName("GetMetrics")
.WithSummary("Get current service metrics")
.WithTags("Metrics")
.WithOpenApi();

// Health check endpoint
app.MapGet("/api/health", async (IPeerRepository peerRepo, IMetricsService metricsService) =>
{
    try
    {
        var peers = await peerRepo.GetAllPeersAsync();
        var metrics = metricsService.GetCurrentMetrics();

        return Results.Ok(new
        {
            status = "healthy",
            service = "peer-service",
            timestamp = DateTimeOffset.UtcNow,
            version = "1.0.0",
            uptime = TimeSpan.FromSeconds(metrics.UptimeSeconds).ToString(@"dd\.hh\:mm\:ss"),
            metrics = new
            {
                activePeers = metrics.ActivePeers,
                totalTransactions = metrics.TotalTransactions,
                throughputPerSecond = metrics.ThroughputPerSecond,
                cpuUsagePercent = metrics.CpuUsagePercent,
                memoryUsageBytes = metrics.MemoryUsageBytes
            }
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            status = "unhealthy",
            service = "peer-service",
            timestamp = DateTimeOffset.UtcNow,
            error = ex.Message
        }, statusCode: 503);
    }
})
.WithName("HealthCheck")
.WithSummary("Service health check with metrics")
.WithTags("Health")
.WithOpenApi();

app.Run();
