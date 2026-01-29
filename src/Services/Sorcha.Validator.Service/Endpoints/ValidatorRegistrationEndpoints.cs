// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.AspNetCore.Mvc;
using Sorcha.Validator.Service.Services.Interfaces;

namespace Sorcha.Validator.Service.Endpoints;

/// <summary>
/// API endpoints for validator registration and management
/// </summary>
public static class ValidatorRegistrationEndpoints
{
    /// <summary>
    /// Maps validator registration endpoints
    /// </summary>
    public static RouteGroupBuilder MapValidatorRegistrationEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/register", RegisterValidator)
            .WithName("RegisterValidator")
            .WithSummary("Register as a validator for a register")
            .WithDescription("Registers this validator node for participation in consensus for the specified register");

        group.MapGet("/{registerId}", GetValidators)
            .WithName("GetValidators")
            .WithSummary("Get validators for a register")
            .WithDescription("Returns all validators registered for the specified register");

        group.MapGet("/{registerId}/{validatorId}", GetValidator)
            .WithName("GetValidator")
            .WithSummary("Get validator details")
            .WithDescription("Returns details for a specific validator");

        group.MapGet("/{registerId}/count", GetValidatorCount)
            .WithName("GetValidatorCount")
            .WithSummary("Get active validator count")
            .WithDescription("Returns the number of active validators for the register");

        group.MapPost("/{registerId}/refresh", RefreshValidators)
            .WithName("RefreshValidators")
            .WithSummary("Refresh validator list from chain")
            .WithDescription("Forces a refresh of the validator list from the transaction chain");

        return group;
    }

    /// <summary>
    /// Register as a validator
    /// </summary>
    private static async Task<IResult> RegisterValidator(
        [FromBody] RegisterValidatorRequest request,
        [FromServices] IValidatorRegistry registry,
        [FromServices] IGenesisConfigService genesisConfig,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Processing validator registration for {ValidatorId} on register {RegisterId}",
                request.ValidatorId, request.RegisterId);

            // Check registration mode
            var validatorConfig = await genesisConfig.GetValidatorConfigAsync(request.RegisterId, cancellationToken);

            if (!validatorConfig.IsPublicRegistration)
            {
                logger.LogWarning(
                    "Registration rejected - register {RegisterId} requires consent mode",
                    request.RegisterId);
                return Results.BadRequest(new
                {
                    error = "Registration requires approval",
                    message = "This register uses consent mode. Contact the register owner for approval."
                });
            }

            var registration = new ValidatorRegistration
            {
                ValidatorId = request.ValidatorId,
                PublicKey = request.PublicKey,
                GrpcEndpoint = request.GrpcEndpoint,
                Metadata = request.Metadata
            };

            var result = await registry.RegisterAsync(request.RegisterId, registration, cancellationToken);

            if (!result.Success)
            {
                logger.LogWarning(
                    "Validator registration failed for {ValidatorId}: {Error}",
                    request.ValidatorId, result.ErrorMessage);
                return Results.BadRequest(new
                {
                    error = "Registration failed",
                    message = result.ErrorMessage
                });
            }

            logger.LogInformation(
                "Validator {ValidatorId} registered successfully for register {RegisterId} (order: {Order})",
                request.ValidatorId, request.RegisterId, result.OrderIndex);

            return Results.Created($"/api/validators/{request.RegisterId}/{request.ValidatorId}", new
            {
                validatorId = request.ValidatorId,
                registerId = request.RegisterId,
                transactionId = result.TransactionId,
                orderIndex = result.OrderIndex,
                status = "active"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registering validator {ValidatorId}", request.ValidatorId);
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Registration error");
        }
    }

    /// <summary>
    /// Get all validators for a register
    /// </summary>
    private static async Task<IResult> GetValidators(
        string registerId,
        [FromServices] IValidatorRegistry registry,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var validators = await registry.GetActiveValidatorsAsync(registerId, cancellationToken);

            return Results.Ok(new
            {
                registerId,
                count = validators.Count,
                validators = validators.Select(v => new
                {
                    validatorId = v.ValidatorId,
                    publicKey = v.PublicKey,
                    grpcEndpoint = v.GrpcEndpoint,
                    status = v.Status.ToString().ToLowerInvariant(),
                    registeredAt = v.RegisteredAt,
                    orderIndex = v.OrderIndex
                })
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting validators for register {RegisterId}", registerId);
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Query error");
        }
    }

    /// <summary>
    /// Get a specific validator
    /// </summary>
    private static async Task<IResult> GetValidator(
        string registerId,
        string validatorId,
        [FromServices] IValidatorRegistry registry,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var validator = await registry.GetValidatorAsync(registerId, validatorId, cancellationToken);

            if (validator == null)
            {
                return Results.NotFound(new
                {
                    error = "Validator not found",
                    validatorId,
                    registerId
                });
            }

            return Results.Ok(new
            {
                validatorId = validator.ValidatorId,
                registerId,
                publicKey = validator.PublicKey,
                grpcEndpoint = validator.GrpcEndpoint,
                status = validator.Status.ToString().ToLowerInvariant(),
                registeredAt = validator.RegisteredAt,
                orderIndex = validator.OrderIndex,
                registrationTxId = validator.RegistrationTxId,
                metadata = validator.Metadata
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting validator {ValidatorId} for register {RegisterId}",
                validatorId, registerId);
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Query error");
        }
    }

    /// <summary>
    /// Get active validator count
    /// </summary>
    private static async Task<IResult> GetValidatorCount(
        string registerId,
        [FromServices] IValidatorRegistry registry,
        [FromServices] IGenesisConfigService genesisConfig,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var count = await registry.GetActiveCountAsync(registerId, cancellationToken);
            var config = await genesisConfig.GetValidatorConfigAsync(registerId, cancellationToken);

            return Results.Ok(new
            {
                registerId,
                activeCount = count,
                minValidators = config.MinValidators,
                maxValidators = config.MaxValidators,
                hasQuorum = count >= config.MinValidators
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting validator count for register {RegisterId}", registerId);
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Query error");
        }
    }

    /// <summary>
    /// Force refresh validator list
    /// </summary>
    private static async Task<IResult> RefreshValidators(
        string registerId,
        [FromServices] IValidatorRegistry registry,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Refreshing validator list for register {RegisterId}", registerId);

            await registry.RefreshAsync(registerId, cancellationToken);

            var count = await registry.GetActiveCountAsync(registerId, cancellationToken);

            return Results.Ok(new
            {
                registerId,
                refreshed = true,
                activeCount = count
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refreshing validators for register {RegisterId}", registerId);
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Refresh error");
        }
    }
}

/// <summary>
/// Request to register as a validator
/// </summary>
public record RegisterValidatorRequest
{
    /// <summary>Register ID to join</summary>
    public required string RegisterId { get; init; }

    /// <summary>Validator's unique identifier (wallet address)</summary>
    public required string ValidatorId { get; init; }

    /// <summary>Validator's public key for signature verification</summary>
    public required string PublicKey { get; init; }

    /// <summary>gRPC endpoint for peer communication</summary>
    public required string GrpcEndpoint { get; init; }

    /// <summary>Optional metadata</summary>
    public Dictionary<string, string>? Metadata { get; init; }
}
