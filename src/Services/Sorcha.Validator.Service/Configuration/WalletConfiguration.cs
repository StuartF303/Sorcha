// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Validator.Service.Configuration;

/// <summary>
/// Configuration settings for wallet integration with the Wallet Service.
/// </summary>
/// <remarks>
/// This configuration controls how the Validator Service connects to and interacts
/// with the Wallet Service for cryptographic operations including docket signing,
/// consensus vote signing, and signature verification.
///
/// Configuration can be loaded from:
/// 1. appsettings.json (WalletService section)
/// 2. Environment variable VALIDATOR_WALLET_ID (overrides WalletId)
/// 3. Tenant Service system organization configuration
/// </remarks>
public class WalletConfiguration
{
    /// <summary>
    /// The wallet ID that the Validator Service will use for signing operations.
    /// </summary>
    /// <remarks>
    /// This is typically a GUID identifying the wallet in the Wallet Service.
    /// Can be overridden by the VALIDATOR_WALLET_ID environment variable.
    /// </remarks>
    public required string WalletId { get; init; }

    /// <summary>
    /// The gRPC endpoint for the Wallet Service.
    /// </summary>
    /// <example>https://localhost:7084</example>
    public required Uri Endpoint { get; init; }

    /// <summary>
    /// Retry policy configuration for Wallet Service communication.
    /// </summary>
    /// <remarks>
    /// Controls exponential backoff retry behavior when Wallet Service calls fail
    /// due to transient network issues or temporary service unavailability.
    /// </remarks>
    public required RetryPolicyConfiguration RetryPolicy { get; init; }
}

/// <summary>
/// Configuration for retry policy with exponential backoff.
/// </summary>
/// <remarks>
/// Implements the retry strategy defined in FR-009:
/// - 3 max retries
/// - 2x backoff multiplier
/// - 1 second initial delay
/// - Total retry window up to 7 seconds (1s + 2s + 4s)
/// </remarks>
public class RetryPolicyConfiguration
{
    /// <summary>
    /// Maximum number of retry attempts for failed operations.
    /// </summary>
    /// <remarks>
    /// Default: 3 retries (total 4 attempts including initial call).
    /// Total time: 1s + 2s + 4s = 7 seconds.
    /// </remarks>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Exponential backoff multiplier for retry delays.
    /// </summary>
    /// <remarks>
    /// Default: 2.0 (each retry waits 2x longer than the previous).
    /// Delay formula: InitialDelaySeconds * (BackoffMultiplier ^ attemptNumber)
    /// </remarks>
    public double BackoffMultiplier { get; init; } = 2.0;

    /// <summary>
    /// Initial delay in seconds before the first retry attempt.
    /// </summary>
    /// <remarks>
    /// Default: 1 second.
    /// Subsequent delays: 2s (attempt 2), 4s (attempt 3), 8s (attempt 4).
    /// </remarks>
    public int InitialDelaySeconds { get; init; } = 1;
}
