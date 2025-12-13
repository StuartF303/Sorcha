// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Security.Cryptography;

var builder = DistributedApplication.CreateBuilder(args);

// Generate or retrieve a shared JWT signing key for all services
// This key is automatically generated on first run and persists in local app data
var jwtSigningKey = GetOrCreateJwtSigningKey();

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
    .WithReference(redis)
    .WithEnvironment("JwtSettings__SigningKey", jwtSigningKey)
    .WithEnvironment("JwtSettings__Issuer", "https://localhost:7080")
    .WithEnvironment("JwtSettings__Audience__0", "https://api.sorcha.io");

// Add Blueprint Service with Redis reference (internal only)
var blueprintService = builder.AddProject<Projects.Sorcha_Blueprint_Service>("blueprint-service")
    .WithReference(redis)
    .WithEnvironment("JwtSettings__SigningKey", jwtSigningKey)
    .WithEnvironment("JwtSettings__Issuer", "https://localhost:7080")
    .WithEnvironment("JwtSettings__Audience", "https://api.sorcha.io");

// Add Wallet Service with database and Redis reference (internal only)
var walletService = builder.AddProject<Projects.Sorcha_Wallet_Service>("wallet-service")
    .WithReference(walletDb)
    .WithReference(redis)
    .WithEnvironment("JwtSettings__SigningKey", jwtSigningKey)
    .WithEnvironment("JwtSettings__Issuer", "https://localhost:7080")
    .WithEnvironment("JwtSettings__Audience", "https://api.sorcha.io");

// Add Register Service with MongoDB and Redis reference (internal only)
var registerService = builder.AddProject<Projects.Sorcha_Register_Service>("register-service")
    .WithReference(registerDb)
    .WithReference(redis)
    .WithEnvironment("JwtSettings__SigningKey", jwtSigningKey)
    .WithEnvironment("JwtSettings__Issuer", "https://localhost:7080")
    .WithEnvironment("JwtSettings__Audience", "https://api.sorcha.io");

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

/// <summary>
/// Gets or creates a JWT signing key for development.
/// The key is stored in the user's local application data folder to persist across restarts.
/// </summary>
static string GetOrCreateJwtSigningKey()
{
    var keyFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Sorcha",
        "dev-jwt-signing-key.txt");

    try
    {
        // Check if we already have a key
        if (File.Exists(keyFilePath))
        {
            var existingKey = File.ReadAllText(keyFilePath).Trim();
            if (!string.IsNullOrEmpty(existingKey) && existingKey.Length >= 32)
            {
                Console.WriteLine($"[JWT] Using existing development signing key from: {keyFilePath}");
                return existingKey;
            }
        }

        // Generate a new secure key
        var keyBytes = new byte[32]; // 256 bits
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(keyBytes);
        }
        var newKey = Convert.ToBase64String(keyBytes);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(keyFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Save the key
        File.WriteAllText(keyFilePath, newKey);

        Console.WriteLine($"[JWT] Generated new development signing key at: {keyFilePath}");

        return newKey;
    }
    catch (Exception ex)
    {
        // If we can't persist, generate one in memory (services will still share via env var)
        Console.WriteLine($"[JWT] Warning: Could not persist development key: {ex.Message}");

        var keyBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(keyBytes);
        }
        return Convert.ToBase64String(keyBytes);
    }
}
