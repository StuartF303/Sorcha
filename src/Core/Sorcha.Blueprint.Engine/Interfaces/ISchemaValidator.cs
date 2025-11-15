// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Nodes;

namespace Sorcha.Blueprint.Engine.Interfaces;

/// <summary>
/// JSON Schema validator implementing JSON Schema Draft 2020-12.
/// </summary>
/// <remarks>
/// Validates data against JSON Schema specifications, providing detailed
/// error messages with JSON Pointers for precise error locations.
/// 
/// Supports:
/// - All JSON Schema Draft 2020-12 keywords
/// - Nested objects and arrays
/// - Custom formats (email, date-time, uri, etc.)
/// - Required fields and type validation
/// - String patterns (regex)
/// - Numeric constraints (min, max, multipleOf)
/// - Array constraints (minItems, maxItems, uniqueItems)
/// - Object constraints (required, properties, additionalProperties)
/// </remarks>
public interface ISchemaValidator
{
    /// <summary>
    /// Validate data against a JSON Schema.
    /// </summary>
    /// <param name="data">The data to validate.</param>
    /// <param name="schema">The JSON Schema (Draft 2020-12) to validate against.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A validation result indicating success or failure.
    /// If validation fails, the result contains detailed error information
    /// with JSON Pointers indicating the exact location of each error.
    /// </returns>
    /// <remarks>
    /// This method is thread-safe and can be called concurrently.
    /// Validation is performed asynchronously to support large schemas
    /// with complex nested structures.
    /// </remarks>
    Task<ValidationResult> ValidateAsync(
        Dictionary<string, object> data,
        JsonNode schema,
        CancellationToken ct = default);
}
