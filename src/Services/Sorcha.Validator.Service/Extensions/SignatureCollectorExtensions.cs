// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Service.Services.Interfaces;

namespace Sorcha.Validator.Service.Extensions;

/// <summary>
/// Extension methods for registering signature collector services
/// </summary>
public static class SignatureCollectorExtensions
{
    /// <summary>
    /// Adds signature collector services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSignatureCollector(this IServiceCollection services)
    {
        services.AddSingleton<ISignatureCollector, SignatureCollector>();
        return services;
    }
}
