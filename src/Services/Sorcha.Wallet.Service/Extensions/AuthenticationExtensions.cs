// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Wallet.Service.Extensions;

/// <summary>
/// Extension methods for configuring authorization policies in Wallet Service.
/// JWT authentication is configured via the shared ServiceDefaults.AddJwtAuthentication().
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds authorization policies for Wallet Service.
    /// Note: Call builder.AddJwtAuthentication() from ServiceDefaults first.
    /// </summary>
    public static IServiceCollection AddWalletAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Require authentication for all wallet operations
            options.AddPolicy("RequireAuthenticated", policy =>
                policy.RequireAuthenticatedUser());

            // Wallet management (create, read wallet list)
            options.AddPolicy("CanManageWallets", policy =>
                policy.RequireAssertion(context =>
                {
                    var hasOrgId = context.User.Claims.Any(c => c.Type == "org_id" && !string.IsNullOrEmpty(c.Value));
                    var isService = context.User.Claims.Any(c => c.Type == "token_type" && c.Value == "service");
                    return hasOrgId || isService;
                }));

            // Wallet operations (sign, encrypt, decrypt) - requires wallet ownership
            options.AddPolicy("CanUseWallet", policy =>
                policy.RequireAuthenticatedUser());

            // Service-to-service operations
            options.AddPolicy("RequireService", policy =>
                policy.RequireClaim("token_type", "service"));

            // Organization member operations
            options.AddPolicy("RequireOrganizationMember", policy =>
                policy.RequireAssertion(context =>
                    context.User.Claims.Any(c => c.Type == "org_id" && !string.IsNullOrEmpty(c.Value))));
        });

        return services;
    }
}
