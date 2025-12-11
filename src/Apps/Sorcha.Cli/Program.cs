using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sorcha.Cli.Commands;
using Sorcha.Cli.Infrastructure;
using Sorcha.Cli.Services;

namespace Sorcha.Cli;

/// <summary>
/// Entry point for the Sorcha CLI administrative tool.
/// </summary>
internal class Program
{
    /// <summary>
    /// Main entry point for the CLI application.
    /// </summary>
    /// <param name="args">Command-line arguments</param>
    /// <returns>Exit code (0 = success, non-zero = error)</returns>
    static async Task<int> Main(string[] args)
    {
        try
        {
            // Build service collection
            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();

            // Build the root command with dependency injection
            var rootCommand = BuildRootCommand(serviceProvider);

            // Invoke the command
            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            if (args.Contains("--verbose") || args.Contains("-v"))
            {
                Console.Error.WriteLine(ex.StackTrace);
            }
            return ExitCodes.GeneralError;
        }
    }

    /// <summary>
    /// Configures services for dependency injection.
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        // Configuration service
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // HTTP client for API calls
        services.AddHttpClient("SorchaApi", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "Sorcha-CLI/1.0");
        });

        services.AddSingleton(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return factory.CreateClient("SorchaApi");
        });

        // Token cache with encryption
        services.AddSingleton(sp =>
        {
            IEncryptionProvider encryptionProvider;

            if (OperatingSystem.IsWindows())
            {
                encryptionProvider = new WindowsDpapiEncryption();
            }
            else if (OperatingSystem.IsMacOS())
            {
                encryptionProvider = new MacOsKeychainEncryption();
            }
            else
            {
                encryptionProvider = new LinuxEncryption();
            }

            return new TokenCache(encryptionProvider);
        });

        // Authentication service
        services.AddSingleton<IAuthenticationService, AuthenticationService>();

        // HTTP client factory for service clients (Tenant, Register, Wallet)
        services.AddSingleton<HttpClientFactory>();

        // Logging (optional, for debugging)
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });
    }

    /// <summary>
    /// Builds the root command with all subcommands.
    /// </summary>
    private static RootCommand BuildRootCommand(ServiceProvider serviceProvider)
    {
        var rootCommand = new RootCommand("Sorcha CLI - Administrative tool for managing Sorcha distributed ledger platform")
        {
            Name = "sorcha"
        };

        // Global options
        var profileOption = new Option<string>(
            aliases: new[] { "--profile", "-p" },
            getDefaultValue: () => "dev",
            description: "Configuration profile to use (dev, local, docker, staging, production)");

        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            getDefaultValue: () => "table",
            description: "Output format (table, json, csv)");

        var quietOption = new Option<bool>(
            aliases: new[] { "--quiet", "-q" },
            description: "Suppress non-essential output");

        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            description: "Enable verbose logging");

        rootCommand.AddGlobalOption(profileOption);
        rootCommand.AddGlobalOption(outputOption);
        rootCommand.AddGlobalOption(quietOption);
        rootCommand.AddGlobalOption(verboseOption);

        // Set global options for BaseCommand
        BaseCommand.ProfileOption = profileOption;
        BaseCommand.OutputOption = outputOption;
        BaseCommand.QuietOption = quietOption;
        BaseCommand.VerboseOption = verboseOption;

        // Get services from DI container
        var authService = serviceProvider.GetRequiredService<IAuthenticationService>();
        var configService = serviceProvider.GetRequiredService<IConfigurationService>();
        var clientFactory = serviceProvider.GetRequiredService<HttpClientFactory>();

        // Subcommands with dependency injection
        // Sprint 2: Tenant Service commands
        rootCommand.AddCommand(new OrganizationCommand(clientFactory, authService, configService));
        rootCommand.AddCommand(new UserCommand(clientFactory, authService, configService));
        rootCommand.AddCommand(new ServicePrincipalCommand(clientFactory, authService, configService));
        rootCommand.AddCommand(new AuthCommand(authService, configService));

        // Sprint 3: Register, Transaction & Wallet commands
        rootCommand.AddCommand(new RegisterCommand(clientFactory, authService, configService));
        rootCommand.AddCommand(new TransactionCommand(clientFactory, authService, configService));
        rootCommand.AddCommand(new WalletCommand(clientFactory, authService, configService));

        // Sprint 4: Peer Service commands
        rootCommand.AddCommand(new PeerCommand(clientFactory, authService, configService));

        // Configuration management commands
        rootCommand.AddCommand(new ConfigCommand());

        // Version command
        var versionCommand = new Command("version", "Display CLI version information");
        versionCommand.SetHandler(() =>
        {
            Console.WriteLine("Sorcha CLI v1.0.0");
            Console.WriteLine($".NET Runtime: {Environment.Version}");
            Console.WriteLine($"OS: {Environment.OSVersion}");
        });
        rootCommand.AddCommand(versionCommand);

        return rootCommand;
    }
}
