// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Validator.Core.Models;

/// <summary>
/// Result of a validation operation indicating success/failure with error details
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether validation succeeded
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// List of validation errors (empty if valid)
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; init; } = Array.Empty<ValidationError>();

    /// <summary>
    /// Optional metadata about the validation
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static ValidationResult Success() => new()
    {
        IsValid = true
    };

    /// <summary>
    /// Creates a successful validation result with metadata
    /// </summary>
    public static ValidationResult Success(Dictionary<string, object> metadata) => new()
    {
        IsValid = true,
        Metadata = metadata
    };

    /// <summary>
    /// Creates a failed validation result with errors
    /// </summary>
    public static ValidationResult Failure(params ValidationError[] errors) => new()
    {
        IsValid = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed validation result with a single error
    /// </summary>
    public static ValidationResult Failure(string code, string message, string? field = null) => new()
    {
        IsValid = false,
        Errors = new[]
        {
            new ValidationError
            {
                Code = code,
                Message = message,
                Field = field
            }
        }
    };

    public override string ToString()
    {
        if (IsValid)
            return "Validation succeeded";

        return $"Validation failed with {Errors.Count} error(s):\n" +
               string.Join("\n", Errors.Select(e => $"  - {e}"));
    }
}
