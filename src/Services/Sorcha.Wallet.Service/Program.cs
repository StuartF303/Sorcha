// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;
using Sorcha.Wallet.Service.Domain;
using Sorcha.Wallet.Service.Domain.ValueObjects;
using Sorcha.Wallet.Service.Encryption.Providers;
using Sorcha.Wallet.Service.Events.Publishers;
using Sorcha.Wallet.Service.Repositories.Implementation;
using Sorcha.Wallet.Service.Services.Implementation;
using Sorcha.Wallet.Service.Services.Interfaces;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add OpenAPI services
builder.Services.AddOpenApi();

// Register Wallet Service dependencies
builder.Services.AddSingleton<Sorcha.Wallet.Service.Repositories.Interfaces.IWalletRepository, InMemoryWalletRepository>();
builder.Services.AddSingleton<Sorcha.Wallet.Service.Encryption.Interfaces.IEncryptionProvider, LocalEncryptionProvider>();
builder.Services.AddSingleton<Sorcha.Wallet.Service.Events.Interfaces.IEventPublisher, InMemoryEventPublisher>();
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

// Helper methods for authentication context
string GetCurrentUser(ClaimsPrincipal user) => 
    user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";

string GetCurrentTenant(ClaimsPrincipal user) => 
    user.FindFirstValue("tenant") ?? "default";

// ===========================
// Wallet Management Endpoints
// ===========================

var walletGroup = app.MapGroup("/api/v1/wallets")
    .WithTags("Wallets")
    .WithOpenApi();

walletGroup.MapPost("/", async (
    IWalletService walletService,
    ClaimsPrincipal user,
    CreateWalletRequest request,
    CancellationToken cancellationToken) =>
{
    try
    {
        var owner = GetCurrentUser(user);
        var tenant = GetCurrentTenant(user);

        var (wallet, mnemonic) = await walletService.CreateWalletAsync(
            request.Name,
            request.Algorithm ?? "ED25519",
            owner,
            tenant,
            request.WordCount ?? 12,
            request.Passphrase,
            cancellationToken);

        return Results.Created(
            $"/api/v1/wallets/{wallet.Address}",
            new CreateWalletResponse
            {
                Wallet = wallet,
                MnemonicWords = mnemonic.Phrase.Split(' '),
                Warning = "IMPORTANT: Save this mnemonic phrase securely. It cannot be recovered if lost."
            });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new ProblemDetails
        {
            Title = "Invalid Request",
            Detail = ex.Message,
            Status = StatusCodes.Status400BadRequest
        });
    }
})
.WithName("CreateWallet")
.WithSummary("Create a new wallet")
.WithDescription("Creates a new wallet with a randomly generated BIP39 mnemonic. The mnemonic MUST be saved by the caller.");

walletGroup.MapPost("/recover", async (
    IWalletService walletService,
    ClaimsPrincipal user,
    RecoverWalletRequest request,
    CancellationToken cancellationToken) =>
{
    try
    {
        var owner = GetCurrentUser(user);
        var tenant = GetCurrentTenant(user);

        var mnemonic = new Mnemonic(string.Join(" ", request.MnemonicWords));
        var wallet = await walletService.RecoverWalletAsync(
            mnemonic,
            request.Name,
            request.Algorithm ?? "ED25519",
            owner,
            tenant,
            request.Passphrase,
            cancellationToken);

        return Results.Ok(wallet);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new ProblemDetails
        {
            Title = "Invalid Request",
            Detail = ex.Message,
            Status = StatusCodes.Status400BadRequest
        });
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
    {
        return Results.Conflict(new ProblemDetails
        {
            Title = "Wallet Already Exists",
            Detail = ex.Message,
            Status = StatusCodes.Status409Conflict
        });
    }
})
.WithName("RecoverWallet")
.WithSummary("Recover a wallet from mnemonic")
.WithDescription("Recovers a wallet from an existing BIP39 mnemonic phrase.");

walletGroup.MapGet("/{address}", async (
    IWalletService walletService,
    string address,
    CancellationToken cancellationToken) =>
{
    var wallet = await walletService.GetWalletAsync(address, cancellationToken);
    return wallet is not null ? Results.Ok(wallet) : Results.NotFound();
})
.WithName("GetWallet")
.WithSummary("Get wallet by address")
.WithDescription("Retrieves a wallet by its address.");

walletGroup.MapGet("/", async (
    IWalletService walletService,
    ClaimsPrincipal user,
    CancellationToken cancellationToken) =>
{
    var owner = GetCurrentUser(user);
    var tenant = GetCurrentTenant(user);

    var wallets = await walletService.GetWalletsByOwnerAsync(owner, tenant, cancellationToken);
    return Results.Ok(wallets);
})
.WithName("ListWallets")
.WithSummary("List wallets for current user")
.WithDescription("Retrieves all wallets for the authenticated user.");

walletGroup.MapPatch("/{address}", async (
    IWalletService walletService,
    string address,
    UpdateWalletRequest request,
    CancellationToken cancellationToken) =>
{
    try
    {
        var wallet = await walletService.UpdateWalletAsync(
            address,
            request.Name,
            tags: request.Tags,
            cancellationToken: cancellationToken);

        return Results.Ok(wallet);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
    {
        return Results.NotFound();
    }
})
.WithName("UpdateWallet")
.WithSummary("Update wallet metadata")
.WithDescription("Updates wallet name, description, and tags.");

walletGroup.MapDelete("/{address}", async (
    IWalletService walletService,
    string address,
    CancellationToken cancellationToken) =>
{
    try
    {
        await walletService.DeleteWalletAsync(address, cancellationToken);
        return Results.NoContent();
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
    {
        return Results.NotFound();
    }
})
.WithName("DeleteWallet")
.WithSummary("Delete wallet")
.WithDescription("Soft deletes a wallet by marking it as deleted.");

// ===========================
// Transaction & Crypto Endpoints
// ===========================

walletGroup.MapPost("/{address}/sign", async (
    IWalletService walletService,
    string address,
    SignTransactionRequest request,
    CancellationToken cancellationToken) =>
{
    try
    {
        var transactionData = Convert.FromBase64String(request.TransactionData);
        var signature = await walletService.SignTransactionAsync(
            address,
            transactionData,
            cancellationToken);

        return Results.Ok(new SignTransactionResponse
        {
            Signature = Convert.ToBase64String(signature),
            SignedBy = address,
            SignedAt = DateTime.UtcNow
        });
    }
    catch (FormatException)
    {
        return Results.BadRequest(new ProblemDetails
        {
            Title = "Invalid Request",
            Detail = "Transaction data must be valid base64 encoded string",
            Status = StatusCodes.Status400BadRequest
        });
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
    {
        return Results.NotFound();
    }
})
.WithName("SignTransaction")
.WithSummary("Sign transaction data")
.WithDescription("Signs transaction data using the wallet's private key.");

walletGroup.MapPost("/{address}/decrypt", async (
    IWalletService walletService,
    string address,
    DecryptPayloadRequest request,
    CancellationToken cancellationToken) =>
{
    try
    {
        var encryptedPayload = Convert.FromBase64String(request.EncryptedPayload);
        var decryptedPayload = await walletService.DecryptPayloadAsync(
            address,
            encryptedPayload,
            cancellationToken);

        return Results.Ok(new DecryptPayloadResponse
        {
            DecryptedPayload = Convert.ToBase64String(decryptedPayload),
            DecryptedBy = address,
            DecryptedAt = DateTime.UtcNow
        });
    }
    catch (FormatException)
    {
        return Results.BadRequest(new ProblemDetails
        {
            Title = "Invalid Request",
            Detail = "Encrypted payload must be valid base64 encoded string",
            Status = StatusCodes.Status400BadRequest
        });
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
    {
        return Results.NotFound();
    }
})
.WithName("DecryptPayload")
.WithSummary("Decrypt a payload")
.WithDescription("Decrypts a payload using the wallet's private key.");

walletGroup.MapPost("/{address}/encrypt", async (
    IWalletService walletService,
    string address,
    EncryptPayloadRequest request,
    CancellationToken cancellationToken) =>
{
    try
    {
        var recipientAddress = request.RecipientAddress ?? address;
        var payload = Convert.FromBase64String(request.Payload);
        var encryptedPayload = await walletService.EncryptPayloadAsync(
            recipientAddress,
            payload,
            cancellationToken);

        return Results.Ok(new EncryptPayloadResponse
        {
            EncryptedPayload = Convert.ToBase64String(encryptedPayload),
            RecipientAddress = recipientAddress,
            EncryptedAt = DateTime.UtcNow
        });
    }
    catch (FormatException)
    {
        return Results.BadRequest(new ProblemDetails
        {
            Title = "Invalid Request",
            Detail = "Payload must be valid base64 encoded string",
            Status = StatusCodes.Status400BadRequest
        });
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
    {
        return Results.NotFound();
    }
})
.WithName("EncryptPayload")
.WithSummary("Encrypt a payload")
.WithDescription("Encrypts a payload for a recipient wallet.");

// ===========================
// Delegation & Access Control Endpoints
// ===========================

var accessGroup = app.MapGroup("/api/v1/wallets/{walletAddress}/access")
    .WithTags("Access Control")
    .WithOpenApi();

accessGroup.MapPost("/", async (
    IDelegationService delegationService,
    ClaimsPrincipal user,
    string walletAddress,
    GrantAccessRequest request,
    CancellationToken cancellationToken) =>
{
    try
    {
        var grantedBy = GetCurrentUser(user);

        if (!Enum.TryParse<AccessRight>(request.AccessRight, out var accessRight))
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid Access Right",
                Detail = $"Invalid access right: {request.AccessRight}. Valid values are: Owner, ReadWrite, ReadOnly",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var access = await delegationService.GrantAccessAsync(
            walletAddress,
            request.Subject,
            accessRight,
            grantedBy,
            request.Reason,
            request.ExpiresAt,
            cancellationToken);

        return Results.Created($"/api/v1/wallets/{walletAddress}/access", access);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new ProblemDetails
        {
            Title = "Invalid Request",
            Detail = ex.Message,
            Status = StatusCodes.Status400BadRequest
        });
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
    {
        return Results.NotFound();
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
    {
        return Results.Conflict(new ProblemDetails
        {
            Title = "Access Already Exists",
            Detail = ex.Message,
            Status = StatusCodes.Status409Conflict
        });
    }
})
.WithName("GrantAccess")
.WithSummary("Grant access to a wallet")
.WithDescription("Grants a subject access to perform operations on a wallet.");

accessGroup.MapGet("/", async (
    IDelegationService delegationService,
    string walletAddress,
    CancellationToken cancellationToken) =>
{
    var access = await delegationService.GetActiveAccessAsync(walletAddress, cancellationToken);
    return Results.Ok(access);
})
.WithName("ListAccess")
.WithSummary("List active access grants")
.WithDescription("Retrieves all active access grants for a wallet.");

accessGroup.MapDelete("/{subject}", async (
    IDelegationService delegationService,
    ClaimsPrincipal user,
    string walletAddress,
    string subject,
    CancellationToken cancellationToken) =>
{
    try
    {
        var revokedBy = GetCurrentUser(user);

        await delegationService.RevokeAccessAsync(
            walletAddress,
            subject,
            revokedBy,
            cancellationToken);

        return Results.NoContent();
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
    {
        return Results.NotFound();
    }
})
.WithName("RevokeAccess")
.WithSummary("Revoke access to a wallet")
.WithDescription("Revokes a subject's access to a wallet.");

accessGroup.MapGet("/{subject}/check", async (
    IDelegationService delegationService,
    string walletAddress,
    string subject,
    string requiredRight = "ReadOnly",
    CancellationToken cancellationToken = default) =>
{
    if (!Enum.TryParse<AccessRight>(requiredRight, out var accessRight))
    {
        return Results.BadRequest(new ProblemDetails
        {
            Title = "Invalid Access Right",
            Detail = $"Invalid access right: {requiredRight}. Valid values are: Owner, ReadWrite, ReadOnly",
            Status = StatusCodes.Status400BadRequest
        });
    }

    var hasAccess = await delegationService.HasAccessAsync(
        walletAddress,
        subject,
        accessRight,
        cancellationToken);

    return Results.Ok(new AccessCheckResponse
    {
        WalletAddress = walletAddress,
        Subject = subject,
        RequiredRight = requiredRight,
        HasAccess = hasAccess
    });
})
.WithName("CheckAccess")
.WithSummary("Check wallet access")
.WithDescription("Checks if a subject has the required access to a wallet.");

app.Run();

// ===========================
// Request/Response Models
// ===========================

record CreateWalletRequest(
    string Name,
    string? Algorithm = "ED25519",
    int? WordCount = 12,
    string? Passphrase = null);

record CreateWalletResponse
{
    public required object Wallet { get; set; }
    public required string[] MnemonicWords { get; set; }
    public required string Warning { get; set; }
}

record RecoverWalletRequest(
    string[] MnemonicWords,
    string Name,
    string? Algorithm = "ED25519",
    string? Passphrase = null);

record UpdateWalletRequest(
    string? Name = null,
    Dictionary<string, string>? Tags = null);

record SignTransactionRequest(
    string TransactionData);

record SignTransactionResponse
{
    public required string Signature { get; set; }
    public required string SignedBy { get; set; }
    public DateTime SignedAt { get; set; }
}

record DecryptPayloadRequest(
    string EncryptedPayload);

record DecryptPayloadResponse
{
    public required string DecryptedPayload { get; set; }
    public required string DecryptedBy { get; set; }
    public DateTime DecryptedAt { get; set; }
}

record EncryptPayloadRequest(
    string Payload,
    string? RecipientAddress = null);

record EncryptPayloadResponse
{
    public required string EncryptedPayload { get; set; }
    public required string RecipientAddress { get; set; }
    public DateTime EncryptedAt { get; set; }
}

record GrantAccessRequest(
    string Subject,
    string AccessRight,
    string? Reason = null,
    DateTime? ExpiresAt = null);

record AccessCheckResponse
{
    public required string WalletAddress { get; set; }
    public required string Subject { get; set; }
    public required string RequiredRight { get; set; }
    public bool HasAccess { get; set; }
}
