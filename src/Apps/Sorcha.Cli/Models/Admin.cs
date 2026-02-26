// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json.Serialization;

namespace Sorcha.Cli.Models;

/// <summary>
/// Service health status.
/// </summary>
public class ServiceHealthStatus
{
    [JsonPropertyName("service")]
    public string Service { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("responseTimeMs")]
    public int ResponseTimeMs { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("checkedAt")]
    public DateTimeOffset CheckedAt { get; set; }
}

/// <summary>
/// Aggregate health response for all services.
/// </summary>
public class HealthCheckResponse
{
    [JsonPropertyName("overallStatus")]
    public string OverallStatus { get; set; } = string.Empty;

    [JsonPropertyName("services")]
    public List<ServiceHealthStatus> Services { get; set; } = new();

    [JsonPropertyName("checkedAt")]
    public DateTimeOffset CheckedAt { get; set; }
}

/// <summary>
/// Schema sector information.
/// </summary>
public class SchemaSector
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("schemaCount")]
    public int SchemaCount { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Schema provider information.
/// </summary>
public class SchemaProvider
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("schemaCount")]
    public int SchemaCount { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; } = string.Empty;
}

/// <summary>
/// System alert.
/// </summary>
public class SystemAlert
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("acknowledgedAt")]
    public DateTimeOffset? AcknowledgedAt { get; set; }

    [JsonPropertyName("resolvedAt")]
    public DateTimeOffset? ResolvedAt { get; set; }
}
