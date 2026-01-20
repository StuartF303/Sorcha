// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Sorcha.Blueprint.Schemas.DTOs;

/// <summary>
/// Request to create a new custom schema.
/// </summary>
/// <param name="Identifier">Unique identifier (lowercase alphanumeric with hyphens, 3-100 chars).</param>
/// <param name="Title">Human-readable title (1-200 chars).</param>
/// <param name="Description">Optional description (max 2000 chars).</param>
/// <param name="Version">Semantic version (e.g., "1.0.0").</param>
/// <param name="Content">Valid JSON Schema content (draft 2020-12).</param>
public sealed record CreateSchemaRequest(
    [Required]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Identifier must be lowercase alphanumeric with hyphens only.")]
    [StringLength(100, MinimumLength = 3)]
    string Identifier,

    [Required]
    [StringLength(200, MinimumLength = 1)]
    string Title,

    [StringLength(2000)]
    string? Description,

    [Required]
    [RegularExpression(@"^\d+\.\d+\.\d+$", ErrorMessage = "Version must be semantic format (e.g., 1.0.0).")]
    string Version,

    [Required]
    JsonElement Content
);
