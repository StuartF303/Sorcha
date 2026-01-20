// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Blueprint.Schemas.Repositories;

/// <summary>
/// Configuration options for MongoDB schema storage.
/// </summary>
public class MongoSchemaStorageConfiguration
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "MongoSchemaStorage";

    /// <summary>
    /// Gets or sets the MongoDB connection string.
    /// </summary>
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    public string DatabaseName { get; set; } = "sorcha_blueprint";

    /// <summary>
    /// Gets or sets the collection name for schemas.
    /// </summary>
    public string CollectionName { get; set; } = "schemas";

    /// <summary>
    /// Gets or sets whether to create indexes on startup.
    /// </summary>
    public bool CreateIndexesOnStartup { get; set; } = true;
}
