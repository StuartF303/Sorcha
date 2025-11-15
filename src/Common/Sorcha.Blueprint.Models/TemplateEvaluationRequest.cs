// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Serialization;
using DataAnnotations = System.ComponentModel.DataAnnotations;

namespace Sorcha.Blueprint.Models;

/// <summary>
/// Request to evaluate a blueprint template with specific parameters
/// </summary>
public class TemplateEvaluationRequest
{
    /// <summary>
    /// The template ID to evaluate
    /// </summary>
    [DataAnnotations.Required]
    [JsonPropertyName("templateId")]
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>
    /// Parameters (context) to pass to the template
    /// </summary>
    [DataAnnotations.Required]
    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Whether to validate the resulting blueprint after evaluation
    /// </summary>
    [JsonPropertyName("validate")]
    public bool Validate { get; set; } = true;

    /// <summary>
    /// Whether to return the evaluation trace for debugging
    /// </summary>
    [JsonPropertyName("includeTrace")]
    public bool IncludeTrace { get; set; } = false;
}

/// <summary>
/// Result of template evaluation
/// </summary>
public class TemplateEvaluationResult
{
    /// <summary>
    /// The evaluated blueprint
    /// </summary>
    [JsonPropertyName("blueprint")]
    public Blueprint? Blueprint { get; set; }

    /// <summary>
    /// Whether the evaluation was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Error message if evaluation failed
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Validation errors if blueprint validation failed
    /// </summary>
    [JsonPropertyName("validationErrors")]
    public List<string>? ValidationErrors { get; set; }

    /// <summary>
    /// Evaluation trace for debugging (if requested)
    /// </summary>
    [JsonPropertyName("trace")]
    public List<string>? Trace { get; set; }

    /// <summary>
    /// Template ID that was evaluated
    /// </summary>
    [JsonPropertyName("templateId")]
    public string? TemplateId { get; set; }

    /// <summary>
    /// Parameters used for evaluation
    /// </summary>
    [JsonPropertyName("parameters")]
    public Dictionary<string, object>? Parameters { get; set; }
}
