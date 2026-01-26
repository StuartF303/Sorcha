// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Service.Services.Interfaces;

namespace Sorcha.Validator.Service.Extensions;

/// <summary>
/// Extension methods for registering transaction pool poller services
/// </summary>
public static class TransactionPoolPollerExtensions
{
    /// <summary>
    /// Adds the transaction pool poller and related services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddTransactionPoolPoller(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration
        services.Configure<TransactionPoolPollerConfiguration>(
            configuration.GetSection(TransactionPoolPollerConfiguration.SectionName));

        // Register the poller
        services.AddSingleton<ITransactionPoolPoller, TransactionPoolPoller>();

        // Register the background services
        services.AddSingleton<TransactionPoolPollerService>();
        services.AddHostedService(sp => sp.GetRequiredService<TransactionPoolPollerService>());

        services.AddHostedService<UnverifiedPoolCleanupService>();

        return services;
    }
}
