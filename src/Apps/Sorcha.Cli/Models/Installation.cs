namespace Sorcha.Cli.Models;

/// <summary>
/// Represents a Sorcha installation record created during bootstrap.
/// Tracks the organization, admin user, and other metadata for a bootstrapped instance.
/// </summary>
public class Installation
{
    /// <summary>
    /// Unique name for this installation (e.g., "local-dev", "docker-demo", "staging").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Profile name used for this installation.
    /// </summary>
    public string ProfileName { get; set; } = string.Empty;

    /// <summary>
    /// Organization ID created during bootstrap.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Organization name.
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// Organization subdomain.
    /// </summary>
    public string OrganizationSubdomain { get; set; } = string.Empty;

    /// <summary>
    /// Admin user ID created during bootstrap.
    /// </summary>
    public Guid AdminUserId { get; set; }

    /// <summary>
    /// Admin email address.
    /// </summary>
    public string AdminEmail { get; set; } = string.Empty;

    /// <summary>
    /// Service principal client ID (if created during bootstrap).
    /// </summary>
    public Guid? ServicePrincipalId { get; set; }

    /// <summary>
    /// Service principal client ID string (if created during bootstrap).
    /// </summary>
    public string? ServicePrincipalClientId { get; set; }

    /// <summary>
    /// Date and time this installation was bootstrapped.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Bootstrap script version or CLI version used.
    /// </summary>
    public string? BootstrapVersion { get; set; }

    /// <summary>
    /// Additional notes or metadata about this installation.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
