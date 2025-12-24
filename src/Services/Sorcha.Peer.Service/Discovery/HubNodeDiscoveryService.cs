// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net;
using Microsoft.Extensions.Options;
using Sorcha.Peer.Service.Core;

namespace Sorcha.Peer.Service.Discovery;

/// <summary>
/// Service for detecting whether this peer is a hub node based on configuration and hostname validation
/// </summary>
/// <remarks>
/// Implements hybrid detection strategy:
/// - Explicit IsHubNode configuration flag
/// - Optional hostname pattern validation (*.sorcha.dev)
/// - Throws InvalidOperationException if misconfigured
/// </remarks>
public class HubNodeDiscoveryService
{
    private readonly ILogger<HubNodeDiscoveryService> _logger;
    private readonly HubNodeConfiguration _config;
    private readonly bool? _isHubNode;

    /// <summary>
    /// Initializes a new instance of the <see cref="HubNodeDiscoveryService"/> class
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="configuration">Hub node configuration from appsettings</param>
    public HubNodeDiscoveryService(
        ILogger<HubNodeDiscoveryService> logger,
        IOptions<HubNodeConfiguration> configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));

        // Perform detection on startup
        _isHubNode = DetectIfHubNode();
    }

    /// <summary>
    /// Detects if this node is a hub node based on configuration and optional hostname validation
    /// </summary>
    /// <returns>True if this is a hub node, false if this is a peer node</returns>
    /// <exception cref="InvalidOperationException">Thrown when IsHubNode=true but hostname validation fails</exception>
    public bool DetectIfHubNode()
    {
        // If not configured as hub node, return false immediately
        if (!_config.IsHubNode)
        {
            _logger.LogInformation("Node configured as peer node (IsHubNode=false)");
            return false;
        }

        // If hostname validation disabled, trust configuration
        if (!_config.ValidateHostname)
        {
            _logger.LogInformation("Node configured as hub node (IsHubNode=true, hostname validation disabled)");
            return true;
        }

        // Validate hostname matches expected pattern
        var hostname = Dns.GetHostName();
        var expectedPattern = _config.ExpectedHostnamePattern ?? "*.sorcha.dev";

        _logger.LogDebug("Validating hostname '{Hostname}' against pattern '{Pattern}'", hostname, expectedPattern);

        // Check if hostname matches n0/n1/n2.sorcha.dev pattern
        if (!HubNodeValidator.IsValidHubNodeHostname(hostname))
        {
            var errorMessage = $"IsHubNode is true but hostname '{hostname}' does not match expected pattern '{expectedPattern}'. " +
                             $"Valid hostnames: n0.sorcha.dev, n1.sorcha.dev, n2.sorcha.dev. " +
                             $"Either fix hostname or set ValidateHostname=false in configuration.";

            _logger.LogError("{ErrorMessage}", errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        _logger.LogInformation("Node validated as hub node: hostname '{Hostname}' matches pattern", hostname);
        return true;
    }

    /// <summary>
    /// Gets whether this node is a hub node (cached result from startup detection)
    /// </summary>
    /// <returns>True if hub node, false if peer node</returns>
    public bool IsHubNode()
    {
        if (!_isHubNode.HasValue)
        {
            _logger.LogWarning("Hub node detection not completed - returning false");
            return false;
        }

        return _isHubNode.Value;
    }

    /// <summary>
    /// Gets the current hostname of this node
    /// </summary>
    /// <returns>DNS hostname</returns>
    public string GetHostname()
    {
        return Dns.GetHostName();
    }

    /// <summary>
    /// Validates that the current hostname matches hub node pattern
    /// </summary>
    /// <returns>True if hostname is valid for hub node</returns>
    public bool ValidateCurrentHostname()
    {
        var hostname = Dns.GetHostName();
        return HubNodeValidator.IsValidHubNodeHostname(hostname);
    }
}
