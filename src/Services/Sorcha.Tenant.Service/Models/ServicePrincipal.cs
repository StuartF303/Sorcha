// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json.Serialization;

namespace Sorcha.Tenant.Service.Models;

/// <summary>
/// Internal Sorcha service identity for service-to-service authentication.
/// Used by Blueprint Service, Wallet Service, Register Service, etc.
/// </summary>
public class ServicePrincipal
{
    /// <summary>
    /// Unique service identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Service name (e.g., "Blueprint", "Wallet", "Register", "Peer").
    /// Must be unique.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 client ID for service authentication.
    /// Format: "service-{servicename}" (e.g., "service-blueprint").
    /// Must be unique.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// AES-256-GCM encrypted client secret.
    /// Encrypted using Sorcha.Cryptography library.
    /// </summary>
    public byte[] ClientSecretEncrypted { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Allowed OAuth2 scopes for this service.
    /// Examples: "wallet:sign", "register:commit", "register:read", "tenant:validate".
    /// </summary>
    public string[] Scopes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Service principal status (Active, Suspended, Revoked).
    /// </summary>
    public ServicePrincipalStatus Status { get; set; } = ServicePrincipalStatus.Active;

    /// <summary>
    /// Service registration timestamp (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Service principal status.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ServicePrincipalStatus
{
    /// <summary>
    /// Service principal is active and can authenticate.
    /// </summary>
    Active,

    /// <summary>
    /// Service principal is temporarily suspended (cannot authenticate).
    /// </summary>
    Suspended,

    /// <summary>
    /// Service principal is permanently revoked (cannot be reactivated).
    /// </summary>
    Revoked
}
