namespace Sorcha.Admin.Models.Configuration;

/// <summary>
/// Factory class for creating default profile configurations.
/// Provides 6 pre-configured environments: dev, local, docker, aspire, staging, production.
/// These defaults match the CLI configuration for consistency across tools.
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
            ["dev"] = new Profile
            {
                Name = "dev",
                TenantServiceUrl = "https://localhost:7080",
                RegisterServiceUrl = "https://localhost:7081",
                PeerServiceUrl = "https://localhost:7082",
                WalletServiceUrl = "https://localhost:7083",
                BlueprintServiceUrl = "https://localhost:7084",
                AuthTokenUrl = "https://localhost:7080/api/service-auth/token",
                DefaultClientId = "sorcha-admin",
                VerifySsl = false,
                TimeoutSeconds = 30,
                CustomSettings = new Dictionary<string, string>()
            },

            ["local"] = new Profile
            {
                Name = "local",
                TenantServiceUrl = "http://localhost:5080",
                RegisterServiceUrl = "http://localhost:5081",
                PeerServiceUrl = "http://localhost:5082",
                WalletServiceUrl = "http://localhost:5083",
                BlueprintServiceUrl = "http://localhost:5084",
                AuthTokenUrl = "http://localhost:5080/api/service-auth/token",
                DefaultClientId = "sorcha-admin",
                VerifySsl = false,
                TimeoutSeconds = 30,
                CustomSettings = new Dictionary<string, string>()
            },

            ["docker"] = new Profile
            {
                Name = "docker",
                TenantServiceUrl = "http://localhost:8080/tenant",
                RegisterServiceUrl = "http://localhost:8080/register",
                PeerServiceUrl = "http://localhost:8080/peer",
                WalletServiceUrl = "http://localhost:8080/wallet",
                BlueprintServiceUrl = "http://localhost:8080/blueprint",
                AuthTokenUrl = "http://localhost:8080/tenant/api/service-auth/token",
                DefaultClientId = "sorcha-admin",
                VerifySsl = false,
                TimeoutSeconds = 30,
                CustomSettings = new Dictionary<string, string>()
            },

            ["aspire"] = new Profile
            {
                Name = "aspire",
                TenantServiceUrl = "https://localhost:7051/api/tenant",
                RegisterServiceUrl = "https://localhost:7051/api/register",
                PeerServiceUrl = "https://localhost:7051/api/peer",
                WalletServiceUrl = "https://localhost:7051/api/wallet",
                BlueprintServiceUrl = "https://localhost:7051/api/blueprint",
                AuthTokenUrl = "https://localhost:7051/api/tenant/api/service-auth/token",
                DefaultClientId = "sorcha-admin",
                VerifySsl = false,
                TimeoutSeconds = 30,
                CustomSettings = new Dictionary<string, string>()
            },

            ["staging"] = new Profile
            {
                Name = "staging",
                TenantServiceUrl = "https://n0.sorcha.dev",
                RegisterServiceUrl = "https://n0.sorcha.dev",
                PeerServiceUrl = "https://n0.sorcha.dev",
                WalletServiceUrl = "https://n0.sorcha.dev",
                BlueprintServiceUrl = "https://n0.sorcha.dev",
                AuthTokenUrl = "https://n0.sorcha.dev/api/service-auth/token",
                DefaultClientId = "sorcha-admin",
                VerifySsl = true,
                TimeoutSeconds = 30,
                CustomSettings = new Dictionary<string, string>()
            },

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
    /// Gets the default active profile name ("dev" for development).
    /// </summary>
    public static string DefaultActiveProfile => "dev";
}
