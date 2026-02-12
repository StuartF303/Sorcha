// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Sorcha.Blueprint.Schemas.DTOs;

/// <summary>
/// Request to update an existing custom schema.
/// Only non-null properties are updated.
/// </summary>
/// <param name="Title">New title (1-200 chars).</param>
/// <param name="Description">New description (max 2000 chars).</param>
/// <param name="Version">New semantic version.</param>
/// <param name="Content">New JSON Schema content.</param>
public sealed record UpdateSchemaRequest(
    [StringLength(200, MinimumLength = 1)]
    string? Title,

    [StringLength(2000)]
    string? Description,

    [RegularExpression(@"^\d+\.\d+\.\d+$", ErrorMessage = "Version must be semantic format (e.g., 1.0.0).")]
    string? Version,

    JsonElement? Content
);
