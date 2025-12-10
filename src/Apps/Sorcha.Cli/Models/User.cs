namespace Sorcha.Cli.Models;

/// <summary>
/// Represents a user in the Sorcha platform.
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier for the user.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Organization ID the user belongs to.
    /// </summary>
    public string OrganizationId { get; set; } = string.Empty;

    /// <summary>
    /// Username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// First name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Whether the user is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// User roles.
    /// </summary>
    public List<string> Roles { get; set; } = new();

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
/// Request to create a new user.
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// Username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// First name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// User roles.
    /// </summary>
    public List<string>? Roles { get; set; }
}

/// <summary>
/// Request to update a user.
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// Email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// First name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Whether the user is active.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// User roles.
    /// </summary>
    public List<string>? Roles { get; set; }
}
