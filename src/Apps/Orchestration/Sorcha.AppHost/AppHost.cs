// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

var builder = DistributedApplication.CreateBuilder(args);

// Add Redis for distributed caching and output caching
var redis = builder.AddRedis("redis")
    .WithRedisCommander(); // Adds Redis Commander UI for development

// Add Blueprint API with Redis reference
var blueprintApi = builder.AddProject<Projects.Sorcha_Blueprint_Api>("blueprint-api")
    .WithReference(redis)
    .WithExternalHttpEndpoints(); // Expose HTTP endpoints externally

// Add Peer Service with Redis reference
var peerService = builder.AddProject<Projects.Sorcha_Peer_Service>("peer-service")
    .WithReference(redis)
    .WithExternalHttpEndpoints(); // Expose HTTP and gRPC endpoints externally

builder.Build().Run();
