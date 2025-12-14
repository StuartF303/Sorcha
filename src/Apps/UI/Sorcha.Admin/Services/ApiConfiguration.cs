// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Admin.Services;

/// <summary>
/// Configuration for API endpoints
/// </summary>
public class ApiConfiguration
{
    /// <summary>
    /// Base URL for the API Gateway
    /// In production, this would come from configuration
    /// </summary>
    public static string GatewayBaseUrl { get; set; } = "https://localhost:7082";

    /// <summary>
    /// Blueprint API endpoint (via gateway)
    /// </summary>
    public static string BlueprintApiUrl => $"{GatewayBaseUrl}/api/blueprint";

    /// <summary>
    /// Peer Service endpoint (via gateway)
    /// </summary>
    public static string PeerServiceUrl => $"{GatewayBaseUrl}/api/peer";

    /// <summary>
    /// Aggregated health endpoint
    /// </summary>
    public static string HealthUrl => $"{GatewayBaseUrl}/api/health";

    /// <summary>
    /// System statistics endpoint
    /// </summary>
    public static string StatsUrl => $"{GatewayBaseUrl}/api/stats";

    /// <summary>
    /// Blueprint service status endpoint
    /// </summary>
    public static string BlueprintStatusUrl => $"{GatewayBaseUrl}/api/blueprint/status";

    /// <summary>
    /// Peer service status endpoint
    /// </summary>
    public static string PeerStatusUrl => $"{GatewayBaseUrl}/api/peer/status";
}
