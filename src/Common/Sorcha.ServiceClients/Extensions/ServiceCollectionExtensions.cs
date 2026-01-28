// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sorcha.ServiceClients.Auth;
using Sorcha.ServiceClients.Wallet;
using Sorcha.ServiceClients.Register;
using Sorcha.ServiceClients.Blueprint;
using Sorcha.ServiceClients.Peer;
using Sorcha.ServiceClients.Participant;
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
    /// - IParticipantServiceClient
    ///
    /// Configuration:
    /// <code>
    /// {
    ///   "ServiceClients": {
    ///     "WalletService": { "Address": "https://localhost:7001", "UseGrpc": false },
    ///     "RegisterService": { "Address": "https://localhost:7002", "UseGrpc": false },
    ///     "BlueprintService": { "Address": "https://localhost:7003", "UseGrpc": false },
    ///     "PeerService": { "Address": "https://localhost:7004", "UseGrpc": true },
    ///     "TenantService": { "Address": "https://localhost:7110" }
    ///   }
    /// }
    /// </code>
    /// </remarks>
    public static IServiceCollection AddServiceClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register service auth client (singleton - caches tokens across requests)
        services.AddHttpClient<ServiceAuthClient>();
        services.AddSingleton<IServiceAuthClient, ServiceAuthClient>();

        // Register all service clients with HttpClient factories
        services.AddHttpClient<WalletServiceClient>();
        services.AddScoped<IWalletServiceClient, WalletServiceClient>();

        services.AddHttpClient<RegisterServiceClient>();
        services.AddScoped<IRegisterServiceClient, RegisterServiceClient>();

        services.AddHttpClient<BlueprintServiceClient>();
        services.AddScoped<IBlueprintServiceClient, BlueprintServiceClient>();

        // Peer Service uses gRPC, registered as singleton for channel reuse
        services.AddSingleton<IPeerServiceClient, PeerServiceClient>();

        services.AddHttpClient<ValidatorServiceClient>();
        services.AddScoped<IValidatorServiceClient, ValidatorServiceClient>();

        services.AddHttpClient<ParticipantServiceClient>();
        services.AddScoped<IParticipantServiceClient, ParticipantServiceClient>();

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
    /// Registers Register Service client with HttpClient factory
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddRegisterServiceClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpClient<RegisterServiceClient>();
        services.AddScoped<IRegisterServiceClient, RegisterServiceClient>();
        return services;
    }

    /// <summary>
    /// Registers Blueprint Service client with HttpClient factory
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddBlueprintServiceClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpClient<BlueprintServiceClient>();
        services.AddScoped<IBlueprintServiceClient, BlueprintServiceClient>();
        return services;
    }

    /// <summary>
    /// Registers Peer Service gRPC client as singleton for channel reuse
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddPeerServiceClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IPeerServiceClient, PeerServiceClient>();
        return services;
    }

    /// <summary>
    /// Registers participant service client
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddParticipantServiceClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpClient<ParticipantServiceClient>();
        services.AddScoped<IParticipantServiceClient, ParticipantServiceClient>();
        return services;
    }
}
