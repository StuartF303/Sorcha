// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Sorcha.Blueprint.Service.Extensions;

/// <summary>
/// Extension methods for configuring JWT authentication in Blueprint Service.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds JWT Bearer authentication that validates tokens from Tenant Service.
    /// </summary>
    public static IServiceCollection AddBlueprintAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtIssuer = configuration["JwtSettings:Issuer"] ?? "https://tenant.sorcha.io";
        var jwtAudience = configuration["JwtSettings:Audience"] ?? "https://api.sorcha.io";
        var signingKey = configuration["JwtSettings:SigningKey"] ?? string.Empty;

        if (string.IsNullOrEmpty(signingKey))
        {
            throw new InvalidOperationException(
                "JWT SigningKey is required. Set JwtSettings:SigningKey in configuration.");
        }

        var keyBytes = Encoding.UTF8.GetBytes(signingKey);
        if (keyBytes.Length < 32)
        {
            Array.Resize(ref keyBytes, 32);
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,

                    ValidateAudience = true,
                    ValidAudience = jwtAudience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5),

                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerHandler>>();

                        logger.LogWarning(
                            context.Exception,
                            "JWT authentication failed: {Message}",
                            context.Exception.Message);

                        return Task.CompletedTask;
                    },

                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerHandler>>();

                        var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var orgId = context.Principal?.FindFirst("org_id")?.Value;

                        logger.LogInformation(
                            "Token validated for user {UserId}, org {OrgId}",
                            userId, orgId);

                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    /// <summary>
    /// Adds authorization policies for Blueprint Service.
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
