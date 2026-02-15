// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json.Serialization;

namespace Sorcha.Tenant.Service.Models;

/// <summary>
/// Represents a tenant organization using the Sorcha platform.
/// Each organization has its own identity provider configuration and can manage users.
/// </summary>
public class Organization
{
    /// <summary>
    /// Unique organization identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Organization display name (e.g., "Acme Corporation").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique subdomain for organization-specific URLs (e.g., "acme" for acme.sorcha.io).
    /// Must be 3-50 characters, alphanumeric + hyphens only.
    /// </summary>
    public string Subdomain { get; set; } = string.Empty;

    /// <summary>
    /// Organization status (Active, Suspended, Deleted).
    /// </summary>
    public OrganizationStatus Status { get; set; } = OrganizationStatus.Active;

    /// <summary>
    /// User who created this organization. Automatically gets Administrator role.
    /// </summary>
    public Guid? CreatorIdentityId { get; set; }

    /// <summary>
    /// Organization creation timestamp (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Optional branding configuration for custom organization UI.
    /// </summary>
    public BrandingConfiguration? Branding { get; set; }

    /// <summary>
    /// Navigation property to identity provider configuration.
    /// Each organization can have one external IDP configured.
    /// </summary>
    public IdentityProviderConfiguration? IdentityProvider { get; set; }
}

/// <summary>
/// Organization lifecycle status.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrganizationStatus
{
    /// <summary>
    /// Organization is active and operational.
    /// </summary>
    Active,

    /// <summary>
    /// Organization is temporarily suspended (billing issue, policy violation, etc.).
    /// Users cannot authenticate while suspended.
    /// </summary>
    Suspended,

    /// <summary>
    /// Organization is soft-deleted. Schema and data retained for 30 days, then purged.
    /// </summary>
    Deleted
}

/// <summary>
/// Customizable branding for organization-specific UI.
/// All fields are optional.
/// </summary>
public class BrandingConfiguration
{
    /// <summary>
    /// URL to organization logo image. Must be HTTPS if provided.
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Primary brand color (hex format, e.g., "#0078D4").
    /// </summary>
    public string? PrimaryColor { get; set; }

    /// <summary>
    /// Secondary brand color (hex format, e.g., "#50E6FF").
    /// </summary>
    public string? SecondaryColor { get; set; }

    /// <summary>
    /// Company tagline or motto.
    /// </summary>
    public string? CompanyTagline { get; set; }
}
