// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Blueprint.Schemas.DTOs;

/// <summary>
/// Response from searching external schema providers.
/// </summary>
/// <param name="Results">The matching schemas.</param>
/// <param name="TotalCount">Total number of matching results.</param>
/// <param name="Provider">The provider that was searched.</param>
/// <param name="Query">The search query that was executed.</param>
/// <param name="IsPartialResult">True if results may be incomplete due to provider limitations.</param>
public record ExternalSchemaSearchResponse(
    IReadOnlyList<ExternalSchemaResult> Results,
    int TotalCount,
    string Provider,
    string Query,
    bool IsPartialResult = false);
