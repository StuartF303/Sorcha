// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

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

    // Convenience methods for common operations with automatic expression setting

    /// <summary>
    /// Multiply two variables: var1 * var2
    /// </summary>
    public CalculationBuilder Multiply(string var1, string var2)
    {
        _expression = Multiply(Variable(var1), Variable(var2));
        return this;
    }

    /// <summary>
    /// Multiply variable by constant: var * constant
    /// </summary>
    public CalculationBuilder Multiply(string variable, double constant)
    {
        _expression = Multiply(Variable(variable), Constant(constant));
        return this;
    }

    /// <summary>
    /// Multiply two constants: a * b
    /// </summary>
    public CalculationBuilder Multiply(double a, double b)
    {
        _expression = Multiply(Constant(a), Constant(b));
        return this;
    }

    /// <summary>
    /// Add two variables: var1 + var2
    /// </summary>
    public CalculationBuilder Add(string var1, string var2)
    {
        _expression = Add(Variable(var1), Variable(var2));
        return this;
    }

    /// <summary>
    /// Add variable and constant: var + constant
    /// </summary>
    public CalculationBuilder Add(string variable, double constant)
    {
        _expression = Add(Variable(variable), Constant(constant));
        return this;
    }

    /// <summary>
    /// Subtract two variables: var1 - var2
    /// </summary>
    public CalculationBuilder Subtract(string var1, string var2)
    {
        _expression = Subtract(Variable(var1), Variable(var2));
        return this;
    }

    /// <summary>
    /// Subtract constant from variable: var - constant
    /// </summary>
    public CalculationBuilder Subtract(string variable, double constant)
    {
        _expression = Subtract(Variable(variable), Constant(constant));
        return this;
    }

    /// <summary>
    /// Divide two variables: var1 / var2
    /// </summary>
    public CalculationBuilder Divide(string var1, string var2)
    {
        _expression = Divide(Variable(var1), Variable(var2));
        return this;
    }

    /// <summary>
    /// Divide variable by constant: var / constant
    /// </summary>
    public CalculationBuilder Divide(string variable, double constant)
    {
        _expression = Divide(Variable(variable), Constant(constant));
        return this;
    }

    internal JsonNode Build()
    {
        if (_expression == null)
            throw new InvalidOperationException("Calculation expression not set. Use WithExpression() or build expression using arithmetic methods.");

        return _expression;
    }
}
