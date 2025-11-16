// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.AspNetCore.OData;
using Microsoft.OData.ModelBuilder;
using Scalar.AspNetCore;
using Sorcha.Register.Core.Events;
using Sorcha.Register.Core.Managers;
using Sorcha.Register.Core.Storage;
using Sorcha.Register.Models;
using Sorcha.Register.Models.Enums;
using Sorcha.Register.Service.Hubs;
using Sorcha.Register.Storage.InMemory;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add SignalR for real-time notifications
builder.Services.AddSignalR();

// Configure OData
var modelBuilder = new ODataConventionModelBuilder();
modelBuilder.EntitySet<Models.Register>("Registers");
modelBuilder.EntitySet<TransactionModel>("Transactions");
modelBuilder.EntitySet<Docket>("Dockets");

builder.Services.AddControllers()
    .AddOData(options => options
        .Select()
        .Filter()
        .OrderBy()
        .Expand()
        .Count()
        .SetMaxTop(100)
        .AddRouteComponents("odata", modelBuilder.GetEdmModel()));

// Add OpenAPI services
builder.Services.AddOpenApi();

// Register storage and event infrastructure
builder.Services.AddSingleton<IRegisterRepository, InMemoryRegisterRepository>();
builder.Services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();

// Register managers
builder.Services.AddScoped<RegisterManager>();
builder.Services.AddScoped<TransactionManager>();
builder.Services.AddScoped<QueryManager>();

var app = builder.Build();

// Map default endpoints (health checks)
app.MapDefaultEndpoints();

// Configure OpenAPI
app.MapOpenApi();

// Configure Scalar API documentation UI
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Register Service")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// Map SignalR hub
app.MapHub<RegisterHub>("/hubs/register");

// ===========================
// Register Management API
// ===========================

var registersGroup = app.MapGroup("/api/registers")
    .WithTags("Registers")
    .WithOpenApi();

/// <summary>
/// Create a new register
/// </summary>
registersGroup.MapPost("/", async (
    RegisterManager manager,
    IHubContext<RegisterHub, IRegisterHubClient> hubContext,
    CreateRegisterRequest request) =>
{
    try
    {
        var register = await manager.CreateRegisterAsync(
            request.Name,
            request.TenantId,
            request.Advertise,
            request.IsFullReplica);

        // Notify via SignalR
        await hubContext.Clients
            .Group($"tenant:{register.TenantId}")
            .RegisterCreated(register.Id, register.Name);

        return Results.Created($"/api/registers/{register.Id}", register);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CreateRegister")
.WithSummary("Create a new register")
.WithDescription("Creates a new distributed ledger register with a unique ID.");

/// <summary>
/// Get all registers
/// </summary>
registersGroup.MapGet("/", async (
    RegisterManager manager,
    string? tenantId = null) =>
{
    var registers = tenantId != null
        ? await manager.GetRegistersByTenantAsync(tenantId)
        : await manager.GetAllRegistersAsync();

    return Results.Ok(registers);
})
.WithName("GetAllRegisters")
.WithSummary("Get all registers")
.WithDescription("Retrieves all registers, optionally filtered by tenant.");

/// <summary>
/// Get register by ID
/// </summary>
registersGroup.MapGet("/{id}", async (
    RegisterManager manager,
    string id) =>
{
    var register = await manager.GetRegisterAsync(id);
    return register is not null ? Results.Ok(register) : Results.NotFound();
})
.WithName("GetRegister")
.WithSummary("Get register by ID")
.WithDescription("Retrieves a specific register by its unique identifier.");

/// <summary>
/// Update register
/// </summary>
registersGroup.MapPut("/{id}", async (
    RegisterManager manager,
    string id,
    UpdateRegisterRequest request) =>
{
    var register = await manager.GetRegisterAsync(id);
    if (register is null)
        return Results.NotFound();

    if (request.Name is not null)
        register.Name = request.Name;
    if (request.Status is not null)
        register.Status = request.Status.Value;
    if (request.Advertise is not null)
        register.Advertise = request.Advertise.Value;

    var updated = await manager.UpdateRegisterAsync(register);
    return Results.Ok(updated);
})
.WithName("UpdateRegister")
.WithSummary("Update register")
.WithDescription("Updates register metadata and settings.");

/// <summary>
/// Delete register
/// </summary>
registersGroup.MapDelete("/{id}", async (
    RegisterManager manager,
    IHubContext<RegisterHub, IRegisterHubClient> hubContext,
    string id,
    string tenantId) =>
{
    try
    {
        await manager.DeleteRegisterAsync(id, tenantId);

        // Notify via SignalR
        await hubContext.Clients
            .Group($"tenant:{tenantId}")
            .RegisterDeleted(id);

        return Results.NoContent();
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
})
.WithName("DeleteRegister")
.WithSummary("Delete register")
.WithDescription("Deletes a register and all associated data.");

/// <summary>
/// Get register count
/// </summary>
registersGroup.MapGet("/stats/count", async (RegisterManager manager) =>
{
    var count = await manager.GetRegisterCountAsync();
    return Results.Ok(new { count });
})
.WithName("GetRegisterCount")
.WithSummary("Get register count")
.WithDescription("Returns the total number of registers.");

// ===========================
// Transaction Management API
// ===========================

var transactionsGroup = app.MapGroup("/api/registers/{registerId}/transactions")
    .WithTags("Transactions")
    .WithOpenApi();

/// <summary>
/// Submit a transaction
/// </summary>
transactionsGroup.MapPost("/", async (
    TransactionManager manager,
    IHubContext<RegisterHub, IRegisterHubClient> hubContext,
    string registerId,
    TransactionModel transaction) =>
{
    try
    {
        transaction.RegisterId = registerId;
        var stored = await manager.StoreTransactionAsync(transaction);

        // Notify via SignalR
        await hubContext.Clients
            .Group($"register:{registerId}")
            .TransactionConfirmed(registerId, stored.TxId);

        return Results.Created($"/api/registers/{registerId}/transactions/{stored.TxId}", stored);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("SubmitTransaction")
.WithSummary("Submit a transaction")
.WithDescription("Stores a validated transaction in the register.");

/// <summary>
/// Get transaction by ID
/// </summary>
transactionsGroup.MapGet("/{txId}", async (
    TransactionManager manager,
    string registerId,
    string txId) =>
{
    var transaction = await manager.GetTransactionAsync(registerId, txId);
    return transaction is not null ? Results.Ok(transaction) : Results.NotFound();
})
.WithName("GetTransaction")
.WithSummary("Get transaction by ID")
.WithDescription("Retrieves a specific transaction by its ID.");

/// <summary>
/// Get all transactions for a register (queryable)
/// </summary>
transactionsGroup.MapGet("/", async (
    TransactionManager manager,
    string registerId,
    int page = 1,
    int pageSize = 20) =>
{
    var transactions = await manager.GetTransactionsAsync(registerId);
    var paged = transactions
        .OrderByDescending(t => t.TimeStamp)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToList();

    return Results.Ok(new
    {
        Page = page,
        PageSize = pageSize,
        Total = transactions.Count(),
        Transactions = paged
    });
})
.WithName("GetTransactions")
.WithSummary("Get all transactions")
.WithDescription("Retrieves all transactions for a register with pagination.");

// ===========================
// Query API
// ===========================

var queryGroup = app.MapGroup("/api/query")
    .WithTags("Query")
    .WithOpenApi();

/// <summary>
/// Query transactions by wallet address
/// </summary>
queryGroup.MapGet("/wallets/{address}/transactions", async (
    QueryManager manager,
    string address,
    string? registerId = null,
    int page = 1,
    int pageSize = 20) =>
{
    if (registerId is not null)
    {
        var result = await manager.GetTransactionsByWalletAsync(registerId, address, page, pageSize);
        return Results.Ok(result);
    }

    // Query across all registers (future enhancement)
    return Results.BadRequest(new { error = "registerId is required" });
})
.WithName("GetTransactionsByWallet")
.WithSummary("Query transactions by wallet")
.WithDescription("Retrieves all transactions for a specific wallet address.");

/// <summary>
/// Query transactions by sender
/// </summary>
queryGroup.MapGet("/senders/{address}/transactions", async (
    QueryManager manager,
    string address,
    string registerId,
    int page = 1,
    int pageSize = 20) =>
{
    var result = await manager.GetTransactionsBySenderAsync(registerId, address, page, pageSize);
    return Results.Ok(result);
})
.WithName("GetTransactionsBySender")
.WithSummary("Query transactions by sender")
.WithDescription("Retrieves all transactions sent by a specific address.");

/// <summary>
/// Query transactions by blueprint
/// </summary>
queryGroup.MapGet("/blueprints/{blueprintId}/transactions", async (
    QueryManager manager,
    string blueprintId,
    string registerId,
    string? instanceId = null,
    int page = 1,
    int pageSize = 20) =>
{
    var result = await manager.GetTransactionsByBlueprintAsync(
        registerId,
        blueprintId,
        instanceId,
        page,
        pageSize);

    return Results.Ok(result);
})
.WithName("GetTransactionsByBlueprint")
.WithSummary("Query transactions by blueprint")
.WithDescription("Retrieves all transactions for a specific blueprint.");

/// <summary>
/// Get transaction statistics
/// </summary>
queryGroup.MapGet("/stats", async (
    QueryManager manager,
    string registerId) =>
{
    var stats = await manager.GetTransactionStatisticsAsync(registerId);
    return Results.Ok(stats);
})
.WithName("GetTransactionStatistics")
.WithSummary("Get transaction statistics")
.WithDescription("Retrieves comprehensive statistics for a register.");

// ===========================
// Docket Management API
// ===========================

var docketsGroup = app.MapGroup("/api/registers/{registerId}/dockets")
    .WithTags("Dockets")
    .WithOpenApi();

/// <summary>
/// Get all dockets for a register
/// </summary>
docketsGroup.MapGet("/", async (
    IRegisterRepository repository,
    string registerId) =>
{
    var dockets = await repository.GetDocketsAsync(registerId);
    return Results.Ok(dockets);
})
.WithName("GetDockets")
.WithSummary("Get all dockets")
.WithDescription("Retrieves all dockets (blocks) for a register.");

/// <summary>
/// Get docket by ID
/// </summary>
docketsGroup.MapGet("/{docketId}", async (
    IRegisterRepository repository,
    string registerId,
    ulong docketId) =>
{
    var docket = await repository.GetDocketAsync(registerId, docketId);
    return docket is not null ? Results.Ok(docket) : Results.NotFound();
})
.WithName("GetDocket")
.WithSummary("Get docket by ID")
.WithDescription("Retrieves a specific docket by its ID (block height).");

/// <summary>
/// Get transactions in a docket
/// </summary>
docketsGroup.MapGet("/{docketId}/transactions", async (
    IRegisterRepository repository,
    string registerId,
    ulong docketId) =>
{
    var transactions = await repository.GetTransactionsByDocketAsync(registerId, docketId);
    return Results.Ok(transactions);
})
.WithName("GetDocketTransactions")
.WithSummary("Get docket transactions")
.WithDescription("Retrieves all transactions sealed in a specific docket.");

app.Run();

// ===========================
// Request/Response Models
// ===========================

record CreateRegisterRequest(
    string Name,
    string TenantId,
    bool Advertise = false,
    bool IsFullReplica = true);

record UpdateRegisterRequest(
    string? Name = null,
    RegisterStatus? Status = null,
    bool? Advertise = null);
