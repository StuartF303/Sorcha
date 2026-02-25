// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Validator.Service.Models;

/// <summary>
/// Request to initialize BLS threshold signing for a register's validator set.
/// </summary>
public class ThresholdSetupRequest
{
    /// <summary>Target register ID.</summary>
    public required string RegisterId { get; set; }

    /// <summary>Minimum number of validators required to sign (t).</summary>
    public required uint Threshold { get; set; }

    /// <summary>Total number of validators in the threshold scheme (n).</summary>
    public required uint TotalValidators { get; set; }

    /// <summary>Validator IDs participating in the threshold scheme.</summary>
    public required string[] ValidatorIds { get; set; }
}

/// <summary>
/// Response from threshold setup.
/// </summary>
public class ThresholdSetupResponse
{
    /// <summary>Register ID.</summary>
    public required string RegisterId { get; set; }

    /// <summary>Group public key (base64 encoded, 96 bytes).</summary>
    public required string GroupPublicKey { get; set; }

    /// <summary>Threshold parameter (t).</summary>
    public required uint Threshold { get; set; }

    /// <summary>Total validators (n).</summary>
    public required uint TotalValidators { get; set; }

    /// <summary>Status message.</summary>
    public required string Status { get; set; }
}

/// <summary>
/// Request to submit a partial BLS signature for a docket.
/// </summary>
public class ThresholdSignRequest
{
    /// <summary>Register ID containing the docket.</summary>
    public required string RegisterId { get; set; }

    /// <summary>Docket ID/hash being signed.</summary>
    public required string DocketHash { get; set; }

    /// <summary>Validator's share index in the threshold scheme.</summary>
    public required uint ShareIndex { get; set; }

    /// <summary>Base64-encoded partial BLS signature.</summary>
    public required string PartialSignature { get; set; }

    /// <summary>Validator ID submitting the share.</summary>
    public required string ValidatorId { get; set; }
}

/// <summary>
/// Response from submitting a threshold signature share.
/// </summary>
public class ThresholdSignResponse
{
    /// <summary>Docket hash that was partially signed.</summary>
    public required string DocketHash { get; set; }

    /// <summary>Current number of collected shares.</summary>
    public required int CollectedShares { get; set; }

    /// <summary>Required threshold.</summary>
    public required uint Threshold { get; set; }

    /// <summary>Whether threshold has been met and aggregate signature is ready.</summary>
    public required bool ThresholdMet { get; set; }

    /// <summary>Base64-encoded aggregate signature (only present when threshold is met).</summary>
    public string? AggregateSignature { get; set; }
}
