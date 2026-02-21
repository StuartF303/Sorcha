// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sorcha.TransactionHandler.Services;

/// <summary>
/// Extension methods for registering TransactionHandler services in the DI container.
/// </summary>
public static class TransactionHandlerServiceExtensions
{
    /// <summary>
    /// Adds the payload encoding service to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Optional configuration for encoding settings.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPayloadEncodingService(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        var threshold = PayloadEncodingService.DefaultCompressionThresholdBytes;

        if (configuration != null)
        {
            var configuredThreshold = configuration.GetValue<int?>("PayloadEncoding:CompressionThresholdBytes");
            if (configuredThreshold.HasValue && configuredThreshold.Value > 0)
            {
                threshold = configuredThreshold.Value;
            }
        }

        services.AddSingleton<IPayloadEncodingService>(new PayloadEncodingService(threshold));
        return services;
    }
}
