using Blazored.LocalStorage;
using Sorcha.Admin.Models.Configuration;
using System.Text.Json;

namespace Sorcha.Admin.Services.Configuration;

/// <summary>
/// Browser-compatible configuration service using LocalStorage.
/// Stores configuration as JSON at key: sorcha:config
///
/// This service manages environment profiles (dev, staging, production, etc.)
/// and provides methods to switch between environments.
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly ILocalStorageService _localStorage;
    private const string CONFIG_KEY = "sorcha:config";
    private AdminConfiguration? _cachedConfig;

    public ConfigurationService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
    }

    /// <summary>
    /// Gets the application configuration from LocalStorage.
    /// Creates default configuration with 6 profiles if none exists.
    /// </summary>
    public async Task<AdminConfiguration> GetConfigurationAsync()
    {
        // Return cached config if available
        if (_cachedConfig != null)
            return _cachedConfig;

        try
        {
            // Try to load from LocalStorage
            var json = await _localStorage.GetItemAsStringAsync(CONFIG_KEY);

            if (string.IsNullOrEmpty(json))
            {
                // Create default configuration
                _cachedConfig = CreateDefaultConfiguration();
                await SaveConfigurationAsync(_cachedConfig);
            }
            else
            {
                // Deserialize existing configuration
                _cachedConfig = JsonSerializer.Deserialize<AdminConfiguration>(json);

                // Validate and ensure defaults exist
                if (_cachedConfig == null || _cachedConfig.Profiles.Count == 0)
                {
                    _cachedConfig = CreateDefaultConfiguration();
                    await SaveConfigurationAsync(_cachedConfig);
                }
            }

            return _cachedConfig;
        }
        catch (JsonException ex)
        {
            // Corrupted config - recreate defaults
            _cachedConfig = CreateDefaultConfiguration();
            await SaveConfigurationAsync(_cachedConfig);
            throw new InvalidOperationException("Configuration was corrupted and has been reset to defaults.", ex);
        }
    }

    /// <summary>
    /// Saves the configuration to LocalStorage and updates the cache.
    /// </summary>
    public async Task SaveConfigurationAsync(AdminConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        try
        {
            // Update cache
            _cachedConfig = configuration;

            // Serialize with indentation for readability in browser DevTools
            var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Save to LocalStorage
            await _localStorage.SetItemAsStringAsync(CONFIG_KEY, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to save configuration to LocalStorage.", ex);
        }
    }

    /// <summary>
    /// Gets a specific profile by name.
    /// </summary>
    public async Task<Profile?> GetProfileAsync(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Profile name cannot be null or empty.", nameof(name));

        var config = await GetConfigurationAsync();
        return config.Profiles.TryGetValue(name, out var profile) ? profile : null;
    }

    /// <summary>
    /// Gets the currently active profile.
    /// </summary>
    public async Task<Profile?> GetActiveProfileAsync()
    {
        var config = await GetConfigurationAsync();
        return config.GetActiveProfile();
    }

    /// <summary>
    /// Sets the active profile by name.
    /// </summary>
    public async Task SetActiveProfileAsync(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Profile name cannot be null or empty.", nameof(name));

        var config = await GetConfigurationAsync();

        if (!config.Profiles.ContainsKey(name))
            throw new InvalidOperationException($"Profile '{name}' does not exist.");

        config.ActiveProfile = name;
        await SaveConfigurationAsync(config);
    }

    /// <summary>
    /// Creates or updates a profile.
    /// </summary>
    public async Task UpsertProfileAsync(Profile profile)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));

        if (string.IsNullOrEmpty(profile.Name))
            throw new ArgumentException("Profile name cannot be null or empty.", nameof(profile));

        // Validate profile name (alphanumeric + dash/underscore only)
        if (!System.Text.RegularExpressions.Regex.IsMatch(profile.Name, @"^[a-zA-Z0-9_-]+$"))
            throw new ArgumentException(
                "Profile name can only contain letters, numbers, dashes, and underscores.", nameof(profile));

        var config = await GetConfigurationAsync();
        config.Profiles[profile.Name] = profile;
        await SaveConfigurationAsync(config);
    }

    /// <summary>
    /// Deletes a profile.
    /// Cannot delete the active profile.
    /// </summary>
    public async Task DeleteProfileAsync(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Profile name cannot be null or empty.", nameof(name));

        var config = await GetConfigurationAsync();

        if (config.ActiveProfile == name)
            throw new InvalidOperationException(
                "Cannot delete the active profile. Please switch to a different profile first.");

        if (!config.Profiles.ContainsKey(name))
            throw new InvalidOperationException($"Profile '{name}' does not exist.");

        config.Profiles.Remove(name);
        await SaveConfigurationAsync(config);
    }

    /// <summary>
    /// Gets all configured profiles.
    /// </summary>
    public async Task<Dictionary<string, Profile>> ListProfilesAsync()
    {
        var config = await GetConfigurationAsync();
        return config.Profiles;
    }

    /// <summary>
    /// Creates the default configuration with 6 standard profiles.
    /// </summary>
    private static AdminConfiguration CreateDefaultConfiguration()
    {
        return new AdminConfiguration
        {
            ActiveProfile = ProfileDefaults.DefaultActiveProfile,
            Profiles = ProfileDefaults.GetDefaultProfiles(),
            VerboseLogging = false
        };
    }
}
