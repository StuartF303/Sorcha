// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Storage.Abstractions;

/// <summary>
/// Configuration for observability per storage tier.
/// </summary>
public class ObservabilityConfiguration
{
    /// <summary>
    /// Observability level for this tier.
    /// Default: Metrics.
    /// </summary>
    public ObservabilityLevel Level { get; set; } = ObservabilityLevel.Metrics;

    /// <summary>
    /// Whether to emit metrics.
    /// </summary>
    public bool MetricsEnabled { get; set; } = true;

    /// <summary>
    /// Whether to emit trace spans.
    /// </summary>
    public bool TracingEnabled { get; set; } = false;

    /// <summary>
    /// Whether to emit structured logs.
    /// </summary>
    public bool LoggingEnabled { get; set; } = true;
}

/// <summary>
/// Observability levels for storage tiers.
/// </summary>
public enum ObservabilityLevel
{
    /// <summary>
    /// No observability.
    /// </summary>
    None,

    /// <summary>
    /// Emit metrics only.
    /// </summary>
    Metrics,

    /// <summary>
    /// Emit metrics and structured logs.
    /// </summary>
    StructuredLogging,

    /// <summary>
    /// Full observability: metrics, logs, and traces.
    /// </summary>
    FullTracing
}
