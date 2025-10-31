// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Nodes;

namespace Sorcha.Blueprint.Fluent;

/// <summary>
/// Fluent builder for creating JSON Logic calculations
/// </summary>
public class CalculationBuilder
{
    private JsonNode? _expression;

    /// <summary>
    /// Addition: a + b + c + ...
    /// </summary>
    public JsonNode Add(params JsonNode[] operands)
    {
        var array = new JsonArray();
        foreach (var operand in operands)
        {
            array.Add(operand?.DeepClone());
        }
        return new JsonObject { ["+"] = array };
    }

    /// <summary>
    /// Subtraction: left - right
    /// </summary>
    public JsonNode Subtract(JsonNode left, JsonNode right)
    {
        return new JsonObject
        {
            ["-"] = new JsonArray(left?.DeepClone(), right?.DeepClone())
        };
    }

    /// <summary>
    /// Multiplication: a * b * c * ...
    /// </summary>
    public JsonNode Multiply(params JsonNode[] operands)
    {
        var array = new JsonArray();
        foreach (var operand in operands)
        {
            array.Add(operand?.DeepClone());
        }
        return new JsonObject { ["*"] = array };
    }

    /// <summary>
    /// Division: numerator / denominator
    /// </summary>
    public JsonNode Divide(JsonNode numerator, JsonNode denominator)
    {
        return new JsonObject
        {
            ["/"] = new JsonArray(numerator?.DeepClone(), denominator?.DeepClone())
        };
    }

    /// <summary>
    /// Modulo: left % right
    /// </summary>
    public JsonNode Modulo(JsonNode left, JsonNode right)
    {
        return new JsonObject
        {
            ["%"] = new JsonArray(left?.DeepClone(), right?.DeepClone())
        };
    }

    /// <summary>
    /// References a data variable: {"var": "variableName"}
    /// </summary>
    public JsonNode Variable(string name)
    {
        return new JsonObject { ["var"] = name };
    }

    /// <summary>
    /// Creates a literal constant value
    /// </summary>
    public JsonNode Constant(object value)
    {
        return JsonValue.Create(value) ?? JsonValue.Create(0);
    }

    /// <summary>
    /// Sets the calculation expression directly
    /// </summary>
    public CalculationBuilder WithExpression(JsonNode expression)
    {
        _expression = expression;
        return this;
    }

    internal JsonNode Build()
    {
        if (_expression == null)
            throw new InvalidOperationException("Calculation expression not set. Use WithExpression() or build expression using arithmetic methods.");

        return _expression;
    }
}
