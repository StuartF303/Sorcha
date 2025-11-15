// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using Sorcha.Blueprint.Engine.Interfaces;
using Sorcha.Blueprint.Engine.Models;

namespace Sorcha.Blueprint.Engine.Implementation;

/// <summary>
/// JSON Schema validator implementing JSON Schema Draft 2020-12.
/// </summary>
/// <remarks>
/// This implementation uses JsonSchema.Net to provide comprehensive
/// validation against JSON Schema specifications.
/// 
/// Thread-safe and can be used concurrently.
/// </remarks>
public class SchemaValidator : ISchemaValidator
{
    /// <summary>
    /// Validate data against a JSON Schema.
    /// </summary>
    public async Task<ValidationResult> ValidateAsync(
        Dictionary<string, object> data,
        JsonNode schema,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(schema);

        try
        {
            // Convert data dictionary to JsonNode
            var dataJson = ConvertToJsonNode(data);

            // Parse the JSON Schema
            var jsonSchema = JsonSchema.FromText(schema.ToJsonString());

            // Perform validation
            var validationResults = jsonSchema.Evaluate(dataJson, new EvaluationOptions
            {
                OutputFormat = OutputFormat.List,
                RequireFormatValidation = true
            });

            // Check if validation succeeded
            if (validationResults.IsValid)
            {
                return ValidationResult.Valid();
            }

            // Convert validation errors to our model
            var errors = ConvertErrors(validationResults);

            return ValidationResult.Invalid(errors);
        }
        catch (Exception ex)
        {
            // If schema parsing or validation throws, create an error
            return ValidationResult.Invalid(
                ValidationError.Create(
                    "",
                    $"Schema validation error: {ex.Message}",
                    null,
                    "schema",
                    new Dictionary<string, object>
                    {
                        ["exceptionType"] = ex.GetType().Name,
                        ["exceptionMessage"] = ex.Message
                    }
                )
            );
        }
    }

    /// <summary>
    /// Converts a dictionary to JsonNode for validation.
    /// </summary>
    private static JsonNode ConvertToJsonNode(Dictionary<string, object> data)
    {
        // Serialize dictionary to JSON string and parse back to JsonNode
        var json = JsonSerializer.Serialize(data);
        return JsonNode.Parse(json) ?? JsonValue.Create(new { });
    }

    /// <summary>
    /// Converts JsonSchema.Net validation results to our ValidationError model.
    /// </summary>
    private static List<ValidationError> ConvertErrors(EvaluationResults results)
    {
        var errors = new List<ValidationError>();

        // JsonSchema.Net returns nested results, we need to flatten them
        CollectErrors(results, errors);

        // If no errors were collected but validation failed, add a generic error
        if (errors.Count == 0)
        {
            errors.Add(ValidationError.Create(
                "",
                "Validation failed but no specific errors were reported"
            ));
        }

        return errors;
    }

    /// <summary>
    /// Recursively collects validation errors from the evaluation results.
    /// </summary>
    private static void CollectErrors(EvaluationResults results, List<ValidationError> errors)
    {
        // If this result has errors, add them
        if (!results.IsValid && results.Errors != null)
        {
            foreach (var (key, value) in results.Errors)
            {
                var error = ValidationError.Create(
                    instanceLocation: results.InstanceLocation.ToString(),
                    message: value,
                    schemaLocation: results.SchemaLocation.ToString(),
                    keyword: key
                );
                errors.Add(error);
            }
        }

        // Recursively process child results
        if (results.Details != null)
        {
            foreach (var detail in results.Details)
            {
                CollectErrors(detail, errors);
            }
        }
    }
}
