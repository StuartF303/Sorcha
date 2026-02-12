// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Blueprint.Schemas.DTOs;

/// <summary>
/// Request to import an external schema into the local schema store.
/// </summary>
/// <param name="SchemaUrl">The URL of the external schema to import.</param>
/// <param name="Identifier">Optional custom identifier for the imported schema. If not provided, derived from the schema name.</param>
/// <param name="Category">The category to assign to the imported schema. Defaults to "External".</param>
/// <param name="OverwriteExisting">If true, overwrites any existing schema with the same identifier.</param>
public record ImportExternalSchemaRequest(
    string SchemaUrl,
    string? Identifier = null,
    string Category = "External",
    bool OverwriteExisting = false);
