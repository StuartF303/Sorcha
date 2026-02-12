// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Blueprint.Schemas.Models;

/// <summary>
/// Categorizes schemas by their origin and mutability.
/// </summary>
public enum SchemaCategory
{
    /// <summary>
    /// Core Sorcha schemas (Installation, Organisation, Participant, Register).
    /// Global scope, immutable.
    /// </summary>
    System,

    /// <summary>
    /// Imported from external sources (e.g., SchemaStore.org).
    /// Global scope, can be deprecated but not modified.
    /// </summary>
    External,

    /// <summary>
    /// User-defined schemas for organization-specific needs.
    /// Organization-scoped by default, can be published globally.
    /// </summary>
    Custom
}
