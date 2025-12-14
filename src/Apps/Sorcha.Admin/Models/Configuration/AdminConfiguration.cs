namespace Sorcha.Admin.Models.Configuration;

/// <summary>
/// Root configuration model for the Sorcha Admin application.
/// Stored in browser LocalStorage as JSON.
/// Adapted from CLI's CliConfiguration for browser constraints.
/// </summary>
public class AdminConfiguration
{
    /// <summary>
    /// Name of the currently active profile.
    /// Determines which environment the Admin UI connects to.
    /// </summary>
    public string? ActiveProfile { get; set; }

    /// <summary>
    /// Dictionary of all configured profiles, keyed by profile name.
    /// Includes both default profiles (dev, local, docker, etc.) and custom user-created profiles.
    /// </summary>
    public Dictionary<string, Profile> Profiles { get; set; } = new();

    /// <summary>
    /// Enable verbose logging in browser console.
    /// Useful for debugging authentication and API calls.
    /// </summary>
    public bool VerboseLogging { get; set; } = false;

    /// <summary>
    /// Gets the currently active profile configuration.
    /// </summary>
    /// <returns>The active Profile object, or null if no active profile is set or profile doesn't exist.</returns>
    public Profile? GetActiveProfile()
    {
        if (string.IsNullOrEmpty(ActiveProfile))
            return null;

        return Profiles.TryGetValue(ActiveProfile, out var profile) ? profile : null;
    }
}
