namespace Sorcha.Admin.Models.Configuration;

/// <summary>
/// Environment profile configuration.
/// Defines API endpoints and settings for a specific deployment environment
/// (e.g., dev, staging, production).
/// This model is identical to the CLI Profile structure for consistency.
/// </summary>
public class Profile
{
    /// <summary>
    /// Unique profile identifier (e.g., "dev", "staging", "production").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for the Tenant Service.
    /// Example: https://localhost:7080 or https://tenant.sorcha.io
    /// </summary>
    public string TenantServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for the Register Service.
    /// Example: https://localhost:7081 or https://register.sorcha.io
    /// </summary>
    public string RegisterServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for the Peer Service.
    /// Example: https://localhost:7082 or https://peer.sorcha.io
    /// </summary>
    public string PeerServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for the Wallet Service.
    /// Example: https://localhost:7083 or https://wallet.sorcha.io
    /// </summary>
    public string WalletServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for the Blueprint Service.
    /// Example: https://localhost:7084 or https://blueprint.sorcha.io
    /// </summary>
    public string BlueprintServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 token endpoint URL for authentication.
    /// Example: https://localhost:7080/api/service-auth/token
    /// </summary>
    public string AuthTokenUrl { get; set; } = string.Empty;

    /// <summary>
    /// Default OAuth2 client ID for this environment.
    /// Typically "sorcha-admin" for the Admin UI.
    /// </summary>
    public string? DefaultClientId { get; set; }

    /// <summary>
    /// Whether to verify SSL certificates for this profile.
    /// Should be false for local development (self-signed certs), true for production.
    /// </summary>
    public bool VerifySsl { get; set; } = true;

    /// <summary>
    /// HTTP request timeout in seconds.
    /// Default is 30 seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Custom settings dictionary for extensibility.
    /// Can store profile-specific configuration key-value pairs.
    /// </summary>
    public Dictionary<string, string> CustomSettings { get; set; } = new();
}
