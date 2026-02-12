// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.DependencyInjection;
using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Service.Services.Interfaces;

namespace Sorcha.Validator.Service.Extensions;

/// <summary>
/// Extension methods for registering Exception Response Handler services
/// </summary>
public static class ExceptionResponseHandlerExtensions
{
    /// <summary>
    /// Adds the Exception Response Handler to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddExceptionResponseHandler(this IServiceCollection services)
    {
        services.AddSingleton<IExceptionResponseHandler, ExceptionResponseHandler>();

        return services;
    }
}
