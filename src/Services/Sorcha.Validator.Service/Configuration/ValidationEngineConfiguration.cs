// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Validator.Service.Configuration;

/// <summary>
/// Configuration for the Validation Engine
/// </summary>
public class ValidationEngineConfiguration
{
    /// <summary>
    /// Configuration section name in appsettings
    /// </summary>
    public const string SectionName = "ValidationEngine";

    /// <summary>
    /// Maximum transactions to validate in a single batch
    /// </summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// Interval between validation batch runs
    /// </summary>
    public TimeSpan ValidationInterval { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Maximum concurrent validations per register
    /// </summary>
    public int MaxConcurrentValidationsPerRegister { get; set; } = 10;

    /// <summary>
    /// Enable parallel validation within a batch
    /// </summary>
    public bool EnableParallelValidation { get; set; } = true;

    /// <summary>
    /// Maximum retries for failed validations (transient errors)
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Timeout for individual transaction validation
    /// </summary>
    public TimeSpan ValidationTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Enable schema validation against blueprint
    /// </summary>
    public bool EnableSchemaValidation { get; set; } = true;

    /// <summary>
    /// Enable cryptographic signature verification
    /// </summary>
    public bool EnableSignatureVerification { get; set; } = true;

    /// <summary>
    /// Enable chain validation (previousId linking)
    /// </summary>
    public bool EnableChainValidation { get; set; } = true;

    /// <summary>
    /// Maximum allowed clock skew for transaction timestamps
    /// </summary>
    public TimeSpan MaxClockSkew { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Whether to enable validation metrics collection
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Maximum age of a transaction before rejection
    /// </summary>
    public TimeSpan MaxTransactionAge { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Enable governance rights enforcement for Control transactions.
    /// When enabled, Control transactions are validated against the register's
    /// admin roster to ensure the submitter has the required role.
    /// </summary>
    public bool EnableGovernanceValidation { get; set; } = true;

    /// <summary>
    /// Enable blueprint conformance validation (sender authorization,
    /// starting action validation, action sequencing via routes).
    /// </summary>
    public bool EnableBlueprintConformance { get; set; } = true;
}
