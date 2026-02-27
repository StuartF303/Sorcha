// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for configuring CORS policies across Sorcha services.
/// </summary>
public static class CorsExtensions
{
    /// <summary>
    /// Registers a permissive CORS policy allowing any origin, method, and header. Suitable for development.
    /// Production CORS restriction is handled at the API Gateway (YARP) level.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static TBuilder AddSorchaCors<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        return builder;
    }
}
