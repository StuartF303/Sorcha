using System.CommandLine;
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
        var profiles = await context.ConfigurationService.ListProfilesAsync();
        var config = await context.ConfigurationService.GetConfigurationAsync();

        var profileList = profiles.Select(p => new
        {
            p.Name,
            p.TenantServiceUrl,
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
