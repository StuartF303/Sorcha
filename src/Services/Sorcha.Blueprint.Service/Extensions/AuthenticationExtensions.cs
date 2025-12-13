// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

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
                    var hasOrgId = context.User.Claims.Any(c => c.Type == "org_id" && !string.IsNullOrEmpty(c.Value));
                    var isService = context.User.Claims.Any(c => c.Type == "token_type" && c.Value == "service");
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
