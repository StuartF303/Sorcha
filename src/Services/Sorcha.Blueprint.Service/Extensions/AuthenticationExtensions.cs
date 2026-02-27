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
        // Register shared authorization policies (RequireAuthenticated, RequireService,
        // RequireOrganizationMember, RequireDelegatedAuthority, RequireAdministrator, CanWriteDockets)
        services.AddSorchaAuthorizationPolicies();

        services.AddAuthorization(options =>
        {
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
                    var isAdmin = context.User.IsInRole("Administrator");
                    return canPublish || isAdmin;
                }));
        });

        return services;
    }
}
