// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.ServiceClients.Auth;

namespace Sorcha.Validator.Service.Extensions;

/// <summary>
/// Extension methods for configuring authorization policies in Validator Service.
/// JWT authentication is configured via the shared ServiceDefaults.AddJwtAuthentication().
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds authorization policies for Validator Service.
    /// Note: Call builder.AddJwtAuthentication() from ServiceDefaults first.
    /// </summary>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Require authentication for all validator operations
            options.AddPolicy("RequireAuthenticated", policy =>
                policy.RequireAuthenticatedUser());

            // Service-to-service operations
            options.AddPolicy("RequireService", policy =>
                policy.RequireClaim(TokenClaimConstants.TokenType, TokenClaimConstants.TokenTypeService));

            // Organization member operations
            options.AddPolicy("RequireOrganizationMember", policy =>
                policy.RequireAssertion(context =>
                    context.User.Claims.Any(c => c.Type == TokenClaimConstants.OrgId && !string.IsNullOrEmpty(c.Value))));

            // Administrator role
            options.AddPolicy("RequireAdministrator", policy =>
                policy.RequireRole("Administrator"));

            // Chain validation - requires service token or admin
            options.AddPolicy("CanValidateChains", policy =>
                policy.RequireAssertion(context =>
                {
                    var isService = context.User.Claims.Any(c =>
                        c.Type == TokenClaimConstants.TokenType && c.Value == TokenClaimConstants.TokenTypeService);
                    var isAdmin = context.User.IsInRole("Administrator");
                    return isService || isAdmin;
                }));

            // Docket write operations - requires service token
            options.AddPolicy("CanWriteDockets", policy =>
                policy.RequireClaim(TokenClaimConstants.TokenType, TokenClaimConstants.TokenTypeService));
        });

        return services;
    }
}
