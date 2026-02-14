// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentValidation;
using Grpc.Net.Client;
using Scalar.AspNetCore;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Endpoints;
using Sorcha.Validator.Service.Extensions;
using Sorcha.Validator.Service.Services;
using Sorcha.Cryptography.Interfaces;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Utilities;
using Sorcha.ServiceClients.Extensions;
using Sorcha.Register.Storage.MongoDB;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add structured logging with Serilog (OPS-001)
builder.AddSerilogLogging();

// Add rate limiting (SEC-002)
builder.AddRateLimiting();

// Add input validation (SEC-003)
builder.AddInputValidation();

// Add Redis for distributed coordination and memory pool persistence
builder.AddRedisClient("redis");

// Add OpenAPI services
builder.Services.AddOpenApi();

// Configure strongly-typed configuration sections
builder.Services.Configure<Sorcha.Validator.Service.Configuration.ValidatorConfiguration>(
    builder.Configuration.GetSection("Validator"));
builder.Services.Configure<Sorcha.Validator.Service.Configuration.ConsensusConfiguration>(
    builder.Configuration.GetSection("Consensus"));
builder.Services.Configure<Sorcha.Validator.Service.Configuration.MemPoolConfiguration>(
    builder.Configuration.GetSection("MemPool"));
builder.Services.Configure<Sorcha.Validator.Service.Configuration.DocketBuildConfiguration>(
    builder.Configuration.GetSection("DocketBuild"));

// Configure WalletConfiguration (T013)
var walletConfig = builder.Configuration.GetSection("WalletService").Get<WalletConfiguration>()
    ?? throw new InvalidOperationException("WalletService configuration is required");
builder.Services.AddSingleton(walletConfig);

// Add Cryptography services (required for hashing and signing operations)
builder.Services.AddScoped<IHashProvider, HashProvider>();
builder.Services.AddSingleton<ICryptoModule, CryptoModule>(); // T013: Register ICryptoModule
builder.Services.AddScoped<MerkleTree>();
builder.Services.AddScoped<DocketHasher>();

// Register read-only repository for GovernanceRosterService (used by RightsEnforcementService).
// Register Service owns read-write access; Validator reads the same MongoDB for governance roster reconstruction.
// Only IReadOnlyRegisterRepository is registered — IRegisterRepository is NOT resolvable here.
builder.Services.AddReadOnlyMongoRegisterStorage(builder.Configuration);

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add Core validation services
builder.Services.AddScoped<Sorcha.Validator.Core.Validators.ITransactionValidator,
    Sorcha.Validator.Core.Validators.TransactionValidator>();
builder.Services.AddScoped<Sorcha.Validator.Core.Validators.IDocketValidator,
    Sorcha.Validator.Core.Validators.DocketValidator>();
builder.Services.AddScoped<Sorcha.Validator.Core.Validators.IConsensusValidator,
    Sorcha.Validator.Core.Validators.ConsensusValidator>();

// Add memory pool manager
builder.Services.AddSingleton<Sorcha.Validator.Service.Services.IMemPoolManager,
    Sorcha.Validator.Service.Services.MemPoolManager>();

// Add register monitoring registry (singleton - shared state across services)
builder.Services.AddSingleton<Sorcha.Validator.Service.Services.IRegisterMonitoringRegistry,
    Sorcha.Validator.Service.Services.RegisterMonitoringRegistry>();

// Add system wallet provider (singleton - maintains wallet reference across service lifetime)
builder.Services.AddSingleton<Sorcha.Validator.Service.Services.ISystemWalletProvider,
    Sorcha.Validator.Service.Services.SystemWalletProvider>();

// Add consensus engine
builder.Services.AddScoped<Sorcha.Validator.Service.Services.IConsensusEngine,
    Sorcha.Validator.Service.Services.ConsensusEngine>();

// Add genesis manager (scoped to match service client lifetimes)
builder.Services.AddScoped<Sorcha.Validator.Service.Services.IGenesisManager,
    Sorcha.Validator.Service.Services.GenesisManager>();

// Add docket builder
builder.Services.AddScoped<Sorcha.Validator.Service.Services.IDocketBuilder,
    Sorcha.Validator.Service.Services.DocketBuilder>();

// Add validator orchestrator (scoped to match service client lifetimes)
builder.Services.AddScoped<Sorcha.Validator.Service.Services.IValidatorOrchestrator,
    Sorcha.Validator.Service.Services.ValidatorOrchestrator>();

// Add consolidated service clients
builder.Services.AddServiceClients(builder.Configuration);

// Add blueprint cache and transaction pool poller (required by validation engine)
builder.Services.AddBlueprintCache(builder.Configuration);
builder.Services.AddTransactionPoolPoller(builder.Configuration);

// Add verified transaction queue (required by validation engine)
builder.Services.AddVerifiedTransactionQueue(builder.Configuration);

// Add validation engine (schema validation, chain validation)
builder.Services.AddValidationEngine(builder.Configuration);

// Configure gRPC channel for Wallet Service (T014)
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<WalletConfiguration>();
    return GrpcChannel.ForAddress(config.Endpoint, new GrpcChannelOptions
    {
        // Configure HTTP/2 keep-alive for long-running connections
        HttpHandler = new SocketsHttpHandler
        {
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            KeepAlivePingDelay = TimeSpan.FromSeconds(60),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
            EnableMultipleHttp2Connections = true
        }
    });
});

// Register WalletIntegrationService as singleton (T014)
// Singleton lifetime chosen for:
// - Wallet details cached for service lifetime (FR-002)
// - Derived key cache persists across requests (performance)
// - Thread-safe implementation with SemaphoreSlim
builder.Services.AddSingleton<IWalletIntegrationService, WalletIntegrationService>();

// Add background services
builder.Services.AddHostedService<Sorcha.Validator.Service.Services.SystemWalletInitializer>();
builder.Services.AddHostedService<Sorcha.Validator.Service.Services.MemPoolCleanupService>();
builder.Services.AddHostedService<Sorcha.Validator.Service.Services.DocketBuildTriggerService>();

// Add genesis configuration service (Sprint 9F)
builder.Services.AddGenesisConfigService(builder.Configuration);

// Add validator registry (Sprint 9F)
builder.Services.AddValidatorRegistry(builder.Configuration);

// Add control blueprint version resolver (Sprint 9F - VAL-9.42)
builder.Services.AddControlBlueprintVersionResolver();

// Add control docket processor (Sprint 9F - VAL-9.41)
builder.Services.AddControlDocketProcessor();

// Add gRPC services
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline
app.MapDefaultEndpoints();

// Add Serilog HTTP request logging (OPS-001)
app.UseSerilogLogging();

// Add OWASP security headers (SEC-004)
app.UseApiSecurityHeaders();

// Enable HTTPS enforcement with HSTS (SEC-001)
app.UseHttpsEnforcement();

// Enable input validation (SEC-003)
app.UseInputValidation();

// Enable rate limiting (SEC-002)
app.UseRateLimiting();

// Map gRPC services
app.MapGrpcService<Sorcha.Validator.Service.GrpcServices.ValidatorGrpcService>();

// Configure OpenAPI and Scalar
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Sorcha Validator Service API")
            .WithTheme(ScalarTheme.Mars)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// Map API endpoints
app.MapGroup("/api/v1/transactions")
    .WithTags("Validation")
    .MapValidationEndpoints();

// Map validator-specific endpoints (genesis)
app.MapGroup("/api/validator")
    .WithTags("Validator")
    .MapGenesisEndpoint();

// Map admin endpoints
app.MapAdminEndpoints();

// Map validator registration endpoints (Sprint 9F)
app.MapGroup("/api/validators")
    .WithTags("Validators")
    .MapValidatorRegistrationEndpoints();

// Map metrics endpoints (VAL-9.45) - disabled pending full service registration
// TODO: Enable once all metrics services are properly registered
// app.MapMetricsEndpoints();

app.Run();
