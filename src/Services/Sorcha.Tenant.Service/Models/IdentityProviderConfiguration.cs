// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json.Serialization;

namespace Sorcha.Tenant.Service.Models;

/// <summary>
/// External identity provider configuration for an organization.
/// Supports Azure Entra ID, AWS Cognito, and generic OIDC providers.
/// </summary>
public class IdentityProviderConfiguration
{
    /// <summary>
    /// Unique configuration identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Associated organization ID.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Type of identity provider.
    /// </summary>
    public IdentityProviderType ProviderType { get; set; }

    /// <summary>
    /// OIDC issuer URL (e.g., https://login.microsoftonline.com/{tenant-id}/v2.0).
    /// </summary>
    public string IssuerUrl { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 client ID.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// AES-256-GCM encrypted client secret.
    /// Encrypted using Sorcha.Cryptography library.
    /// </summary>
    public byte[] ClientSecretEncrypted { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// OAuth2 scopes (e.g., openid, profile, email).
    /// Must include at least "openid" scope.
    /// </summary>
    public string[] Scopes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Optional override for authorization endpoint (non-standard IDPs).
    /// </summary>
    public string? AuthorizationEndpoint { get; set; }

    /// <summary>
    /// Optional override for token endpoint (non-standard IDPs).
    /// </summary>
    public string? TokenEndpoint { get; set; }

    /// <summary>
    /// OIDC discovery URL (/.well-known/openid-configuration).
    /// Used to auto-discover endpoints if not manually specified.
    /// </summary>
    public string? MetadataUrl { get; set; }

    /// <summary>
    /// Configuration creation timestamp (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Last update timestamp (UTC).
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Navigation property to owning organization.
    /// </summary>
    public Organization Organization { get; set; } = null!;
}

/// <summary>
/// Supported external identity provider types.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IdentityProviderType
{
    /// <summary>
    /// Microsoft Azure Entra ID (formerly Azure AD).
    /// </summary>
    AzureEntra,

    /// <summary>
    /// Amazon Web Services Cognito.
    /// </summary>
    AwsCognito,

    /// <summary>
    /// Generic OpenID Connect compliant provider.
    /// </summary>
    GenericOidc
}
