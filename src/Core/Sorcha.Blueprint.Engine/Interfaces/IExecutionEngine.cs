// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Nodes;

namespace Sorcha.Blueprint.Engine.Interfaces;

/// <summary>
/// Stateless blueprint execution engine that can run client-side or server-side.
/// This is the main entry point for blueprint workflow execution.
/// </summary>
/// <remarks>
/// The execution engine orchestrates the complete action processing workflow:
/// - Schema validation against JSON Schema Draft 2020-12
/// - JSON Logic calculation execution
/// - Conditional routing determination
/// - Selective data disclosure
/// 
/// The engine is designed to be portable and run in multiple contexts:
/// - Server-side: In Sorcha.Blueprint.Service for processing action submissions
/// - Client-side: In Blazor WASM Designer for pre-submission validation
/// </remarks>
public interface IExecutionEngine
{
    /// <summary>
    /// Execute an action within a blueprint workflow.
    /// </summary>
    /// <param name="context">The execution context containing blueprint, action, and data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of action execution including validation, calculations, routing, and disclosures.</returns>
    /// <remarks>
    /// This method performs the complete action execution workflow:
    /// 1. Validates the action data against the schema
    /// 2. Applies calculations using JSON Logic
    /// 3. Determines the next action via routing conditions
    /// 4. Creates selective disclosure payloads for each participant
    /// </remarks>
    Task<ActionExecutionResult> ExecuteActionAsync(
        ExecutionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Validate action data against schema without executing.
    /// </summary>
    /// <param name="schema">The JSON Schema to validate against (Draft 2020-12).</param>
    /// <param name="data">The data to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation result with detailed error information if validation fails.</returns>
    /// <remarks>
    /// This is useful for client-side validation before submission,
    /// providing immediate feedback without executing the full workflow.
    /// </remarks>
    Task<ValidationResult> ValidateAsync(
        JsonNode schema,
        Dictionary<string, object> data,
        CancellationToken ct = default);

    /// <summary>
    /// Apply calculations to data using JSON Logic without full execution.
    /// </summary>
    /// <param name="data">The input data.</param>
    /// <param name="calculations">The calculations to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The data with calculated fields added.</returns>
    /// <remarks>
    /// Useful for previewing calculation results in the designer
    /// before publishing a blueprint.
    /// </remarks>
    Task<Dictionary<string, object>> ApplyCalculationsAsync(
        Dictionary<string, object> data,
        IEnumerable<Calculation> calculations,
        CancellationToken ct = default);

    /// <summary>
    /// Determine routing without full execution.
    /// </summary>
    /// <param name="blueprint">The blueprint definition.</param>
    /// <param name="currentAction">The current action.</param>
    /// <param name="data">The action data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Routing decision indicating next action and participant.</returns>
    /// <remarks>
    /// Useful for testing routing logic in the designer
    /// before publishing a blueprint.
    /// </remarks>
    Task<RoutingResult> DetermineRoutingAsync(
        Blueprint blueprint,
        Models.Action currentAction,
        Dictionary<string, object> data,
        CancellationToken ct = default);

    /// <summary>
    /// Apply disclosure rules without full execution.
    /// </summary>
    /// <param name="data">The action data.</param>
    /// <param name="disclosures">The disclosure rules to apply.</param>
    /// <returns>List of disclosure results, one per participant.</returns>
    /// <remarks>
    /// Useful for previewing what data each participant will see
    /// before publishing a blueprint.
    /// </remarks>
    List<DisclosureResult> ApplyDisclosures(
        Dictionary<string, object> data,
        IEnumerable<Disclosure> disclosures);
}
