namespace Sorcha.Cli.Models;

/// <summary>
/// Represents a configuration profile for connecting to Sorcha services.
/// </summary>
public class Profile
{
    /// <summary>
    /// Profile name (e.g., "dev", "staging", "production").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for the Tenant Service API.
    /// </summary>
    public string TenantServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for the Register Service API.
    /// </summary>
    public string RegisterServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for the Peer Service API.
    /// </summary>
    public string PeerServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for the Wallet Service API.
    /// </summary>
    public string WalletServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 token endpoint for authentication.
    /// </summary>
    public string AuthTokenUrl { get; set; } = string.Empty;

    /// <summary>
    /// Default client ID for user authentication.
    /// </summary>
    public string? DefaultClientId { get; set; }

    /// <summary>
    /// Whether to verify SSL certificates (disable for local dev).
    /// </summary>
    public bool VerifySsl { get; set; } = true;

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Additional custom settings for this profile.
    /// </summary>
    public Dictionary<string, string> CustomSettings { get; set; } = new();
}
