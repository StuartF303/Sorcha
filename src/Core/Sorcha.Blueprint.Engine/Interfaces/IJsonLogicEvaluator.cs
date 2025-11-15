// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Nodes;

namespace Sorcha.Blueprint.Engine.Interfaces;

/// <summary>
/// JSON Logic expression evaluator for calculations and conditional routing.
/// </summary>
/// <remarks>
/// Evaluates JSON Logic expressions as defined at https://jsonlogic.com
/// 
/// Supported operators:
/// - Comparison: ==, !=, ===, !==, &lt;, &gt;, &lt;=, &gt;=
/// - Logical: and, or, !, !!
/// - Arithmetic: +, -, *, /, %
/// - Array: map, filter, reduce, all, none, some, merge, in
/// - String: cat (concatenation), substr, in (contains)
/// - Data access: var (variable reference)
/// - Miscellaneous: if, missing, missing_some, log
/// 
/// Variable references use {"var": "fieldName"} syntax to access data fields.
/// Nested field access is supported: {"var": "address.city"}
/// </remarks>
public interface IJsonLogicEvaluator
{
    /// <summary>
    /// Evaluate a JSON Logic expression against data.
    /// </summary>
    /// <param name="expression">The JSON Logic expression to evaluate.</param>
    /// <param name="data">The data context for variable references.</param>
    /// <returns>The result of the expression evaluation.</returns>
    /// <remarks>
    /// This method is synchronous because JSON Logic evaluation is
    /// typically fast and doesn't require I/O operations.
    /// </remarks>
    /// <exception cref="ArgumentNullException">If expression is null.</exception>
    /// <exception cref="InvalidOperationException">If the expression is malformed or uses unsupported operators.</exception>
    object Evaluate(
        JsonNode expression,
        Dictionary<string, object> data);

    /// <summary>
    /// Apply multiple calculations to data.
    /// </summary>
    /// <param name="data">The input data.</param>
    /// <param name="calculations">The calculations to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The data with calculated fields added.
    /// Calculated fields are added using the outputField name from each calculation.
    /// </returns>
    /// <remarks>
    /// Calculations are applied in order, so later calculations can reference
    /// the results of earlier calculations.
    /// 
    /// Example calculation:
    /// {
    ///   "name": "totalPrice",
    ///   "outputField": "total",
    ///   "expression": { "*": [{"var": "quantity"}, {"var": "unitPrice"}] }
    /// }
    /// </remarks>
    Task<Dictionary<string, object>> ApplyCalculationsAsync(
        Dictionary<string, object> data,
        IEnumerable<Calculation> calculations,
        CancellationToken ct = default);

    /// <summary>
    /// Evaluate routing conditions to determine next participant.
    /// </summary>
    /// <param name="data">The action data.</param>
    /// <param name="conditions">The routing conditions to evaluate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The ID of the next participant whose condition evaluates to true,
    /// or null if no conditions match.
    /// </returns>
    /// <remarks>
    /// Conditions are evaluated in order. The first condition that evaluates
    /// to true determines the next participant. If no conditions match,
    /// returns null (which typically means workflow completion).
    /// </remarks>
    Task<string?> EvaluateConditionsAsync(
        Dictionary<string, object> data,
        IEnumerable<Condition> conditions,
        CancellationToken ct = default);
}
