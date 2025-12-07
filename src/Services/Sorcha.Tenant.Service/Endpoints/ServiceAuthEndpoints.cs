// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.AspNetCore.Http.HttpResults;
using Sorcha.Tenant.Service.Models.Dtos;
using Sorcha.Tenant.Service.Services;

namespace Sorcha.Tenant.Service.Endpoints;

/// <summary>
/// Service-to-Service authentication API endpoints.
/// </summary>
public static class ServiceAuthEndpoints
{
    /// <summary>
    /// Maps service authentication endpoints to the application.
    /// </summary>
    public static IEndpointRouteBuilder MapServiceAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/service-auth")
            .WithTags("Service Authentication");

        // OAuth2 client credentials token endpoint
        group.MapPost("/token", GetServiceToken)
            .WithName("GetServiceToken")
            .WithSummary("Get service token (client credentials)")
            .WithDescription("Authenticates a service using OAuth2 client credentials flow.")
            .AllowAnonymous()
            .Produces<TokenResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        // Delegated authority token
        group.MapPost("/token/delegated", GetDelegatedToken)
            .WithName("GetDelegatedToken")
            .WithSummary("Get delegated authority token")
            .WithDescription("Gets a service token that acts on behalf of a user.")
            .AllowAnonymous()
            .Produces<TokenResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        // Service principal management (admin only)
        var adminGroup = app.MapGroup("/api/service-principals")
            .WithTags("Service Principals")
            .RequireAuthorization("RequireAdministrator");

        adminGroup.MapPost("/", RegisterServicePrincipal)
            .WithName("RegisterServicePrincipal")
            .WithSummary("Register a new service principal")
            .WithDescription("Registers a new service for service-to-service authentication. Returns credentials only once.")
            .Produces<ServicePrincipalRegistrationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status409Conflict);

        adminGroup.MapGet("/", ListServicePrincipals)
            .WithName("ListServicePrincipals")
            .WithSummary("List all service principals")
            .WithDescription("Lists all registered service principals.")
            .Produces<ServicePrincipalListResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        adminGroup.MapGet("/{id:guid}", GetServicePrincipal)
            .WithName("GetServicePrincipal")
            .WithSummary("Get service principal details")
            .WithDescription("Gets details of a specific service principal.")
            .Produces<ServicePrincipalResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        adminGroup.MapGet("/by-client/{clientId}", GetServicePrincipalByClientId)
            .WithName("GetServicePrincipalByClientId")
            .WithSummary("Get service principal by client ID")
            .WithDescription("Gets a service principal by its client ID.")
            .Produces<ServicePrincipalResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        adminGroup.MapPut("/{id:guid}/scopes", UpdateServicePrincipalScopes)
            .WithName("UpdateServicePrincipalScopes")
            .WithSummary("Update service principal scopes")
            .WithDescription("Updates the allowed scopes for a service principal.")
            .Produces<ServicePrincipalResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        adminGroup.MapPost("/{id:guid}/suspend", SuspendServicePrincipal)
            .WithName("SuspendServicePrincipal")
            .WithSummary("Suspend service principal")
            .WithDescription("Temporarily suspends a service principal.")
            .Produces<SuccessResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        adminGroup.MapPost("/{id:guid}/reactivate", ReactivateServicePrincipal)
            .WithName("ReactivateServicePrincipal")
            .WithSummary("Reactivate service principal")
            .WithDescription("Reactivates a suspended service principal.")
            .Produces<SuccessResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        adminGroup.MapDelete("/{id:guid}", RevokeServicePrincipal)
            .WithName("RevokeServicePrincipal")
            .WithSummary("Revoke service principal")
            .WithDescription("Permanently revokes a service principal.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        // Secret rotation (requires current secret)
        group.MapPost("/rotate-secret", RotateSecret)
            .WithName("RotateServiceSecret")
            .WithSummary("Rotate service principal secret")
            .WithDescription("Rotates the client secret for a service principal. Requires current secret.")
            .AllowAnonymous()
            .Produces<RotateSecretResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<Results<Ok<TokenResponse>, UnauthorizedHttpResult, ValidationProblem>> GetServiceToken(
        ClientCredentialsRequest request,
        IServiceAuthService serviceAuthService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["clientId"] = ["Client ID is required"]
            });
        }

        if (string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["clientSecret"] = ["Client secret is required"]
            });
        }

        if (request.GrantType != "client_credentials")
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["grantType"] = ["Grant type must be 'client_credentials'"]
            });
        }

        var response = await serviceAuthService.AuthenticateServiceAsync(
            request.ClientId, request.ClientSecret, request.Scope, cancellationToken);

        if (response == null)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<TokenResponse>, UnauthorizedHttpResult, ValidationProblem>> GetDelegatedToken(
        DelegatedTokenRequest request,
        IServiceAuthService serviceAuthService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["clientId"] = ["Client ID is required"]
            });
        }

        if (string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["clientSecret"] = ["Client secret is required"]
            });
        }

        if (request.DelegatedUserId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["delegatedUserId"] = ["Delegated user ID is required"]
            });
        }

        var response = await serviceAuthService.AuthenticateWithDelegationAsync(
            request.ClientId,
            request.ClientSecret,
            request.DelegatedUserId,
            request.DelegatedOrganizationId,
            request.Scope,
            cancellationToken);

        if (response == null)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(response);
    }

    private static async Task<Results<Created<ServicePrincipalRegistrationResponse>, ValidationProblem, Conflict<string>>> RegisterServicePrincipal(
        RegisterServicePrincipalRequest request,
        IServiceAuthService serviceAuthService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ServiceName))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["serviceName"] = ["Service name is required"]
            });
        }

        try
        {
            var response = await serviceAuthService.RegisterServicePrincipalAsync(
                request.ServiceName, request.Scopes, cancellationToken);

            return TypedResults.Created($"/api/service-principals/{response.Id}", response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return TypedResults.Conflict(ex.Message);
        }
    }

    private static async Task<Ok<ServicePrincipalListResponse>> ListServicePrincipals(
        IServiceAuthService serviceAuthService,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var response = await serviceAuthService.ListServicePrincipalsAsync(includeInactive, cancellationToken);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<ServicePrincipalResponse>, NotFound>> GetServicePrincipal(
        Guid id,
        IServiceAuthService serviceAuthService,
        CancellationToken cancellationToken)
    {
        var response = await serviceAuthService.GetServicePrincipalAsync(id, cancellationToken);
        return response != null
            ? TypedResults.Ok(response)
            : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<ServicePrincipalResponse>, NotFound>> GetServicePrincipalByClientId(
        string clientId,
        IServiceAuthService serviceAuthService,
        CancellationToken cancellationToken)
    {
        var response = await serviceAuthService.GetServicePrincipalByClientIdAsync(clientId, cancellationToken);
        return response != null
            ? TypedResults.Ok(response)
            : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<ServicePrincipalResponse>, NotFound, ValidationProblem>> UpdateServicePrincipalScopes(
        Guid id,
        string[] scopes,
        IServiceAuthService serviceAuthService,
        CancellationToken cancellationToken)
    {
        if (scopes == null || scopes.Length == 0)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["scopes"] = ["At least one scope is required"]
            });
        }

        var response = await serviceAuthService.UpdateServicePrincipalScopesAsync(id, scopes, cancellationToken);
        return response != null
            ? TypedResults.Ok(response)
            : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<SuccessResponse>, NotFound>> SuspendServicePrincipal(
        Guid id,
        IServiceAuthService serviceAuthService,
        CancellationToken cancellationToken)
    {
        var success = await serviceAuthService.SuspendServicePrincipalAsync(id, cancellationToken);
        return success
            ? TypedResults.Ok(new SuccessResponse { Message = "Service principal suspended" })
            : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<SuccessResponse>, NotFound>> ReactivateServicePrincipal(
        Guid id,
        IServiceAuthService serviceAuthService,
        CancellationToken cancellationToken)
    {
        var success = await serviceAuthService.ReactivateServicePrincipalAsync(id, cancellationToken);
        return success
            ? TypedResults.Ok(new SuccessResponse { Message = "Service principal reactivated" })
            : TypedResults.NotFound();
    }

    private static async Task<Results<NoContent, NotFound>> RevokeServicePrincipal(
        Guid id,
        IServiceAuthService serviceAuthService,
        CancellationToken cancellationToken)
    {
        var success = await serviceAuthService.RevokeServicePrincipalAsync(id, cancellationToken);
        return success
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<RotateSecretResponse>, UnauthorizedHttpResult, ValidationProblem>> RotateSecret(
        RotateSecretRequest request,
        string clientId,
        IServiceAuthService serviceAuthService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["clientId"] = ["Client ID is required (query parameter)"]
            });
        }

        if (string.IsNullOrWhiteSpace(request.CurrentSecret))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["currentSecret"] = ["Current secret is required"]
            });
        }

        var response = await serviceAuthService.RotateSecretAsync(clientId, request.CurrentSecret, cancellationToken);

        if (response == null)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(response);
    }
}
