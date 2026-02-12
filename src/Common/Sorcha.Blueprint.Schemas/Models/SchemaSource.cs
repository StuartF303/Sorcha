// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Blueprint.Schemas.Models;

/// <summary>
/// Tracks the origin information for a schema.
/// </summary>
public sealed record SchemaSource
{
    /// <summary>
    /// Gets the type of source (Internal, External, Custom).
    /// </summary>
    public required SourceType Type { get; init; }

    /// <summary>
    /// Gets the source URL for external schemas.
    /// </summary>
    public string? Uri { get; init; }

    /// <summary>
    /// Gets the provider name (e.g., "SchemaStore.org").
    /// </summary>
    public string? Provider { get; init; }

    /// <summary>
    /// Gets when the external schema was retrieved.
    /// </summary>
    public DateTimeOffset? FetchedAt { get; init; }

    /// <summary>
    /// Creates a source for internal system schemas.
    /// </summary>
    public static SchemaSource Internal() => new()
    {
        Type = SourceType.Internal
    };

    /// <summary>
    /// Creates a source for external schemas.
    /// </summary>
    public static SchemaSource FromExternal(string uri, string provider) => new()
    {
        Type = SourceType.External,
        Uri = uri,
        Provider = provider,
        FetchedAt = DateTimeOffset.UtcNow
    };

    /// <summary>
    /// Creates a source for custom user-defined schemas.
    /// </summary>
    public static SchemaSource Custom() => new()
    {
        Type = SourceType.Custom
    };
}
