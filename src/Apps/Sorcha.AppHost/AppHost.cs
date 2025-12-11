// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL for service databases
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin(); // Adds pgAdmin UI for development

var tenantDb = postgres.AddDatabase("tenant-db", "sorcha_tenant");
var walletDb = postgres.AddDatabase("wallet-db", "sorcha_wallet");

// Add MongoDB for Register Service transaction storage
var mongodb = builder.AddMongoDB("mongodb")
    .WithMongoExpress(); // Adds Mongo Express UI for development

var registerDb = mongodb.AddDatabase("register-db", "sorcha_register");

// Add Redis for distributed caching and output caching
var redis = builder.AddRedis("redis")
    .WithRedisCommander(); // Adds Redis Commander UI for development

// Add Tenant Service (authentication and authorization)
var tenantService = builder.AddProject<Projects.Sorcha_Tenant_Service>("tenant-service")
    .WithReference(tenantDb)
    .WithReference(redis);

// Add Blueprint Service with Redis reference (internal only)
var blueprintService = builder.AddProject<Projects.Sorcha_Blueprint_Service>("blueprint-service")
    .WithReference(redis);

// Add Wallet Service with database and Redis reference (internal only)
var walletService = builder.AddProject<Projects.Sorcha_Wallet_Service>("wallet-service")
    .WithReference(walletDb)
    .WithReference(redis);

// Add Register Service with MongoDB and Redis reference (internal only)
var registerService = builder.AddProject<Projects.Sorcha_Register_Service>("register-service")
    .WithReference(registerDb)
    .WithReference(redis);

// Add Peer Service with Redis reference (internal only)
var peerService = builder.AddProject<Projects.Sorcha_Peer_Service>("peer-service")
    .WithReference(redis);

// Add API Gateway as the single external entry point
var apiGateway = builder.AddProject<Projects.Sorcha_ApiGateway>("api-gateway")
    .WithReference(tenantService)
    .WithReference(blueprintService)
    .WithReference(walletService)
    .WithReference(registerService)
    .WithReference(peerService)
    .WithReference(redis)
    .WithExternalHttpEndpoints(); // Only the gateway is exposed externally

// Add Blazor WebAssembly client
// Note: Blazor WASM is a static client app
var blazorClient = builder.AddProject<Projects.Sorcha_Blueprint_Designer_Client>("blazor-client")
    .WithExternalHttpEndpoints(); // Expose client for browser access

builder.Build().Run();
