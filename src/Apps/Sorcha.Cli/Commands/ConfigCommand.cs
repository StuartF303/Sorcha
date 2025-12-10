using System.CommandLine;

namespace Sorcha.Cli.Commands;

/// <summary>
/// Configuration management commands (example command using BaseCommand).
/// </summary>
public class ConfigCommand : BaseCommand
{
    public ConfigCommand()
        : base("config", "Manage CLI configuration and profiles")
    {
        // Add subcommands in future sprints
        // - config profile list
        // - config profile show <name>
        // - config profile set-active <name>

        AddCommand(new ProfileListCommand());
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
