// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;
using Sorcha.Admin.Models;
using Sorcha.Blueprint.Models;

namespace Sorcha.Admin.Services;

/// <summary>
/// Service for blueprint storage with server and offline support.
/// </summary>
public class BlueprintStorageService : IBlueprintStorageService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly IOfflineSyncService _syncService;
    private readonly ILogger<BlueprintStorageService> _logger;

    private const string LOCAL_CACHE_KEY = "sorcha:blueprints:cache";
    private const string LAST_SYNC_KEY = "sorcha:blueprints:lastSync";
    private const string API_BASE = "/api/blueprint";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public event EventHandler<BlueprintSavedEventArgs>? BlueprintSaved;

    public BlueprintStorageService(
        HttpClient httpClient,
        ILocalStorageService localStorage,
        IOfflineSyncService syncService,
        ILogger<BlueprintStorageService> logger)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _syncService = syncService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Blueprint.Models.Blueprint>> GetBlueprintsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try server first
            if (await IsServerAvailableAsync(cancellationToken))
            {
                var blueprints = await _httpClient.GetFromJsonAsync<List<Blueprint.Models.Blueprint>>(
                    $"{API_BASE}/list",
                    JsonOptions,
                    cancellationToken);

                if (blueprints != null)
                {
                    // Cache locally for offline use
                    await CacheLocally(blueprints);
                    return blueprints;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch blueprints from server, falling back to local cache");
        }

        // Fall back to local cache
        return await GetFromLocalCache();
    }

    /// <inheritdoc />
    public async Task<Blueprint.Models.Blueprint?> GetBlueprintAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            if (await IsServerAvailableAsync(cancellationToken))
            {
                return await _httpClient.GetFromJsonAsync<Blueprint.Models.Blueprint>(
                    $"{API_BASE}/{id}",
                    JsonOptions,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch blueprint {Id} from server", id);
        }

        // Fall back to local cache
        var cached = await GetFromLocalCache();
        return cached.FirstOrDefault(b => b.Id == id);
    }

    /// <inheritdoc />
    public async Task<BlueprintSaveResult> SaveBlueprintAsync(Blueprint.Models.Blueprint blueprint, CancellationToken cancellationToken = default)
    {
        blueprint.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            if (await IsServerAvailableAsync(cancellationToken))
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"{API_BASE}/save",
                    blueprint,
                    JsonOptions,
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    // Update local cache
                    await UpdateLocalCache(blueprint);

                    BlueprintSaved?.Invoke(this, new BlueprintSavedEventArgs
                    {
                        BlueprintId = blueprint.Id,
                        SavedToServer = true
                    });

                    return BlueprintSaveResult.ServerSuccess(blueprint.Id);
                }

                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Server save failed: {Error}", error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save blueprint to server, queuing for sync");
        }

        // Server unavailable - save locally and queue for sync
        await UpdateLocalCache(blueprint);
        var blueprintJson = JsonSerializer.Serialize(blueprint, JsonOptions);
        await _syncService.QueueOperationAsync(SyncOperation.Update, blueprint.Id, blueprintJson);

        BlueprintSaved?.Invoke(this, new BlueprintSavedEventArgs
        {
            BlueprintId = blueprint.Id,
            SavedToServer = false
        });

        return BlueprintSaveResult.Queued(blueprint.Id);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteBlueprintAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            if (await IsServerAvailableAsync(cancellationToken))
            {
                var response = await _httpClient.DeleteAsync($"{API_BASE}/{id}", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    await RemoveFromLocalCache(id);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete blueprint from server, queuing for sync");
        }

        // Queue deletion for later sync
        await RemoveFromLocalCache(id);
        await _syncService.QueueOperationAsync(SyncOperation.Delete, id);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> IsServerAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await _httpClient.GetAsync($"{API_BASE}/health", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<MigrationResult> MigrateLocalBlueprintsAsync(CancellationToken cancellationToken = default)
    {
        var result = new MigrationResult();

        if (!await IsServerAvailableAsync(cancellationToken))
        {
            _logger.LogWarning("Server not available for migration");
            return result;
        }

        var localBlueprints = await GetFromLocalCache();
        if (!localBlueprints.Any())
        {
            return result;
        }

        foreach (var blueprint in localBlueprints)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"{API_BASE}/save",
                    blueprint,
                    JsonOptions,
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    result.MigratedCount++;
                }
                else
                {
                    result.FailedIds.Add(blueprint.Id);
                    result.FailedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate blueprint {Id}", blueprint.Id);
                result.FailedIds.Add(blueprint.Id);
                result.FailedCount++;
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task ClearLocalCacheAsync(CancellationToken cancellationToken = default)
    {
        await _localStorage.RemoveItemAsync(LOCAL_CACHE_KEY, cancellationToken);
        await _localStorage.RemoveItemAsync(LAST_SYNC_KEY, cancellationToken);
    }

    private async Task<List<Blueprint.Models.Blueprint>> GetFromLocalCache()
    {
        try
        {
            var json = await _localStorage.GetItemAsStringAsync(LOCAL_CACHE_KEY);
            if (!string.IsNullOrEmpty(json))
            {
                return JsonSerializer.Deserialize<List<Blueprint.Models.Blueprint>>(json, JsonOptions) ?? [];
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read local cache");
        }

        return [];
    }

    private async Task CacheLocally(List<Blueprint.Models.Blueprint> blueprints)
    {
        try
        {
            var json = JsonSerializer.Serialize(blueprints, JsonOptions);
            await _localStorage.SetItemAsStringAsync(LOCAL_CACHE_KEY, json);
            await _localStorage.SetItemAsStringAsync(LAST_SYNC_KEY, DateTimeOffset.UtcNow.ToString("O"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache blueprints locally");
        }
    }

    private async Task UpdateLocalCache(Blueprint.Models.Blueprint blueprint)
    {
        var cached = await GetFromLocalCache();
        var existing = cached.FindIndex(b => b.Id == blueprint.Id);

        if (existing >= 0)
        {
            cached[existing] = blueprint;
        }
        else
        {
            cached.Add(blueprint);
        }

        await CacheLocally(cached);
    }

    private async Task RemoveFromLocalCache(string id)
    {
        var cached = await GetFromLocalCache();
        cached.RemoveAll(b => b.Id == id);
        await CacheLocally(cached);
    }
}
