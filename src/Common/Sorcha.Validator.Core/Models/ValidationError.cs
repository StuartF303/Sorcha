// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Validator.Core.Models;

/// <summary>
/// Represents a validation error with code, message, and optional field context
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Error code for programmatic handling
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Optional field name where the error occurred
    /// </summary>
    public string? Field { get; init; }

    /// <summary>
    /// Optional additional details
    /// </summary>
    public Dictionary<string, object>? Details { get; init; }

    public override string ToString() => Field != null
        ? $"[{Code}] {Field}: {Message}"
        : $"[{Code}] {Message}";
}
