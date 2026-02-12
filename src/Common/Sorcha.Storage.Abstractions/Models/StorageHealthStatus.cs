// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Storage.Abstractions;

/// <summary>
/// Overall health status for storage layer.
/// </summary>
public record StorageHealthStatus(
    bool IsHealthy,
    TierHealthStatus? HotTier,
    TierHealthStatus? WarmTier,
    TierHealthStatus? ColdTier,
    DateTime Timestamp)
{
    /// <summary>
    /// Creates a healthy status.
    /// </summary>
    public static StorageHealthStatus Healthy(
        TierHealthStatus? hot = null,
        TierHealthStatus? warm = null,
        TierHealthStatus? cold = null) =>
        new(true, hot, warm, cold, DateTime.UtcNow);

    /// <summary>
    /// Creates an unhealthy status.
    /// </summary>
    public static StorageHealthStatus Unhealthy(
        TierHealthStatus? hot = null,
        TierHealthStatus? warm = null,
        TierHealthStatus? cold = null) =>
        new(false, hot, warm, cold, DateTime.UtcNow);
}

/// <summary>
/// Health status for a single storage tier.
/// </summary>
public record TierHealthStatus(
    bool IsHealthy,
    string Provider,
    double ResponseTimeMs,
    string? ErrorMessage = null,
    CircuitState CircuitState = CircuitState.Closed)
{
    /// <summary>
    /// Creates a healthy tier status.
    /// </summary>
    public static TierHealthStatus Healthy(string provider, double responseTimeMs) =>
        new(true, provider, responseTimeMs);

    /// <summary>
    /// Creates an unhealthy tier status.
    /// </summary>
    public static TierHealthStatus Unhealthy(string provider, string errorMessage, CircuitState state = CircuitState.Open) =>
        new(false, provider, -1, errorMessage, state);
}

/// <summary>
/// Circuit breaker state.
/// </summary>
public enum CircuitState
{
    /// <summary>
    /// Circuit is closed, requests flow normally.
    /// </summary>
    Closed,

    /// <summary>
    /// Circuit is open, requests are blocked.
    /// </summary>
    Open,

    /// <summary>
    /// Circuit is half-open, testing if service recovered.
    /// </summary>
    HalfOpen
}
