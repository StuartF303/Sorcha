namespace Sorcha.Cli.Models;

/// <summary>
/// Root configuration for the Sorcha CLI.
/// Stored in ~/.sorcha/config.json
/// </summary>
public class CliConfiguration
{
    /// <summary>
    /// Name of the currently active profile.
    /// </summary>
    public string? ActiveProfile { get; set; }

    /// <summary>
    /// Collection of available profiles.
    /// </summary>
    public Dictionary<string, Profile> Profiles { get; set; } = new();

    /// <summary>
    /// Default output format (table, json, csv).
    /// </summary>
    public string DefaultOutputFormat { get; set; } = "table";

    /// <summary>
    /// Whether to enable verbose logging by default.
    /// </summary>
    public bool VerboseLogging { get; set; } = false;

    /// <summary>
    /// Whether to enable quiet mode by default.
    /// </summary>
    public bool QuietMode { get; set; } = false;

    /// <summary>
    /// Collection of installation records (bootstrapped instances).
    /// Key is the installation name (e.g., "local-dev", "docker-demo").
    /// </summary>
    public Dictionary<string, Installation> Installations { get; set; } = new();

    /// <summary>
    /// Name of the currently active installation (for quick access to org/user IDs).
    /// </summary>
    public string? ActiveInstallation { get; set; }

    /// <summary>
    /// Gets the currently active profile, or null if not set.
    /// </summary>
    public Profile? GetActiveProfile()
    {
        if (string.IsNullOrEmpty(ActiveProfile))
            return null;

        return Profiles.TryGetValue(ActiveProfile, out var profile) ? profile : null;
    }

    /// <summary>
    /// Gets the currently active installation, or null if not set.
    /// </summary>
    public Installation? GetActiveInstallation()
    {
        if (string.IsNullOrEmpty(ActiveInstallation))
            return null;

        return Installations.TryGetValue(ActiveInstallation, out var installation) ? installation : null;
    }
}
