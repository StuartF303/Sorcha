// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Blueprint.Engine.Models;

/// <summary>
/// Result of JSON Schema validation.
/// </summary>
/// <remarks>
/// Contains validation success/failure status and detailed error information.
/// 
/// If validation fails, the Errors list contains one or more ValidationError
/// objects, each describing a specific validation failure with its location
/// in the data (JSON Pointer) and the violated constraint.
/// </remarks>
public class ValidationResult
{
    /// <summary>
    /// Indicates whether the data passed validation.
    /// </summary>
    /// <remarks>
    /// True if the data conforms to all schema constraints.
    /// False if any validation errors occurred.
    /// </remarks>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors.
    /// </summary>
    /// <remarks>
    /// Empty if IsValid is true.
    /// Contains one or more errors if IsValid is false.
    /// 
    /// Each error includes:
    /// - The location in the data where the error occurred (JSON Pointer)
    /// - The specific constraint that was violated
    /// - A human-readable error message
    /// - The schema path that defined the constraint
    /// </remarks>
    public List<ValidationError> Errors { get; set; } = new();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Valid() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with errors.
    /// </summary>
    public static ValidationResult Invalid(List<ValidationError> errors) => new()
    {
        IsValid = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    public static ValidationResult Invalid(ValidationError error) => new()
    {
        IsValid = false,
        Errors = new List<ValidationError> { error }
    };
}
