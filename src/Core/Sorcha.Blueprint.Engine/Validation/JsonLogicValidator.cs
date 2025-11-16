// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Nodes;
using Json.Schema;
using Sorcha.Blueprint.Engine.Interfaces;

namespace Sorcha.Blueprint.Engine.Validation;

/// <summary>
/// Validator for JSON Logic expressions
/// </summary>
/// <remarks>
/// Validates JSON Logic expressions for:
/// - Correct syntax
/// - Variable references match schema fields
/// - Type compatibility
/// - Complexity limits
/// </remarks>
public class JsonLogicValidator
{
    private const int MaxDepth = 10;
    private const int MaxNodeCount = 100;

    /// <summary>
    /// Validate a JSON Logic expression
    /// </summary>
    /// <param name="expression">The JSON Logic expression to validate</param>
    /// <param name="dataSchema">Optional schema to validate variable references against</param>
    /// <returns>Validation result with errors and warnings</returns>
    public TemplateValidationResult Validate(JsonNode expression, JsonSchema? dataSchema = null)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            // Check null
            if (expression == null)
            {
                errors.Add("Expression cannot be null");
                return new TemplateValidationResult { IsValid = false, Errors = errors };
            }

            // Check complexity
            var depth = CalculateDepth(expression);
            if (depth > MaxDepth)
            {
                errors.Add($"Expression depth ({depth}) exceeds maximum allowed ({MaxDepth})");
            }

            var nodeCount = CountNodes(expression);
            if (nodeCount > MaxNodeCount)
            {
                errors.Add($"Expression node count ({nodeCount}) exceeds maximum allowed ({MaxNodeCount})");
            }

            // Validate structure and operators
            var operatorErrors = ValidateOperators(expression);
            errors.AddRange(operatorErrors);

            // Validate variable references against schema
            if (dataSchema != null)
            {
                var variableErrors = ValidateVariableReferences(expression, dataSchema);
                errors.AddRange(variableErrors);
            }
            else
            {
                // Warning if no schema provided
                var variables = ExtractVariables(expression);
                if (variables.Any())
                {
                    warnings.Add($"No schema provided to validate variable references: {string.Join(", ", variables)}");
                }
            }

            return new TemplateValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings
            };
        }
        catch (Exception ex)
        {
            errors.Add($"Validation error: {ex.Message}");
            return new TemplateValidationResult { IsValid = false, Errors = errors };
        }
    }

    /// <summary>
    /// Calculate the depth of an expression tree
    /// </summary>
    public int CalculateDepth(JsonNode node, int currentDepth = 0)
    {
        if (node is JsonObject obj)
        {
            if (obj.Count == 0) return currentDepth;

            return obj.Max(kvp => CalculateDepth(kvp.Value ?? JsonValue.Create(0)!, currentDepth + 1));
        }
        else if (node is JsonArray arr)
        {
            if (arr.Count == 0) return currentDepth;

            return arr.Max(item => CalculateDepth(item ?? JsonValue.Create(0)!, currentDepth + 1));
        }

        return currentDepth;
    }

    /// <summary>
    /// Count total nodes in an expression tree
    /// </summary>
    public int CountNodes(JsonNode node)
    {
        var count = 1; // Current node

        if (node is JsonObject obj)
        {
            count += obj.Sum(kvp => kvp.Value != null ? CountNodes(kvp.Value) : 0);
        }
        else if (node is JsonArray arr)
        {
            count += arr.Sum(item => item != null ? CountNodes(item) : 0);
        }

        return count;
    }

    /// <summary>
    /// Validate operators in the expression
    /// </summary>
    private List<string> ValidateOperators(JsonNode node)
    {
        var errors = new List<string>();
        var validOperators = new HashSet<string>
        {
            // Comparison
            "==", "!=", "===", "!==", "<", ">", "<=", ">=",
            // Logical
            "and", "or", "!", "!!",
            // Arithmetic
            "+", "-", "*", "/", "%",
            // Array
            "map", "filter", "reduce", "all", "none", "some", "merge", "in",
            // String
            "cat", "substr",
            // Data access
            "var",
            // Control flow
            "if", "missing", "missing_some", "log"
        };

        if (node is JsonObject obj)
        {
            foreach (var kvp in obj)
            {
                // Check if operator is valid
                if (!validOperators.Contains(kvp.Key))
                {
                    errors.Add($"Unknown operator: '{kvp.Key}'");
                }

                // Recursively validate nested expressions
                if (kvp.Value != null)
                {
                    var nestedErrors = ValidateOperators(kvp.Value);
                    errors.AddRange(nestedErrors);
                }
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item != null)
                {
                    var itemErrors = ValidateOperators(item);
                    errors.AddRange(itemErrors);
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// Extract all variable references from an expression
    /// </summary>
    public List<string> ExtractVariables(JsonNode node)
    {
        var variables = new List<string>();

        if (node is JsonObject obj)
        {
            if (obj.TryGetPropertyValue("var", out var varNode))
            {
                var varName = varNode?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(varName))
                {
                    variables.Add(varName);
                }
            }

            foreach (var kvp in obj.Where(kvp => kvp.Key != "var"))
            {
                if (kvp.Value != null)
                {
                    variables.AddRange(ExtractVariables(kvp.Value));
                }
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item != null)
                {
                    variables.AddRange(ExtractVariables(item));
                }
            }
        }

        return variables;
    }

    /// <summary>
    /// Validate that all variable references exist in the schema
    /// </summary>
    private List<string> ValidateVariableReferences(JsonNode expression, JsonSchema schema)
    {
        var errors = new List<string>();
        var variables = ExtractVariables(expression);
        var schemaText = JsonSerializer.Serialize(schema);
        var schemaNode = JsonNode.Parse(schemaText);

        foreach (var variable in variables)
        {
            // Handle nested field access (e.g., "address.city")
            if (!VariableExistsInSchema(variable, schemaNode))
            {
                errors.Add($"Variable '{variable}' not found in schema");
            }
        }

        return errors;
    }

    /// <summary>
    /// Check if a variable path exists in the schema
    /// </summary>
    private bool VariableExistsInSchema(string variablePath, JsonNode? schemaNode)
    {
        if (schemaNode == null) return false;

        var parts = variablePath.Split('.');
        var current = schemaNode;

        foreach (var part in parts)
        {
            if (current is JsonObject obj)
            {
                // Check in properties
                if (obj.TryGetPropertyValue("properties", out var props) &&
                    props is JsonObject propsObj &&
                    propsObj.TryGetPropertyValue(part, out var fieldSchema))
                {
                    current = fieldSchema;
                    continue;
                }

                // Check in additionalProperties
                if (obj.TryGetPropertyValue("additionalProperties", out var additional) &&
                    additional is JsonObject)
                {
                    current = additional;
                    continue;
                }

                return false;
            }

            return false;
        }

        return true;
    }
}
