// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.AspNetCore.Server.Kestrel.Core;
using Scalar.AspNetCore;
using Sorcha.Peer.Service;
using Sorcha.Peer.Service.Communication;
using Sorcha.Peer.Service.Connection;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Discovery;
using Sorcha.Peer.Service.Distribution;
using Sorcha.Peer.Service.Monitoring;
using Sorcha.Peer.Service.Network;
using Sorcha.Peer.Service.Observability;
using Sorcha.Peer.Service.Replication;
using Sorcha.Peer.Service.Services;
using Sorcha.Register.Service.Repositories;
using Sorcha.Register.Service.Services;
using MongoDB.Driver;

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

// Configure central node
builder.Services.Configure<CentralNodeConfiguration>(
    builder.Configuration.GetSection("PeerService:CentralNode"));

// Configure system register
builder.Services.Configure<SystemRegisterConfiguration>(
    builder.Configuration.GetSection("PeerService:SystemRegister"));

// Configure MongoDB for system register
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var connectionString = builder.Configuration.GetValue<string>("MongoDB:ConnectionString")
        ?? "mongodb://localhost:27017";
    return new MongoClient(connectionString);
});

builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var databaseName = builder.Configuration.GetValue<string>("MongoDB:DatabaseName")
        ?? "sorcha_system_register";
    return client.GetDatabase(databaseName);
});

// Register MongoDB system register repository
builder.Services.AddSingleton<ISystemRegisterRepository, MongoSystemRegisterRepository>();

// Register system register service (for central nodes)
builder.Services.AddSingleton<SystemRegisterService>();

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

// Register central node connection services
builder.Services.AddSingleton<CentralNodeDiscoveryService>();
builder.Services.AddSingleton<CentralNodeConnectionManager>();

// Register observability services (OpenTelemetry)
builder.Services.AddSingleton<PeerServiceMetrics>();
builder.Services.AddSingleton<PeerServiceActivitySource>();

// Register replication services
builder.Services.AddSingleton<SystemRegisterCache>();
builder.Services.AddSingleton<SystemRegisterReplicationService>();
builder.Services.AddSingleton<PushNotificationHandler>();

// Register gRPC service implementations
builder.Services.AddSingleton<PeerDiscoveryServiceImpl>();
builder.Services.AddSingleton<CentralNodeConnectionService>();
builder.Services.AddSingleton<SystemRegisterSyncService>();
builder.Services.AddSingleton<Sorcha.Peer.Service.Services.HeartbeatService>();

// Register background services
builder.Services.AddHostedService<PeerService>();
builder.Services.AddHostedService<HeartbeatMonitorService>();
builder.Services.AddHostedService<PeriodicSyncService>();

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
app.MapGrpcService<CentralNodeConnectionService>();
app.MapGrpcService<SystemRegisterSyncService>();
app.MapGrpcService<Sorcha.Peer.Service.Services.HeartbeatService>();

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
    .WithSummary("List all known peers in the network")
    .WithDescription(@"Returns a comprehensive list of all peer nodes currently known to this node, including connection metadata, latency metrics, and bootstrap node status. This endpoint provides visibility into the peer-to-peer network topology.")
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
    .WithSummary("Get detailed information about a specific peer")
    .WithDescription(@"Retrieves comprehensive details for a single peer node identified by its peer ID, including connection history, latency statistics, supported protocols, and operational status.")
    .WithTags("Peers")
    .WithOpenApi();

app.MapGet("/api/peers/stats", (StatisticsAggregator statisticsAggregator) =>
{
    var stats = statisticsAggregator.GetStatistics();
    return Results.Ok(stats);
})
    .WithName("GetPeerStatistics")
    .WithSummary("Get aggregated peer network statistics")
    .WithDescription(@"Returns comprehensive statistics about the peer-to-peer network including total peer count, connection quality metrics, throughput statistics, and network health indicators. Useful for monitoring and diagnostics.")
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
    .WithSummary("Get peer network health status")
    .WithDescription(@"Returns health status analysis of all peer nodes in the network, categorizing peers as healthy or unhealthy based on connectivity and responsiveness metrics. Includes overall health percentage and detailed metrics for healthy peers.")
    .WithTags("Monitoring")
    .WithOpenApi();

app.Run();
