// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Blueprint.Schemas.Models;

namespace Sorcha.Blueprint.Service.Models;

/// <summary>
/// Runtime status tracking for a configured schema provider.
/// </summary>
public sealed class SchemaProviderStatus
{
    /// <summary>
    /// Provider identifier.
    /// </summary>
    public required string ProviderName { get; set; }

    /// <summary>
    /// Whether this provider is active.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Provider's base URL (if applicable).
    /// </summary>
    public string? BaseUri { get; set; }

    /// <summary>
    /// How the provider fetches schemas.
    /// </summary>
    public ProviderType ProviderType { get; set; }

    /// <summary>
    /// Max requests per second.
    /// </summary>
    public double RateLimitPerSecond { get; set; } = 2.0;

    /// <summary>
    /// Hours between automatic refreshes.
    /// </summary>
    public int RefreshIntervalHours { get; set; } = 24;

    /// <summary>
    /// Last successful refresh timestamp.
    /// </summary>
    public DateTimeOffset? LastSuccessfulFetch { get; set; }

    /// <summary>
    /// Last error message (if failed).
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// When the last error occurred.
    /// </summary>
    public DateTimeOffset? LastErrorAt { get; set; }

    /// <summary>
    /// Number of schemas indexed from this provider.
    /// </summary>
    public int SchemaCount { get; set; }

    /// <summary>
    /// Current health status.
    /// </summary>
    public ProviderHealth HealthStatus { get; set; } = ProviderHealth.Unknown;

    /// <summary>
    /// Exponential backoff expiry.
    /// </summary>
    public DateTimeOffset? BackoffUntil { get; set; }

    /// <summary>
    /// Failure counter for backoff calculation.
    /// </summary>
    public int ConsecutiveFailures { get; set; }
}
