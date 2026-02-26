// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json.Serialization;

namespace Sorcha.Cli.Models;

/// <summary>
/// Validator service status.
/// </summary>
public class ValidatorStatus
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("isRunning")]
    public bool IsRunning { get; set; }

    [JsonPropertyName("registersMonitored")]
    public int RegistersMonitored { get; set; }

    [JsonPropertyName("totalValidations")]
    public long TotalValidations { get; set; }

    [JsonPropertyName("failedValidations")]
    public long FailedValidations { get; set; }

    [JsonPropertyName("lastValidationAt")]
    public DateTimeOffset? LastValidationAt { get; set; }

    [JsonPropertyName("uptime")]
    public string Uptime { get; set; } = string.Empty;

    [JsonPropertyName("consensusProtocol")]
    public string ConsensusProtocol { get; set; } = string.Empty;
}

/// <summary>
/// Validator processing result.
/// </summary>
public class ValidatorProcessResult
{
    [JsonPropertyName("registerId")]
    public string RegisterId { get; set; } = string.Empty;

    [JsonPropertyName("transactionsProcessed")]
    public int TransactionsProcessed { get; set; }

    [JsonPropertyName("transactionsValidated")]
    public int TransactionsValidated { get; set; }

    [JsonPropertyName("transactionsRejected")]
    public int TransactionsRejected { get; set; }

    [JsonPropertyName("processedAt")]
    public DateTimeOffset ProcessedAt { get; set; }
}

/// <summary>
/// Integrity check result.
/// </summary>
public class IntegrityCheckResult
{
    [JsonPropertyName("registerId")]
    public string RegisterId { get; set; } = string.Empty;

    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("chainLength")]
    public long ChainLength { get; set; }

    [JsonPropertyName("checkedAt")]
    public DateTimeOffset CheckedAt { get; set; }

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();

    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Validator start/stop response.
/// </summary>
public class ValidatorActionResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
