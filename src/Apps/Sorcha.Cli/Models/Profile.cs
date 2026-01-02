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
    /// Base URL for all Sorcha services (e.g., "http://localhost" or "https://api.sorcha.io").
    /// Individual service URLs are derived from this unless explicitly overridden.
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Base URL for the Tenant Service API.
    /// If not specified, derived from ServiceUrl.
    /// </summary>
    public string? TenantServiceUrl { get; set; }

    /// <summary>
    /// Base URL for the Register Service API.
    /// If not specified, derived from ServiceUrl.
    /// </summary>
    public string? RegisterServiceUrl { get; set; }

    /// <summary>
    /// Base URL for the Peer Service API.
    /// If not specified, derived from ServiceUrl.
    /// </summary>
    public string? PeerServiceUrl { get; set; }

    /// <summary>
    /// Base URL for the Wallet Service API.
    /// If not specified, derived from ServiceUrl.
    /// </summary>
    public string? WalletServiceUrl { get; set; }

    /// <summary>
    /// OAuth2 token endpoint for authentication.
    /// If not specified, derived from ServiceUrl as {ServiceUrl}/api/service-auth/token.
    /// </summary>
    public string? AuthTokenUrl { get; set; }

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

    /// <summary>
    /// Gets the effective Tenant Service URL, deriving from ServiceUrl if not explicitly set.
    /// </summary>
    public string GetTenantServiceUrl() => TenantServiceUrl ?? ServiceUrl ?? string.Empty;

    /// <summary>
    /// Gets the effective Register Service URL, deriving from ServiceUrl if not explicitly set.
    /// </summary>
    public string GetRegisterServiceUrl() => RegisterServiceUrl ?? ServiceUrl ?? string.Empty;

    /// <summary>
    /// Gets the effective Peer Service URL, deriving from ServiceUrl if not explicitly set.
    /// </summary>
    public string GetPeerServiceUrl() => PeerServiceUrl ?? ServiceUrl ?? string.Empty;

    /// <summary>
    /// Gets the effective Wallet Service URL, deriving from ServiceUrl if not explicitly set.
    /// </summary>
    public string GetWalletServiceUrl() => WalletServiceUrl ?? ServiceUrl ?? string.Empty;

    /// <summary>
    /// Gets the effective Auth Token URL, deriving from ServiceUrl if not explicitly set.
    /// </summary>
    public string GetAuthTokenUrl() => AuthTokenUrl ?? (string.IsNullOrEmpty(ServiceUrl) ? string.Empty : $"{ServiceUrl}/api/service-auth/token");
}
