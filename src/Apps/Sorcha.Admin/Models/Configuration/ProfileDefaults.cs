namespace Sorcha.Admin.Models.Configuration;

/// <summary>
/// Factory class for creating default profile configurations.
/// Provides 3 standardized environments: local (Aspire), docker, and production.
/// Port configuration is consistent across environments for easier development and deployment.
/// </summary>
public static class ProfileDefaults
{
    /// <summary>
    /// Gets the default set of profiles for Sorcha Admin.
    /// </summary>
    /// <returns>Dictionary of default profiles keyed by profile name.</returns>
    public static Dictionary<string, Profile> GetDefaultProfiles()
    {
        return new Dictionary<string, Profile>
        {
            // Local development with Aspire/AppHost
            // Uses standardized HTTPS ports (7xxx series)
            ["local"] = new Profile
            {
                Name = "local",
                TenantServiceUrl = "https://localhost:7110",
                RegisterServiceUrl = "https://localhost:7290",
                PeerServiceUrl = "https://localhost:7002",
                WalletServiceUrl = "https://localhost:7001",
                BlueprintServiceUrl = "https://localhost:7000",
                AuthTokenUrl = "https://localhost:7110/api/service-auth/token",
                DefaultClientId = "sorcha-admin",
                VerifySsl = false,
                TimeoutSeconds = 30,
                CustomSettings = new Dictionary<string, string>()
            },

            // Docker Compose environment
            // Uses API Gateway for routing (all services behind gateway on port 80)
            // Admin UI is hosted at /admin, all API services at /api/*
            ["docker"] = new Profile
            {
                Name = "docker",
                TenantServiceUrl = "http://localhost/api/tenant",
                RegisterServiceUrl = "http://localhost/api/register",
                PeerServiceUrl = "http://localhost/api/peer",
                WalletServiceUrl = "http://localhost/api/wallet",
                BlueprintServiceUrl = "http://localhost/api/blueprint",
                AuthTokenUrl = "http://localhost/api/service-auth/token",
                DefaultClientId = "sorcha-admin",
                VerifySsl = false,
                TimeoutSeconds = 30,
                CustomSettings = new Dictionary<string, string>()
            },

            // Production environment
            // Uses standard HTTPS (port 443) with dedicated service domains
            ["production"] = new Profile
            {
                Name = "production",
                TenantServiceUrl = "https://tenant.sorcha.io",
                RegisterServiceUrl = "https://register.sorcha.io",
                PeerServiceUrl = "https://peer.sorcha.io",
                WalletServiceUrl = "https://wallet.sorcha.io",
                BlueprintServiceUrl = "https://blueprint.sorcha.io",
                AuthTokenUrl = "https://tenant.sorcha.io/api/service-auth/token",
                DefaultClientId = "sorcha-admin",
                VerifySsl = true,
                TimeoutSeconds = 30,
                CustomSettings = new Dictionary<string, string>()
            }
        };
    }

    /// <summary>
    /// Gets the default active profile name ("docker" for Docker Compose deployments).
    /// Changed from "local" to match Docker deployment default.
    /// </summary>
    public static string DefaultActiveProfile => "docker";
}
