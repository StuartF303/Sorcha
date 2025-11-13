// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.AspNetCore.Server.Kestrel.Core;
using Sorcha.Peer.Service;
using Sorcha.Peer.Service.Communication;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Discovery;
using Sorcha.Peer.Service.Distribution;
using Sorcha.Peer.Service.Monitoring;
using Sorcha.Peer.Service.Network;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for gRPC
builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP/2 for gRPC
    options.ListenAnyIP(5000, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

// Add services
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

// Configure peer service
builder.Services.Configure<PeerServiceConfiguration>(
    builder.Configuration.GetSection("PeerService"));

// Register core services
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

// Register gRPC service implementations
builder.Services.AddSingleton<PeerDiscoveryServiceImpl>();

// Register background service
builder.Services.AddHostedService<PeerService>();

// Add HttpClient
builder.Services.AddHttpClient();

var app = builder.Build();

// Map gRPC services
app.MapGrpcService<PeerDiscoveryServiceImpl>();

// Enable gRPC reflection for development
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGet("/", () => "Sorcha Peer Service - gRPC endpoints available on port 5000");

app.Run();
