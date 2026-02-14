// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Sorcha.Tenant.Service.Data.Repositories;
using Sorcha.Tenant.Service.Models;
using Sorcha.Tenant.Service.Models.Dtos;
using Sorcha.Tenant.Service.Services;

namespace Sorcha.Tenant.Service.Endpoints;

/// <summary>
/// Authentication and token management API endpoints.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Maps authentication endpoints to the application.
    /// </summary>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        // Login with email/password (public endpoint)
        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Login with email and password")
            .WithDescription("Authenticates a user with email and password and returns access and refresh tokens.")
            .AllowAnonymous()
            .Produces<TokenResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        // Token refresh (public endpoint - requires valid refresh token)
        group.MapPost("/token/refresh", RefreshToken)
            .WithName("RefreshToken")
            .WithSummary("Refresh access token")
            .WithDescription("Exchanges a valid refresh token for a new access token.")
            .AllowAnonymous()
            .Produces<TokenResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        // Token revocation (requires authentication)
        group.MapPost("/token/revoke", RevokeToken)
            .WithName("RevokeToken")
            .WithSummary("Revoke a token")
            .WithDescription("Revokes an access or refresh token, preventing future use.")
            .RequireAuthorization()
            .Produces<SuccessResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        // Token introspection (service-to-service)
        group.MapPost("/token/introspect", IntrospectToken)
            .WithName("IntrospectToken")
            .WithSummary("Introspect a token")
            .WithDescription("Returns information about a token, including whether it is active. Service tokens only.")
            .RequireAuthorization("RequireService")
            .Produces<TokenIntrospectionResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        // Revoke all user tokens (admin only)
        group.MapPost("/token/revoke-user", RevokeUserTokens)
            .WithName("RevokeUserTokens")
            .WithSummary("Revoke all tokens for a user")
            .WithDescription("Revokes all access and refresh tokens for a specific user. Administrator only.")
            .RequireAuthorization("RequireAdministrator")
            .Produces<SuccessResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        // Revoke all organization tokens (admin only)
        group.MapPost("/token/revoke-organization", RevokeOrganizationTokens)
            .WithName("RevokeOrganizationTokens")
            .WithSummary("Revoke all tokens for an organization")
            .WithDescription("Revokes all access and refresh tokens for all users in an organization. Administrator only.")
            .RequireAuthorization("RequireAdministrator")
            .Produces<SuccessResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        // Current user info
        group.MapGet("/me", GetCurrentUser)
            .WithName("GetCurrentUser")
            .WithSummary("Get current user information")
            .WithDescription("Returns information about the currently authenticated user from their token claims.")
            .RequireAuthorization()
            .Produces<CurrentUserResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        // Logout (revokes current token)
        group.MapPost("/logout", Logout)
            .WithName("Logout")
            .WithSummary("Logout and revoke current token")
            .WithDescription("Logs out the current user by revoking their access token.")
            .RequireAuthorization()
            .Produces<SuccessResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<Results<Ok<TokenResponse>, UnauthorizedHttpResult, ValidationProblem, ProblemHttpResult>> Login(
        LoginRequest request,
        IIdentityRepository identityRepository,
        IOrganizationRepository organizationRepository,
        ITokenService tokenService,
        ITokenRevocationService tokenRevocationService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["email"] = ["Email is required"]
            });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["password"] = ["Password is required"]
            });
        }

        // Rate limiting check
        if (await tokenRevocationService.IsRateLimitedAsync(request.Email, cancellationToken))
        {
            logger.LogWarning("Login rate-limited for {Email}", request.Email);
            return TypedResults.Problem("Too many failed login attempts. Please try again later.",
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        try
        {
            // Look up user by email
            var user = await identityRepository.GetUserByEmailAsync(request.Email, cancellationToken);

            if (user == null || user.Status != IdentityStatus.Active)
            {
                logger.LogWarning("Login failed: User not found or inactive - {Email}", request.Email);
                await tokenRevocationService.IncrementFailedAuthAttemptsAsync(request.Email, cancellationToken);
                return TypedResults.Unauthorized();
            }

            // Verify password hash exists (local auth user)
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                logger.LogWarning("Login failed: User has no password (external IDP user?) - {Email}", request.Email);
                await tokenRevocationService.IncrementFailedAuthAttemptsAsync(request.Email, cancellationToken);
                return TypedResults.Unauthorized();
            }

            // Verify password using BCrypt
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                logger.LogWarning("Login failed: Invalid password - {Email}", request.Email);
                await tokenRevocationService.IncrementFailedAuthAttemptsAsync(request.Email, cancellationToken);
                return TypedResults.Unauthorized();
            }

            // Get user's organization
            var organization = await organizationRepository.GetByIdAsync(user.OrganizationId, cancellationToken);

            if (organization == null)
            {
                logger.LogError("Login failed: Organization not found - {OrgId}", user.OrganizationId);
                return TypedResults.Unauthorized();
            }

            // Reset failed attempts on successful login
            await tokenRevocationService.ResetFailedAuthAttemptsAsync(request.Email, cancellationToken);

            // Update last login timestamp
            user.LastLoginAt = DateTimeOffset.UtcNow;
            await identityRepository.UpdateUserAsync(user, cancellationToken);

            // Generate tokens
            var tokenResponse = await tokenService.GenerateUserTokenAsync(user, organization, cancellationToken);

            logger.LogInformation("User logged in successfully - {Email} (UserId: {UserId}, OrgId: {OrgId})",
                user.Email, user.Id, organization.Id);

            return TypedResults.Ok(tokenResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Login failed with exception - {Email}", request.Email);
            return TypedResults.Unauthorized();
        }
    }

    private static async Task<Results<Ok<TokenResponse>, UnauthorizedHttpResult, ValidationProblem>> RefreshToken(
        TokenRefreshRequest request,
        ITokenService tokenService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["refreshToken"] = ["Refresh token is required"]
            });
        }

        var response = await tokenService.RefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (response == null)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<SuccessResponse>, ValidationProblem>> RevokeToken(
        TokenRevocationRequest request,
        ITokenService tokenService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["token"] = ["Token is required"]
            });
        }

        var success = await tokenService.RevokeTokenAsync(request.Token, cancellationToken);

        return TypedResults.Ok(new SuccessResponse
        {
            Success = success,
            Message = success ? "Token revoked successfully" : "Token could not be revoked"
        });
    }

    private static async Task<Ok<TokenIntrospectionResponse>> IntrospectToken(
        TokenIntrospectionRequest request,
        ITokenService tokenService,
        CancellationToken cancellationToken)
    {
        var response = await tokenService.IntrospectTokenAsync(request.Token, cancellationToken);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<SuccessResponse>, ValidationProblem>> RevokeUserTokens(
        RevokeUserTokensRequest request,
        ITokenService tokenService,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["userId"] = ["User ID is required"]
            });
        }

        await tokenService.RevokeAllUserTokensAsync(request.UserId, cancellationToken);

        return TypedResults.Ok(new SuccessResponse
        {
            Success = true,
            Message = $"All tokens for user {request.UserId} have been revoked"
        });
    }

    private static async Task<Results<Ok<SuccessResponse>, ValidationProblem>> RevokeOrganizationTokens(
        RevokeOrganizationTokensRequest request,
        ITokenService tokenService,
        CancellationToken cancellationToken)
    {
        if (request.OrganizationId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["organizationId"] = ["Organization ID is required"]
            });
        }

        await tokenService.RevokeAllOrganizationTokensAsync(request.OrganizationId, cancellationToken);

        return TypedResults.Ok(new SuccessResponse
        {
            Success = true,
            Message = $"All tokens for organization {request.OrganizationId} have been revoked"
        });
    }

    private static Ok<CurrentUserResponse> GetCurrentUser(
        ClaimsPrincipal user)
    {
        var response = new CurrentUserResponse
        {
            UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value,
            Email = user.FindFirst(ClaimTypes.Email)?.Value
                ?? user.FindFirst("email")?.Value,
            DisplayName = user.FindFirst(ClaimTypes.Name)?.Value
                ?? user.FindFirst("name")?.Value,
            OrganizationId = user.FindFirst("org_id")?.Value,
            OrganizationName = user.FindFirst("org_name")?.Value,
            TokenType = user.FindFirst("token_type")?.Value ?? "user",
            Roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray(),
            Scopes = user.FindAll("scope").Select(c => c.Value).ToArray(),
            AuthMethod = user.FindFirst("auth_method")?.Value
        };

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<SuccessResponse>> Logout(
        HttpContext context,
        ITokenService tokenService,
        CancellationToken cancellationToken)
    {
        // Get the current access token from the Authorization header
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();
            await tokenService.RevokeTokenAsync(token, cancellationToken);
        }

        return TypedResults.Ok(new SuccessResponse
        {
            Success = true,
            Message = "Logged out successfully"
        });
    }
}
