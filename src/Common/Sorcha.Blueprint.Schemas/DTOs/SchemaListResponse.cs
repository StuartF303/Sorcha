// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Blueprint.Schemas.DTOs;

/// <summary>
/// Response for paginated schema list queries.
/// </summary>
/// <param name="Schemas">List of schema entries.</param>
/// <param name="TotalCount">Total number of matching schemas.</param>
/// <param name="NextCursor">Cursor for next page, null if no more results.</param>
public sealed record SchemaListResponse(
    IReadOnlyList<SchemaEntryDto> Schemas,
    int TotalCount,
    string? NextCursor
);
