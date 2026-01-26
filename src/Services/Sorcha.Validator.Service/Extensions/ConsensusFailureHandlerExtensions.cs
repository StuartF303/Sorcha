// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Service.Services.Interfaces;

namespace Sorcha.Validator.Service.Extensions;

/// <summary>
/// Extension methods for registering consensus failure handler services
/// </summary>
public static class ConsensusFailureHandlerExtensions
{
    /// <summary>
    /// Adds consensus failure handler services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddConsensusFailureHandler(this IServiceCollection services)
    {
        services.AddSingleton<IConsensusFailureHandler, ConsensusFailureHandler>();
        return services;
    }
}
