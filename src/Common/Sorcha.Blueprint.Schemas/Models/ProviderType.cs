// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Blueprint.Schemas.Models;

/// <summary>
/// Type of external schema provider based on how it fetches schemas.
/// </summary>
public enum ProviderType
{
    /// <summary>
    /// Fetches schemas from a live API at runtime.
    /// </summary>
    LiveApi,

    /// <summary>
    /// Downloads a ZIP/bundle and extracts schemas.
    /// </summary>
    ZipBundle,

    /// <summary>
    /// Reads from embedded/static files bundled in the application.
    /// </summary>
    StaticFile
}
