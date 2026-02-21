// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.UI.Core.Models.Admin;

/// <summary>
/// View model for organization configuration settings.
/// </summary>
public class OrganizationConfigurationViewModel
{
    public SecurityPoliciesViewModel SecurityPolicies { get; set; } = new();
    public List<ExternalIdpViewModel> ExternalProviders { get; set; } = [];
    public BrandingConfigViewModel Branding { get; set; } = new();
}

/// <summary>
/// Security policy settings (UI stubs — not yet persisted on backend).
/// </summary>
public class SecurityPoliciesViewModel
{
    public bool Enforce2Fa { get; set; }
    public int MinPasswordLength { get; set; } = 8;
    public int SessionTimeoutMinutes { get; set; } = 60;
}

/// <summary>
/// External identity provider entry (placeholder).
/// </summary>
public class ExternalIdpViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Protocol { get; set; } = "OIDC";
    public string Status { get; set; } = "Active";
}

/// <summary>
/// Branding configuration (backed by existing BrandingDto API).
/// </summary>
public class BrandingConfigViewModel
{
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? CompanyTagline { get; set; }
}
