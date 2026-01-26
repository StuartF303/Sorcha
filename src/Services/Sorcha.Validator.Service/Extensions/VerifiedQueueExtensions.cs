// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Service.Services.Interfaces;

namespace Sorcha.Validator.Service.Extensions;

/// <summary>
/// Extension methods for registering verified transaction queue services
/// </summary>
public static class VerifiedQueueExtensions
{
    /// <summary>
    /// Adds the verified transaction queue and related services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddVerifiedTransactionQueue(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration
        services.Configure<VerifiedQueueConfiguration>(
            configuration.GetSection(VerifiedQueueConfiguration.SectionName));

        // Register the queue as singleton (in-memory state)
        services.AddSingleton<IVerifiedTransactionQueue, VerifiedTransactionQueue>();

        // Register cleanup background service
        services.AddHostedService<VerifiedQueueCleanupService>();

        return services;
    }
}
