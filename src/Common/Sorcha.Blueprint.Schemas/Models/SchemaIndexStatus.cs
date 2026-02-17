// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Blueprint.Schemas.Models;

/// <summary>
/// Status of a schema in the schema index.
/// </summary>
public enum SchemaIndexStatus
{
    /// <summary>
    /// Available and up-to-date from source.
    /// </summary>
    Active,

    /// <summary>
    /// Marked as deprecated by source, still queryable.
    /// </summary>
    Deprecated,

    /// <summary>
    /// Source unreachable, entry retained from last successful fetch.
    /// </summary>
    Unavailable
}
