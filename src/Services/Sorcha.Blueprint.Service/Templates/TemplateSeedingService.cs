// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using System.Text.Json.Nodes;
using Sorcha.Blueprint.Models;

namespace Sorcha.Blueprint.Service.Templates;

/// <summary>
/// Hosted service that seeds built-in blueprint templates at startup.
/// Reads template JSON files from the examples/templates directory and saves them
/// via IBlueprintTemplateService. Idempotent — skips templates that already exist.
/// </summary>
public class TemplateSeedingService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TemplateSeedingService> _logger;

    // Well-known template files shipped with the installation
    private static readonly string[] BuiltInTemplateFiles =
    [
        "ping-pong-template.json",
        "approval-workflow-template.json",
        "loan-application-template.json",
        "supply-chain-order-template.json"
    ];

    public TemplateSeedingService(
        IServiceScopeFactory scopeFactory,
        ILogger<TemplateSeedingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Template seeding service starting — checking for built-in templates");

        try
        {
            var result = await SeedTemplatesAsync(cancellationToken);
            _logger.LogInformation(
                "Template seeding complete: {Seeded} seeded, {Skipped} skipped, {Errors} errors",
                result.Seeded, result.Skipped, result.Errors);
        }
        catch (Exception ex)
        {
            // Don't block startup — log error and continue
            _logger.LogError(ex, "Template seeding failed — templates can be loaded manually via POST /api/templates/seed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Seeds all built-in templates. Returns counts for seeded, skipped, and error'd templates.
    /// </summary>
    public async Task<SeedResult> SeedTemplatesAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var templateService = scope.ServiceProvider.GetRequiredService<IBlueprintTemplateService>();

        var seeded = 0;
        var skipped = 0;
        var errors = new List<string>();

        var templatesDir = FindTemplatesDirectory();
        if (templatesDir == null)
        {
            _logger.LogWarning("Templates directory not found — skipping seeding");
            return new SeedResult(0, 0, ["Templates directory not found"]);
        }

        foreach (var templateFile in BuiltInTemplateFiles)
        {
            try
            {
                var filePath = Path.Combine(templatesDir, templateFile);
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Template file not found: {FilePath}", filePath);
                    errors.Add($"File not found: {templateFile}");
                    continue;
                }

                var json = await File.ReadAllTextAsync(filePath, ct);
                var templateData = JsonSerializer.Deserialize<JsonElement>(json);

                var templateId = templateData.GetProperty("id").GetString()!;

                // Check if template already exists (idempotent)
                var existing = await templateService.GetTemplateAsync(templateId, ct);
                if (existing != null)
                {
                    _logger.LogDebug("Template {TemplateId} already exists — skipping", templateId);
                    skipped++;
                    continue;
                }

                // Build BlueprintTemplate from JSON
                var template = new BlueprintTemplate
                {
                    Id = templateId,
                    Title = templateData.GetProperty("title").GetString() ?? templateFile,
                    Description = templateData.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                    Version = templateData.TryGetProperty("version", out var ver) ? ver.GetInt32() : 1,
                    Category = templateData.TryGetProperty("category", out var cat) ? cat.GetString() : null,
                    Tags = templateData.TryGetProperty("tags", out var tags)
                        ? tags.EnumerateArray().Select(t => t.GetString()!).ToList()
                        : null,
                    Author = templateData.TryGetProperty("author", out var author) ? author.GetString() : null,
                    Template = JsonNode.Parse(templateData.GetProperty("template").GetRawText())!,
                    ParameterSchema = templateData.TryGetProperty("parameterSchema", out var schema) && schema.ValueKind != JsonValueKind.Null
                        ? JsonDocument.Parse(schema.GetRawText())
                        : null,
                    DefaultParameters = templateData.TryGetProperty("defaultParameters", out var defaults) && defaults.ValueKind != JsonValueKind.Null
                        ? JsonSerializer.Deserialize<Dictionary<string, object>>(defaults.GetRawText())
                        : null,
                    Examples = templateData.TryGetProperty("examples", out var examples) && examples.ValueKind != JsonValueKind.Null
                        ? JsonSerializer.Deserialize<List<TemplateExample>>(examples.GetRawText())
                        : null,
                    Published = templateData.TryGetProperty("published", out var pub) && pub.GetBoolean(),
                };

                await templateService.SaveTemplateAsync(template, ct);
                _logger.LogInformation("Seeded template: {TemplateId} ({TemplateTitle})", templateId, template.Title);
                seeded++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed template from {TemplateFile}", templateFile);
                errors.Add($"Error seeding {templateFile}: {ex.Message}");
            }
        }

        return new SeedResult(seeded, skipped, errors);
    }

    /// <summary>
    /// Finds the examples/templates directory relative to the application base.
    /// Searches upward from the base directory to find the repository root.
    /// </summary>
    private string? FindTemplatesDirectory()
    {
        // Try relative to app base (Docker: /app or development: bin/Debug/...)
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "templates"),
            Path.Combine(baseDir, "examples", "templates"),
            Path.Combine(baseDir, "..", "..", "..", "..", "..", "examples", "templates"),
            Path.Combine(baseDir, "..", "..", "..", "..", "..", "..", "examples", "templates"),
            // Docker deployment: copy templates to /app/templates
            "/app/templates",
        };

        foreach (var candidate in candidates)
        {
            var resolved = Path.GetFullPath(candidate);
            if (Directory.Exists(resolved))
            {
                _logger.LogDebug("Found templates directory: {Path}", resolved);
                return resolved;
            }
        }

        // Fallback: search upward from base directory for examples/templates
        var dir = new DirectoryInfo(baseDir);
        while (dir != null)
        {
            var templatesDir = Path.Combine(dir.FullName, "examples", "templates");
            if (Directory.Exists(templatesDir))
            {
                _logger.LogDebug("Found templates directory (upward search): {Path}", templatesDir);
                return templatesDir;
            }
            dir = dir.Parent;
        }

        return null;
    }
}

/// <summary>
/// Result of template seeding operation
/// </summary>
public record SeedResult(int Seeded, int Skipped, List<string> Errors);
