// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.ServiceClients.Auth;

namespace Sorcha.Register.Service.Extensions;

/// <summary>
/// Extension methods for configuring authorization policies in Register Service.
/// JWT authentication is configured via the shared ServiceDefaults.AddJwtAuthentication().
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds authorization policies for Register Service.
    /// Note: Call builder.AddJwtAuthentication() from ServiceDefaults first.
    /// </summary>
    public static IServiceCollection AddRegisterAuthorization(this IServiceCollection services)
    {
        // Register shared authorization policies (RequireAuthenticated, RequireService,
        // RequireOrganizationMember, RequireDelegatedAuthority, RequireAdministrator, CanWriteDockets)
        services.AddSorchaAuthorizationPolicies();

        services.AddAuthorization(options =>
        {
            // Register management (create, configure)
            options.AddPolicy("CanManageRegisters", policy =>
                policy.RequireAssertion(context =>
                {
                    var hasOrgId = context.User.Claims.Any(c => c.Type == TokenClaimConstants.OrgId && !string.IsNullOrEmpty(c.Value));
                    var isService = context.User.Claims.Any(c => c.Type == TokenClaimConstants.TokenType && c.Value == TokenClaimConstants.TokenTypeService);
                    return hasOrgId || isService;
                }));

            // Transaction submission (write operations)
            options.AddPolicy("CanSubmitTransactions", policy =>
                policy.RequireAuthenticatedUser());

            // Transaction reading (query operations)
            options.AddPolicy("CanReadTransactions", policy =>
                policy.RequireAuthenticatedUser());
        });

        return services;
    }
}
