// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

var builder = DistributedApplication.CreateBuilder(args);

// Add Redis for distributed caching and output caching
var redis = builder.AddRedis("redis")
    .WithRedisCommander(); // Adds Redis Commander UI for development

// Add Blueprint API with Redis reference (internal only)
var blueprintApi = builder.AddProject<Projects.Sorcha_Blueprint_Api>("blueprint-api")
    .WithReference(redis);

// Add Peer Service with Redis reference (internal only)
var peerService = builder.AddProject<Projects.Sorcha_Peer_Service>("peer-service")
    .WithReference(redis);

// Add API Gateway as the single external entry point
var apiGateway = builder.AddProject<Projects.Sorcha_ApiGateway>("api-gateway")
    .WithReference(blueprintApi)
    .WithReference(peerService)
    .WithReference(redis)
    .WithExternalHttpEndpoints(); // Only the gateway is exposed externally

// Add Blazor WebAssembly client
var blazorClient = builder.AddProject<Projects.Sorcha_Blueprint_Designer_Client>("blazor-client")
    .WithReference(apiGateway)
    .WithExternalHttpEndpoints(); // Expose client for browser access

builder.Build().Run();
