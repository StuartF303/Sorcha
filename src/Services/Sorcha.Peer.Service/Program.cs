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

// Add rate limiting (SEC-002)
builder.AddRateLimiting();

// Add input validation (SEC-003)
builder.AddInputValidation();

// Configure Kestrel for gRPC with HTTP/1.1 fallback for health checks
// Listen on port 8080/8052 for HTTP/health checks (Aspire/Docker standard)
// Listen on port 5000/5003 for gRPC peer-to-peer communication
var healthCheckPort = int.TryParse(Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS"), out var envPort) ? envPort : 8080;
var grpcPort = builder.Configuration.GetValue<int>("PeerService:Port", 5000);
var enableTls = builder.Configuration.GetValue<bool>("PeerService:EnableTls", false);

builder.WebHost.ConfigureKestrel(options =>
{
    // Disable minimum protocol version to allow HTTP/2 without TLS
    options.ConfigureEndpointDefaults(listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });

    // Port 8080/8052: HTTP/1.1 + HTTP/2 for health checks and REST API
    options.ListenAnyIP(healthCheckPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });

    // Port 5000/5003: HTTP/2 for gRPC peer communication (only if different from health port)
    if (grpcPort != healthCheckPort)
    {
        options.ListenAnyIP(grpcPort, listenOptions =>
        {
            // Enable HTTP/2 without TLS for development (cleartext HTTP/2)
            // This allows gRPC to work without certificates
            listenOptions.Protocols = HttpProtocols.Http2;
        });
    }
});

// Add services
builder.Services.AddGrpc(options =>
{
    // Allow gRPC to work without TLS in development
    // This is necessary for cleartext HTTP/2
    options.EnableDetailedErrors = true;
});
builder.Services.AddGrpcReflection();

// Configure AppContext to allow HTTP/2 without TLS
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

// Add OpenAPI for REST endpoints
builder.Services.AddOpenApi();

// Configure peer service
builder.Services.Configure<PeerServiceConfiguration>(
    builder.Configuration.GetSection("PeerService"));

// Configure hub node
builder.Services.Configure<HubNodeConfiguration>(
    builder.Configuration.GetSection("PeerService:HubNode"));

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

// Register system register service (for hub nodes)
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

// Register hub node connection services
builder.Services.AddSingleton<HubNodeDiscoveryService>();
builder.Services.AddSingleton<HubNodeConnectionManager>();

// Register observability services (OpenTelemetry)
builder.Services.AddSingleton<PeerServiceMetrics>();
builder.Services.AddSingleton<PeerServiceActivitySource>();

// Register replication services
builder.Services.AddSingleton<SystemRegisterCache>();
builder.Services.AddSingleton<SystemRegisterReplicationService>();
builder.Services.AddSingleton<PushNotificationHandler>();

// Register gRPC service implementations
builder.Services.AddSingleton<PeerDiscoveryServiceImpl>();
builder.Services.AddSingleton<HubNodeConnectionService>();
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

// Enable HTTPS enforcement with HSTS (SEC-001)
app.UseHttpsEnforcement();

// Enable input validation (SEC-003)
app.UseInputValidation();

// Enable rate limiting (SEC-002)
app.UseRateLimiting();

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
app.MapGrpcService<HubNodeConnectionService>();
app.MapGrpcService<SystemRegisterSyncService>();
app.MapGrpcService<Sorcha.Peer.Service.Services.HeartbeatService>();

// Enable gRPC reflection for development
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

// Root endpoint
app.MapGet("/", (IConfiguration config) =>
{
    var httpPort = int.TryParse(Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS"), out var envHttpPort) ? envHttpPort : 8080;
    var grpcPort = config.GetValue<int>("PeerService:Port", 5000);
    return $"Sorcha Peer Service - HTTP API: port {httpPort}, gRPC: port {grpcPort}";
})
    .WithName("GetServiceInfo")
    .WithSummary("Get service information")
    .WithDescription("Returns basic information about the Peer Service and its available ports")
    .WithTags("Info");

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
    .WithTags("Peers");

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
    .WithTags("Peers");

app.MapGet("/api/peers/stats", (StatisticsAggregator statisticsAggregator) =>
{
    var stats = statisticsAggregator.GetStatistics();
    return Results.Ok(stats);
})
    .WithName("GetPeerStatistics")
    .WithSummary("Get aggregated peer network statistics")
    .WithDescription(@"Returns comprehensive statistics about the peer-to-peer network including total peer count, connection quality metrics, throughput statistics, and network health indicators. Useful for monitoring and diagnostics.")
    .WithTags("Monitoring");

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
    .WithTags("Monitoring");

// Get count of connected peers (anonymous) and optional list (authenticated)
app.MapGet("/api/peers/connected", (PeerListManager peerListManager, HttpContext context) =>
{
    var healthyPeers = peerListManager.GetHealthyPeers();
    var count = healthyPeers.Count;

    // Check if user is authenticated
    var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;

    if (isAuthenticated)
    {
        // Return count and full list for authenticated users
        return Results.Ok(new
        {
            ConnectedPeerCount = count,
            Peers = healthyPeers.Select(p => new
            {
                p.PeerId,
                p.Address,
                p.Port,
                p.SupportedProtocols,
                p.LastSeen,
                p.AverageLatencyMs,
                p.IsBootstrapNode
            })
        });
    }
    else
    {
        // Return only count for anonymous users
        return Results.Ok(new
        {
            ConnectedPeerCount = count
        });
    }
})
    .WithName("GetConnectedPeers")
    .WithSummary("Get count of connected peers")
    .WithDescription(@"Returns the count of currently connected (healthy) peers. Anonymous users receive only the count, while authenticated users also receive the full list of connected peers with their details.")
    .WithTags("Peers")
    .AllowAnonymous();

// Service health check with metrics (for admin dashboard)
app.MapGet("/api/health", (PeerListManager peerListManager, StatisticsAggregator statisticsAggregator) =>
{
    try
    {
        var allPeers = peerListManager.GetAllPeers();
        var healthyPeers = peerListManager.GetHealthyPeers();
        var stats = statisticsAggregator.GetStatistics();

        return Results.Ok(new
        {
            status = "healthy",
            service = "peer-service",
            timestamp = DateTimeOffset.UtcNow,
            version = "1.0.0",
            uptime = TimeSpan.FromMilliseconds(Environment.TickCount64).ToString(@"dd\.hh\:mm\:ss"),
            metrics = new
            {
                totalPeers = stats.PeerStats.TotalPeers,
                healthyPeers = stats.PeerStats.HealthyPeers,
                unhealthyPeers = stats.PeerStats.UnhealthyPeers,
                averageLatencyMs = stats.PeerStats.AverageLatencyMs,
                queueSize = stats.QueueStats.QueueSize
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
.WithTags("Health");

app.Run();

// Make the implicit Program class accessible to integration tests
namespace Sorcha.Peer.Service
{
    public partial class Program { }
}
