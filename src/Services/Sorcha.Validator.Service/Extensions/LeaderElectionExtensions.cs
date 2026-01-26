// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.DependencyInjection;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Service.Services.Interfaces;

namespace Sorcha.Validator.Service.Extensions;

/// <summary>
/// Extension methods for registering Leader Election services
/// </summary>
public static class LeaderElectionExtensions
{
    /// <summary>
    /// Adds the Leader Election service to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configure">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLeaderElection(
        this IServiceCollection services,
        Action<LeaderElectionConfiguration>? configure = null)
    {
        // Register configuration
        if (configure != null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<LeaderElectionConfiguration>(_ => { });
        }

        // Register the rotating leader election service
        services.AddSingleton<ILeaderElectionService, RotatingLeaderElectionService>();

        return services;
    }
}
