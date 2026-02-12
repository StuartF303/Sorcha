// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Linq.Expressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Sorcha.Storage.Abstractions;

namespace Sorcha.Storage.MongoDB;

/// <summary>
/// Extension methods for registering MongoDB storage services.
/// </summary>
public static class MongoServiceExtensions
{
    /// <summary>
    /// Adds MongoDB client as a singleton service.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration instance.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddMongoClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetSection("Storage:Warm:Documents:ConnectionString").Value
            ?? configuration.GetSection("Storage:Cold:ConnectionString").Value;

        if (!string.IsNullOrEmpty(connectionString))
        {
            services.TryAddSingleton<IMongoClient>(new MongoClient(connectionString));
        }

        return services;
    }

    /// <summary>
    /// Adds MongoDB client with explicit connection string.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="connectionString">MongoDB connection string.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddMongoClient(
        this IServiceCollection services,
        string connectionString)
    {
        services.TryAddSingleton<IMongoClient>(new MongoClient(connectionString));
        return services;
    }

    /// <summary>
    /// Adds a MongoDB database as a singleton service.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="databaseName">Database name.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddMongoDatabase(
        this IServiceCollection services,
        string databaseName)
    {
        services.TryAddSingleton(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(databaseName);
        });

        return services;
    }

    /// <summary>
    /// Adds a MongoDB document store for a specific document type.
    /// </summary>
    /// <typeparam name="TDocument">Document type.</typeparam>
    /// <typeparam name="TId">Document identifier type.</typeparam>
    /// <param name="services">Service collection.</param>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="idSelector">Function to extract ID from document.</param>
    /// <param name="idExpression">Expression to extract ID.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddMongoDocumentStore<TDocument, TId>(
        this IServiceCollection services,
        string collectionName,
        Func<TDocument, TId> idSelector,
        Expression<Func<TDocument, TId>> idExpression)
        where TDocument : class
        where TId : notnull
    {
        services.AddSingleton<IDocumentStore<TDocument, TId>>(sp =>
        {
            var database = sp.GetRequiredService<IMongoDatabase>();
            return new MongoDocumentStore<TDocument, TId>(database, collectionName, idSelector, idExpression);
        });

        return services;
    }

    /// <summary>
    /// Adds a MongoDB WORM store for a specific document type.
    /// </summary>
    /// <typeparam name="TDocument">Document type.</typeparam>
    /// <typeparam name="TId">Document identifier type.</typeparam>
    /// <param name="services">Service collection.</param>
    /// <param name="collectionName">Collection name.</param>
    /// <param name="idSelector">Function to extract ID from document.</param>
    /// <param name="idExpression">Expression to extract ID.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddMongoWormStore<TDocument, TId>(
        this IServiceCollection services,
        string collectionName,
        Func<TDocument, TId> idSelector,
        Expression<Func<TDocument, TId>> idExpression)
        where TDocument : class
        where TId : notnull
    {
        services.AddSingleton<IWormStore<TDocument, TId>>(sp =>
        {
            var database = sp.GetRequiredService<IMongoDatabase>();
            return new MongoWormStore<TDocument, TId>(database, collectionName, idSelector, idExpression);
        });

        return services;
    }
}
