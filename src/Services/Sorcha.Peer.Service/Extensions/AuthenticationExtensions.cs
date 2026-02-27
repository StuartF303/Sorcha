// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.ServiceClients.Auth;

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
        // Register shared authorization policies (RequireAuthenticated, RequireService,
        // RequireOrganizationMember, RequireDelegatedAuthority, RequireAdministrator, CanWriteDockets)
        services.AddSorchaAuthorizationPolicies();

        services.AddAuthorization(options =>
        {
            // Peer management (ban, unban, reset) - requires org member or service token
            options.AddPolicy("CanManagePeers", policy =>
                policy.RequireAssertion(context =>
                {
                    var hasOrgId = context.User.Claims.Any(c => c.Type == TokenClaimConstants.OrgId && !string.IsNullOrEmpty(c.Value));
                    var isService = context.User.Claims.Any(c => c.Type == TokenClaimConstants.TokenType && c.Value == TokenClaimConstants.TokenTypeService);
                    return hasOrgId || isService;
                }));
        });

        return services;
    }
}
