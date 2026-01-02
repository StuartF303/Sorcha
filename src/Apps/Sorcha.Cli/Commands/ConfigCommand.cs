using System.CommandLine;
using System.CommandLine.Invocation;
using Sorcha.Cli.Models;
using Sorcha.Cli.Services;

namespace Sorcha.Cli.Commands;

/// <summary>
/// Configuration management commands (example command using BaseCommand).
/// </summary>
public class ConfigCommand : BaseCommand
{
    public ConfigCommand()
        : base("config", "Manage CLI configuration and profiles")
    {
        AddCommand(new ConfigInitCommand());
        AddCommand(new ProfileListCommand());
        AddCommand(new ProfileSetActiveCommand());
    }

    protected override Task<int> ExecuteAsync(CommandContext context)
    {
        // Show help when config is called without subcommand
        WriteMessage(context, "Use 'sorcha config --help' to see available commands.");
        return Task.FromResult(ExitCodes.Success);
    }
}

/// <summary>
/// Lists all available profiles.
/// </summary>
public class ProfileListCommand : BaseCommand
{
    public ProfileListCommand()
        : base("list", "List all available profiles")
    {
    }

    protected override async Task<int> ExecuteAsync(CommandContext context)
    {
        var configService = new ConfigurationService();
        var profiles = await configService.ListProfilesAsync();
        var config = await configService.GetConfigurationAsync();

        var profileList = profiles.Select(p => new
        {
            p.Name,
            ServiceUrl = !string.IsNullOrWhiteSpace(p.ServiceUrl) ? p.ServiceUrl : p.GetTenantServiceUrl(),
            Active = p.Name == config.ActiveProfile
        });

        WriteCollection(context, profileList);
        return ExitCodes.Success;
    }
}

/// <summary>
/// Sets the active profile.
/// </summary>
public class ProfileSetActiveCommand : Command
{
    public ProfileSetActiveCommand()
        : base("set-active", "Set the active profile")
    {
        var profileNameArgument = new Argument<string>(
            name: "name",
            description: "Name of the profile to activate");

        AddArgument(profileNameArgument);

        this.SetHandler(async (profileName) =>
        {
            var configService = new ConfigurationService();

            try
            {
                // Check if profile exists
                var profile = await configService.GetProfileAsync(profileName);
                if (profile == null)
                {
                    Console.Error.WriteLine($"Error: Profile '{profileName}' does not exist.");
                    Console.WriteLine("\nAvailable profiles:");

                    var profiles = await configService.ListProfilesAsync();
                    foreach (var p in profiles)
                    {
                        Console.WriteLine($"  - {p.Name}");
                    }

                    Environment.Exit(ExitCodes.ValidationError);
                    return;
                }

                // Set active profile
                await configService.SetActiveProfileAsync(profileName);
                Console.WriteLine($"✓ Active profile set to '{profileName}'");
                Console.WriteLine($"  Tenant Service URL: {profile.TenantServiceUrl}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: Failed to set active profile: {ex.Message}");
                Environment.Exit(ExitCodes.GeneralError);
            }
        }, profileNameArgument);
    }
}

/// <summary>
/// Initializes a new CLI configuration profile.
/// CLI-BOOTSTRAP-001: Initialize CLI configuration profile with service URLs.
/// </summary>
public class ConfigInitCommand : Command
{
    public ConfigInitCommand()
        : base("init", "Initialize a new configuration profile")
    {
        var profileOption = new Option<string>(
            aliases: ["--profile", "-p"],
            description: "Profile name",
            getDefaultValue: () => "docker");

        var serviceUrlOption = new Option<string?>(
            aliases: ["--service-url", "-s"],
            description: "Base URL for all services (recommended - e.g., 'http://localhost')");

        var tenantUrlOption = new Option<string?>(
            aliases: ["--tenant-url", "-t"],
            description: "Tenant Service URL (optional override)");

        var registerUrlOption = new Option<string?>(
            aliases: ["--register-url", "-r"],
            description: "Register Service URL (optional override)");

        var walletUrlOption = new Option<string?>(
            aliases: ["--wallet-url", "-w"],
            description: "Wallet Service URL (optional override)");

        var peerUrlOption = new Option<string?>(
            aliases: ["--peer-url"],
            description: "Peer Service URL (optional override)");

        var authUrlOption = new Option<string?>(
            aliases: ["--auth-url", "-a"],
            description: "Auth Token URL (optional override)");

        var clientIdOption = new Option<string>(
            aliases: ["--client-id", "-c"],
            description: "Default client ID",
            getDefaultValue: () => "sorcha-cli");

        var verifySslOption = new Option<bool>(
            aliases: ["--verify-ssl"],
            description: "Verify SSL certificates",
            getDefaultValue: () => false);

        var timeoutOption = new Option<int>(
            aliases: ["--timeout"],
            description: "Request timeout in seconds",
            getDefaultValue: () => 30);

        var checkConnectivityOption = new Option<bool>(
            aliases: ["--check-connectivity"],
            description: "Verify connectivity to service URLs",
            getDefaultValue: () => true);

        var setActiveOption = new Option<bool>(
            aliases: ["--set-active"],
            description: "Set as active profile",
            getDefaultValue: () => true);

        AddOption(profileOption);
        AddOption(serviceUrlOption);
        AddOption(tenantUrlOption);
        AddOption(registerUrlOption);
        AddOption(walletUrlOption);
        AddOption(peerUrlOption);
        AddOption(authUrlOption);
        AddOption(clientIdOption);
        AddOption(verifySslOption);
        AddOption(timeoutOption);
        AddOption(checkConnectivityOption);
        AddOption(setActiveOption);

        this.SetHandler(async (InvocationContext context) =>
        {
            try
            {
                var profileName = context.ParseResult.GetValueForOption(profileOption)!;
                var serviceUrl = context.ParseResult.GetValueForOption(serviceUrlOption);
                var tenantUrl = context.ParseResult.GetValueForOption(tenantUrlOption);
                var registerUrl = context.ParseResult.GetValueForOption(registerUrlOption);
                var walletUrl = context.ParseResult.GetValueForOption(walletUrlOption);
                var peerUrl = context.ParseResult.GetValueForOption(peerUrlOption);
                var authUrl = context.ParseResult.GetValueForOption(authUrlOption);
                var clientId = context.ParseResult.GetValueForOption(clientIdOption)!;
                var verifySsl = context.ParseResult.GetValueForOption(verifySslOption);
                var timeout = context.ParseResult.GetValueForOption(timeoutOption);
                var checkConnectivity = context.ParseResult.GetValueForOption(checkConnectivityOption);
                var setActive = context.ParseResult.GetValueForOption(setActiveOption);

                var configService = new ConfigurationService();

                // Validate that either base service URL or specific URLs are provided
                if (string.IsNullOrWhiteSpace(serviceUrl) &&
                    string.IsNullOrWhiteSpace(tenantUrl) &&
                    string.IsNullOrWhiteSpace(registerUrl) &&
                    string.IsNullOrWhiteSpace(walletUrl) &&
                    string.IsNullOrWhiteSpace(peerUrl))
                {
                    Console.Error.WriteLine("Error: Either --service-url or at least one specific service URL must be provided.");
                    Environment.Exit(ExitCodes.ValidationError);
                    return;
                }

                // Create profile
                var profile = new Profile
                {
                    Name = profileName,
                    ServiceUrl = serviceUrl,
                    TenantServiceUrl = tenantUrl,
                    RegisterServiceUrl = registerUrl,
                    WalletServiceUrl = walletUrl,
                    PeerServiceUrl = peerUrl,
                    AuthTokenUrl = authUrl,
                    DefaultClientId = clientId,
                    VerifySsl = verifySsl,
                    TimeoutSeconds = timeout
                };

                // Check if profile already exists
                var existingProfile = await configService.GetProfileAsync(profileName);
                var isUpdate = existingProfile != null;

                // Validate connectivity if requested
                var connectivityStatus = "skipped";
                if (checkConnectivity)
                {
                    Console.WriteLine($"Checking connectivity to service URLs...");
                    connectivityStatus = await CheckConnectivityAsync(profile);
                }

                // Save profile
                await configService.UpsertProfileAsync(profile);

                // Set as active if requested
                if (setActive)
                {
                    await configService.SetActiveProfileAsync(profileName);
                }

                // Output result
                Console.WriteLine($"✓ Profile '{profileName}' {(isUpdate ? "updated" : "created")}");

                if (!string.IsNullOrWhiteSpace(serviceUrl))
                {
                    Console.WriteLine($"  Base Service URL: {serviceUrl}");
                }

                Console.WriteLine($"  Tenant Service:   {profile.GetTenantServiceUrl()}{(string.IsNullOrWhiteSpace(tenantUrl) ? " (from base)" : "")}");
                Console.WriteLine($"  Register Service: {profile.GetRegisterServiceUrl()}{(string.IsNullOrWhiteSpace(registerUrl) ? " (from base)" : "")}");
                Console.WriteLine($"  Wallet Service:   {profile.GetWalletServiceUrl()}{(string.IsNullOrWhiteSpace(walletUrl) ? " (from base)" : "")}");
                Console.WriteLine($"  Peer Service:     {profile.GetPeerServiceUrl()}{(string.IsNullOrWhiteSpace(peerUrl) ? " (from base)" : "")}");
                Console.WriteLine($"  Auth Token URL:   {profile.GetAuthTokenUrl()}{(string.IsNullOrWhiteSpace(authUrl) ? " (from base)" : "")}");
                Console.WriteLine($"  Connectivity:     {connectivityStatus}");
                if (setActive)
                {
                    Console.WriteLine($"  Active profile:   {profileName}");
                }
                Console.WriteLine($"  Config file:      {configService.GetConfigFilePath()}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: Failed to initialize configuration: {ex.Message}");
                Environment.Exit(ExitCodes.GeneralError);
            }
        });
    }

    private static async Task<string> CheckConnectivityAsync(Profile profile)
    {
        var urls = new[]
        {
            ("Tenant", profile.GetTenantServiceUrl()),
            ("Register", profile.GetRegisterServiceUrl()),
            ("Wallet", profile.GetWalletServiceUrl()),
            ("Peer", profile.GetPeerServiceUrl())
        };

        var results = new List<(string Service, bool Success)>();

        HttpClient httpClient;
        if (!profile.VerifySsl)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        }
        else
        {
            httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        }

        using (httpClient)
        {
            foreach (var (service, url) in urls)
            {
                try
                {
                    // Try to reach the health endpoint
                    var healthUrl = url.TrimEnd('/') + "/health";
                    var response = await httpClient.GetAsync(healthUrl);
                    results.Add((service, response.IsSuccessStatusCode));

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"    {service}: OK");
                    }
                    else
                    {
                        Console.WriteLine($"    {service}: HTTP {(int)response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    results.Add((service, false));
                    Console.WriteLine($"    {service}: {ex.Message}");
                }
            }
        }

        var successCount = results.Count(r => r.Success);
        var totalCount = results.Count;

        return successCount == totalCount ? "passed"
            : successCount > 0 ? "partial"
            : "failed";
    }
}
