namespace Sorcha.Cli.Models;

/// <summary>
/// Represents an organization (tenant) in the Sorcha platform.
/// </summary>
public class Organization
{
    /// <summary>
    /// Unique identifier for the organization.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Organization name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Subdomain for the organization.
    /// </summary>
    public string? Subdomain { get; set; }

    /// <summary>
    /// Organization description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the organization is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>
/// Request to create a new organization.
/// </summary>
public class CreateOrganizationRequest
{
    /// <summary>
    /// Organization name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Subdomain for the organization.
    /// </summary>
    public string? Subdomain { get; set; }

    /// <summary>
    /// Organization description.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Request to update an organization.
/// </summary>
public class UpdateOrganizationRequest
{
    /// <summary>
    /// Organization name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Organization description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the organization is active.
    /// </summary>
    public bool? IsActive { get; set; }
}
