// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Validator.Service.Configuration;

/// <summary>
/// Main configuration for the Validator Service instance
/// </summary>
public class ValidatorConfiguration
{
    /// <summary>
    /// Unique identifier for this validator instance
    /// </summary>
    public string ValidatorId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// System wallet ID (managed by Wallet Service)
    /// </summary>
    public string? SystemWalletId { get; set; }

    /// <summary>
    /// Maximum depth for blockchain reorganization (fork resolution)
    /// </summary>
    public int MaxReorgDepth { get; set; } = 10;

    /// <summary>
    /// gRPC endpoint for this validator (for peer-to-peer communication)
    /// </summary>
    public string? GrpcEndpoint { get; set; }
}
