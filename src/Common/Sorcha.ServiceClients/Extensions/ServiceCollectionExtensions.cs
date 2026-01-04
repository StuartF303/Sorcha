// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sorcha.ServiceClients.Wallet;
using Sorcha.ServiceClients.Register;
using Sorcha.ServiceClients.Blueprint;
using Sorcha.ServiceClients.Peer;
using Sorcha.ServiceClients.Validator;

namespace Sorcha.ServiceClients.Extensions;

/// <summary>
/// Dependency injection extensions for service clients
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Sorcha service clients
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    /// <remarks>
    /// Registers:
    /// - IWalletServiceClient
    /// - IRegisterServiceClient
    /// - IBlueprintServiceClient
    /// - IPeerServiceClient
    ///
    /// Configuration:
    /// <code>
    /// {
    ///   "ServiceClients": {
    ///     "WalletService": { "Address": "https://localhost:7001", "UseGrpc": false },
    ///     "RegisterService": { "Address": "https://localhost:7002", "UseGrpc": false },
    ///     "BlueprintService": { "Address": "https://localhost:7003", "UseGrpc": false },
    ///     "PeerService": { "Address": "https://localhost:7004", "UseGrpc": true }
    ///   }
    /// }
    /// </code>
    /// </remarks>
    public static IServiceCollection AddServiceClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register all service clients as scoped (one per request)
        services.AddScoped<IWalletServiceClient, WalletServiceClient>();
        services.AddScoped<IRegisterServiceClient, RegisterServiceClient>();
        services.AddScoped<IBlueprintServiceClient, BlueprintServiceClient>();
        services.AddScoped<IPeerServiceClient, PeerServiceClient>();
        services.AddScoped<IValidatorServiceClient, ValidatorServiceClient>();

        // Register HttpClient for ValidatorServiceClient
        services.AddHttpClient<ValidatorServiceClient>();

        return services;
    }

    /// <summary>
    /// Registers individual service client
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddWalletServiceClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IWalletServiceClient, WalletServiceClient>();
        return services;
    }

    /// <summary>
    /// Registers individual service client
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddRegisterServiceClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IRegisterServiceClient, RegisterServiceClient>();
        return services;
    }

    /// <summary>
    /// Registers individual service client
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddBlueprintServiceClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IBlueprintServiceClient, BlueprintServiceClient>();
        return services;
    }

    /// <summary>
    /// Registers individual service client
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddPeerServiceClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IPeerServiceClient, PeerServiceClient>();
        return services;
    }
}
