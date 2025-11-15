// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Blueprint.Engine.Interfaces;

/// <summary>
/// Action processor that orchestrates the complete action execution workflow.
/// </summary>
/// <remarks>
/// The action processor coordinates all aspects of blueprint action execution:
/// 
/// 1. **Validation** - Validates action data against JSON Schema
/// 2. **Calculation** - Applies JSON Logic calculations to compute derived fields
/// 3. **Routing** - Determines the next action and participant using conditions
/// 4. **Disclosure** - Creates selective data views for each participant
/// 
/// This interface is the main orchestrator used by IExecutionEngine.
/// It delegates to specialized components (validator, evaluator, etc.)
/// but coordinates the overall workflow.
/// 
/// Execution flow:
/// ```
/// 1. Validate data against action schema
///    ↓ (if invalid, stop and return errors)
/// 2. Apply calculations to data
///    ↓
/// 3. Determine next action via routing
///    ↓
/// 4. Create disclosure payloads for participants
///    ↓
/// 5. Return complete execution result
/// ```
/// 
/// The processor is stateless and can be used concurrently.
/// </remarks>
public interface IActionProcessor
{
    /// <summary>
    /// Process an action through the complete execution workflow.
    /// </summary>
    /// <param name="context">
    /// The execution context containing:
    /// - Blueprint definition
    /// - Current action details
    /// - Action data submitted by participant
    /// - Historical context (previous actions)
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// An action execution result containing:
    /// - Validation result (success/failure with errors)
    /// - Calculated data (original data + computed fields)
    /// - Routing decision (next action and participant)
    /// - Disclosure results (filtered data for each participant)
    /// </returns>
    /// <remarks>
    /// If validation fails, the method returns immediately with validation errors.
    /// Calculations, routing, and disclosure are only performed if validation succeeds.
    /// 
    /// The processor uses dependency injection to obtain:
    /// - ISchemaValidator for data validation
    /// - IJsonLogicEvaluator for calculations and routing
    /// - IDisclosureProcessor for selective disclosure
    /// - IRoutingEngine for next action determination
    /// 
    /// All operations are performed asynchronously to support large datasets
    /// and complex blueprint definitions.
    /// </remarks>
    /// <exception cref="ArgumentNullException">If context is null.</exception>
    /// <exception cref="InvalidOperationException">If the blueprint or action is malformed.</exception>
    Task<ActionExecutionResult> ProcessAsync(
        ExecutionContext context,
        CancellationToken ct = default);
}
