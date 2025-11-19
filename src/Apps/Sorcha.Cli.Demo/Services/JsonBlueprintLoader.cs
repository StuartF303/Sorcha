using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sorcha.Cli.Demo.Services;

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

        // Blueprints directory is in the application base directory
        _blueprintsPath = Path.Combine(AppContext.BaseDirectory, "Blueprints");

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
    /// Loads a blueprint from a JSON file (returns raw JSON template, not yet processed)
    /// </summary>
    /// <param name="fileName">JSON file name (e.g., "expense-approval.json")</param>
    /// <returns>Raw JSON string with JSON-e templates</returns>
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
    /// Parses a JSON string into a Blueprint object
    /// </summary>
    /// <param name="json">Processed JSON string (after JSON-e evaluation)</param>
    /// <returns>Deserialized Blueprint object</returns>
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error parsing blueprint");
            throw;
        }
    }

    /// <summary>
    /// Gets the full path to the blueprints directory
    /// </summary>
    public string GetBlueprintsPath() => _blueprintsPath;

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
}
