// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentValidation;
using Scalar.AspNetCore;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Endpoints;
using Sorcha.Cryptography.Interfaces;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Utilities;
using Sorcha.ServiceClients.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

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

// Add Cryptography services (required for hashing operations)
builder.Services.AddScoped<IHashProvider, HashProvider>();
builder.Services.AddScoped<MerkleTree>();
builder.Services.AddScoped<DocketHasher>();

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

// Add background services
builder.Services.AddHostedService<Sorcha.Validator.Service.Services.MemPoolCleanupService>();

// Add gRPC services
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline
app.MapDefaultEndpoints();

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

// Map admin endpoints
app.MapAdminEndpoints();

app.Run();
