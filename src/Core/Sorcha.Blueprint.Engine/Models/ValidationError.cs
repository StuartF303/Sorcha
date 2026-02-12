// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Blueprint.Engine.Models;

/// <summary>
/// Represents a single validation error with precise location information.
/// </summary>
/// <remarks>
/// Validation errors use JSON Pointers (RFC 6901) to indicate exactly
/// where in the data the validation failure occurred.
/// 
/// Example:
/// If the field "/user/email" fails validation, the error would have:
/// - InstanceLocation: "/user/email"
/// - Message: "The value is not a valid email address"
/// - SchemaLocation: "#/properties/user/properties/email/format"
/// 
/// This precise location information allows UIs to highlight the specific
/// field that needs correction.
/// </remarks>
public class ValidationError
{
    /// <summary>
    /// JSON Pointer (RFC 6901) to the location in the data that failed validation.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// - "" (root)
    /// - "/email"
    /// - "/address/zipCode"
    /// - "/items/0/price"
    /// </remarks>
    public required string InstanceLocation { get; init; }

    /// <summary>
    /// Human-readable error message describing the validation failure.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// - "The value is required but was not provided"
    /// - "The value 'abc' is not a valid number"
    /// - "The string length must be at least 5 characters"
    /// - "The value must match the pattern '^[A-Z]{3}$'"
    /// </remarks>
    public required string Message { get; init; }

    /// <summary>
    /// JSON Pointer to the schema location that defined the constraint.
    /// </summary>
    /// <remarks>
    /// Points to the specific keyword in the schema that was violated.
    /// Useful for debugging schema issues.
    /// 
    /// Examples:
    /// - "#/properties/email/format"
    /// - "#/properties/age/minimum"
    /// - "#/required"
    /// </remarks>
    public string? SchemaLocation { get; init; }

    /// <summary>
    /// The schema keyword that was violated.
    /// </summary>
    /// <remarks>
    /// Examples: "required", "type", "format", "minimum", "pattern", etc.
    /// </remarks>
    public string? Keyword { get; init; }

    /// <summary>
    /// Additional context about the validation error.
    /// </summary>
    /// <remarks>
    /// May contain information like:
    /// - The expected type vs actual type
    /// - The minimum/maximum constraint values
    /// - The required field names
    /// </remarks>
    public Dictionary<string, object>? AdditionalInfo { get; init; }

    /// <summary>
    /// Creates a simple validation error with just location and message.
    /// </summary>
    public static ValidationError Create(string instanceLocation, string message) => new()
    {
        InstanceLocation = instanceLocation,
        Message = message
    };

    /// <summary>
    /// Creates a detailed validation error with all information.
    /// </summary>
    public static ValidationError Create(
        string instanceLocation,
        string message,
        string? schemaLocation = null,
        string? keyword = null,
        Dictionary<string, object>? additionalInfo = null) => new()
    {
        InstanceLocation = instanceLocation,
        Message = message,
        SchemaLocation = schemaLocation,
        Keyword = keyword,
        AdditionalInfo = additionalInfo
    };
}
