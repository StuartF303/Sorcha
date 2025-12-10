namespace Sorcha.Cli.Models;

/// <summary>
/// Represents a service principal (application identity) in the Sorcha platform.
/// </summary>
public class ServicePrincipal
{
    /// <summary>
    /// Unique identifier (client ID) for the service principal.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Organization ID the service principal belongs to.
    /// </summary>
    public string OrganizationId { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the service principal.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the service principal's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the service principal is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Scopes/permissions granted to this service principal.
    /// </summary>
    public List<string> Scopes { get; set; } = new();

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// When the client secret was last rotated.
    /// </summary>
    public DateTimeOffset? SecretRotatedAt { get; set; }
}

/// <summary>
/// Request to create a new service principal.
/// </summary>
public class CreateServicePrincipalRequest
{
    /// <summary>
    /// Display name for the service principal.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the service principal's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Scopes/permissions to grant.
    /// </summary>
    public List<string>? Scopes { get; set; }
}

/// <summary>
/// Response when creating a service principal (includes the client secret).
/// </summary>
public class CreateServicePrincipalResponse
{
    /// <summary>
    /// The created service principal.
    /// </summary>
    public ServicePrincipal ServicePrincipal { get; set; } = null!;

    /// <summary>
    /// Client secret (only returned on creation, cannot be retrieved later).
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;
}

/// <summary>
/// Response when rotating a service principal secret.
/// </summary>
public class RotateSecretResponse
{
    /// <summary>
    /// New client secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// When the secret was rotated.
    /// </summary>
    public DateTimeOffset RotatedAt { get; set; }
}
