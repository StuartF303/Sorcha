// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Blueprint.Schemas.Models;

/// <summary>
/// Represents the lifecycle state of a schema.
/// </summary>
public enum SchemaStatus
{
    /// <summary>
    /// Schema is available for use in blueprints.
    /// </summary>
    Active,

    /// <summary>
    /// Schema should not be used for new work.
    /// Still accessible but visually flagged in UI.
    /// </summary>
    Deprecated
}
