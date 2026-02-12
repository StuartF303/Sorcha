// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Blueprint.Schemas.Models;

/// <summary>
/// Indicates the origin type of a schema.
/// </summary>
public enum SourceType
{
    /// <summary>
    /// Sorcha-defined system schemas.
    /// </summary>
    Internal,

    /// <summary>
    /// Fetched from an external source (e.g., SchemaStore.org).
    /// </summary>
    External,

    /// <summary>
    /// Created by an organization user.
    /// </summary>
    Custom
}
