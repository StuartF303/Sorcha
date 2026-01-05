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
    /// System wallet address for signing control records and dockets
    /// </summary>
    /// <remarks>
    /// The system wallet is managed by the Wallet Service and is used exclusively by the Validator Service
    /// for system-level signing operations:
    /// - Signing complete control records after attestation collection
    /// - Signing finalized dockets after transaction validation
    /// This wallet address must be configured and accessible via the Wallet Service.
    /// </remarks>
    public required string SystemWalletAddress { get; set; }

    /// <summary>
    /// Maximum depth for blockchain reorganization (fork resolution)
    /// </summary>
    public int MaxReorgDepth { get; set; } = 10;

    /// <summary>
    /// gRPC endpoint for this validator (for peer-to-peer communication)
    /// </summary>
    public string? GrpcEndpoint { get; set; }
}
