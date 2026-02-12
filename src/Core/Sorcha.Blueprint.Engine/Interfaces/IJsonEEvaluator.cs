// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json.Nodes;

namespace Sorcha.Blueprint.Engine.Interfaces;

/// <summary>
/// JSON-e template evaluator for dynamic blueprint generation.
/// </summary>
/// <remarks>
/// Evaluates JSON-e templates as defined at https://json-e.js.org
///
/// JSON-e provides:
/// - Variable substitution: $eval
/// - Conditional rendering: $if/$then/$else
/// - Iteration: $map, $flattenDeep
/// - Expression evaluation
/// - Nested template composition
/// </remarks>
public interface IJsonEEvaluator
{
    /// <summary>
    /// Evaluate a JSON-e template with the given context.
    /// </summary>
    /// <param name="template">The JSON-e template to evaluate.</param>
    /// <param name="context">The context data for template variables.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The evaluated JSON result.</returns>
    /// <exception cref="ArgumentNullException">If template is null.</exception>
    /// <exception cref="InvalidOperationException">If the template is malformed or evaluation fails.</exception>
    Task<JsonNode> EvaluateAsync(
        JsonNode template,
        Dictionary<string, object> context,
        CancellationToken ct = default);

    /// <summary>
    /// Evaluate a JSON-e template and deserialize to a specific type.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize to.</typeparam>
    /// <param name="template">The JSON-e template to evaluate.</param>
    /// <param name="context">The context data for template variables.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The evaluated and deserialized result.</returns>
    Task<T?> EvaluateAsync<T>(
        JsonNode template,
        Dictionary<string, object> context,
        CancellationToken ct = default) where T : class;

    /// <summary>
    /// Validate a JSON-e template for syntax errors.
    /// </summary>
    /// <param name="template">The template to validate.</param>
    /// <returns>Validation result with any errors found.</returns>
    Task<TemplateValidationResult> ValidateTemplateAsync(JsonNode template);

    /// <summary>
    /// Evaluate with detailed trace information for debugging.
    /// </summary>
    /// <param name="template">The JSON-e template to evaluate.</param>
    /// <param name="context">The context data for template variables.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Evaluation result with trace information.</returns>
    Task<EvaluationTrace> EvaluateWithTraceAsync(
        JsonNode template,
        Dictionary<string, object> context,
        CancellationToken ct = default);
}

/// <summary>
/// Result of template validation
/// </summary>
public class TemplateValidationResult
{
    /// <summary>
    /// Whether the template is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Validation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    public static TemplateValidationResult Success() => new() { IsValid = true };

    public static TemplateValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };
}

/// <summary>
/// Evaluation trace for debugging
/// </summary>
public class EvaluationTrace
{
    /// <summary>
    /// The evaluation result
    /// </summary>
    public JsonNode? Result { get; set; }

    /// <summary>
    /// Step-by-step evaluation trace
    /// </summary>
    public List<TraceStep> Steps { get; set; } = new();

    /// <summary>
    /// Total evaluation time
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Whether evaluation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if evaluation failed
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// A single step in the evaluation trace
/// </summary>
public class TraceStep
{
    /// <summary>
    /// Step number
    /// </summary>
    public int Step { get; set; }

    /// <summary>
    /// Description of what was evaluated
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Input to this step
    /// </summary>
    public string? Input { get; set; }

    /// <summary>
    /// Output from this step
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// Time taken for this step
    /// </summary>
    public TimeSpan Duration { get; set; }
}
