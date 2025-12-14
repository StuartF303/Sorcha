// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net;
using Microsoft.Extensions.Options;
using Sorcha.Peer.Service.Core;

namespace Sorcha.Peer.Service.Discovery;

/// <summary>
/// Service for detecting whether this peer is a central node based on configuration and hostname validation
/// </summary>
/// <remarks>
/// Implements hybrid detection strategy:
/// - Explicit IsCentralNode configuration flag
/// - Optional hostname pattern validation (*.sorcha.dev)
/// - Throws InvalidOperationException if misconfigured
/// </remarks>
public class CentralNodeDiscoveryService
{
    private readonly ILogger<CentralNodeDiscoveryService> _logger;
    private readonly CentralNodeConfiguration _config;
    private readonly bool? _isCentralNode;

    /// <summary>
    /// Initializes a new instance of the <see cref="CentralNodeDiscoveryService"/> class
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="configuration">Central node configuration from appsettings</param>
    public CentralNodeDiscoveryService(
        ILogger<CentralNodeDiscoveryService> logger,
        IOptions<CentralNodeConfiguration> configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));

        // Perform detection on startup
        _isCentralNode = DetectIfCentralNode();
    }

    /// <summary>
    /// Detects if this node is a central node based on configuration and optional hostname validation
    /// </summary>
    /// <returns>True if this is a central node, false if this is a peer node</returns>
    /// <exception cref="InvalidOperationException">Thrown when IsCentralNode=true but hostname validation fails</exception>
    public bool DetectIfCentralNode()
    {
        // If not configured as central node, return false immediately
        if (!_config.IsCentralNode)
        {
            _logger.LogInformation("Node configured as peer node (IsCentralNode=false)");
            return false;
        }

        // If hostname validation disabled, trust configuration
        if (!_config.ValidateHostname)
        {
            _logger.LogInformation("Node configured as central node (IsCentralNode=true, hostname validation disabled)");
            return true;
        }

        // Validate hostname matches expected pattern
        var hostname = Dns.GetHostName();
        var expectedPattern = _config.ExpectedHostnamePattern ?? "*.sorcha.dev";

        _logger.LogDebug("Validating hostname '{Hostname}' against pattern '{Pattern}'", hostname, expectedPattern);

        // Check if hostname matches n0/n1/n2.sorcha.dev pattern
        if (!CentralNodeValidator.IsValidCentralNodeHostname(hostname))
        {
            var errorMessage = $"IsCentralNode is true but hostname '{hostname}' does not match expected pattern '{expectedPattern}'. " +
                             $"Valid hostnames: n0.sorcha.dev, n1.sorcha.dev, n2.sorcha.dev. " +
                             $"Either fix hostname or set ValidateHostname=false in configuration.";

            _logger.LogError("{ErrorMessage}", errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        _logger.LogInformation("Node validated as central node: hostname '{Hostname}' matches pattern", hostname);
        return true;
    }

    /// <summary>
    /// Gets whether this node is a central node (cached result from startup detection)
    /// </summary>
    /// <returns>True if central node, false if peer node</returns>
    public bool IsCentralNode()
    {
        if (!_isCentralNode.HasValue)
        {
            _logger.LogWarning("Central node detection not completed - returning false");
            return false;
        }

        return _isCentralNode.Value;
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
    /// Validates that the current hostname matches central node pattern
    /// </summary>
    /// <returns>True if hostname is valid for central node</returns>
    public bool ValidateCurrentHostname()
    {
        var hostname = Dns.GetHostName();
        return CentralNodeValidator.IsValidCentralNodeHostname(hostname);
    }
}
