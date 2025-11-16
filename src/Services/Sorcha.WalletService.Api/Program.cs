// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Scalar.AspNetCore;
using Sorcha.WalletService.Domain.ValueObjects;
using Sorcha.WalletService.Encryption.Providers;
using Sorcha.WalletService.Events.Publishers;
using Sorcha.WalletService.Repositories.Implementation;
using Sorcha.WalletService.Services.Implementation;
using Sorcha.WalletService.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add OpenAPI services
builder.Services.AddOpenApi();

// Register Wallet Service dependencies
builder.Services.AddSingleton<Sorcha.WalletService.Repositories.Interfaces.IWalletRepository, InMemoryWalletRepository>();
builder.Services.AddSingleton<Sorcha.WalletService.Encryption.Interfaces.IEncryptionProvider, LocalEncryptionProvider>();
builder.Services.AddSingleton<Sorcha.WalletService.Events.Interfaces.IEventPublisher, InMemoryEventPublisher>();
builder.Services.AddScoped<IKeyManagementService, KeyManagementService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IDelegationService, DelegationService>();
builder.Services.AddScoped<IWalletService, WalletManager>();

// Add Sorcha.Cryptography services
builder.Services.AddSingleton<Sorcha.Cryptography.Interfaces.ICryptoModule, Sorcha.Cryptography.Core.CryptoModule>();
builder.Services.AddSingleton<Sorcha.Cryptography.Interfaces.IWalletUtilities, Sorcha.Cryptography.Utilities.WalletUtilities>();

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
            .WithTitle("Wallet Service API")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// ===========================
// Wallet Management Endpoints
// ===========================

var walletGroup = app.MapGroup("/api/wallets")
    .WithTags("Wallets")
    .WithOpenApi();

/// <summary>
/// Create a new wallet with a randomly generated mnemonic
/// </summary>
walletGroup.MapPost("/", async (
    IWalletService walletService,
    CreateWalletRequest request) =>
{
    var (wallet, mnemonic) = await walletService.CreateWalletAsync(
        request.Name,
        request.Algorithm ?? "ED25519",
        request.Owner,
        request.Tenant,
        request.WordCount ?? 12,
        request.Passphrase);

    return Results.Ok(new CreateWalletResponse
    {
        Wallet = wallet,
        Mnemonic = mnemonic.Phrase,
        Warning = "IMPORTANT: Save this mnemonic phrase securely. It cannot be recovered if lost."
    });
})
.WithName("CreateWallet")
.WithSummary("Create a new wallet")
.WithDescription("Creates a new wallet with a randomly generated BIP39 mnemonic. The mnemonic MUST be saved by the caller.");

/// <summary>
/// Recover a wallet from an existing mnemonic
/// </summary>
walletGroup.MapPost("/recover", async (
    IWalletService walletService,
    RecoverWalletRequest request) =>
{
    var mnemonic = new Mnemonic(request.MnemonicPhrase);
    var wallet = await walletService.RecoverWalletAsync(
        mnemonic,
        request.Name,
        request.Algorithm ?? "ED25519",
        request.Owner,
        request.Tenant,
        request.Passphrase);

    return Results.Ok(wallet);
})
.WithName("RecoverWallet")
.WithSummary("Recover a wallet from mnemonic")
.WithDescription("Recovers a wallet from an existing BIP39 mnemonic phrase.");

/// <summary>
/// Get wallet by address
/// </summary>
walletGroup.MapGet("/{address}", async (
    IWalletService walletService,
    string address) =>
{
    var wallet = await walletService.GetWalletAsync(address);
    return wallet is not null ? Results.Ok(wallet) : Results.NotFound();
})
.WithName("GetWallet")
.WithSummary("Get wallet by address")
.WithDescription("Retrieves a wallet by its address.");

/// <summary>
/// Get all wallets for an owner
/// </summary>
walletGroup.MapGet("/", async (
    IWalletService walletService,
    string owner,
    string tenant) =>
{
    var wallets = await walletService.GetWalletsByOwnerAsync(owner, tenant);
    return Results.Ok(wallets);
})
.WithName("GetWalletsByOwner")
.WithSummary("Get wallets by owner")
.WithDescription("Retrieves all wallets for a specific owner and tenant.");

/// <summary>
/// Update wallet metadata
/// </summary>
walletGroup.MapPut("/{address}", async (
    IWalletService walletService,
    string address,
    UpdateWalletRequest request,
    CancellationToken cancellationToken) =>
{
    var wallet = await walletService.UpdateWalletAsync(
        address,
        request.Name,
        request.Description,
        request.Tags,
        cancellationToken);
    return Results.Ok(wallet);
})
.WithName("UpdateWallet")
.WithSummary("Update wallet metadata")
.WithDescription("Updates wallet name, description, and tags.");

/// <summary>
/// Delete wallet (soft delete)
/// </summary>
walletGroup.MapDelete("/{address}", async (
    IWalletService walletService,
    string address) =>
{
    await walletService.DeleteWalletAsync(address);
    return Results.NoContent();
})
.WithName("DeleteWallet")
.WithSummary("Delete wallet")
.WithDescription("Soft deletes a wallet by marking it as deleted.");

// ===========================
// Transaction Endpoints
// ===========================

var txGroup = app.MapGroup("/api/wallets/{address}/transactions")
    .WithTags("Transactions")
    .WithOpenApi();

/// <summary>
/// Sign transaction data
/// </summary>
txGroup.MapPost("/sign", async (
    IWalletService walletService,
    string address,
    SignTransactionRequest request,
    CancellationToken cancellationToken) =>
{
    var signatureBytes = await walletService.SignTransactionAsync(
        address,
        request.TransactionData,
        cancellationToken);
    
    return Results.Ok(new SignTransactionResponse
    {
        Signature = Convert.ToBase64String(signatureBytes),
        Algorithm = "ED25519" // TODO: Get from wallet
    });
})
.WithName("SignTransaction")
.WithSummary("Sign transaction data")
.WithDescription("Signs transaction data using the wallet's private key.");

// ===========================
// Delegation Endpoints (Commented out for MVP - needs proper auth context)
// ===========================

// var delegateGroup = app.MapGroup("/api/wallets/{address}/delegates")
//     .WithTags("Delegation")
//     .WithOpenApi();

// TODO: Implement delegation endpoints with proper authentication context
// The IDelegationService requires grantedBy/revokedBy parameters that need to come from auth context

app.Run();

// ===========================
// Request/Response Models
// ===========================

record CreateWalletRequest(
    string Name,
    string Owner,
    string Tenant,
    string? Algorithm = "ED25519",
    int? WordCount = 12,
    string? Passphrase = null);

record CreateWalletResponse
{
    public required object Wallet { get; set; }
    public required string Mnemonic { get; set; }
    public required string Warning { get; set; }
}

record RecoverWalletRequest(
    string MnemonicPhrase,
    string Name,
    string Owner,
    string Tenant,
    string? Algorithm = "ED25519",
    string? Passphrase = null);

record UpdateWalletRequest(
    string? Name = null,
    string? Description = null,
    Dictionary<string, string>? Tags = null);

record SignTransactionRequest(
    byte[] TransactionData,
    Dictionary<string, string>? Metadata = null);

record SignTransactionResponse
{
    public required string Signature { get; set; }
    public required string Algorithm { get; set; }
}

record GrantAccessRequest(
    string DelegateAddress,
    string AccessRight,
    DateTime? ExpiresAt = null);
