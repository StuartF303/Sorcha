using Sorcha.UI.Core.Models.Configuration;

namespace Sorcha.UI.Core.Services.Configuration;

/// <summary>
/// Service for managing user profiles and UI configuration
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets all available profiles
    /// </summary>
    /// <returns>List of profiles</returns>
    Task<IReadOnlyList<Profile>> GetProfilesAsync();

    /// <summary>
    /// Gets a profile by name
    /// </summary>
    /// <param name="name">Profile name</param>
    /// <returns>Profile or null if not found</returns>
    Task<Profile?> GetProfileAsync(string name);

    /// <summary>
    /// Saves a profile (creates or updates)
    /// </summary>
    /// <param name="profile">Profile to save</param>
    Task SaveProfileAsync(Profile profile);

    /// <summary>
    /// Deletes a profile by name
    /// </summary>
    /// <param name="name">Profile name</param>
    /// <returns>True if deleted, false if not found or system profile</returns>
    Task<bool> DeleteProfileAsync(string name);

    /// <summary>
    /// Gets the active profile name
    /// </summary>
    /// <returns>Active profile name</returns>
    Task<string> GetActiveProfileNameAsync();

    /// <summary>
    /// Sets the active profile
    /// </summary>
    /// <param name="profileName">Profile name to activate</param>
    Task SetActiveProfileAsync(string profileName);

    /// <summary>
    /// Gets the UI configuration
    /// </summary>
    /// <returns>UI configuration</returns>
    Task<UiConfiguration> GetUiConfigurationAsync();

    /// <summary>
    /// Saves the UI configuration
    /// </summary>
    /// <param name="configuration">UI configuration to save</param>
    Task SaveUiConfigurationAsync(UiConfiguration configuration);

    /// <summary>
    /// Initializes default profiles if none exist
    /// </summary>
    Task InitializeDefaultProfilesAsync();
}
