// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Peer.Service.Extensions;

/// <summary>
/// Extension methods for configuring authorization policies in Peer Service.
/// JWT authentication is configured via the shared ServiceDefaults.AddJwtAuthentication().
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds authorization policies for Peer Service.
    /// Note: Call builder.AddJwtAuthentication() from ServiceDefaults first.
    /// </summary>
    public static IServiceCollection AddPeerServiceAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Require authentication for all protected operations
            options.AddPolicy("RequireAuthenticated", policy =>
                policy.RequireAuthenticatedUser());

            // Peer management (ban, unban, reset) - requires org member or service token
            options.AddPolicy("CanManagePeers", policy =>
                policy.RequireAssertion(context =>
                {
                    var hasOrgId = context.User.Claims.Any(c => c.Type == "org_id" && !string.IsNullOrEmpty(c.Value));
                    var isService = context.User.Claims.Any(c => c.Type == "token_type" && c.Value == "service");
                    return hasOrgId || isService;
                }));

            // Organization member operations
            options.AddPolicy("RequireOrganizationMember", policy =>
                policy.RequireAssertion(context =>
                    context.User.Claims.Any(c => c.Type == "org_id" && !string.IsNullOrEmpty(c.Value))));

            // Service-to-service operations
            options.AddPolicy("RequireService", policy =>
                policy.RequireClaim("token_type", "service"));
        });

        return services;
    }
}
