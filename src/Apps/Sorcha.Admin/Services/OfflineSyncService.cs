// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;
using Sorcha.Admin.Models;

namespace Sorcha.Admin.Services;

/// <summary>
/// Service for managing offline sync queue and automatic synchronization.
/// </summary>
public class OfflineSyncService : IOfflineSyncService
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OfflineSyncService> _logger;

    private const string QUEUE_KEY = "sorcha:sync:queue";
    private const string API_BASE = "/api/blueprint";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private List<SyncQueueItem> _queue = [];
    private bool _autoSyncEnabled;
    private Timer? _syncTimer;

    public int PendingCount => _queue.Count;
    public bool HasPendingChanges => _queue.Count > 0;

    public event Action<int>? OnQueueChanged;
    public event EventHandler<SyncCompletedEventArgs>? OnSyncCompleted;
    public event EventHandler<ConflictDetectedEventArgs>? OnConflictDetected;

    public OfflineSyncService(
        ILocalStorageService localStorage,
        HttpClient httpClient,
        ILogger<OfflineSyncService> logger)
    {
        _localStorage = localStorage;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task QueueOperationAsync(SyncOperation operation, string blueprintId, string? blueprintJson = null)
    {
        await LoadQueueAsync();

        // Check if there's an existing operation for this blueprint
        var existing = _queue.FirstOrDefault(q => q.BlueprintId == blueprintId);

        if (existing != null)
        {
            // Update existing entry
            if (operation == SyncOperation.Delete)
            {
                // Delete supersedes other operations
                existing.Operation = SyncOperation.Delete;
                existing.BlueprintJson = null;
            }
            else
            {
                existing.Operation = operation;
                existing.BlueprintJson = blueprintJson;
            }
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            existing.RetryCount = 0;
        }
        else
        {
            // Add new entry
            _queue.Add(new SyncQueueItem
            {
                Id = Guid.NewGuid().ToString(),
                BlueprintId = blueprintId,
                Operation = operation,
                BlueprintJson = blueprintJson,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        await SaveQueueAsync();
        OnQueueChanged?.Invoke(_queue.Count);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SyncResult>> SyncNowAsync(CancellationToken cancellationToken = default)
    {
        await LoadQueueAsync();
        var results = new List<SyncResult>();

        if (_queue.Count == 0)
        {
            return results;
        }

        var itemsToProcess = _queue.ToList();

        foreach (var item in itemsToProcess)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var result = await ProcessSyncItemAsync(item, cancellationToken);
            results.Add(result);

            if (result.Success)
            {
                _queue.Remove(item);
            }
            else
            {
                item.RetryCount++;
                item.UpdatedAt = DateTimeOffset.UtcNow;
                item.LastError = result.Error;
            }
        }

        await SaveQueueAsync();
        OnQueueChanged?.Invoke(_queue.Count);

        var eventArgs = new SyncCompletedEventArgs
        {
            SuccessCount = results.Count(r => r.Success),
            FailedCount = results.Count(r => !r.Success && !r.HasConflict),
            ConflictCount = results.Count(r => r.HasConflict),
            Results = results
        };

        OnSyncCompleted?.Invoke(this, eventArgs);

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SyncQueueItem>> GetQueuedItemsAsync()
    {
        await LoadQueueAsync();
        return _queue.ToList();
    }

    /// <inheritdoc />
    public async Task RemoveFromQueueAsync(string itemId)
    {
        await LoadQueueAsync();
        _queue.RemoveAll(q => q.Id == itemId);
        await SaveQueueAsync();
        OnQueueChanged?.Invoke(_queue.Count);
    }

    /// <inheritdoc />
    public async Task ClearQueueAsync()
    {
        _queue.Clear();
        await SaveQueueAsync();
        OnQueueChanged?.Invoke(0);
    }

    /// <inheritdoc />
    public Task StartAutoSyncAsync()
    {
        if (_autoSyncEnabled)
            return Task.CompletedTask;

        _autoSyncEnabled = true;

        // Check for sync every 30 seconds when online
        _syncTimer = new Timer(async _ =>
        {
            if (HasPendingChanges && await IsServerAvailableAsync())
            {
                try
                {
                    await SyncNowAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Auto-sync failed");
                }
            }
        }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAutoSyncAsync()
    {
        _autoSyncEnabled = false;
        _syncTimer?.Dispose();
        _syncTimer = null;
        return Task.CompletedTask;
    }

    private async Task<SyncResult> ProcessSyncItemAsync(SyncQueueItem item, CancellationToken cancellationToken)
    {
        string? blueprintTitle = null;

        try
        {
            HttpResponseMessage response;

            switch (item.Operation)
            {
                case SyncOperation.Create:
                case SyncOperation.Update:
                    if (string.IsNullOrEmpty(item.BlueprintJson))
                    {
                        return new SyncResult
                        {
                            Item = item,
                            Success = false,
                            Error = "No blueprint data to sync"
                        };
                    }

                    var blueprint = JsonSerializer.Deserialize<Sorcha.Blueprint.Models.Blueprint>(item.BlueprintJson, JsonOptions);
                    blueprintTitle = blueprint?.Title;

                    // Check for conflict before saving (for updates only)
                    if (item.Operation == SyncOperation.Update && blueprint != null)
                    {
                        var conflictResult = await CheckForConflictAsync(item, blueprint, cancellationToken);
                        if (conflictResult != null)
                        {
                            return conflictResult;
                        }
                    }

                    response = await _httpClient.PostAsJsonAsync(
                        $"{API_BASE}/save",
                        blueprint,
                        JsonOptions,
                        cancellationToken);
                    break;

                case SyncOperation.Delete:
                    response = await _httpClient.DeleteAsync($"{API_BASE}/{item.BlueprintId}", cancellationToken);
                    break;

                default:
                    return new SyncResult
                    {
                        Item = item,
                        Success = false,
                        Error = $"Unknown operation: {item.Operation}"
                    };
            }

            if (response.IsSuccessStatusCode)
            {
                return new SyncResult
                {
                    Item = item,
                    Success = true,
                    BlueprintTitle = blueprintTitle
                };
            }

            // Check if conflict (409)
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var serverBlueprint = await response.Content.ReadFromJsonAsync<Sorcha.Blueprint.Models.Blueprint>(JsonOptions, cancellationToken);
                var localBlueprint = JsonSerializer.Deserialize<Sorcha.Blueprint.Models.Blueprint>(item.BlueprintJson!, JsonOptions);

                if (serverBlueprint != null && localBlueprint != null)
                {
                    var conflict = new BlueprintConflict
                    {
                        LocalVersion = localBlueprint,
                        ServerVersion = serverBlueprint
                    };

                    OnConflictDetected?.Invoke(this, new ConflictDetectedEventArgs
                    {
                        Conflict = conflict,
                        QueueItem = item
                    });

                    return new SyncResult
                    {
                        Item = item,
                        Success = false,
                        HasConflict = true,
                        Conflict = conflict,
                        BlueprintTitle = blueprintTitle,
                        Error = "Version conflict detected"
                    };
                }
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            return new SyncResult
            {
                Item = item,
                Success = false,
                Error = $"Server returned {response.StatusCode}: {error}",
                BlueprintTitle = blueprintTitle
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync item {ItemId}", item.Id);
            return new SyncResult
            {
                Item = item,
                Success = false,
                Error = ex.Message,
                BlueprintTitle = blueprintTitle
            };
        }
    }

    private async Task<SyncResult?> CheckForConflictAsync(
        SyncQueueItem item,
        Sorcha.Blueprint.Models.Blueprint localBlueprint,
        CancellationToken cancellationToken)
    {
        try
        {
            // Fetch current server version
            var serverBlueprint = await _httpClient.GetFromJsonAsync<Sorcha.Blueprint.Models.Blueprint>(
                $"{API_BASE}/{item.BlueprintId}",
                JsonOptions,
                cancellationToken);

            if (serverBlueprint == null)
            {
                // Blueprint doesn't exist on server, no conflict
                return null;
            }

            // Compare versions - conflict if server version is newer than when we last synced
            if (serverBlueprint.UpdatedAt > item.CreatedAt)
            {
                var conflict = new BlueprintConflict
                {
                    LocalVersion = localBlueprint,
                    ServerVersion = serverBlueprint
                };

                OnConflictDetected?.Invoke(this, new ConflictDetectedEventArgs
                {
                    Conflict = conflict,
                    QueueItem = item
                });

                return new SyncResult
                {
                    Item = item,
                    Success = false,
                    HasConflict = true,
                    Conflict = conflict,
                    BlueprintTitle = localBlueprint.Title,
                    Error = "Version conflict detected - server has newer changes"
                };
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Blueprint doesn't exist on server, no conflict
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check for conflict on blueprint {Id}", item.BlueprintId);
            // Continue with sync attempt
        }

        return null;
    }

    private async Task<bool> IsServerAvailableAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.GetAsync($"{API_BASE}/health", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task LoadQueueAsync()
    {
        try
        {
            var json = await _localStorage.GetItemAsStringAsync(QUEUE_KEY);
            if (!string.IsNullOrEmpty(json))
            {
                _queue = JsonSerializer.Deserialize<List<SyncQueueItem>>(json, JsonOptions) ?? [];
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load sync queue");
            _queue = [];
        }
    }

    private async Task SaveQueueAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_queue, JsonOptions);
            await _localStorage.SetItemAsStringAsync(QUEUE_KEY, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save sync queue");
        }
    }
}
