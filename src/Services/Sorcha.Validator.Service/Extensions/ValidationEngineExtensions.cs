// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Register.Core.Services;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Service.Services.Interfaces;

namespace Sorcha.Validator.Service.Extensions;

/// <summary>
/// Extension methods for registering validation engine services
/// </summary>
public static class ValidationEngineExtensions
{
    /// <summary>
    /// Adds the validation engine and related services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddValidationEngine(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration
        services.Configure<ValidationEngineConfiguration>(
            configuration.GetSection(ValidationEngineConfiguration.SectionName));

        // Register governance services (scoped to match IRegisterServiceClient lifetime)
        services.AddScoped<IGovernanceRosterService, GovernanceRosterService>();
        services.AddScoped<IRightsEnforcementService, RightsEnforcementService>();

        // Register the validation engine as scoped (matches IRegisterServiceClient lifetime)
        services.AddScoped<IValidationEngine, ValidationEngine>();

        // Register the background service
        services.AddHostedService<ValidationEngineService>();

        return services;
    }
}
