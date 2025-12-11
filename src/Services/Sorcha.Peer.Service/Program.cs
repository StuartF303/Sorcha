// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.AspNetCore.Server.Kestrel.Core;
using Scalar.AspNetCore;
using Sorcha.Peer.Service;
using Sorcha.Peer.Service.Communication;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Discovery;
using Sorcha.Peer.Service.Distribution;
using Sorcha.Peer.Service.Monitoring;
using Sorcha.Peer.Service.Network;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Configure Kestrel for gRPC with HTTP/1.1 fallback for health checks
// Get port from environment variable (Docker uses 8080, local uses 5000)
var httpPort = int.TryParse(Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS"), out var envPort) ? envPort : 5000;

builder.WebHost.ConfigureKestrel(options =>
{
    // Support both HTTP/1.1 (for health checks) and HTTP/2 (for gRPC)
    options.ListenAnyIP(httpPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
});

// Add services
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

// Add OpenAPI for REST endpoints
builder.Services.AddOpenApi();

// Configure peer service
builder.Services.Configure<PeerServiceConfiguration>(
    builder.Configuration.GetSection("PeerService"));

// Register core services
builder.Services.AddSingleton<StunClient>();
builder.Services.AddSingleton<PeerListManager>();
builder.Services.AddSingleton<NetworkAddressService>();
builder.Services.AddSingleton<PeerDiscoveryService>();
builder.Services.AddSingleton<HealthMonitorService>();
builder.Services.AddSingleton<CommunicationProtocolManager>();
builder.Services.AddSingleton<ConnectionQualityTracker>();
builder.Services.AddSingleton<ConnectionTestingService>();
builder.Services.AddSingleton<GossipProtocolEngine>();
builder.Services.AddSingleton<TransactionQueueManager>();
builder.Services.AddSingleton<TransactionDistributionService>();
builder.Services.AddSingleton<StatisticsAggregator>();

// Register gRPC service implementations
builder.Services.AddSingleton<PeerDiscoveryServiceImpl>();

// Register background service
builder.Services.AddHostedService<PeerService>();

// Add HttpClient
builder.Services.AddHttpClient();

var app = builder.Build();

// Map default endpoints (health checks)
app.MapDefaultEndpoints();

// Add OWASP security headers (SEC-004)
app.UseApiSecurityHeaders();

// Configure OpenAPI (available in all environments for API consumers)
app.MapOpenApi();

// Configure Scalar API documentation UI (development only)
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Peer Service")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// Map gRPC services
app.MapGrpcService<PeerDiscoveryServiceImpl>();

// Enable gRPC reflection for development
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

// Root endpoint
app.MapGet("/", () => "Sorcha Peer Service - gRPC endpoints available on port 5000")
    .WithName("GetServiceInfo")
    .WithSummary("Get service information")
    .WithDescription("Returns basic information about the Peer Service and its gRPC endpoints")
    .WithTags("Info")
    .WithOpenApi();

// REST monitoring endpoints for CLI and admin tools
// These endpoints provide read-only access to peer network status

app.MapGet("/api/peers", (PeerListManager peerListManager) =>
{
    var peers = peerListManager.GetAllPeers();
    return Results.Ok(peers.Select(p => new
    {
        p.PeerId,
        p.Address,
        p.Port,
        p.SupportedProtocols,
        p.FirstSeen,
        p.LastSeen,
        p.FailureCount,
        p.IsBootstrapNode,
        p.AverageLatencyMs
    }));
})
    .WithName("GetAllPeers")
    .WithSummary("List all known peers")
    .WithDescription("Returns a list of all peers currently known to this node")
    .WithTags("Peers")
    .WithOpenApi();

app.MapGet("/api/peers/{peerId}", (string peerId, PeerListManager peerListManager) =>
{
    var peer = peerListManager.GetPeer(peerId);
    if (peer == null)
    {
        return Results.NotFound(new { error = $"Peer '{peerId}' not found" });
    }

    return Results.Ok(new
    {
        peer.PeerId,
        peer.Address,
        peer.Port,
        peer.SupportedProtocols,
        peer.FirstSeen,
        peer.LastSeen,
        peer.FailureCount,
        peer.IsBootstrapNode,
        peer.AverageLatencyMs
    });
})
    .WithName("GetPeerById")
    .WithSummary("Get peer details by ID")
    .WithDescription("Returns detailed information about a specific peer")
    .WithTags("Peers")
    .WithOpenApi();

app.MapGet("/api/peers/stats", (StatisticsAggregator statisticsAggregator) =>
{
    var stats = statisticsAggregator.GetStatistics();
    return Results.Ok(stats);
})
    .WithName("GetPeerStatistics")
    .WithSummary("Get peer network statistics")
    .WithDescription("Returns aggregated statistics about the peer network")
    .WithTags("Monitoring")
    .WithOpenApi();

app.MapGet("/api/peers/health", (PeerListManager peerListManager) =>
{
    var healthyPeers = peerListManager.GetHealthyPeers();
    var allPeers = peerListManager.GetAllPeers();

    return Results.Ok(new
    {
        TotalPeers = allPeers.Count,
        HealthyPeers = healthyPeers.Count,
        UnhealthyPeers = allPeers.Count - healthyPeers.Count,
        HealthPercentage = allPeers.Count > 0 ? (double)healthyPeers.Count / allPeers.Count * 100 : 0,
        Peers = healthyPeers.Select(p => new
        {
            p.PeerId,
            p.Address,
            p.Port,
            p.LastSeen,
            p.AverageLatencyMs
        })
    });
})
    .WithName("GetPeerHealth")
    .WithSummary("Get peer health status")
    .WithDescription("Returns health status of all peers and identifies healthy vs unhealthy peers")
    .WithTags("Monitoring")
    .WithOpenApi();

app.Run();
