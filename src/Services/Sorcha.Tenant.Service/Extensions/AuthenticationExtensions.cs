// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Sorcha.Tenant.Service.Extensions;

/// <summary>
/// Configuration for JWT authentication.
/// </summary>
public class JwtConfiguration
{
    /// <summary>
    /// JWT token issuer (iss claim).
    /// </summary>
    public string Issuer { get; set; } = "https://tenant.sorcha.io";

    /// <summary>
    /// Valid audiences for tokens (aud claim).
    /// </summary>
    public string[] Audiences { get; set; } = { "https://api.sorcha.io" };

    /// <summary>
    /// Signing key for development (production uses Azure Key Vault).
    /// </summary>
    public string? SigningKey { get; set; }

    /// <summary>
    /// Access token lifetime in minutes.
    /// </summary>
    public int AccessTokenLifetimeMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh token lifetime in hours.
    /// </summary>
    public int RefreshTokenLifetimeHours { get; set; } = 24;

    /// <summary>
    /// Service token lifetime in hours.
    /// </summary>
    public int ServiceTokenLifetimeHours { get; set; } = 8;

    /// <summary>
    /// Clock skew tolerance for token validation.
    /// </summary>
    public int ClockSkewMinutes { get; set; } = 5;

    /// <summary>
    /// Whether to validate the issuer.
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Whether to validate the audience.
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Whether to validate the signing key.
    /// </summary>
    public bool ValidateIssuerSigningKey { get; set; } = true;

    /// <summary>
    /// Whether to validate token lifetime.
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;
}

/// <summary>
/// Extension methods for configuring JWT authentication.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds JWT Bearer authentication to the service collection.
    /// </summary>
    public static IServiceCollection AddTenantAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtConfig = configuration.GetSection("JwtSettings").Get<JwtConfiguration>()
            ?? new JwtConfiguration();

        services.Configure<JwtConfiguration>(configuration.GetSection("JwtSettings"));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = jwtConfig.ValidateIssuer,
                ValidIssuer = jwtConfig.Issuer,

                ValidateAudience = jwtConfig.ValidateAudience,
                ValidAudiences = jwtConfig.Audiences,

                ValidateIssuerSigningKey = jwtConfig.ValidateIssuerSigningKey,
                IssuerSigningKey = GetSigningKey(jwtConfig),

                ValidateLifetime = jwtConfig.ValidateLifetime,
                ClockSkew = TimeSpan.FromMinutes(jwtConfig.ClockSkewMinutes),

                // Map standard claims
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

                    logger.LogDebug(
                        "Token validated for user {UserId}, org {OrgId}",
                        userId, orgId);

                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    /// <summary>
    /// Adds authorization policies for Tenant Service.
    /// </summary>
    public static IServiceCollection AddTenantAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Policy for organization administrators
            options.AddPolicy("RequireAdministrator", policy =>
                policy.RequireRole("Administrator"));

            // Policy for auditors (read-only)
            options.AddPolicy("RequireAuditor", policy =>
                policy.RequireRole("Administrator", "Auditor"));

            // Policy for authenticated organization members
            options.AddPolicy("RequireOrganizationMember", policy =>
                policy.RequireClaim("org_id"));

            // Policy for service-to-service authentication
            options.AddPolicy("RequireService", policy =>
                policy.RequireClaim("token_type", "service"));

            // Policy for delegated authority (service acting on behalf of user)
            options.AddPolicy("RequireDelegatedAuthority", policy =>
            {
                policy.RequireClaim("token_type", "service");
                policy.RequireClaim("delegated_user_id");
            });

            // Policy for public users (PassKey authenticated)
            options.AddPolicy("RequirePublicUser", policy =>
                policy.RequireClaim("token_type", "user"));

            // Policy for any authenticated user (org or public)
            options.AddPolicy("RequireAuthenticated", policy =>
                policy.RequireAuthenticatedUser());

            // Policy for blockchain creation
            options.AddPolicy("CanCreateBlockchain", policy =>
                policy.RequireClaim("can_create_blockchain", "true"));

            // Policy for blueprint publishing
            options.AddPolicy("CanPublishBlueprint", policy =>
                policy.RequireClaim("can_publish_blueprint", "true"));
        });

        return services;
    }

    /// <summary>
    /// Gets the signing key from configuration.
    /// </summary>
    private static SecurityKey? GetSigningKey(JwtConfiguration config)
    {
        if (string.IsNullOrEmpty(config.SigningKey))
        {
            return null;
        }

        // For development, use symmetric key
        // Production should use RSA keys from Key Vault
        var keyBytes = Encoding.UTF8.GetBytes(config.SigningKey);

        // Ensure key is at least 256 bits for HS256
        if (keyBytes.Length < 32)
        {
            // Pad with zeros (not recommended for production)
            Array.Resize(ref keyBytes, 32);
        }

        return new SymmetricSecurityKey(keyBytes);
    }
}
