// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Tenant.Service.Models.Dtos;

/// <summary>
/// Request to create a new organization.
/// </summary>
public record CreateOrganizationRequest
{
    /// <summary>
    /// Organization display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Unique subdomain (3-50 alphanumeric characters with hyphens).
    /// </summary>
    public required string Subdomain { get; init; }

    /// <summary>
    /// Optional branding configuration.
    /// </summary>
    public BrandingConfigurationDto? Branding { get; init; }
}

/// <summary>
/// Request to update an existing organization.
/// </summary>
public record UpdateOrganizationRequest
{
    /// <summary>
    /// Updated organization name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Updated organization status.
    /// </summary>
    public OrganizationStatus? Status { get; init; }

    /// <summary>
    /// Updated branding configuration.
    /// </summary>
    public BrandingConfigurationDto? Branding { get; init; }
}

/// <summary>
/// Branding configuration DTO.
/// </summary>
public record BrandingConfigurationDto
{
    /// <summary>
    /// URL to organization logo (HTTPS required).
    /// </summary>
    public string? LogoUrl { get; init; }

    /// <summary>
    /// Primary brand color (hex format).
    /// </summary>
    public string? PrimaryColor { get; init; }

    /// <summary>
    /// Secondary brand color (hex format).
    /// </summary>
    public string? SecondaryColor { get; init; }

    /// <summary>
    /// Company tagline.
    /// </summary>
    public string? CompanyTagline { get; init; }
}

/// <summary>
/// Organization response DTO.
/// </summary>
public record OrganizationResponse
{
    /// <summary>
    /// Organization ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Organization name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Organization subdomain.
    /// </summary>
    public string Subdomain { get; init; } = string.Empty;

    /// <summary>
    /// Organization status.
    /// </summary>
    public OrganizationStatus Status { get; init; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Branding configuration.
    /// </summary>
    public BrandingConfigurationDto? Branding { get; init; }

    /// <summary>
    /// Creates a response from an Organization entity.
    /// </summary>
    public static OrganizationResponse FromEntity(Organization org) => new()
    {
        Id = org.Id,
        Name = org.Name,
        Subdomain = org.Subdomain,
        Status = org.Status,
        CreatedAt = org.CreatedAt,
        Branding = org.Branding != null ? new BrandingConfigurationDto
        {
            LogoUrl = org.Branding.LogoUrl,
            PrimaryColor = org.Branding.PrimaryColor,
            SecondaryColor = org.Branding.SecondaryColor,
            CompanyTagline = org.Branding.CompanyTagline
        } : null
    };
}

/// <summary>
/// Organization list response with pagination.
/// </summary>
public record OrganizationListResponse
{
    /// <summary>
    /// List of organizations.
    /// </summary>
    public IReadOnlyList<OrganizationResponse> Organizations { get; init; } = [];

    /// <summary>
    /// Total count of organizations.
    /// </summary>
    public int TotalCount { get; init; }
}
