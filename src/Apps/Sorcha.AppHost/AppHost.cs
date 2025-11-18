// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

var builder = DistributedApplication.CreateBuilder(args);

// Add Redis for distributed caching and output caching
var redis = builder.AddRedis("redis")
    .WithRedisCommander(); // Adds Redis Commander UI for development

// Add Blueprint Service with Redis reference (internal only)
var blueprintService = builder.AddProject<Projects.Sorcha_Blueprint_Service>("blueprint-service")
    .WithReference(redis);

// Add Wallet Service with Redis reference (internal only)
var walletService = builder.AddProject<Projects.Sorcha_Wallet_Service>("wallet-service")
    .WithReference(redis);

// Add Register Service with Redis reference (internal only)
var registerService = builder.AddProject<Projects.Sorcha_Register_Service>("register-service")
    .WithReference(redis);

// Add Peer Service with Redis reference (internal only)
var peerService = builder.AddProject<Projects.Sorcha_Peer_Service>("peer-service")
    .WithReference(redis);

// Add API Gateway as the single external entry point
var apiGateway = builder.AddProject<Projects.Sorcha_ApiGateway>("api-gateway")
    .WithReference(blueprintService)
    .WithReference(walletService)
    .WithReference(registerService)
    .WithReference(peerService)
    .WithReference(redis)
    .WithExternalHttpEndpoints(); // Only the gateway is exposed externally

// Add Blazor WebAssembly client
// Note: Blazor WASM is a static client app, so we disable health checks
var blazorClient = builder.AddProject<Projects.Sorcha_Blueprint_Designer_Client>("blazor-client")
    .WithReference(apiGateway)
    .WithExternalHttpEndpoints() // Expose client for browser access
    .WithHttpHealthCheck("/"); // Check root path instead of /health

builder.Build().Run();
