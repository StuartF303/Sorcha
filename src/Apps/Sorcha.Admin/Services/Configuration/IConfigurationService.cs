using Sorcha.Admin.Models.Configuration;

namespace Sorcha.Admin.Services.Configuration;

/// <summary>
/// Configuration service interface for managing application profiles and settings.
/// Browser-compatible version that stores configuration in LocalStorage.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets the complete application configuration.
    /// Creates default configuration with 6 profiles if none exists.
    /// </summary>
    /// <returns>The current configuration.</returns>
    Task<AdminConfiguration> GetConfigurationAsync();

    /// <summary>
    /// Saves the application configuration to LocalStorage.
    /// </summary>
    /// <param name="configuration">The configuration to save.</param>
    Task SaveConfigurationAsync(AdminConfiguration configuration);

    /// <summary>
    /// Gets a specific profile by name.
    /// </summary>
    /// <param name="name">Profile name to retrieve.</param>
    /// <returns>The profile, or null if not found.</returns>
    Task<Profile?> GetProfileAsync(string name);

    /// <summary>
    /// Gets the currently active profile.
    /// </summary>
    /// <returns>The active profile, or null if no active profile is set.</returns>
    Task<Profile?> GetActiveProfileAsync();

    /// <summary>
    /// Sets the active profile by name.
    /// </summary>
    /// <param name="name">Profile name to activate.</param>
    /// <exception cref="InvalidOperationException">Thrown if the profile doesn't exist.</exception>
    Task SetActiveProfileAsync(string name);

    /// <summary>
    /// Creates or updates a profile.
    /// </summary>
    /// <param name="profile">The profile to upsert.</param>
    Task UpsertProfileAsync(Profile profile);

    /// <summary>
    /// Deletes a profile.
    /// Cannot delete the active profile.
    /// </summary>
    /// <param name="name">Profile name to delete.</param>
    /// <exception cref="InvalidOperationException">Thrown if trying to delete the active profile.</exception>
    Task DeleteProfileAsync(string name);

    /// <summary>
    /// Gets all configured profiles.
    /// </summary>
    /// <returns>Dictionary of all profiles keyed by profile name.</returns>
    Task<Dictionary<string, Profile>> ListProfilesAsync();
}
