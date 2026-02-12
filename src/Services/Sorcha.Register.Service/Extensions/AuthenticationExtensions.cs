// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

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
        services.AddAuthorization(options =>
        {
            // Require authentication for all register operations
            options.AddPolicy("RequireAuthenticated", policy =>
                policy.RequireAuthenticatedUser());

            // Register management (create, configure)
            options.AddPolicy("CanManageRegisters", policy =>
                policy.RequireAssertion(context =>
                {
                    var hasOrgId = context.User.Claims.Any(c => c.Type == "org_id" && !string.IsNullOrEmpty(c.Value));
                    var isService = context.User.Claims.Any(c => c.Type == "token_type" && c.Value == "service");
                    return hasOrgId || isService;
                }));

            // Transaction submission (write operations)
            options.AddPolicy("CanSubmitTransactions", policy =>
                policy.RequireAuthenticatedUser());

            // Transaction reading (query operations)
            options.AddPolicy("CanReadTransactions", policy =>
                policy.RequireAuthenticatedUser());

            // Docket writing (Validator Service only)
            options.AddPolicy("CanWriteDockets", policy =>
                policy.RequireClaim("token_type", "service"));

            // Service-to-service operations (notifications, etc.)
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
