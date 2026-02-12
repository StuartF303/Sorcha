// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Blueprint.Models;
using ValidationResult = Sorcha.Blueprint.Engine.Interfaces.TemplateValidationResult;

namespace Sorcha.Blueprint.Service.Templates;

/// <summary>
/// Service for managing and evaluating blueprint templates
/// </summary>
public interface IBlueprintTemplateService
{
    /// <summary>
    /// Get a template by ID
    /// </summary>
    Task<BlueprintTemplate?> GetTemplateAsync(string templateId, CancellationToken ct = default);

    /// <summary>
    /// Get all published templates
    /// </summary>
    Task<IEnumerable<BlueprintTemplate>> GetPublishedTemplatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Get templates by category
    /// </summary>
    Task<IEnumerable<BlueprintTemplate>> GetTemplatesByCategoryAsync(
        string category,
        CancellationToken ct = default);

    /// <summary>
    /// Create or update a template
    /// </summary>
    Task<BlueprintTemplate> SaveTemplateAsync(BlueprintTemplate template, CancellationToken ct = default);

    /// <summary>
    /// Delete a template
    /// </summary>
    Task<bool> DeleteTemplateAsync(string templateId, CancellationToken ct = default);

    /// <summary>
    /// Evaluate a template with parameters to generate a blueprint
    /// </summary>
    Task<TemplateEvaluationResult> EvaluateTemplateAsync(
        TemplateEvaluationRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Validate template parameters against the template's parameter schema
    /// </summary>
    Task<ValidationResult> ValidateParametersAsync(
        string templateId,
        Dictionary<string, object> parameters,
        CancellationToken ct = default);

    /// <summary>
    /// Increment the usage count for a template
    /// </summary>
    Task IncrementUsageAsync(string templateId, CancellationToken ct = default);

    /// <summary>
    /// Evaluate a template example
    /// </summary>
    Task<TemplateEvaluationResult> EvaluateExampleAsync(
        string templateId,
        string exampleName,
        CancellationToken ct = default);
}
