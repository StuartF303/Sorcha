// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Validator.Core.Models;

namespace Sorcha.Validator.Core.Validators;

/// <summary>
/// Pure validator for docket structure, hash integrity, and chain continuity
/// This validator is stateless and can run in secure enclaves (Intel SGX, AMD SEV)
/// </summary>
public interface IDocketValidator
{
    /// <summary>
    /// Validates a docket's structure and basic integrity
    /// </summary>
    /// <param name="docket">Docket to validate</param>
    /// <returns>Validation result with errors if any</returns>
    ValidationResult ValidateDocketStructure(DocketData docket);

    /// <summary>
    /// Validates that the docket hash is correctly computed
    /// </summary>
    /// <param name="docket">Docket to validate</param>
    /// <param name="expectedHash">Expected docket hash</param>
    /// <returns>Validation result with errors if any</returns>
    ValidationResult ValidateDocketHash(DocketData docket, string expectedHash);

    /// <summary>
    /// Validates chain continuity between two dockets
    /// </summary>
    /// <param name="currentDocket">Current docket</param>
    /// <param name="previousDocket">Previous docket in chain</param>
    /// <returns>Validation result with errors if any</returns>
    ValidationResult ValidateChainContinuity(DocketData currentDocket, DocketData previousDocket);

    /// <summary>
    /// Validates a genesis docket (first docket in chain)
    /// </summary>
    /// <param name="docket">Genesis docket to validate</param>
    /// <returns>Validation result with errors if any</returns>
    ValidationResult ValidateGenesisDocket(DocketData docket);

    /// <summary>
    /// Computes the hash for a docket
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="docketNumber">Docket sequence number</param>
    /// <param name="previousHash">Hash of previous docket (null for genesis)</param>
    /// <param name="merkleRoot">Merkle root of transaction hashes</param>
    /// <param name="createdAt">Timestamp when docket was created</param>
    /// <returns>Computed docket hash</returns>
    string ComputeDocketHash(
        string registerId,
        long docketNumber,
        string? previousHash,
        string merkleRoot,
        DateTimeOffset createdAt);
}

/// <summary>
/// Pure data structure for docket validation (no service dependencies)
/// </summary>
public record DocketData
{
    public required string DocketId { get; init; }
    public required string RegisterId { get; init; }
    public required long DocketNumber { get; init; }
    public string? PreviousHash { get; init; }
    public required string DocketHash { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string MerkleRoot { get; init; }
    public required string ProposerValidatorId { get; init; }
    public int TransactionCount { get; init; }
}
