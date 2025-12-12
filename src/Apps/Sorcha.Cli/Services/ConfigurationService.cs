using System.Text.Json;
using Sorcha.Cli.Models;

namespace Sorcha.Cli.Services;

/// <summary>
/// Service for managing CLI configuration and profiles.
/// Configuration is stored in ~/.sorcha/config.json
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private static string GetConfigDirectory()
    {
        // Allow override via environment variable for testing
        var overrideDir = Environment.GetEnvironmentVariable("SORCHA_CONFIG_DIR");
        if (!string.IsNullOrEmpty(overrideDir))
        {
            return overrideDir;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".sorcha");
    }

    private string ConfigDirectory => GetConfigDirectory();
    private string ConfigFilePath => Path.Combine(ConfigDirectory, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc/>
    public string GetConfigFilePath() => ConfigFilePath;

    /// <inheritdoc/>
    public async Task EnsureConfigDirectoryAsync()
    {
        if (!Directory.Exists(ConfigDirectory))
        {
            Directory.CreateDirectory(ConfigDirectory);

            // Set restrictive permissions on Unix systems
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(ConfigDirectory,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }

        // Create default config if it doesn't exist (without calling SaveConfigurationAsync to avoid recursion)
        if (!File.Exists(ConfigFilePath))
        {
            var config = CreateDefaultConfiguration();
            var json = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(ConfigFilePath, json);

            // Small delay to ensure file handle is released
            await Task.Delay(100);

            // Set restrictive permissions on Unix systems
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(ConfigFilePath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<CliConfiguration> GetConfigurationAsync()
    {
        await EnsureConfigDirectoryAsync();

        if (!File.Exists(ConfigFilePath))
        {
            return CreateDefaultConfiguration();
        }

        var json = await File.ReadAllTextAsync(ConfigFilePath);
        var config = JsonSerializer.Deserialize<CliConfiguration>(json, JsonOptions);

        return config ?? CreateDefaultConfiguration();
    }

    /// <inheritdoc/>
    public async Task SaveConfigurationAsync(CliConfiguration configuration)
    {
        await EnsureConfigDirectoryAsync();

        var json = JsonSerializer.Serialize(configuration, JsonOptions);
        await File.WriteAllTextAsync(ConfigFilePath, json);

        // Set restrictive permissions on Unix systems
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(ConfigFilePath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    /// <inheritdoc/>
    public async Task<Profile?> GetProfileAsync(string name)
    {
        var config = await GetConfigurationAsync();
        return config.Profiles.TryGetValue(name, out var profile) ? profile : null;
    }

    /// <inheritdoc/>
    public async Task<Profile?> GetActiveProfileAsync()
    {
        var config = await GetConfigurationAsync();
        return config.GetActiveProfile();
    }

    /// <inheritdoc/>
    public async Task SetActiveProfileAsync(string name)
    {
        var config = await GetConfigurationAsync();

        if (!config.Profiles.ContainsKey(name))
        {
            throw new InvalidOperationException($"Profile '{name}' does not exist.");
        }

        config.ActiveProfile = name;
        await SaveConfigurationAsync(config);
    }

    /// <inheritdoc/>
    public async Task UpsertProfileAsync(Profile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            throw new ArgumentException("Profile name cannot be empty.", nameof(profile));
        }

        var config = await GetConfigurationAsync();
        config.Profiles[profile.Name] = profile;

        // If this is the first profile, make it active
        if (config.Profiles.Count == 1 || string.IsNullOrEmpty(config.ActiveProfile))
        {
            config.ActiveProfile = profile.Name;
        }

        await SaveConfigurationAsync(config);
    }

    /// <inheritdoc/>
    public async Task DeleteProfileAsync(string name)
    {
        var config = await GetConfigurationAsync();

        if (!config.Profiles.Remove(name))
        {
            throw new InvalidOperationException($"Profile '{name}' does not exist.");
        }

        // If we deleted the active profile, clear it or set to another profile
        if (config.ActiveProfile == name)
        {
            config.ActiveProfile = config.Profiles.Keys.FirstOrDefault();
        }

        await SaveConfigurationAsync(config);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Profile>> ListProfilesAsync()
    {
        var config = await GetConfigurationAsync();
        return config.Profiles.Values;
    }

    /// <summary>
    /// Creates a default configuration with dev, local (Docker), staging, and production profiles.
    /// </summary>
    private static CliConfiguration CreateDefaultConfiguration()
    {
        return new CliConfiguration
        {
            ActiveProfile = "dev",
            DefaultOutputFormat = "table",
            VerboseLogging = false,
            QuietMode = false,
            Profiles = new Dictionary<string, Profile>
            {
                ["dev"] = new Profile
                {
                    Name = "dev",
                    TenantServiceUrl = "https://localhost:7080",
                    RegisterServiceUrl = "https://localhost:7081",
                    PeerServiceUrl = "https://localhost:7082",
                    WalletServiceUrl = "https://localhost:7083",
                    AuthTokenUrl = "https://localhost:7080/api/service-auth/token",
                    DefaultClientId = "sorcha-cli",
                    VerifySsl = false,
                    TimeoutSeconds = 30
                },
                ["local"] = new Profile
                {
                    Name = "local",
                    TenantServiceUrl = "http://localhost:5080",
                    RegisterServiceUrl = "http://localhost:5081",
                    PeerServiceUrl = "http://localhost:5082",
                    WalletServiceUrl = "http://localhost:5083",
                    AuthTokenUrl = "http://localhost:5080/api/service-auth/token",
                    DefaultClientId = "sorcha-cli",
                    VerifySsl = false,
                    TimeoutSeconds = 30
                },
                ["docker"] = new Profile
                {
                    Name = "docker",
                    TenantServiceUrl = "http://localhost:8080/tenant",
                    RegisterServiceUrl = "http://localhost:8080/register",
                    PeerServiceUrl = "http://localhost:8080/peer",
                    WalletServiceUrl = "http://localhost:8080/wallet",
                    AuthTokenUrl = "http://localhost:8080/tenant/api/service-auth/token",
                    DefaultClientId = "sorcha-cli",
                    VerifySsl = false,
                    TimeoutSeconds = 30
                },
                ["aspire"] = new Profile
                {
                    Name = "aspire",
                    TenantServiceUrl = "https://localhost:7051/api/tenant",
                    RegisterServiceUrl = "https://localhost:7051/api/register",
                    PeerServiceUrl = "https://localhost:7051/api/peer",
                    WalletServiceUrl = "https://localhost:7051/api/wallet",
                    AuthTokenUrl = "https://localhost:7051/api/tenant/api/service-auth/token",
                    DefaultClientId = "sorcha-cli",
                    VerifySsl = false,
                    TimeoutSeconds = 30
                },
                ["staging"] = new Profile
                {
                    Name = "staging",
                    TenantServiceUrl = "https://n0.sorcha.dev",
                    RegisterServiceUrl = "https://n0.sorcha.dev",
                    PeerServiceUrl = "https://n0.sorcha.dev",
                    WalletServiceUrl = "https://n0.sorcha.dev",
                    AuthTokenUrl = "https://n0.sorcha.dev/api/service-auth/token",
                    DefaultClientId = "sorcha-cli",
                    VerifySsl = true,
                    TimeoutSeconds = 30
                },
                ["production"] = new Profile
                {
                    Name = "production",
                    TenantServiceUrl = "https://tenant.sorcha.io",
                    RegisterServiceUrl = "https://register.sorcha.io",
                    PeerServiceUrl = "https://peer.sorcha.io",
                    WalletServiceUrl = "https://wallet.sorcha.io",
                    AuthTokenUrl = "https://tenant.sorcha.io/api/service-auth/token",
                    DefaultClientId = "sorcha-cli",
                    VerifySsl = true,
                    TimeoutSeconds = 30
                }
            }
        };
    }
}
