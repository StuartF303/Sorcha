// API Contract: IConfigurationService
// Purpose: Profile and UI configuration management
// Location: Sorcha.UI.Core/Services/Configuration/IConfigurationService.cs

using Sorcha.UI.Core.Models.Configuration;

namespace Sorcha.UI.Core.Services.Configuration;

/// <summary>
/// Configuration service for managing profiles and UI settings
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets all configured profiles
    /// </summary>
    /// <returns>List of profiles</returns>
    Task<List<Profile>> GetProfilesAsync();

    /// <summary>
    /// Gets a specific profile by name
    /// </summary>
    /// <param name="name">Profile name</param>
    /// <returns>Profile or null if not found</returns>
    Task<Profile?> GetProfileAsync(string name);

    /// <summary>
    /// Gets the currently active profile
    /// </summary>
    /// <returns>Active profile or null if not set</returns>
    Task<Profile?> GetActiveProfileAsync();

    /// <summary>
    /// Gets the active profile name
    /// </summary>
    /// <returns>Active profile name or "Development" default</returns>
    Task<string> GetActiveProfileNameAsync();

    /// <summary>
    /// Sets the active profile (requires logout if switching)
    /// </summary>
    /// <param name="profileName">Profile name</param>
    /// <exception cref="InvalidOperationException">Profile not found</exception>
    Task SetActiveProfileAsync(string profileName);

    /// <summary>
    /// Adds or updates a profile
    /// </summary>
    /// <param name="profile">Profile to save</param>
    /// <exception cref="ArgumentException">Invalid profile data</exception>
    Task SaveProfileAsync(Profile profile);

    /// <summary>
    /// Deletes a profile
    /// </summary>
    /// <param name="profileName">Profile name</param>
    /// <exception cref="InvalidOperationException">Cannot delete active profile</exception>
    Task DeleteProfileAsync(string profileName);

    /// <summary>
    /// Gets UI configuration
    /// </summary>
    /// <returns>UI configuration</returns>
    Task<UiConfiguration> GetUiConfigurationAsync();

    /// <summary>
    /// Updates UI configuration
    /// </summary>
    /// <param name="config">UI configuration</param>
    Task SaveUiConfigurationAsync(UiConfiguration config);

    /// <summary>
    /// Resets to default profiles (Development, Docker)
    /// </summary>
    Task ResetToDefaultProfilesAsync();
}
