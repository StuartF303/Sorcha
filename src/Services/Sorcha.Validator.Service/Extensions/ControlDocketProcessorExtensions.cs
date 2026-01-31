// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Service.Services.Interfaces;

namespace Sorcha.Validator.Service.Extensions;

/// <summary>
/// Extension methods for registering the Control Docket Processor service
/// </summary>
public static class ControlDocketProcessorExtensions
{
    /// <summary>
    /// Adds the control docket processor service to the service collection.
    /// The processor handles governance transactions for validator management,
    /// configuration updates, and blueprint publications.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    /// <remarks>
    /// Dependencies:
    /// - IGenesisConfigService (scoped)
    /// - IControlBlueprintVersionResolver (scoped)
    /// - IValidatorRegistry (scoped)
    ///
    /// The processor is registered as scoped to match its dependencies.
    /// </remarks>
    public static IServiceCollection AddControlDocketProcessor(this IServiceCollection services)
    {
        services.AddScoped<IControlDocketProcessor, ControlDocketProcessor>();
        return services;
    }
}
