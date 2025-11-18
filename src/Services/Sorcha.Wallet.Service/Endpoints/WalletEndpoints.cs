// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.AspNetCore.Mvc;
using Sorcha.Wallet.Service.Mappers;
using Sorcha.Wallet.Service.Models;
using Sorcha.Wallet.Core.Domain.ValueObjects;
using Sorcha.Wallet.Core.Services.Implementation;
using System.Security.Claims;

namespace Sorcha.Wallet.Service.Endpoints;

/// <summary>
/// Wallet management minimal API endpoints
/// </summary>
public static class WalletEndpoints
{
    /// <summary>
    /// Map all wallet-related endpoints
    /// </summary>
    public static IEndpointRouteBuilder MapWalletEndpoints(this IEndpointRouteBuilder app)
    {
        var walletGroup = app.MapGroup("/api/v1/wallets")
            .WithTags("Wallets")
            .WithOpenApi();

        // POST /api/v1/wallets - Create new wallet
        walletGroup.MapPost("/", CreateWallet)
            .WithName("CreateWallet")
            .WithSummary("Create a new wallet")
            .WithDescription("Creates a new HD wallet with the specified algorithm and returns the mnemonic phrase for backup");

        // POST /api/v1/wallets/recover - Recover wallet from mnemonic
        walletGroup.MapPost("/recover", RecoverWallet)
            .WithName("RecoverWallet")
            .WithSummary("Recover a wallet from mnemonic phrase")
            .WithDescription("Recovers an existing wallet from a BIP39 mnemonic phrase");

        // GET /api/v1/wallets - List wallets for current user
        walletGroup.MapGet("/", ListWallets)
            .WithName("ListWallets")
            .WithSummary("List wallets for current user")
            .WithDescription("Retrieve all wallets owned by the current user in the current tenant");

        // GET /api/v1/wallets/{address} - Get wallet by address
        walletGroup.MapGet("/{address}", GetWallet)
            .WithName("GetWallet")
            .WithSummary("Get wallet by address")
            .WithDescription("Retrieve detailed information about a specific wallet");

        // PATCH /api/v1/wallets/{address} - Update wallet metadata
        walletGroup.MapPatch("/{address}", UpdateWallet)
            .WithName("UpdateWallet")
            .WithSummary("Update wallet metadata")
            .WithDescription("Update wallet name and tags");

        // DELETE /api/v1/wallets/{address} - Delete wallet (soft delete)
        walletGroup.MapDelete("/{address}", DeleteWallet)
            .WithName("DeleteWallet")
            .WithSummary("Delete wallet")
            .WithDescription("Soft delete a wallet (can be recovered by support)");

        // POST /api/v1/wallets/{address}/sign - Sign transaction
        walletGroup.MapPost("/{address}/sign", SignTransaction)
            .WithName("SignTransaction")
            .WithSummary("Sign a transaction")
            .WithDescription("Sign transaction data with the wallet's private key");

        // POST /api/v1/wallets/{address}/decrypt - Decrypt payload
        walletGroup.MapPost("/{address}/decrypt", DecryptPayload)
            .WithName("DecryptPayload")
            .WithSummary("Decrypt a payload")
            .WithDescription("Decrypt an encrypted payload using the wallet's private key");

        // POST /api/v1/wallets/{address}/encrypt - Encrypt payload
        walletGroup.MapPost("/{address}/encrypt", EncryptPayload)
            .WithName("EncryptPayload")
            .WithSummary("Encrypt a payload")
            .WithDescription("Encrypt a payload for a recipient wallet using their public key");

        // POST /api/v1/wallets/{address}/addresses - Generate new address
        walletGroup.MapPost("/{address}/addresses", GenerateAddress)
            .WithName("GenerateAddress")
            .WithSummary("Generate a new address")
            .WithDescription("Generate a new derived address for the wallet (requires mnemonic - not yet implemented)");

        return app;
    }

    /// <summary>
    /// Create a new wallet
    /// </summary>
    private static async Task<IResult> CreateWallet(
        [FromBody] CreateWalletRequest request,
        WalletManager walletManager,
        HttpContext context,
        ILogger<Program> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var owner = GetCurrentUser(context);
            var tenant = GetCurrentTenant(context);

            logger.LogInformation("Creating wallet for user {Owner} in tenant {Tenant}", owner, tenant);

            var (wallet, mnemonic) = await walletManager.CreateWalletAsync(
                request.Name,
                request.Algorithm,
                owner,
                tenant,
                request.WordCount,
                request.Passphrase,
                cancellationToken);

            var response = new CreateWalletResponse
            {
                Wallet = wallet.ToDto(),
                MnemonicWords = mnemonic.Phrase.Split(' ')
            };

            return Results.Created($"/api/v1/wallets/{wallet.Address}", response);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid wallet creation request");
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create wallet");
            return Results.Problem(
                title: "Wallet Creation Failed",
                detail: "An error occurred while creating the wallet",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Recover a wallet from mnemonic phrase
    /// </summary>
    private static async Task<IResult> RecoverWallet(
        [FromBody] RecoverWalletRequest request,
        WalletManager walletManager,
        HttpContext context,
        ILogger<Program> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var owner = GetCurrentUser(context);
            var tenant = GetCurrentTenant(context);

            logger.LogInformation("Recovering wallet for user {Owner} in tenant {Tenant}", owner, tenant);

            var mnemonic = new Mnemonic(string.Join(" ", request.MnemonicWords));

            var wallet = await walletManager.RecoverWalletAsync(
                mnemonic,
                request.Name,
                request.Algorithm,
                owner,
                tenant,
                request.Passphrase,
                cancellationToken);

            return Results.Ok(wallet.ToDto());
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid wallet recovery request");
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            logger.LogWarning(ex, "Wallet already exists");
            return Results.Conflict(new ProblemDetails
            {
                Title = "Wallet Already Exists",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to recover wallet");
            return Results.Problem(
                title: "Wallet Recovery Failed",
                detail: "An error occurred while recovering the wallet",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get wallet by address
    /// </summary>
    private static async Task<IResult> GetWallet(
        string address,
        WalletManager walletManager,
        CancellationToken cancellationToken = default)
    {
        var wallet = await walletManager.GetWalletAsync(address, cancellationToken);

        if (wallet == null)
        {
            return Results.NotFound();
        }

        // TODO: Add authorization check - user should own the wallet or have delegated access
        return Results.Ok(wallet.ToDto());
    }

    /// <summary>
    /// List wallets for current user
    /// </summary>
    private static async Task<IResult> ListWallets(
        HttpContext context,
        WalletManager walletManager,
        CancellationToken cancellationToken = default)
    {
        var owner = GetCurrentUser(context);
        var tenant = GetCurrentTenant(context);

        var wallets = await walletManager.GetWalletsByOwnerAsync(owner, tenant, cancellationToken);

        return Results.Ok(wallets.Select(w => w.ToDto()));
    }

    /// <summary>
    /// Update wallet metadata
    /// </summary>
    private static async Task<IResult> UpdateWallet(
        string address,
        [FromBody] UpdateWalletRequest request,
        WalletManager walletManager,
        ILogger<Program> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var wallet = await walletManager.UpdateWalletAsync(
                address,
                request.Name,
                tags: request.Tags,
                cancellationToken: cancellationToken);

            return Results.Ok(wallet.ToDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return Results.NotFound();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update wallet {Address}", address);
            return Results.Problem(
                title: "Wallet Update Failed",
                detail: "An error occurred while updating the wallet",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Delete (soft delete) a wallet
    /// </summary>
    private static async Task<IResult> DeleteWallet(
        string address,
        WalletManager walletManager,
        ILogger<Program> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await walletManager.DeleteWalletAsync(address, cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return Results.NotFound();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete wallet {Address}", address);
            return Results.Problem(
                title: "Wallet Deletion Failed",
                detail: "An error occurred while deleting the wallet",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Sign a transaction with a wallet
    /// </summary>
    private static async Task<IResult> SignTransaction(
        string address,
        [FromBody] SignTransactionRequest request,
        WalletManager walletManager,
        ILogger<Program> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var transactionData = Convert.FromBase64String(request.TransactionData);
            var signature = await walletManager.SignTransactionAsync(
                address,
                transactionData,
                cancellationToken);

            var response = new SignTransactionResponse
            {
                Signature = Convert.ToBase64String(signature),
                SignedBy = address,
                SignedAt = DateTime.UtcNow
            };

            return Results.Ok(response);
        }
        catch (FormatException ex)
        {
            logger.LogWarning(ex, "Invalid base64 transaction data");
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to sign transaction for wallet {Address}", address);
            return Results.Problem(
                title: "Transaction Signing Failed",
                detail: "An error occurred while signing the transaction",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Decrypt a payload using a wallet's private key
    /// </summary>
    private static async Task<IResult> DecryptPayload(
        string address,
        [FromBody] DecryptPayloadRequest request,
        WalletManager walletManager,
        ILogger<Program> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var encryptedPayload = Convert.FromBase64String(request.EncryptedPayload);
            var decryptedPayload = await walletManager.DecryptPayloadAsync(
                address,
                encryptedPayload,
                cancellationToken);

            var response = new DecryptPayloadResponse
            {
                DecryptedPayload = Convert.ToBase64String(decryptedPayload),
                DecryptedBy = address,
                DecryptedAt = DateTime.UtcNow
            };

            return Results.Ok(response);
        }
        catch (FormatException ex)
        {
            logger.LogWarning(ex, "Invalid base64 encrypted payload");
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to decrypt payload for wallet {Address}", address);
            return Results.Problem(
                title: "Payload Decryption Failed",
                detail: "An error occurred while decrypting the payload",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Encrypt a payload for a recipient wallet
    /// </summary>
    private static async Task<IResult> EncryptPayload(
        string address,
        [FromBody] EncryptPayloadRequest request,
        WalletManager walletManager,
        ILogger<Program> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use RecipientAddress from request body if provided, otherwise use address from route
            var recipientAddress = request.RecipientAddress ?? address;

            var payload = Convert.FromBase64String(request.Payload);
            var encryptedPayload = await walletManager.EncryptPayloadAsync(
                recipientAddress,
                payload,
                cancellationToken);

            var response = new EncryptPayloadResponse
            {
                EncryptedPayload = Convert.ToBase64String(encryptedPayload),
                RecipientAddress = recipientAddress,
                EncryptedAt = DateTime.UtcNow
            };

            return Results.Ok(response);
        }
        catch (FormatException ex)
        {
            logger.LogWarning(ex, "Invalid base64 payload");
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to encrypt payload for recipient {Address}", address);
            return Results.Problem(
                title: "Payload Encryption Failed",
                detail: "An error occurred while encrypting the payload",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Generate a new address for a wallet
    /// </summary>
    private static async Task<IResult> GenerateAddress(
        string address,
        [FromBody] GenerateAddressRequest request,
        ILogger<Program> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse derivation path or use default
            // Note: This functionality requires the wallet's mnemonic which is not stored
            // This is a placeholder for future implementation
            throw new NotImplementedException(
                "Address generation requires the wallet's mnemonic, which is not stored for security. " +
                "Consider implementing this via a secure enclave or requiring the user to provide their mnemonic.");
        }
        catch (NotImplementedException ex)
        {
            logger.LogWarning("Address generation attempted but not implemented: {Message}", ex.Message);
            return Results.Json(
                new ProblemDetails
                {
                    Title = "Not Implemented",
                    Detail = ex.Message,
                    Status = StatusCodes.Status501NotImplemented
                },
                statusCode: StatusCodes.Status501NotImplemented);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return Results.NotFound();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate address for wallet {Address}", address);
            return Results.Problem(
                title: "Address Generation Failed",
                detail: "An error occurred while generating the address",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    // Helper methods for authentication/authorization
    private static string GetCurrentUser(HttpContext context)
    {
        // TODO: Extract from JWT claims
        return context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
    }

    private static string GetCurrentTenant(HttpContext context)
    {
        // TODO: Extract from JWT claims or headers
        return context.User.FindFirstValue("tenant") ?? "default";
    }
}
