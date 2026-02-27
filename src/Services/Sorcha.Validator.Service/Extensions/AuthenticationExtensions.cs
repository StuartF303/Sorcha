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
    public static IServiceCollection AddValidatorAuthorization(this IServiceCollection services)
    {
        // Register shared authorization policies (RequireAuthenticated, RequireService,
        // RequireOrganizationMember, RequireDelegatedAuthority, RequireAdministrator, CanWriteDockets)
        services.AddSorchaAuthorizationPolicies();

        services.AddAuthorization(options =>
        {
            // Chain validation - requires service token or admin
            options.AddPolicy("CanValidateChains", policy =>
                policy.RequireAssertion(context =>
                {
                    var isService = context.User.Claims.Any(c =>
                        c.Type == TokenClaimConstants.TokenType && c.Value == TokenClaimConstants.TokenTypeService);
                    var isAdmin = context.User.IsInRole("Administrator");
                    return isService || isAdmin;
                }));
        });

        return services;
    }
}
