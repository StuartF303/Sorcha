using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sorcha.Cli.Commands;

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
            // Build the root command
            var rootCommand = BuildRootCommand();

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
    /// Builds the root command with all subcommands.
    /// </summary>
    private static RootCommand BuildRootCommand()
    {
        var rootCommand = new RootCommand("Sorcha CLI - Administrative tool for managing Sorcha distributed ledger platform")
        {
            Name = "sorcha"
        };

        // Global options
        var profileOption = new Option<string>(
            aliases: new[] { "--profile", "-p" },
            getDefaultValue: () => "dev",
            description: "Configuration profile to use (dev, staging, production)");

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

        // Subcommands
        // Sprint 2: Tenant Service commands
        rootCommand.AddCommand(new OrganizationCommand());
        rootCommand.AddCommand(new UserCommand());
        rootCommand.AddCommand(new ServicePrincipalCommand());
        rootCommand.AddCommand(new AuthCommand());

        // Sprint 3: Register, Transaction & Wallet commands
        rootCommand.AddCommand(new RegisterCommand());
        rootCommand.AddCommand(new TransactionCommand());
        rootCommand.AddCommand(new WalletCommand());

        // Sprint 4: Peer Service commands
        rootCommand.AddCommand(new PeerCommand());

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
