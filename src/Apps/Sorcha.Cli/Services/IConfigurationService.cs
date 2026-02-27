// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors
using Sorcha.Cli.Models;

namespace Sorcha.Cli.Services;

/// <summary>
/// Service for managing CLI configuration and profiles.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets the current CLI configuration.
    /// </summary>
    Task<CliConfiguration> GetConfigurationAsync();

    /// <summary>
    /// Saves the CLI configuration to disk.
    /// </summary>
    Task SaveConfigurationAsync(CliConfiguration configuration);

    /// <summary>
    /// Gets a profile by name.
    /// </summary>
    Task<Profile?> GetProfileAsync(string name);

    /// <summary>
    /// Gets the currently active profile.
    /// </summary>
    Task<Profile?> GetActiveProfileAsync();

    /// <summary>
    /// Sets the active profile.
    /// </summary>
    Task SetActiveProfileAsync(string name);

    /// <summary>
    /// Creates or updates a profile.
    /// </summary>
    Task UpsertProfileAsync(Profile profile);

    /// <summary>
    /// Deletes a profile.
    /// </summary>
    Task DeleteProfileAsync(string name);

    /// <summary>
    /// Lists all available profiles.
    /// </summary>
    Task<IEnumerable<Profile>> ListProfilesAsync();

    /// <summary>
    /// Ensures the configuration directory exists.
    /// </summary>
    Task EnsureConfigDirectoryAsync();

    /// <summary>
    /// Gets the path to the configuration file.
    /// </summary>
    string GetConfigFilePath();
}
