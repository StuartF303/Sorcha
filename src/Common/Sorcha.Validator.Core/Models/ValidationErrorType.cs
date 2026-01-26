// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Validator.Core.Models;

/// <summary>
/// Enumeration of validation error types for the Validator Service
/// </summary>
public enum ValidationErrorType
{
    // Schema Errors (100-199)
    SchemaMismatch = 100,
    MissingRequiredField = 101,
    InvalidDataType = 102,
    InvalidFormat = 103,
    ValueOutOfRange = 104,

    // Logic Errors (200-299)
    ConditionEvaluationFailed = 200,
    InvalidNextAction = 201,
    InvalidConditionSyntax = 202,

    // Authorization Errors (300-399)
    UnauthorizedSender = 300,
    InvalidParticipant = 301,
    SignatureVerificationFailed = 302,
    InvalidSignatureAlgorithm = 303,
    ExpiredSignature = 304,

    // Workflow Errors (400-499)
    BlueprintNotFound = 400,
    ActionNotFound = 401,
    InvalidActionSequence = 402,
    BlueprintVersionMismatch = 403,
    InstanceNotFound = 404,

    // Chain Validation Errors (500-599)
    InvalidPreviousId = 500,
    BrokenChain = 501,
    PreviousDataMismatch = 502,
    InvalidBlueprintVersion = 503,
    InvalidChainBranch = 504,
    ChainMergeDetected = 505,
    GenesisViolation = 506,
    OrphanedTransaction = 507,
    InvalidTimestamp = 508,
    MissingBlueprintId = 509,

    // Docket Errors (600-699)
    InvalidDocketStructure = 600,
    InvalidMerkleRoot = 601,
    InvalidDocketHash = 602,
    InvalidDocketSequence = 603,
    DocketChainBroken = 604,
    EmptyDocket = 605,
    DocketTooLarge = 606,

    // Consensus Errors (700-799)
    InvalidInitiatorSignature = 700,
    InvalidValidatorSignature = 701,
    ThresholdNotMet = 702,
    DocketTimeout = 703,
    InvalidTerm = 704,
    NotLeader = 705,
    LeaderMismatch = 706,
    DuplicateVote = 707,

    // System Errors (900-999)
    ValidationTimeout = 900,
    ServiceUnavailable = 901,
    InternalError = 902,
    ConfigurationError = 903
}

/// <summary>
/// Extension methods for ValidationErrorType
/// </summary>
public static class ValidationErrorTypeExtensions
{
    /// <summary>
    /// Converts an error type to a string code for use in ValidationError
    /// </summary>
    public static string ToCode(this ValidationErrorType errorType) =>
        $"VAL_{(int)errorType:D3}";

    /// <summary>
    /// Gets a default message for an error type
    /// </summary>
    public static string GetDefaultMessage(this ValidationErrorType errorType) => errorType switch
    {
        ValidationErrorType.SchemaMismatch => "Data does not match expected schema",
        ValidationErrorType.MissingRequiredField => "Required field is missing",
        ValidationErrorType.InvalidDataType => "Invalid data type",
        ValidationErrorType.UnauthorizedSender => "Sender is not authorized for this action",
        ValidationErrorType.SignatureVerificationFailed => "Signature verification failed",
        ValidationErrorType.BlueprintNotFound => "Blueprint not found",
        ValidationErrorType.ActionNotFound => "Action not found in blueprint",
        ValidationErrorType.InvalidPreviousId => "Invalid previous transaction reference",
        ValidationErrorType.BrokenChain => "Transaction chain is broken",
        ValidationErrorType.InvalidMerkleRoot => "Merkle root verification failed",
        ValidationErrorType.InvalidInitiatorSignature => "Docket initiator signature is invalid",
        ValidationErrorType.ThresholdNotMet => "Consensus signature threshold not met",
        ValidationErrorType.DocketTimeout => "Docket consensus timed out",
        ValidationErrorType.NotLeader => "This validator is not the current leader",
        _ => errorType.ToString()
    };

    /// <summary>
    /// Creates a ValidationError from an error type
    /// </summary>
    public static ValidationError ToValidationError(
        this ValidationErrorType errorType,
        string? message = null,
        string? field = null,
        Dictionary<string, object>? details = null) => new()
    {
        Code = errorType.ToCode(),
        Message = message ?? errorType.GetDefaultMessage(),
        Field = field,
        Details = details
    };
}
