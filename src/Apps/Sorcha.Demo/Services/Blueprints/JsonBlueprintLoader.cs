// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Logging;
using System.Text.Json;
using Sorcha.Blueprint.Models;

namespace Sorcha.Demo.Services.Blueprints;

/// <summary>
/// Loads blueprint definitions from JSON files
/// </summary>
public class JsonBlueprintLoader
{
    private readonly ILogger<JsonBlueprintLoader> _logger;
    private readonly string _blueprintsPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonBlueprintLoader(ILogger<JsonBlueprintLoader> logger)
    {
        _logger = logger;

        // Blueprints are in Examples/Blueprints relative to application base directory
        _blueprintsPath = Path.Combine(AppContext.BaseDirectory, "Examples", "Blueprints");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        _logger.LogInformation("Blueprint loader initialized. Path: {Path}", _blueprintsPath);

        // Ensure blueprints directory exists
        if (!Directory.Exists(_blueprintsPath))
        {
            _logger.LogWarning("Blueprints directory does not exist: {Path}", _blueprintsPath);
        }
    }

    /// <summary>
    /// Loads a blueprint from a JSON file (returns raw JSON template)
    /// </summary>
    public async Task<string> LoadBlueprintTemplateAsync(string fileName)
    {
        var filePath = Path.Combine(_blueprintsPath, fileName);

        if (!File.Exists(filePath))
        {
            var errorMessage = $"Blueprint file not found: {filePath}";
            _logger.LogError(errorMessage);
            throw new FileNotFoundException(errorMessage);
        }

        try
        {
            var jsonContent = await File.ReadAllTextAsync(filePath);
            _logger.LogInformation("Loaded blueprint template from {FileName} ({Size} bytes)",
                fileName, jsonContent.Length);

            return jsonContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read blueprint file: {FileName}", fileName);
            throw;
        }
    }

    /// <summary>
    /// Loads a blueprint from a custom file path
    /// </summary>
    public async Task<string> LoadBlueprintTemplateFromPathAsync(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Blueprint file not found: {fullPath}");
        }

        try
        {
            var jsonContent = await File.ReadAllTextAsync(fullPath);
            _logger.LogInformation("Loaded blueprint template from custom path ({Size} bytes)", jsonContent.Length);
            return jsonContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read blueprint file from path: {Path}", fullPath);
            throw;
        }
    }

    /// <summary>
    /// Parses a JSON string into a Blueprint object
    /// </summary>
    public Sorcha.Blueprint.Models.Blueprint ParseBlueprint(string json)
    {
        try
        {
            var blueprint = JsonSerializer.Deserialize<Sorcha.Blueprint.Models.Blueprint>(json, _jsonOptions);

            if (blueprint == null)
            {
                throw new InvalidOperationException("Failed to deserialize blueprint - result was null");
            }

            _logger.LogInformation("Parsed blueprint: {Title} (ID: {Id}, {ParticipantCount} participants, {ActionCount} actions)",
                blueprint.Title,
                blueprint.Id,
                blueprint.Participants?.Count ?? 0,
                blueprint.Actions?.Count ?? 0);

            return blueprint;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse blueprint JSON. Invalid JSON structure.");
            throw;
        }
    }

    /// <summary>
    /// Checks if a blueprint file exists
    /// </summary>
    public bool BlueprintExists(string fileName)
    {
        var filePath = Path.Combine(_blueprintsPath, fileName);
        return File.Exists(filePath);
    }

    /// <summary>
    /// Lists all available blueprint JSON files
    /// </summary>
    public string[] ListAvailableBlueprints()
    {
        if (!Directory.Exists(_blueprintsPath))
        {
            return Array.Empty<string>();
        }

        return Directory.GetFiles(_blueprintsPath, "*.json")
            .Select(Path.GetFileName)
            .Where(f => f != null)
            .ToArray()!;
    }

    /// <summary>
    /// Gets the full path to the blueprints directory
    /// </summary>
    public string GetBlueprintsPath() => _blueprintsPath;
}
