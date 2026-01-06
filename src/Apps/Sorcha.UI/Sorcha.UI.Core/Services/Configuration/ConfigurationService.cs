using System.Text.Json;
using Microsoft.JSInterop;
using Sorcha.UI.Core.Models.Configuration;

namespace Sorcha.UI.Core.Services.Configuration;

/// <summary>
/// Service for managing user profiles and UI configuration
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private const string ProfilesStorageKey = "sorcha:profiles";
    private const string UiConfigStorageKey = "sorcha:ui-config";
    private const string ActiveProfileKey = "sorcha:active-profile";

    private readonly IJSRuntime _jsRuntime;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ConfigurationService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Profile>> GetProfilesAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ProfilesStorageKey);

            if (string.IsNullOrEmpty(json))
            {
                // Initialize default profiles directly without re-acquiring lock
                await InitializeDefaultProfilesInternalAsync();
                json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ProfilesStorageKey);
            }

            var profiles = JsonSerializer.Deserialize<List<Profile>>(json!) ?? new List<Profile>();
            return profiles;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Profile?> GetProfileAsync(string name)
    {
        var profiles = await GetProfilesAsync();
        return profiles.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task SaveProfileAsync(Profile profile)
    {
        if (profile == null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        if (!profile.IsValid())
        {
            throw new ArgumentException("Invalid profile configuration", nameof(profile));
        }

        await _lock.WaitAsync();
        try
        {
            var profiles = (await GetProfilesAsync()).ToList();
            var existingIndex = profiles.FindIndex(p => p.Name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
            {
                profiles[existingIndex] = profile.WithUpdatedTimestamp();
            }
            else
            {
                profiles.Add(profile);
            }

            var json = JsonSerializer.Serialize(profiles);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ProfilesStorageKey, json);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteProfileAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Profile name cannot be null or empty", nameof(name));
        }

        await _lock.WaitAsync();
        try
        {
            var profiles = (await GetProfilesAsync()).ToList();
            var profile = profiles.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (profile == null)
            {
                return false;
            }

            if (profile.IsSystemProfile)
            {
                return false; // Cannot delete system profiles
            }

            profiles.Remove(profile);

            var json = JsonSerializer.Serialize(profiles);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ProfilesStorageKey, json);

            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<string> GetActiveProfileNameAsync()
    {
        var name = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ActiveProfileKey);
        return name ?? "Development";
    }

    /// <inheritdoc />
    public async Task SetActiveProfileAsync(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            throw new ArgumentException("Profile name cannot be null or empty", nameof(profileName));
        }

        var profile = await GetProfileAsync(profileName);
        if (profile == null)
        {
            throw new InvalidOperationException($"Profile '{profileName}' not found");
        }

        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ActiveProfileKey, profileName);
    }

    /// <inheritdoc />
    public async Task<UiConfiguration> GetUiConfigurationAsync()
    {
        var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", UiConfigStorageKey);

        if (string.IsNullOrEmpty(json))
        {
            return UiConfiguration.Default();
        }

        return JsonSerializer.Deserialize<UiConfiguration>(json) ?? UiConfiguration.Default();
    }

    /// <inheritdoc />
    public async Task SaveUiConfigurationAsync(UiConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var json = JsonSerializer.Serialize(configuration);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", UiConfigStorageKey, json);
    }

    /// <inheritdoc />
    public async Task InitializeDefaultProfilesAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await InitializeDefaultProfilesInternalAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Internal method to initialize default profiles without acquiring the semaphore lock.
    /// Used when the lock is already held by the calling method.
    /// </summary>
    private async Task InitializeDefaultProfilesInternalAsync()
    {
        var profiles = new List<Profile>
        {
            new Profile
            {
                Name = "Development",
                ApiGatewayUrl = "https://localhost:7082",
                Description = "Local .NET Aspire development environment",
                IsSystemProfile = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Profile
            {
                Name = "Docker",
                ApiGatewayUrl = "", // Empty = use same origin as UI (relative URLs)
                Description = "Docker Compose backend services (same origin as UI)",
                IsSystemProfile = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        var json = JsonSerializer.Serialize(profiles);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ProfilesStorageKey, json);
    }
}
