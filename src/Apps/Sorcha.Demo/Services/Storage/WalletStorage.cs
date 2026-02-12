// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.Logging;
using Sorcha.Demo.Models;
using System.Text.Json;

namespace Sorcha.Demo.Services.Storage;

/// <summary>
/// Persists and loads participant wallet data to/from local storage
/// WARNING: Stores mnemonics in plain text - FOR DEMO USE ONLY!
/// </summary>
public class WalletStorage
{
    private readonly ILogger<WalletStorage> _logger;
    private readonly string _storagePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public WalletStorage(ILogger<WalletStorage> logger, string storagePath)
    {
        _logger = logger;
        _storagePath = ExpandPath(storagePath);
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Ensure directory exists
        var directory = Path.GetDirectoryName(_storagePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.LogInformation("Created wallet storage directory: {Directory}", directory);
        }
    }

    /// <summary>
    /// Saves participant wallets to storage
    /// </summary>
    public async Task SaveWalletsAsync(Dictionary<string, ParticipantContext> participants)
    {
        try
        {
            var json = JsonSerializer.Serialize(participants, _jsonOptions);
            await File.WriteAllTextAsync(_storagePath, json);
            _logger.LogInformation("Saved {Count} participant wallets to {Path}",
                participants.Count, _storagePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save wallets to {Path}", _storagePath);
            throw;
        }
    }

    /// <summary>
    /// Loads participant wallets from storage
    /// </summary>
    public async Task<Dictionary<string, ParticipantContext>?> LoadWalletsAsync()
    {
        if (!File.Exists(_storagePath))
        {
            _logger.LogInformation("No wallet storage file found at {Path}", _storagePath);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_storagePath);
            var participants = JsonSerializer.Deserialize<Dictionary<string, ParticipantContext>>(json, _jsonOptions);
            _logger.LogInformation("Loaded {Count} participant wallets from {Path}",
                participants?.Count ?? 0, _storagePath);
            return participants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load wallets from {Path}", _storagePath);
            return null;
        }
    }

    /// <summary>
    /// Checks if wallet storage file exists
    /// </summary>
    public bool WalletsExist()
    {
        return File.Exists(_storagePath);
    }

    /// <summary>
    /// Deletes the wallet storage file
    /// </summary>
    public void ClearWallets()
    {
        if (File.Exists(_storagePath))
        {
            File.Delete(_storagePath);
            _logger.LogInformation("Deleted wallet storage file: {Path}", _storagePath);
        }
    }

    /// <summary>
    /// Expands ~ to user home directory
    /// </summary>
    private static string ExpandPath(string path)
    {
        if (path.StartsWith("~/"))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, path[2..]);
        }
        return path;
    }
}
