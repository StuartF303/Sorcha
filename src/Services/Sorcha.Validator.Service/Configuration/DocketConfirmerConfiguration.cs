// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Validator.Service.Configuration;

/// <summary>
/// Configuration for the docket confirmer service.
/// </summary>
public class DocketConfirmerConfiguration
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "DocketConfirmer";

    /// <summary>
    /// Maximum time allowed for validating a docket (default: 30 seconds)
    /// </summary>
    public TimeSpan ValidationTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether to validate transactions in parallel (default: true)
    /// </summary>
    public bool EnableParallelValidation { get; set; } = true;

    /// <summary>
    /// Maximum concurrent transaction validations (default: 10)
    /// </summary>
    public int MaxConcurrentValidations { get; set; } = 10;

    /// <summary>
    /// Whether to verify the initiator's signature (default: true)
    /// </summary>
    public bool VerifyInitiatorSignature { get; set; } = true;

    /// <summary>
    /// Whether to verify the Merkle root (default: true)
    /// </summary>
    public bool VerifyMerkleRoot { get; set; } = true;

    /// <summary>
    /// Whether to verify the docket hash (default: true)
    /// </summary>
    public bool VerifyDocketHash { get; set; } = true;

    /// <summary>
    /// Maximum allowed clock skew for docket timestamps (default: 5 minutes)
    /// </summary>
    public TimeSpan MaxClockSkew { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Whether to log detailed rejection information (default: true)
    /// </summary>
    public bool LogDetailedRejections { get; set; } = true;

    /// <summary>
    /// Maximum age of a docket to accept for confirmation (default: 10 minutes)
    /// </summary>
    public TimeSpan MaxDocketAge { get; set; } = TimeSpan.FromMinutes(10);
}
