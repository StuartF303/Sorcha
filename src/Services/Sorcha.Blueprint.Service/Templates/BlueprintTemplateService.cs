// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Json.Schema;
using Sorcha.Blueprint.Engine.Interfaces;
using Sorcha.Blueprint.Models;
using Sorcha.Storage.Abstractions;
using ValidationResult = Sorcha.Blueprint.Engine.Interfaces.TemplateValidationResult;

namespace Sorcha.Blueprint.Service.Templates;

/// <summary>
/// Service for managing and evaluating blueprint templates
/// Uses in-memory storage (can be replaced with database implementation)
/// </summary>
public class BlueprintTemplateService : IBlueprintTemplateService
{
    private readonly IDocumentStore<BlueprintTemplate, string> _store;
    private readonly IJsonEEvaluator _jsonEEvaluator;
    private readonly ILogger<BlueprintTemplateService> _logger;

    public BlueprintTemplateService(
        IDocumentStore<BlueprintTemplate, string> store,
        IJsonEEvaluator jsonEEvaluator,
        ILogger<BlueprintTemplateService> logger)
    {
        _store = store;
        _jsonEEvaluator = jsonEEvaluator;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<BlueprintTemplate?> GetTemplateAsync(string templateId, CancellationToken ct = default)
    {
        return _store.GetAsync(templateId, ct);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BlueprintTemplate>> GetPublishedTemplatesAsync(CancellationToken ct = default)
    {
        return await _store.QueryAsync(t => t.Published, cancellationToken: ct);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BlueprintTemplate>> GetTemplatesByCategoryAsync(
        string category,
        CancellationToken ct = default)
    {
        var all = await _store.QueryAsync(t => t.Published, cancellationToken: ct);
        return all.Where(t => t.Category != null && t.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<BlueprintTemplate> SaveTemplateAsync(BlueprintTemplate template, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(template);

        template.UpdatedAt = DateTimeOffset.UtcNow;

        var existing = await _store.GetAsync(template.Id, ct);
        if (existing == null)
        {
            template.CreatedAt = DateTimeOffset.UtcNow;
        }

        var saved = await _store.UpsertAsync(template.Id, template, ct);

        _logger.LogInformation("Saved template {TemplateId}: {TemplateTitle}", template.Id, template.Title);

        return saved;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTemplateAsync(string templateId, CancellationToken ct = default)
    {
        var removed = await _store.DeleteAsync(templateId, ct);

        if (removed)
        {
            _logger.LogInformation("Deleted template {TemplateId}", templateId);
        }

        return removed;
    }

    /// <inheritdoc />
    public async Task<TemplateEvaluationResult> EvaluateTemplateAsync(
        TemplateEvaluationRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            // Get the template
            var template = await GetTemplateAsync(request.TemplateId, ct);
            if (template == null)
            {
                return new TemplateEvaluationResult
                {
                    Success = false,
                    Error = $"Template '{request.TemplateId}' not found",
                    TemplateId = request.TemplateId
                };
            }

            // Merge with default parameters
            var context = MergeParameters(template.DefaultParameters, request.Parameters);

            // Validate parameters if schema is defined
            if (template.ParameterSchema != null)
            {
                var validationResult = await ValidateParametersAsync(
                    template.Id,
                    context,
                    ct);

                if (!validationResult.IsValid)
                {
                    return new TemplateEvaluationResult
                    {
                        Success = false,
                        Error = "Parameter validation failed",
                        ValidationErrors = validationResult.Errors,
                        TemplateId = template.Id,
                        Parameters = context
                    };
                }
            }

            // Evaluate the template
            Sorcha.Blueprint.Models.Blueprint? blueprint;
            List<string>? trace = null;

            if (request.IncludeTrace)
            {
                var traceResult = await _jsonEEvaluator.EvaluateWithTraceAsync(
                    template.Template,
                    context,
                    ct);

                if (!traceResult.Success)
                {
                    return new TemplateEvaluationResult
                    {
                        Success = false,
                        Error = traceResult.Error,
                        TemplateId = template.Id,
                        Parameters = context
                    };
                }

                blueprint = traceResult.Result?.Deserialize<Sorcha.Blueprint.Models.Blueprint>();
                trace = traceResult.Steps.Select(s =>
                    $"Step {s.Step} ({s.Duration.TotalMilliseconds:F2}ms): {s.Description}").ToList();
            }
            else
            {
                blueprint = await _jsonEEvaluator.EvaluateAsync<Sorcha.Blueprint.Models.Blueprint>(
                    template.Template,
                    context,
                    ct);
            }

            if (blueprint == null)
            {
                return new TemplateEvaluationResult
                {
                    Success = false,
                    Error = "Template evaluation resulted in null blueprint",
                    TemplateId = template.Id,
                    Parameters = context,
                    Trace = trace
                };
            }

            // Validate the blueprint if requested
            if (request.Validate)
            {
                var blueprintValidation = ValidateBlueprint(blueprint);
                if (!blueprintValidation.IsValid)
                {
                    return new TemplateEvaluationResult
                    {
                        Success = false,
                        Error = "Generated blueprint validation failed",
                        ValidationErrors = blueprintValidation.Errors,
                        Blueprint = blueprint,
                        TemplateId = template.Id,
                        Parameters = context,
                        Trace = trace
                    };
                }
            }

            _logger.LogInformation(
                "Successfully evaluated template {TemplateId} to generate blueprint {BlueprintId}",
                template.Id,
                blueprint.Id);

            return new TemplateEvaluationResult
            {
                Success = true,
                Blueprint = blueprint,
                TemplateId = template.Id,
                Parameters = context,
                Trace = trace
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating template {TemplateId}", request.TemplateId);

            return new TemplateEvaluationResult
            {
                Success = false,
                Error = $"Template evaluation failed: {ex.Message}",
                TemplateId = request.TemplateId,
                Parameters = request.Parameters
            };
        }
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateParametersAsync(
        string templateId,
        Dictionary<string, object> parameters,
        CancellationToken ct = default)
    {
        var template = await GetTemplateAsync(templateId, ct);
        if (template == null)
        {
            return ValidationResult.Failure($"Template '{templateId}' not found");
        }

        if (template.ParameterSchema == null)
        {
            return ValidationResult.Success();
        }

        try
        {
            // Parse the parameter schema
            var schema = JsonSchema.FromText(template.ParameterSchema.RootElement.GetRawText());

            // Convert parameters to JSON for validation
            var parametersJson = JsonSerializer.Serialize(parameters);

            // Parse to JsonElement for schema validation
            using var jsonDocument = JsonDocument.Parse(parametersJson);
            var parametersElement = jsonDocument.RootElement;

            // Validate
            var validationResult = schema.Evaluate(parametersElement);

            if (validationResult.IsValid)
            {
                return ValidationResult.Success();
            }

            var errors = validationResult.Details?
                .Where(d => !d.IsValid)
                .Select(d => $"{d.InstanceLocation}: {d.Errors?.FirstOrDefault().Value ?? "Validation failed"}")
                .ToList() ?? new List<string> { "Validation failed" };

            return new ValidationResult
            {
                IsValid = false,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating parameters for template {TemplateId}", templateId);
            return ValidationResult.Failure($"Parameter validation error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task IncrementUsageAsync(string templateId, CancellationToken ct = default)
    {
        var template = await _store.GetAsync(templateId, ct);
        if (template is null)
            return;

        template.Metadata ??= new Dictionary<string, string>();

        var currentCount = 0;
        if (template.Metadata.TryGetValue("usageCount", out var countStr))
            int.TryParse(countStr, out currentCount);

        template.Metadata["usageCount"] = (currentCount + 1).ToString();
        template.UpdatedAt = DateTimeOffset.UtcNow;

        await _store.UpsertAsync(template.Id, template, ct);

        _logger.LogInformation("Incremented usage count for template {TemplateId} to {Count}", templateId, currentCount + 1);
    }

    /// <inheritdoc />
    public async Task<TemplateEvaluationResult> EvaluateExampleAsync(
        string templateId,
        string exampleName,
        CancellationToken ct = default)
    {
        var template = await GetTemplateAsync(templateId, ct);
        if (template == null)
        {
            return new TemplateEvaluationResult
            {
                Success = false,
                Error = $"Template '{templateId}' not found",
                TemplateId = templateId
            };
        }

        var example = template.Examples?.FirstOrDefault(e =>
            e.Name.Equals(exampleName, StringComparison.OrdinalIgnoreCase));

        if (example == null)
        {
            return new TemplateEvaluationResult
            {
                Success = false,
                Error = $"Example '{exampleName}' not found in template '{templateId}'",
                TemplateId = templateId
            };
        }

        var request = new TemplateEvaluationRequest
        {
            TemplateId = templateId,
            Parameters = example.Parameters,
            Validate = true,
            IncludeTrace = false
        };

        return await EvaluateTemplateAsync(request, ct);
    }

    /// <summary>
    /// Merge default parameters with provided parameters
    /// </summary>
    private Dictionary<string, object> MergeParameters(
        Dictionary<string, object>? defaults,
        Dictionary<string, object> provided)
    {
        var merged = new Dictionary<string, object>();

        if (defaults != null)
        {
            foreach (var kvp in defaults)
            {
                merged[kvp.Key] = kvp.Value;
            }
        }

        foreach (var kvp in provided)
        {
            merged[kvp.Key] = kvp.Value;
        }

        return merged;
    }

    /// <summary>
    /// Validate a generated blueprint using data annotations
    /// </summary>
    private ValidationResult ValidateBlueprint(Sorcha.Blueprint.Models.Blueprint blueprint)
    {
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(blueprint);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        bool isValid = Validator.TryValidateObject(
            blueprint,
            validationContext,
            validationResults,
            validateAllProperties: true);

        if (isValid)
        {
            return ValidationResult.Success();
        }

        var errors = validationResults
            .Select(vr => vr.ErrorMessage ?? "Validation error")
            .ToList();

        return new ValidationResult
        {
            IsValid = false,
            Errors = errors
        };
    }
}
