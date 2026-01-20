// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Blueprint.Schemas.DTOs;

/// <summary>
/// Data transfer object for schema source information.
/// </summary>
/// <param name="Type">Source type: Internal, External, or Custom.</param>
/// <param name="Uri">Source URL for external schemas.</param>
/// <param name="Provider">Provider name (e.g., "SchemaStore.org").</param>
/// <param name="FetchedAt">When the external schema was retrieved.</param>
public sealed record SchemaSourceDto(
    string Type,
    string? Uri,
    string? Provider,
    DateTimeOffset? FetchedAt
);
