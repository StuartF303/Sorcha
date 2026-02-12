// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json.Nodes;

namespace Sorcha.Blueprint.Engine.Models;

/// <summary>
/// Represents a calculation that uses JSON Logic to compute a derived field.
/// </summary>
/// <remarks>
/// Calculations allow blueprints to compute values from submitted data
/// using JSON Logic expressions.
/// 
/// Example:
/// ```json
/// {
///   "outputField": "totalPrice",
///   "expression": { "*": [{"var": "quantity"}, {"var": "unitPrice"}] }
/// }
/// ```
/// 
/// This would calculate totalPrice = quantity * unitPrice
/// </remarks>
public class Calculation
{
    /// <summary>
    /// The name of the field where the calculated value will be stored.
    /// </summary>
    /// <remarks>
    /// This field name is added to the action data with the result
    /// of evaluating the expression.
    /// 
    /// If a field with this name already exists, it will be overwritten.
    /// </remarks>
    public required string OutputField { get; init; }

    /// <summary>
    /// The JSON Logic expression to evaluate.
    /// </summary>
    /// <remarks>
    /// This expression has access to all fields in the action data
    /// via the {"var": "fieldName"} syntax.
    /// 
    /// Calculations can reference:
    /// - Original submitted fields
    /// - Results from previous calculations (if applied in order)
    /// 
    /// Example expressions:
    /// - Simple arithmetic: { "*": [{"var": "quantity"}, {"var": "price"}] }
    /// - Conditional: { "if": [{"&gt;": [{"var": "age"}, 18]}, "adult", "minor"] }
    /// - String concatenation: { "cat": [{"var": "firstName"}, " ", {"var": "lastName"}] }
    /// </remarks>
    public required JsonNode Expression { get; init; }

    /// <summary>
    /// Optional description of what this calculation does.
    /// </summary>
    /// <remarks>
    /// Useful for documentation and debugging.
    /// Not used during execution.
    /// </remarks>
    public string? Description { get; init; }

    /// <summary>
    /// Creates a calculation.
    /// </summary>
    public static Calculation Create(string outputField, JsonNode expression, string? description = null) => new()
    {
        OutputField = outputField,
        Expression = expression,
        Description = description
    };
}
