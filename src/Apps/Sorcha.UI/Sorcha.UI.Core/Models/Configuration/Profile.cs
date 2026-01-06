namespace Sorcha.UI.Core.Models.Configuration;

/// <summary>
/// User-defined profile for connecting to different backend environments
/// </summary>
public sealed record Profile
{
    /// <summary>
    /// Profile name (unique identifier)
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// API Gateway base URL (e.g., https://localhost:7082)
    /// </summary>
    public string ApiGatewayUrl { get; init; } = string.Empty;

    /// <summary>
    /// Display description for the profile
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Profile creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Last modified timestamp
    /// </summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates if this is a system-defined profile (cannot be deleted)
    /// </summary>
    public bool IsSystemProfile { get; init; }

    /// <summary>
    /// Validates the profile configuration
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name) &&
               !string.IsNullOrWhiteSpace(ApiGatewayUrl) &&
               Uri.TryCreate(ApiGatewayUrl, UriKind.Absolute, out _);
    }

    /// <summary>
    /// Creates a new profile with updated timestamp
    /// </summary>
    public Profile WithUpdatedTimestamp()
    {
        return this with { UpdatedAt = DateTime.UtcNow };
    }
}
