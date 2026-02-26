// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.ServiceClients.Auth;

namespace Sorcha.Blueprint.Service.Extensions;

/// <summary>
/// Extension methods for configuring authorization policies in Blueprint Service.
/// JWT authentication is configured via the shared ServiceDefaults.AddJwtAuthentication().
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds authorization policies for Blueprint Service.
    /// Note: Call builder.AddJwtAuthentication() from ServiceDefaults first.
    /// </summary>
    public static IServiceCollection AddBlueprintAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Require authentication for all blueprint operations
            options.AddPolicy("RequireAuthenticated", policy =>
                policy.RequireAuthenticatedUser());

            // Blueprint management (create, update, delete) - requires org member or service token
            options.AddPolicy("CanManageBlueprints", policy =>
                policy.RequireAssertion(context =>
                {
                    var hasOrgId = context.User.Claims.Any(c => c.Type == TokenClaimConstants.OrgId && !string.IsNullOrEmpty(c.Value));
                    var isService = context.User.Claims.Any(c => c.Type == TokenClaimConstants.TokenType && c.Value == TokenClaimConstants.TokenTypeService);
                    return hasOrgId || isService;
                }));

            // Blueprint execution - any authenticated user
            options.AddPolicy("CanExecuteBlueprints", policy =>
                policy.RequireAuthenticatedUser());

            // Blueprint publishing - requires specific claim or admin role
            options.AddPolicy("CanPublishBlueprints", policy =>
                policy.RequireAssertion(context =>
                {
                    var canPublish = context.User.Claims.Any(c => c.Type == "can_publish_blueprint" && c.Value == "true");
                    var isAdmin = context.User.IsInRole("RequireAdministrator");
                    return canPublish || isAdmin;
                }));

            // Administrator role - for schema import and other admin operations
            options.AddPolicy("RequireAdministrator", policy =>
                policy.RequireRole("RequireAdministrator"));

            // Service-to-service operations
            options.AddPolicy("RequireService", policy =>
                policy.RequireClaim(TokenClaimConstants.TokenType, TokenClaimConstants.TokenTypeService));

            // Organization member operations
            options.AddPolicy("RequireOrganizationMember", policy =>
                policy.RequireAssertion(context =>
                    context.User.Claims.Any(c => c.Type == TokenClaimConstants.OrgId && !string.IsNullOrEmpty(c.Value))));

            // Delegated authority — service acting on behalf of a user
            options.AddPolicy("RequireDelegatedAuthority", policy =>
                policy.RequireAssertion(context =>
                {
                    var isService = context.User.Claims.Any(c =>
                        c.Type == TokenClaimConstants.TokenType &&
                        c.Value == TokenClaimConstants.TokenTypeService);
                    var hasDelegatedUser = context.User.Claims.Any(c =>
                        c.Type == TokenClaimConstants.DelegatedUserId &&
                        !string.IsNullOrEmpty(c.Value));
                    return isService && hasDelegatedUser;
                }));
        });

        return services;
    }
}
