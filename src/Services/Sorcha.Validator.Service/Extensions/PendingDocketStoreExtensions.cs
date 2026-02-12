// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Service.Services.Interfaces;

namespace Sorcha.Validator.Service.Extensions;

/// <summary>
/// Extension methods for registering pending docket store services
/// </summary>
public static class PendingDocketStoreExtensions
{
    /// <summary>
    /// Adds pending docket store services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPendingDocketStore(this IServiceCollection services)
    {
        services.AddSingleton<IPendingDocketStore, PendingDocketStore>();
        return services;
    }
}
