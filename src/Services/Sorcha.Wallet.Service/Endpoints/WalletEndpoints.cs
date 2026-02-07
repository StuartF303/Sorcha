// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.AspNetCore.Mvc;
using Sorcha.Wallet.Service.Mappers;
using Sorcha.Wallet.Service.Models;
using Sorcha.Wallet.Core.Domain.ValueObjects;
using Sorcha.Wallet.Core.Domain;
using Sorcha.Wallet.Core.Domain.Entities;
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
            .RequireAuthorization("CanManageWallets");

        // POST /api/v1/wallets/system - Create or retrieve system wallet (for validators)
        walletGroup.MapPost("/system", CreateOrRetrieveSystemWallet)
            .WithName("CreateOrRetrieveSystemWallet")
            .WithSummary("Create or retrieve system wallet")
            .WithDescription("Creates or retrieves a system wallet for a validator. Used by Validator Service for signing operations.")
            .AllowAnonymous(); // System wallets are service-to-service, use service auth

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

        // POST /api/v1/wallets/{address}/addresses - Register derived address
        walletGroup.MapPost("/{address}/addresses", RegisterDerivedAddress)
            .WithName("RegisterDerivedAddress")
            .WithSummary("Register a client-derived HD address")
            .WithDescription("Register an HD wallet address that was derived client-side. " +
                "The client must derive the address using their mnemonic and provide the public key and derivation path. " +
                "This maintains security by never storing the mnemonic on the server.");

        // GET /api/v1/wallets/{address}/addresses - List derived addresses
        walletGroup.MapGet("/{address}/addresses", ListAddresses)
            .WithName("ListAddresses")
            .WithSummary("List wallet addresses")
            .WithDescription("List all derived addresses for a wallet with optional filtering by type (receive/change), used status, account, and labels");

        // GET /api/v1/wallets/{address}/addresses/{id} - Get specific address
        walletGroup.MapGet("/{address}/addresses/{id:guid}", GetAddress)
            .WithName("GetAddress")
            .WithSummary("Get address by ID")
            .WithDescription("Retrieve detailed information about a specific derived address");

        // PATCH /api/v1/wallets/{address}/addresses/{id} - Update address metadata
        walletGroup.MapPatch("/{address}/addresses/{id:guid}", UpdateAddress)
            .WithName("UpdateAddress")
            .WithSummary("Update address metadata")
            .WithDescription("Update address label, notes, tags, and metadata");

        // POST /api/v1/wallets/{address}/addresses/{id}/mark-used - Mark address as used
        walletGroup.MapPost("/{address}/addresses/{id:guid}/mark-used", MarkAddressAsUsed)
            .WithName("MarkAddressAsUsed")
            .WithSummary("Mark address as used")
            .WithDescription("Mark an address as used (received a transaction). Updates gap limit calculations.");

        // GET /api/v1/wallets/{address}/accounts - List accounts
        walletGroup.MapGet("/{address}/accounts", ListAccounts)
            .WithName("ListAccounts")
            .WithSummary("List BIP44 accounts")
            .WithDescription("List all BIP44 accounts for this wallet with address counts and gap status");

        // GET /api/v1/wallets/{address}/gap-status - Get gap limit status
        walletGroup.MapGet("/{address}/gap-status", GetGapStatus)
            .WithName("GetGapStatus")
            .WithSummary("Get gap limit status")
            .WithDescription("Check BIP44 gap limit compliance for all accounts. Shows unused address counts and warnings.");

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
            if (owner is null)
                return Results.Unauthorized();
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
            if (owner is null)
                return Results.Unauthorized();
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
        HttpContext context,
        WalletManager walletManager,
        DelegationService delegationService,
        CancellationToken cancellationToken = default)
    {
        var currentUser = GetCurrentUser(context);
        if (currentUser is null)
            return Results.Unauthorized();

        var wallet = await walletManager.GetWalletAsync(address, cancellationToken);

        if (wallet == null)
        {
            return Results.NotFound();
        }

        // Authorization: caller must be owner or have delegated access
        if (wallet.Owner != currentUser)
        {
            var hasAccess = await delegationService.HasAccessAsync(
                address, currentUser, AccessRight.ReadOnly, cancellationToken);
            if (!hasAccess)
            {
                return Results.Forbid();
            }
        }

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
        if (owner is null)
            return Results.Unauthorized();
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
            var (signature, publicKey) = await walletManager.SignTransactionAsync(
                address,
                transactionData,
                request.DerivationPath,
                request.IsPreHashed,
                cancellationToken);

            var response = new SignTransactionResponse
            {
                Signature = Convert.ToBase64String(signature),
                SignedBy = address,
                SignedAt = DateTime.UtcNow,
                PublicKey = Convert.ToBase64String(publicKey)
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
    /// Register a client-derived HD wallet address
    /// </summary>
    private static async Task<IResult> RegisterDerivedAddress(
        string address,
        [FromBody] RegisterDerivedAddressRequest request,
        WalletManager walletManager,
        ILogger<Program> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Registering derived address for wallet {WalletAddress}", address);

            // Register the client-derived address
            var walletAddress = await walletManager.RegisterDerivedAddressAsync(
                walletAddress: address,
                derivedPublicKey: request.DerivedPublicKey,
                derivedAddress: request.DerivedAddress,
                derivationPath: request.DerivationPath,
                label: request.Label,
                notes: request.Notes,
                tags: request.Tags,
                metadata: request.Metadata,
                cancellationToken: cancellationToken);

            // Map to DTO
            var dto = walletAddress.ToDto();

            logger.LogInformation(
                "Successfully registered address {DerivedAddress} at path {Path}",
                request.DerivedAddress, request.DerivationPath);

            return Results.Created($"/api/v1/wallets/{address}/addresses/{walletAddress.Id}", dto);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid request for wallet {Address}: {Message}", address, ex.Message);
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            logger.LogWarning("Wallet {Address} not found", address);
            return Results.NotFound(new ProblemDetails
            {
                Title = "Wallet Not Found",
                Detail = $"Wallet {address} not found",
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            logger.LogWarning(ex, "Duplicate address for wallet {Address}", address);
            return Results.Conflict(new ProblemDetails
            {
                Title = "Address Already Exists",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Gap limit"))
        {
            logger.LogWarning(ex, "Gap limit exceeded for wallet {Address}", address);
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Gap Limit Exceeded",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register derived address for wallet {Address}", address);
            return Results.Problem(
                title: "Address Registration Failed",
                detail: "An error occurred while registering the derived address",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// List all addresses for a wallet with optional filtering
    /// </summary>
    private static async Task<IResult> ListAddresses(
        string address,
        WalletManager walletManager,
        ILogger<Program> logger,
        [FromQuery] string? type = null,
        [FromQuery] bool? used = null,
        [FromQuery] uint? account = null,
        [FromQuery] string? label = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get wallet with addresses
            var wallet = await walletManager.GetWalletAsync(address, cancellationToken);
            if (wallet == null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Wallet Not Found",
                    Detail = $"Wallet {address} not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Apply filters
            var addresses = wallet.Addresses.AsEnumerable();

            if (!string.IsNullOrEmpty(type))
            {
                var isChange = type.Equals("change", StringComparison.OrdinalIgnoreCase);
                addresses = addresses.Where(a => a.IsChange == isChange);
            }

            if (used.HasValue)
            {
                addresses = addresses.Where(a => a.IsUsed == used.Value);
            }

            if (account.HasValue)
            {
                addresses = addresses.Where(a => a.Account == account.Value);
            }

            if (!string.IsNullOrEmpty(label))
            {
                addresses = addresses.Where(a => a.Label != null && a.Label.Contains(label, StringComparison.OrdinalIgnoreCase));
            }

            // Pagination
            var totalCount = addresses.Count();
            var paginatedAddresses = addresses
                .OrderBy(a => a.Account)
                .ThenBy(a => a.IsChange)
                .ThenBy(a => a.Index)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => a.ToDto())
                .ToList();

            var response = new AddressListResponse
            {
                WalletAddress = address,
                Addresses = paginatedAddresses,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list addresses for wallet {Address}", address);
            return Results.Problem(
                title: "Failed to List Addresses",
                detail: "An error occurred while listing addresses",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get a specific address by ID
    /// </summary>
    private static async Task<IResult> GetAddress(
        string address,
        Guid id,
        WalletManager walletManager,
        ILogger<Program> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var wallet = await walletManager.GetWalletAsync(address, cancellationToken);
            if (wallet == null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Wallet Not Found",
                    Detail = $"Wallet {address} not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            var walletAddress = wallet.Addresses.FirstOrDefault(a => a.Id == id);
            if (walletAddress == null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Address Not Found",
                    Detail = $"Address {id} not found for wallet {address}",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Results.Ok(walletAddress.ToDto());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get address {Id} for wallet {Address}", id, address);
            return Results.Problem(
                title: "Failed to Get Address",
                detail: "An error occurred while retrieving the address",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Update address metadata
    /// </summary>
    private static async Task<IResult> UpdateAddress(
        string address,
        Guid id,
        [FromBody] UpdateAddressRequest request,
        WalletManager walletManager,
        ILogger<Program> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var wallet = await walletManager.GetWalletAsync(address, cancellationToken);
            if (wallet == null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Wallet Not Found",
                    Detail = $"Wallet {address} not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            var walletAddress = wallet.Addresses.FirstOrDefault(a => a.Id == id);
            if (walletAddress == null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Address Not Found",
                    Detail = $"Address {id} not found for wallet {address}",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Update fields if provided
            if (request.Label != null)
                walletAddress.Label = request.Label;
            if (request.Notes != null)
                walletAddress.Notes = request.Notes;
            if (request.Tags != null)
                walletAddress.Tags = request.Tags;
            if (request.Metadata != null)
            {
                foreach (var (key, value) in request.Metadata)
                {
                    walletAddress.Metadata[key] = value;
                }
            }

            // Note: Changes to wallet.Addresses collection are tracked, no explicit update needed
            logger.LogInformation("Updated address {Id} for wallet {Address}", id, address);
            return Results.Ok(walletAddress.ToDto());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update address {Id} for wallet {Address}", id, address);
            return Results.Problem(
                title: "Failed to Update Address",
                detail: "An error occurred while updating the address",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Mark an address as used
    /// </summary>
    private static async Task<IResult> MarkAddressAsUsed(
        string address,
        Guid id,
        WalletManager walletManager,
        ILogger<Program> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var wallet = await walletManager.GetWalletAsync(address, cancellationToken);
            if (wallet == null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Wallet Not Found",
                    Detail = $"Wallet {address} not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            var walletAddress = wallet.Addresses.FirstOrDefault(a => a.Id == id);
            if (walletAddress == null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Address Not Found",
                    Detail = $"Address {id} not found for wallet {address}",
                    Status = StatusCodes.Status404NotFound
                });
            }

            if (!walletAddress.IsUsed)
            {
                walletAddress.IsUsed = true;
                walletAddress.FirstUsedAt = DateTime.UtcNow;
                walletAddress.LastUsedAt = DateTime.UtcNow;
                logger.LogInformation("Marked address {Id} as used for wallet {Address}", id, address);
            }
            else
            {
                walletAddress.LastUsedAt = DateTime.UtcNow;
                logger.LogInformation("Updated last used timestamp for address {Id} on wallet {Address}", id, address);
            }

            // Note: Changes to wallet.Addresses collection are tracked, no explicit save needed

            return Results.Ok(walletAddress.ToDto());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mark address {Id} as used for wallet {Address}", id, address);
            return Results.Problem(
                title: "Failed to Mark Address as Used",
                detail: "An error occurred while marking the address as used",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// List BIP44 accounts with address counts
    /// </summary>
    private static async Task<IResult> ListAccounts(
        string address,
        WalletManager walletManager,
        ILogger<Program> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var wallet = await walletManager.GetWalletAsync(address, cancellationToken);
            if (wallet == null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Wallet Not Found",
                    Detail = $"Wallet {address} not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Group addresses by account
            var accountGroups = wallet.Addresses
                .GroupBy(a => a.Account)
                .Select(g => new
                {
                    Account = g.Key,
                    TotalAddresses = g.Count(),
                    ReceiveAddresses = g.Count(a => !a.IsChange),
                    ChangeAddresses = g.Count(a => a.IsChange),
                    UsedAddresses = g.Count(a => a.IsUsed),
                    UnusedReceive = g.Count(a => !a.IsChange && !a.IsUsed),
                    UnusedChange = g.Count(a => a.IsChange && !a.IsUsed),
                    LastUsedReceiveIndex = g.Where(a => !a.IsChange && a.IsUsed).Max(a => (int?)a.Index),
                    LastUsedChangeIndex = g.Where(a => a.IsChange && a.IsUsed).Max(a => (int?)a.Index)
                })
                .OrderBy(a => a.Account)
                .ToList();

            return Results.Ok(new
            {
                WalletAddress = address,
                Accounts = accountGroups
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list accounts for wallet {Address}", address);
            return Results.Problem(
                title: "Failed to List Accounts",
                detail: "An error occurred while listing accounts",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get gap limit status for all accounts
    /// </summary>
    private static async Task<IResult> GetGapStatus(
        string address,
        WalletManager walletManager,
        ILogger<Program> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var wallet = await walletManager.GetWalletAsync(address, cancellationToken);
            if (wallet == null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Wallet Not Found",
                    Detail = $"Wallet {address} not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            var accountStatuses = new List<AccountGapStatus>();

            // Group by account and address type
            var groups = wallet.Addresses
                .GroupBy(a => new { a.Account, a.IsChange });

            foreach (var group in groups)
            {
                var unusedCount = group.Count(a => !a.IsUsed);
                var lastUsedIndex = group.Where(a => a.IsUsed).Max(a => (int?)a.Index);

                accountStatuses.Add(new AccountGapStatus
                {
                    Account = group.Key.Account,
                    AddressType = group.Key.IsChange ? "change" : "receive",
                    UnusedCount = unusedCount,
                    LastUsedIndex = lastUsedIndex,
                    MaxRecommendedGap = 20
                });
            }

            var response = new GapStatusResponse
            {
                WalletAddress = address,
                Accounts = accountStatuses
            };

            // Add warning if approaching limit
            var approaching = accountStatuses.Where(a => a.UnusedCount >= 15 && a.UnusedCount < 20).ToList();
            if (approaching.Any())
            {
                response.Warning = $"Warning: {approaching.Count} account/type combinations have 15+ unused addresses. " +
                    "Consider marking addresses as used or avoid generating more until existing addresses are used.";
            }

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get gap status for wallet {Address}", address);
            return Results.Problem(
                title: "Failed to Get Gap Status",
                detail: "An error occurred while checking gap status",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Create or retrieve a system wallet for a validator
    /// </summary>
    private static async Task<IResult> CreateOrRetrieveSystemWallet(
        [FromBody] SystemWalletRequest request,
        WalletManager walletManager,
        ILogger<Program> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validatorId = request.ValidatorId ?? "default-validator";
            var systemWalletName = $"system-wallet-{validatorId}";
            var systemTenant = "system";
            var systemOwner = $"validator:{validatorId}";

            logger.LogInformation(
                "Creating or retrieving system wallet for validator {ValidatorId}",
                validatorId);

            // Try to find existing system wallet
            var existingWallets = await walletManager.GetWalletsByOwnerAsync(
                systemOwner, systemTenant, cancellationToken);

            var existingWallet = existingWallets.FirstOrDefault(w =>
                w.Name == systemWalletName && w.Status == Wallet.Core.Domain.WalletStatus.Active);

            if (existingWallet != null)
            {
                logger.LogInformation(
                    "Found existing system wallet {Address} for validator {ValidatorId}",
                    existingWallet.Address,
                    validatorId);

                return Results.Ok(new SystemWalletResponse { Address = existingWallet.Address });
            }

            // Create new system wallet with ED25519 (fast signing)
            var (wallet, _) = await walletManager.CreateWalletAsync(
                systemWalletName,
                "ED25519",
                systemOwner,
                systemTenant,
                wordCount: 24, // Strong entropy for system wallets
                passphrase: null,
                cancellationToken);

            logger.LogInformation(
                "Created new system wallet {Address} for validator {ValidatorId}",
                wallet.Address,
                validatorId);

            return Results.Created(
                $"/api/v1/wallets/{wallet.Address}",
                new SystemWalletResponse { Address = wallet.Address });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create/retrieve system wallet");
            return Results.Problem(
                title: "System Wallet Creation Failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    // Helper methods for authentication/authorization
    private static string? GetCurrentUser(HttpContext context)
    {
        return context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private static string GetCurrentTenant(HttpContext context)
    {
        return context.User.FindFirstValue("tenant") ?? "default";
    }
}
