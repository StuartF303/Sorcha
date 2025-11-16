// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Scalar.AspNetCore;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add OpenAPI services
builder.Services.AddOpenApi();

// In-memory transaction storage (replace with database later)
builder.Services.AddSingleton<TransactionStore>();

var app = builder.Build();

// Map default endpoints (health checks)
app.MapDefaultEndpoints();

// Configure OpenAPI
app.MapOpenApi();

// Configure Scalar API documentation UI (development only)
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

// ===========================
// Register/Ledger Endpoints
// ===========================

var registerGroup = app.MapGroup("/api/register")
    .WithTags("Register")
    .WithOpenApi();

/// <summary>
/// Submit a transaction to the register
/// </summary>
registerGroup.MapPost("/transactions", async (
    TransactionStore store,
    TransactionSubmitRequest request) =>
{
    var transaction = new StoredTransaction
    {
        Id = Guid.NewGuid().ToString(),
        WalletAddress = request.WalletAddress,
        RegisterId = request.RegisterId,
        BlueprintId = request.BlueprintId,
        Data = request.Data,
        Signature = request.Signature,
        Timestamp = DateTimeOffset.UtcNow,
        Status = "Confirmed"
    };

    store.Add(transaction);

    return Results.Ok(new TransactionSubmitResponse
    {
        TransactionId = transaction.Id,
        Status = transaction.Status,
        Timestamp = transaction.Timestamp
    });
})
.WithName("SubmitTransaction")
.WithSummary("Submit a transaction to the register")
.WithDescription("Submits a signed transaction to be stored in the distributed ledger.");

/// <summary>
/// Get transaction by ID
/// </summary>
registerGroup.MapGet("/transactions/{id}", (
    TransactionStore store,
    string id) =>
{
    var transaction = store.GetById(id);
    return transaction is not null ? Results.Ok(transaction) : Results.NotFound();
})
.WithName("GetTransaction")
.WithSummary("Get transaction by ID")
.WithDescription("Retrieves a transaction from the register by its ID.");

/// <summary>
/// Get transactions by wallet address
/// </summary>
registerGroup.MapGet("/wallets/{address}/transactions", (
    TransactionStore store,
    string address,
    int page = 1,
    int pageSize = 20) =>
{
    var transactions = store.GetByWallet(address, page, pageSize);
    return Results.Ok(new
    {
        Page = page,
        PageSize = pageSize,
        Total = transactions.Count,
        Transactions = transactions
    });
})
.WithName("GetTransactionsByWallet")
.WithSummary("Get transactions by wallet")
.WithDescription("Retrieves all transactions for a specific wallet address.");

/// <summary>
/// Get transactions by register
/// </summary>
registerGroup.MapGet("/registers/{registerId}/transactions", (
    TransactionStore store,
    string registerId,
    int page = 1,
    int pageSize = 20) =>
{
    var transactions = store.GetByRegister(registerId, page, pageSize);
    return Results.Ok(new
    {
        Page = page,
        PageSize = pageSize,
        Total = transactions.Count,
        Transactions = transactions
    });
})
.WithName("GetTransactionsByRegister")
.WithSummary("Get transactions by register")
.WithDescription("Retrieves all transactions for a specific register.");

/// <summary>
/// Get register statistics
/// </summary>
registerGroup.MapGet("/stats", (TransactionStore store) =>
{
    var stats = new
    {
        TotalTransactions = store.Count(),
        UniqueWallets = store.GetUniqueWalletCount(),
        UniqueRegisters = store.GetUniqueRegisterCount(),
        LastTransaction = store.GetLatest()?.Timestamp
    };
    return Results.Ok(stats);
})
.WithName("GetRegisterStats")
.WithSummary("Get register statistics")
.WithDescription("Retrieves statistics about the register.");

app.Run();

// ===========================
// Models
// ===========================

record TransactionSubmitRequest(
    string WalletAddress,
    string RegisterId,
    string? BlueprintId,
    string Data,
    string Signature);

record TransactionSubmitResponse
{
    public required string TransactionId { get; set; }
    public required string Status { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
}

class StoredTransaction
{
    public required string Id { get; set; }
    public required string WalletAddress { get; set; }
    public required string RegisterId { get; set; }
    public string? BlueprintId { get; set; }
    public required string Data { get; set; }
    public required string Signature { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
    public required string Status { get; set; }
}

/// <summary>
/// In-memory transaction storage for MVP/testing
/// </summary>
class TransactionStore
{
    private readonly ConcurrentDictionary<string, StoredTransaction> _transactions = new();

    public void Add(StoredTransaction transaction)
    {
        _transactions[transaction.Id] = transaction;
    }

    public StoredTransaction? GetById(string id)
    {
        _transactions.TryGetValue(id, out var transaction);
        return transaction;
    }

    public List<StoredTransaction> GetByWallet(string address, int page, int pageSize)
    {
        return _transactions.Values
            .Where(t => t.WalletAddress == address)
            .OrderByDescending(t => t.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public List<StoredTransaction> GetByRegister(string registerId, int page, int pageSize)
    {
        return _transactions.Values
            .Where(t => t.RegisterId == registerId)
            .OrderByDescending(t => t.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public int Count() => _transactions.Count;

    public int GetUniqueWalletCount() => _transactions.Values.Select(t => t.WalletAddress).Distinct().Count();

    public int GetUniqueRegisterCount() => _transactions.Values.Select(t => t.RegisterId).Distinct().Count();

    public StoredTransaction? GetLatest() => _transactions.Values.OrderByDescending(t => t.Timestamp).FirstOrDefault();
}
