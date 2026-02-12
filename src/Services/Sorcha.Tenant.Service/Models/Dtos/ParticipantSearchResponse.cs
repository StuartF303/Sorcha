// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Tenant.Service.Models.Dtos;

/// <summary>
/// Response model for participant search results.
/// </summary>
public record ParticipantSearchResponse
{
    /// <summary>
    /// List of matching participants.
    /// </summary>
    public List<ParticipantResponse> Results { get; init; } = new();

    /// <summary>
    /// Total number of participants matching the criteria.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Whether there are more pages after the current page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Whether there are pages before the current page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Search query that was used.
    /// </summary>
    public string? Query { get; init; }
}
