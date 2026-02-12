// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Validator.Service.Configuration;

/// <summary>
/// Configuration for the BlueprintFetcher service.
/// </summary>
public class BlueprintFetcherConfiguration
{
    /// <summary>
    /// Configuration section name in appsettings.
    /// </summary>
    public const string SectionName = "BlueprintFetcher";

    /// <summary>
    /// Timeout for fetching blueprints.
    /// Default: 30 seconds.
    /// </summary>
    public TimeSpan FetchTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum retry attempts for failed fetches.
    /// Default: 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts.
    /// Default: 1 second.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Whether to validate payload schemas locally when possible.
    /// Default: true.
    /// </summary>
    public bool EnableLocalSchemaValidation { get; set; } = true;

    /// <summary>
    /// Maximum size of a blueprint in bytes.
    /// Default: 5 MB.
    /// </summary>
    public int MaxBlueprintSizeBytes { get; set; } = 5 * 1024 * 1024;
}
