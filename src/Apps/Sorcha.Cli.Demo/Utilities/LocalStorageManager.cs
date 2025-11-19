using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Sorcha.Cli.Demo.Utilities;

/// <summary>
/// Manages local file storage for demo wallet data (INSECURE - demo only!)
/// WARNING: This stores mnemonics in plain text. Never use in production!
/// </summary>
public class LocalStorageManager
{
    private readonly ILogger<LocalStorageManager> _logger;
    private readonly string _storagePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public LocalStorageManager(ILogger<LocalStorageManager> logger)
    {
        _logger = logger;
        _storagePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".sorcha-demo");

        // Ensure storage directory exists
        Directory.CreateDirectory(_storagePath);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _logger.LogInformation("Local storage initialized at: {Path}", _storagePath);
    }

    /// <summary>
    /// Gets the full path to the wallets JSON file
    /// </summary>
    public string WalletsFilePath => Path.Combine(_storagePath, "wallets.json");

    /// <summary>
    /// Loads all wallet data from local storage
    /// </summary>
    public async Task<WalletStorage> LoadWalletsAsync()
    {
        if (!File.Exists(WalletsFilePath))
        {
            _logger.LogInformation("No existing wallet file found, returning empty storage");
            return new WalletStorage();
        }

        try
        {
            var json = await File.ReadAllTextAsync(WalletsFilePath);
            var storage = JsonSerializer.Deserialize<WalletStorage>(json, _jsonOptions);
            _logger.LogInformation("Loaded {Count} wallets from storage", storage?.Wallets.Count ?? 0);
            return storage ?? new WalletStorage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load wallets from storage");
            return new WalletStorage();
        }
    }

    /// <summary>
    /// Saves wallet data to local storage
    /// </summary>
    public async Task SaveWalletsAsync(WalletStorage storage)
    {
        try
        {
            var json = JsonSerializer.Serialize(storage, _jsonOptions);
            await File.WriteAllTextAsync(WalletsFilePath, json);
            _logger.LogInformation("Saved {Count} wallets to storage at {Path}",
                storage.Wallets.Count, WalletsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save wallets to storage");
            throw;
        }
    }

    /// <summary>
    /// Clears all wallet data (deletes the file)
    /// </summary>
    public async Task ClearWalletsAsync()
    {
        try
        {
            if (File.Exists(WalletsFilePath))
            {
                File.Delete(WalletsFilePath);
                _logger.LogWarning("Cleared all wallet data from storage");
            }
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear wallet storage");
            throw;
        }
    }

    /// <summary>
    /// Checks if wallet storage exists
    /// </summary>
    public bool WalletStorageExists() => File.Exists(WalletsFilePath);
}

/// <summary>
/// Container for wallet storage data
/// </summary>
public class WalletStorage
{
    [JsonPropertyName("wallets")]
    public Dictionary<string, StoredWallet> Wallets { get; set; } = new();

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("lastModified")]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Stored wallet information (INSECURE - contains plain text mnemonic!)
/// </summary>
public class StoredWallet
{
    [JsonPropertyName("participantName")]
    public string ParticipantName { get; set; } = string.Empty;

    [JsonPropertyName("walletAddress")]
    public string WalletAddress { get; set; } = string.Empty;

    [JsonPropertyName("mnemonic")]
    public string Mnemonic { get; set; } = string.Empty; // WARNING: INSECURE!

    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = "ED25519";

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
