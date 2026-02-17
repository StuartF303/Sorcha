// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Sorcha.Validator.Service.Models;
using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Core.Validators;
using Sorcha.Cryptography.Interfaces;
using Sorcha.ServiceClients.Wallet;
using Sorcha.Register.Models.Constants;
using Sorcha.Validator.Service.Services.Interfaces;

namespace Sorcha.Validator.Service.Endpoints;

/// <summary>
/// API endpoints for transaction validation
/// </summary>
public static class ValidationEndpoints
{
    public static RouteGroupBuilder MapValidationEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/validate", ValidateTransaction)
            .WithName("ValidateTransaction")
            .WithSummary("Validates a transaction and adds it to the memory pool")
            .WithDescription("Validates transaction structure, signatures, and blueprint compliance before adding to memory pool");

        group.MapGet("/mempool/{registerId}", GetMemPoolStats)
            .WithName("GetMemPoolStats")
            .WithSummary("Gets memory pool statistics for a register")
            .WithDescription("Returns current transaction counts, fill percentage, and other memory pool metrics");

        return group;
    }

    public static RouteGroupBuilder MapGenesisEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/genesis", SubmitGenesisTransaction)
            .WithName("SubmitGenesisTransaction")
            .WithSummary("Submits a genesis transaction for register creation")
            .WithDescription("Accepts genesis transactions from Register Service with control record payloads");

        return group;
    }

    /// <summary>
    /// Validates a transaction and adds it to the memory pool
    /// </summary>
    private static async Task<IResult> ValidateTransaction(
        [FromBody] ValidateTransactionRequest request,
        [FromServices] ITransactionValidator validator,
        [FromServices] ITransactionPoolPoller poolPoller,
        [FromServices] IRegisterMonitoringRegistry monitoringRegistry,
        [FromServices] IHashProvider hashProvider,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Validating transaction {TransactionId} for register {RegisterId}",
                request.TransactionId, request.RegisterId);

            // Convert request to transaction model
            var transaction = new Transaction
            {
                TransactionId = request.TransactionId,
                RegisterId = request.RegisterId,
                BlueprintId = request.BlueprintId,
                ActionId = request.ActionId,
                Payload = request.Payload,
                CreatedAt = request.CreatedAt,
                ExpiresAt = request.ExpiresAt,
                Signatures = request.Signatures.Select(s => new Signature
                {
                    PublicKey = Convert.FromBase64String(s.PublicKey),
                    SignatureValue = Convert.FromBase64String(s.SignatureValue),
                    Algorithm = s.Algorithm,
                    SignedAt = request.CreatedAt
                }).ToList(),
                PayloadHash = request.PayloadHash,
                PreviousTransactionId = request.PreviousTransactionId,
                Priority = request.Priority,
                Metadata = request.Metadata ?? new Dictionary<string, string>()
            };

            // Validate transaction structure
            var signatures = request.Signatures.Select(s =>
                new TransactionSignature(s.PublicKey, s.SignatureValue, s.Algorithm)).ToList();

            var structureValidation = validator.ValidateTransactionStructure(
                request.TransactionId,
                request.RegisterId,
                request.BlueprintId,
                request.Payload,
                request.PayloadHash,
                signatures,
                request.CreatedAt);

            if (!structureValidation.IsValid)
            {
                logger.LogWarning("Transaction {TransactionId} failed structure validation", request.TransactionId);
                return Results.BadRequest(new
                {
                    IsValid = false,
                    Errors = structureValidation.Errors.Select(e => new { e.Code, e.Message, e.Field })
                });
            }

            // Validate payload hash
            var payloadValidation = validator.ValidatePayloadHash(request.Payload, request.PayloadHash);
            if (!payloadValidation.IsValid)
            {
                logger.LogWarning("Transaction {TransactionId} failed payload hash validation", request.TransactionId);
                return Results.BadRequest(new
                {
                    IsValid = false,
                    Errors = payloadValidation.Errors.Select(e => new { e.Code, e.Message, e.Field })
                });
            }

            // Validate signatures
            var signatureValidation = validator.ValidateSignatures(signatures, request.TransactionId);
            if (!signatureValidation.IsValid)
            {
                logger.LogWarning("Transaction {TransactionId} failed signature validation", request.TransactionId);
                return Results.BadRequest(new
                {
                    IsValid = false,
                    Errors = signatureValidation.Errors.Select(e => new { e.Code, e.Message, e.Field })
                });
            }

            // Submit to unverified pool (ValidationEngineService will validate and promote to verified queue)
            var added = await poolPoller.SubmitTransactionAsync(request.RegisterId, transaction, cancellationToken);

            if (!added)
            {
                logger.LogWarning("Failed to submit transaction {TransactionId} to unverified pool", request.TransactionId);
                return Results.Conflict(new
                {
                    IsValid = true,
                    Added = false,
                    Message = "Failed to submit transaction to unverified pool (pool full or duplicate)"
                });
            }

            logger.LogInformation("Transaction {TransactionId} validated and submitted to unverified pool", request.TransactionId);

            // Register for docket building so DocketBuildTriggerService polls this register
            monitoringRegistry.RegisterForMonitoring(request.RegisterId);

            return Results.Ok(new
            {
                IsValid = true,
                Added = true,
                TransactionId = request.TransactionId,
                RegisterId = request.RegisterId,
                AddedAt = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating transaction {TransactionId}", request.TransactionId);
            return Results.Problem(
                title: "Internal server error",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Submits a genesis transaction for register creation
    /// </summary>
    private static async Task<IResult> SubmitGenesisTransaction(
        [FromBody] GenesisTransactionRequest request,
        [FromServices] ITransactionPoolPoller poolPoller,
        [FromServices] IRegisterMonitoringRegistry monitoringRegistry,
        [FromServices] ISystemWalletProvider systemWalletProvider,
        [FromServices] Microsoft.Extensions.Options.IOptions<Sorcha.Validator.Service.Configuration.ValidatorConfiguration> validatorConfig,
        [FromServices] Sorcha.ServiceClients.Wallet.IWalletServiceClient walletClient,
        [FromServices] Sorcha.Cryptography.Interfaces.IHashProvider hashProvider,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Submitting genesis transaction for register {RegisterId}", request.RegisterId);

            // Serialize control record to canonical JSON
            var controlRecordJson = System.Text.Json.JsonSerializer.Serialize(
                request.ControlRecordPayload,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });

            // Compute SHA-256 hash of control record
            var controlRecordBytes = System.Text.Encoding.UTF8.GetBytes(controlRecordJson);
            var controlRecordHash = hashProvider.ComputeHash(
                controlRecordBytes,
                Sorcha.Cryptography.Enums.HashType.SHA256);
            var controlRecordHashHex = Convert.ToHexString(controlRecordHash).ToLowerInvariant();

            logger.LogDebug(
                "Control record hash for register {RegisterId}: {Hash}",
                request.RegisterId,
                controlRecordHashHex);

            // Build the signing data using the same "{TxId}:{PayloadHash}" contract as action transactions.
            // This ensures the Validator's VerifySignaturesAsync can verify genesis signatures
            // using the standard verification path.
            var signingData = $"{request.TransactionId}:{controlRecordHashHex}";
            var signingHash = hashProvider.ComputeHash(
                System.Text.Encoding.UTF8.GetBytes(signingData),
                Sorcha.Cryptography.Enums.HashType.SHA256);

            // Sign with system wallet using the register-control derivation path
            // If system wallet doesn't exist, auto-create it (lazy initialization)
            var systemWalletAddress = systemWalletProvider.GetSystemWalletId();

            if (string.IsNullOrWhiteSpace(systemWalletAddress))
            {
                // System wallet not initialized yet, create it now
                logger.LogInformation(
                    "System wallet not initialized, creating on-demand for validator {ValidatorId}",
                    validatorConfig.Value.ValidatorId);

                systemWalletAddress = await walletClient.CreateOrRetrieveSystemWalletAsync(
                    validatorConfig.Value.ValidatorId,
                    cancellationToken);

                systemWalletProvider.SetSystemWalletId(systemWalletAddress);

                logger.LogInformation(
                    "Created system wallet {WalletAddress} for validator {ValidatorId}",
                    systemWalletAddress,
                    validatorConfig.Value.ValidatorId);
            }

            WalletSignResult systemSignResult;
            try
            {
                systemSignResult = await walletClient.SignTransactionAsync(
                    systemWalletAddress,
                    signingHash,
                    "sorcha:register-control",
                    isPreHashed: true,
                    cancellationToken);
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("not found") ||
                ex.Message.Contains("Authentication failed") ||
                ex.Message.Contains("401"))
            {
                // Wallet was deleted or became unavailable, recreate it
                logger.LogWarning(
                    "System wallet {WalletAddress} not available ({Reason}), recreating for validator {ValidatorId}",
                    systemWalletAddress,
                    ex.Message.Contains("Authentication") ? "auth failed" : "not found",
                    validatorConfig.Value.ValidatorId);

                try
                {
                    // Recreate system wallet using the validator ID
                    systemWalletAddress = await walletClient.CreateOrRetrieveSystemWalletAsync(
                        validatorConfig.Value.ValidatorId,
                        cancellationToken);

                    systemWalletProvider.SetSystemWalletId(systemWalletAddress);

                    logger.LogInformation(
                        "Recreated system wallet {WalletAddress} for validator {ValidatorId}",
                        systemWalletAddress,
                        validatorConfig.Value.ValidatorId);

                    // Retry signing with newly created wallet
                    systemSignResult = await walletClient.SignTransactionAsync(
                        systemWalletAddress,
                        signingHash,
                        "sorcha:register-control",
                        isPreHashed: true,
                        cancellationToken);
                }
                catch (Exception createEx)
                {
                    logger.LogError(
                        createEx,
                        "Failed to create system wallet for validator {ValidatorId}",
                        validatorConfig.Value.ValidatorId);
                    return Results.Problem(
                        title: "Platform bootstrap incomplete",
                        detail: $"Failed to create system wallet: {createEx.Message}",
                        statusCode: 503);
                }
            }

            logger.LogInformation(
                "Control record signed by system wallet {WalletAddress} for register {RegisterId} using {Algorithm}",
                systemWalletAddress,
                request.RegisterId,
                systemSignResult.Algorithm);

            // Convert attestation signatures from request
            var signatures = request.Signatures.Select(s => new Signature
            {
                PublicKey = Convert.FromBase64String(s.PublicKey),
                SignatureValue = Convert.FromBase64String(s.SignatureValue),
                Algorithm = s.Algorithm,
                SignedAt = request.CreatedAt
            }).ToList();

            // Add system wallet signature with real public key and signature bytes
            signatures.Add(new Signature
            {
                PublicKey = systemSignResult.PublicKey,
                SignatureValue = systemSignResult.Signature,
                Algorithm = systemSignResult.Algorithm,
                SignedAt = DateTimeOffset.UtcNow
            });

            logger.LogDebug(
                "Total signatures for register {RegisterId}: {Count} (attestations + system wallet)",
                request.RegisterId,
                signatures.Count);

            // Create genesis transaction for memory pool
            var transaction = new Transaction
            {
                TransactionId = request.TransactionId,
                RegisterId = request.RegisterId,
                BlueprintId = GenesisConstants.BlueprintId,
                ActionId = GenesisConstants.ActionId,
                Payload = request.ControlRecordPayload,
                CreatedAt = request.CreatedAt,
                ExpiresAt = null, // Genesis transactions don't expire
                Signatures = signatures,
                PayloadHash = controlRecordHashHex,
                Priority = TransactionPriority.High, // Genesis has highest priority
                Metadata = new Dictionary<string, string>
                {
                    { "Type", "Genesis" },
                    { "RegisterName", request.RegisterName ?? string.Empty },
                    { "TenantId", request.TenantId ?? string.Empty },
                    { "SystemWalletAddress", systemWalletAddress }
                }
            };

            // Submit to unverified pool (ValidationEngineService will validate and promote to verified queue)
            var added = await poolPoller.SubmitTransactionAsync(request.RegisterId, transaction, cancellationToken);

            if (!added)
            {
                logger.LogWarning("Failed to submit genesis transaction for register {RegisterId} to unverified pool", request.RegisterId);
                return Results.Conflict(new
                {
                    Success = false,
                    Message = "Failed to submit genesis transaction to unverified pool (pool full or duplicate)"
                });
            }

            logger.LogInformation("Genesis transaction for register {RegisterId} submitted to unverified pool successfully", request.RegisterId);

            // Register for docket building monitoring
            monitoringRegistry.RegisterForMonitoring(request.RegisterId);

            return Results.Ok(new
            {
                Success = true,
                TransactionId = request.TransactionId,
                RegisterId = request.RegisterId,
                AddedAt = DateTimeOffset.UtcNow,
                Message = "Genesis transaction accepted and queued for docket creation"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error submitting genesis transaction for register {RegisterId}", request.RegisterId);
            return Results.Problem(
                title: "Internal server error",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Gets memory pool statistics for a register
    /// </summary>
    private static async Task<IResult> GetMemPoolStats(
        string registerId,
        [FromServices] IMemPoolManager memPoolManager,
        CancellationToken cancellationToken)
    {
        var stats = await memPoolManager.GetStatsAsync(registerId, cancellationToken);
        return Results.Ok(stats);
    }
}

/// <summary>
/// Request model for transaction validation
/// </summary>
public record ValidateTransactionRequest
{
    public required string TransactionId { get; init; }
    public required string RegisterId { get; init; }
    public required string BlueprintId { get; init; }
    public required string ActionId { get; init; }
    public required JsonElement Payload { get; init; }
    public required string PayloadHash { get; init; }
    public required List<SignatureRequest> Signatures { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public string? PreviousTransactionId { get; init; }
    public TransactionPriority Priority { get; init; } = TransactionPriority.Normal;
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Signature in request
/// </summary>
public record SignatureRequest
{
    public required string PublicKey { get; init; }
    public required string SignatureValue { get; init; }
    public required string Algorithm { get; init; }
}

/// <summary>
/// Request model for genesis transaction submission
/// </summary>
public record GenesisTransactionRequest
{
    public required string TransactionId { get; init; }
    public required string RegisterId { get; init; }
    public required JsonElement ControlRecordPayload { get; init; }
    public required string PayloadHash { get; init; }
    public required List<SignatureRequest> Signatures { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public string? RegisterName { get; init; }
    public string? TenantId { get; init; }
}
