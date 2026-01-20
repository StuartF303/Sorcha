// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using System.Text.Json.Serialization;
using Sorcha.Admin.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Sorcha.Admin.Services;

/// <summary>
/// Service for serializing and deserializing blueprints to/from JSON and YAML formats.
/// </summary>
public class BlueprintSerializationService
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ISerializer _yamlSerializer;
    private readonly IDeserializer _yamlDeserializer;

    public BlueprintSerializationService()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Serializes a blueprint to JSON format.
    /// </summary>
    public string ToJson(Blueprint.Models.Blueprint blueprint)
    {
        var exportModel = new BlueprintExportModel
        {
            Blueprint = blueprint,
            ExportedAt = DateTimeOffset.UtcNow
        };

        return JsonSerializer.Serialize(exportModel, _jsonOptions);
    }

    /// <summary>
    /// Serializes a blueprint to YAML format.
    /// </summary>
    public string ToYaml(Blueprint.Models.Blueprint blueprint)
    {
        var exportModel = new BlueprintExportModel
        {
            Blueprint = blueprint,
            ExportedAt = DateTimeOffset.UtcNow
        };

        return _yamlSerializer.Serialize(exportModel);
    }

    /// <summary>
    /// Validates and parses a blueprint from file content.
    /// Automatically detects JSON or YAML format.
    /// </summary>
    public ImportValidationResult ValidateAndParse(string content, string fileName)
    {
        var isYaml = fileName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                     fileName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase);

        try
        {
            BlueprintExportModel? exportModel;

            if (isYaml)
            {
                exportModel = _yamlDeserializer.Deserialize<BlueprintExportModel>(content);
            }
            else
            {
                exportModel = JsonSerializer.Deserialize<BlueprintExportModel>(content, _jsonOptions);
            }

            if (exportModel?.Blueprint is null)
            {
                return ImportValidationResult.Failure(new ImportValidationError(
                    "/",
                    "File does not contain a valid blueprint",
                    ImportErrorType.InvalidFormat));
            }

            var errors = new List<ImportValidationError>();
            var warnings = new List<ImportValidationWarning>();

            // Validate required fields
            if (string.IsNullOrWhiteSpace(exportModel.Blueprint.Title))
            {
                errors.Add(new ImportValidationError(
                    "/blueprint/title",
                    "Blueprint title is required",
                    ImportErrorType.MissingRequiredField));
            }

            if (string.IsNullOrWhiteSpace(exportModel.Blueprint.Id))
            {
                // Auto-generate ID if missing
                exportModel.Blueprint.Id = Guid.NewGuid().ToString();
                warnings.Add(new ImportValidationWarning(
                    "/blueprint/id",
                    "Blueprint ID was missing and has been auto-generated"));
            }

            // Validate participant references in actions
            var participantIds = exportModel.Blueprint.Participants?.Select(p => p.Id).ToHashSet() ?? [];
            if (exportModel.Blueprint.Actions is not null)
            {
                foreach (var action in exportModel.Blueprint.Actions)
                {
                    if (!string.IsNullOrEmpty(action.ParticipantId) && !participantIds.Contains(action.ParticipantId))
                    {
                        warnings.Add(new ImportValidationWarning(
                            $"/blueprint/actions/{action.Id}/participantId",
                            $"Action '{action.Title}' references non-existent participant '{action.ParticipantId}'"));
                    }
                }
            }

            // Check format version
            if (!string.IsNullOrEmpty(exportModel.FormatVersion) &&
                !exportModel.FormatVersion.StartsWith("1."))
            {
                warnings.Add(new ImportValidationWarning(
                    "/formatVersion",
                    $"Blueprint was exported with format version {exportModel.FormatVersion}, current version is 1.0"));
            }

            if (errors.Count > 0)
            {
                return new ImportValidationResult
                {
                    IsValid = false,
                    Errors = errors,
                    Warnings = warnings
                };
            }

            return new ImportValidationResult
            {
                IsValid = true,
                Blueprint = exportModel.Blueprint,
                Warnings = warnings
            };
        }
        catch (JsonException ex)
        {
            return ImportValidationResult.Failure(new ImportValidationError(
                ex.Path ?? "/",
                $"Invalid JSON: {ex.Message}",
                ImportErrorType.InvalidFormat));
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            return ImportValidationResult.Failure(new ImportValidationError(
                $"Line {ex.Start.Line}",
                $"Invalid YAML: {ex.Message}",
                ImportErrorType.InvalidFormat));
        }
        catch (Exception ex)
        {
            return ImportValidationResult.Failure(new ImportValidationError(
                "/",
                $"Failed to parse file: {ex.Message}",
                ImportErrorType.InvalidFormat));
        }
    }

    /// <summary>
    /// Gets the appropriate file extension for a format.
    /// </summary>
    public static string GetFileExtension(ExportFormat format) => format switch
    {
        ExportFormat.Json => ".json",
        ExportFormat.Yaml => ".yaml",
        _ => ".json"
    };

    /// <summary>
    /// Gets the appropriate MIME type for a format.
    /// </summary>
    public static string GetMimeType(ExportFormat format) => format switch
    {
        ExportFormat.Json => "application/json",
        ExportFormat.Yaml => "text/yaml",
        _ => "application/octet-stream"
    };
}

/// <summary>
/// Supported export formats.
/// </summary>
public enum ExportFormat
{
    Json,
    Yaml
}
