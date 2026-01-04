// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sorcha.Register.Models;

/// <summary>
/// Request to initiate register creation (Phase 1)
/// </summary>
public class InitiateRegisterCreationRequest
{
    /// <summary>
    /// Human-readable register name
    /// </summary>
    [Required]
    [StringLength(38, MinimumLength = 1)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Purpose and scope of the register
    /// </summary>
    [StringLength(500)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Owning tenant/organization identifier
    /// </summary>
    [Required]
    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Creator information
    /// </summary>
    [Required]
    [JsonPropertyName("creator")]
    public CreatorInfo Creator { get; set; } = new();

    /// <summary>
    /// Additional administrators to grant access
    /// </summary>
    [JsonPropertyName("additionalAdmins")]
    public List<AdditionalAdminInfo>? AdditionalAdmins { get; set; }

    /// <summary>
    /// Additional register metadata
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Creator information for register initialization
/// </summary>
public class CreatorInfo
{
    /// <summary>
    /// User identifier
    /// </summary>
    [Required]
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Wallet identifier for signing
    /// </summary>
    [Required]
    [JsonPropertyName("walletId")]
    public string WalletId { get; set; } = string.Empty;
}

/// <summary>
/// Additional administrator information
/// </summary>
public class AdditionalAdminInfo
{
    /// <summary>
    /// User identifier
    /// </summary>
    [Required]
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Wallet identifier for signing
    /// </summary>
    [Required]
    [JsonPropertyName("walletId")]
    public string WalletId { get; set; } = string.Empty;

    /// <summary>
    /// Role to grant (defaults to Admin)
    /// </summary>
    [JsonPropertyName("role")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RegisterRole Role { get; set; } = RegisterRole.Admin;
}

/// <summary>
/// Response from register initiation (Phase 1)
/// </summary>
public class InitiateRegisterCreationResponse
{
    /// <summary>
    /// Generated register identifier
    /// </summary>
    [JsonPropertyName("registerId")]
    public string RegisterId { get; set; } = string.Empty;

    /// <summary>
    /// Control record template to sign
    /// </summary>
    [JsonPropertyName("controlRecord")]
    public RegisterControlRecord ControlRecord { get; set; } = new();

    /// <summary>
    /// SHA-256 hash of control record to sign
    /// </summary>
    [JsonPropertyName("dataToSign")]
    public string DataToSign { get; set; } = string.Empty;

    /// <summary>
    /// Expiration time for this pending registration
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Nonce for replay protection
    /// </summary>
    [JsonPropertyName("nonce")]
    public string Nonce { get; set; } = string.Empty;
}

/// <summary>
/// Request to finalize register creation (Phase 2)
/// </summary>
public class FinalizeRegisterCreationRequest
{
    /// <summary>
    /// Register identifier from initiation phase
    /// </summary>
    [Required]
    [StringLength(32, MinimumLength = 32)]
    [RegularExpression("^[a-f0-9]{32}$")]
    [JsonPropertyName("registerId")]
    public string RegisterId { get; set; } = string.Empty;

    /// <summary>
    /// Nonce from initiation (replay protection)
    /// </summary>
    [Required]
    [JsonPropertyName("nonce")]
    public string Nonce { get; set; } = string.Empty;

    /// <summary>
    /// Control record with signed attestations
    /// </summary>
    [Required]
    [JsonPropertyName("controlRecord")]
    public RegisterControlRecord ControlRecord { get; set; } = new();
}

/// <summary>
/// Response from register finalization (Phase 2)
/// </summary>
public class FinalizeRegisterCreationResponse
{
    /// <summary>
    /// Register identifier
    /// </summary>
    [JsonPropertyName("registerId")]
    public string RegisterId { get; set; } = string.Empty;

    /// <summary>
    /// Creation status
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "created";

    /// <summary>
    /// Genesis transaction identifier
    /// </summary>
    [JsonPropertyName("genesisTransactionId")]
    public string GenesisTransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Genesis docket identifier (always "0")
    /// </summary>
    [JsonPropertyName("genesisDocketId")]
    public string GenesisDocketId { get; set; } = "0";

    /// <summary>
    /// When the register was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Pending register registration (stored temporarily during two-phase creation)
/// </summary>
public class PendingRegistration
{
    /// <summary>
    /// Generated register identifier
    /// </summary>
    public string RegisterId { get; set; } = string.Empty;

    /// <summary>
    /// Control record template
    /// </summary>
    public RegisterControlRecord ControlRecord { get; set; } = new();

    /// <summary>
    /// SHA-256 hash of canonical control record JSON
    /// </summary>
    public string ControlRecordHash { get; set; } = string.Empty;

    /// <summary>
    /// When this pending registration was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When this pending registration expires (5 minutes from creation)
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Nonce for replay protection
    /// </summary>
    public string Nonce { get; set; } = string.Empty;

    /// <summary>
    /// Checks if this pending registration has expired
    /// </summary>
    public bool IsExpired() => DateTimeOffset.UtcNow > ExpiresAt;
}
