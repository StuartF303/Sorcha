using System.CommandLine;
using System.Net;
using Refit;
using Sorcha.Cli.Infrastructure;
using Sorcha.Cli.Models;
using Sorcha.Cli.Services;
using Spectre.Console;

namespace Sorcha.Cli.Commands;

/// <summary>
/// Bootstrap command for initial Sorcha platform setup.
/// </summary>
public class BootstrapCommand : Command
{
    public BootstrapCommand(
        HttpClientFactory clientFactory,
        IConfigurationService configService)
        : base("bootstrap", "Bootstrap a fresh Sorcha installation")
    {
        // Options
        var orgNameOption = new Option<string?>(
            aliases: new[] { "--org-name", "-n" },
            description: "Organization name");

        var subdomainOption = new Option<string?>(
            aliases: new[] { "--subdomain", "-s" },
            description: "Organization subdomain");

        var descriptionOption = new Option<string?>(
            aliases: new[] { "--description", "-d" },
            description: "Organization description");

        var adminEmailOption = new Option<string?>(
            aliases: new[] { "--admin-email", "-e" },
            description: "Administrator email address");

        var adminNameOption = new Option<string?>(
            aliases: new[] { "--admin-name", "-a" },
            description: "Administrator display name");

        var adminPasswordOption = new Option<string?>(
            aliases: new[] { "--admin-password", "-p" },
            description: "Administrator password (prompted if not provided)");

        var createSpOption = new Option<bool>(
            aliases: new[] { "--create-sp" },
            getDefaultValue: () => false,
            description: "Create service principal for automation");

        var spNameOption = new Option<string?>(
            aliases: new[] { "--sp-name" },
            description: "Service principal name");

        var nonInteractiveOption = new Option<bool>(
            aliases: new[] { "--non-interactive", "-y" },
            getDefaultValue: () => false,
            description: "Non-interactive mode (all options must be provided)");

        AddOption(orgNameOption);
        AddOption(subdomainOption);
        AddOption(descriptionOption);
        AddOption(adminEmailOption);
        AddOption(adminNameOption);
        AddOption(adminPasswordOption);
        AddOption(createSpOption);
        AddOption(spNameOption);
        AddOption(nonInteractiveOption);

        this.SetHandler(async (context) =>
        {
            var orgName = context.ParseResult.GetValueForOption(orgNameOption);
            var subdomain = context.ParseResult.GetValueForOption(subdomainOption);
            var description = context.ParseResult.GetValueForOption(descriptionOption);
            var adminEmail = context.ParseResult.GetValueForOption(adminEmailOption);
            var adminName = context.ParseResult.GetValueForOption(adminNameOption);
            var adminPassword = context.ParseResult.GetValueForOption(adminPasswordOption);
            var createSp = context.ParseResult.GetValueForOption(createSpOption);
            var spName = context.ParseResult.GetValueForOption(spNameOption);
            var nonInteractive = context.ParseResult.GetValueForOption(nonInteractiveOption);

            try
            {
                // Get profile name from global --profile option or use active profile
                var profileName = BaseCommand.ProfileOption != null
                    ? context.ParseResult.GetValueForOption(BaseCommand.ProfileOption)
                    : null;

                if (string.IsNullOrEmpty(profileName))
                {
                    var profile = await configService.GetActiveProfileAsync();
                    profileName = profile?.Name ?? "dev";
                }

                // Interactive prompts if values not provided
                if (!nonInteractive)
                {
                    AnsiConsole.Write(new FigletText("Sorcha Bootstrap")
                        .LeftJustified()
                        .Color(Color.Blue));

                    AnsiConsole.MarkupLine("[bold]Initial Sorcha platform setup[/]");
                    AnsiConsole.WriteLine();

                    if (string.IsNullOrEmpty(orgName))
                    {
                        orgName = AnsiConsole.Ask<string>("[cyan]Organization name:[/]");
                    }

                    if (string.IsNullOrEmpty(subdomain))
                    {
                        subdomain = AnsiConsole.Ask<string>("[cyan]Organization subdomain:[/]",
                            defaultValue: orgName?.ToLowerInvariant().Replace(" ", "-") ?? "");
                    }

                    if (string.IsNullOrEmpty(description))
                    {
                        description = AnsiConsole.Prompt(
                            new TextPrompt<string>("[cyan]Organization description (optional):[/]")
                                .AllowEmpty());
                    }

                    if (string.IsNullOrEmpty(adminEmail))
                    {
                        adminEmail = AnsiConsole.Ask<string>("[cyan]Administrator email:[/]");
                    }

                    if (string.IsNullOrEmpty(adminName))
                    {
                        adminName = AnsiConsole.Ask<string>("[cyan]Administrator name:[/]");
                    }

                    if (string.IsNullOrEmpty(adminPassword))
                    {
                        adminPassword = AnsiConsole.Prompt(
                            new TextPrompt<string>("[cyan]Administrator password:[/]")
                                .Secret());
                    }

                    createSp = AnsiConsole.Confirm("[cyan]Create service principal for automation?[/]", defaultValue: false);

                    if (createSp && string.IsNullOrEmpty(spName))
                    {
                        spName = AnsiConsole.Prompt(
                            new TextPrompt<string>("[cyan]Service principal name:[/]")
                                .DefaultValue("bootstrap-principal"));
                    }

                    AnsiConsole.WriteLine();
                }

                // Validate required fields
                if (string.IsNullOrEmpty(orgName) ||
                    string.IsNullOrEmpty(subdomain) ||
                    string.IsNullOrEmpty(adminEmail) ||
                    string.IsNullOrEmpty(adminName) ||
                    string.IsNullOrEmpty(adminPassword))
                {
                    ConsoleHelper.WriteError("All required fields must be provided in non-interactive mode.");
                    Environment.ExitCode = ExitCodes.ValidationError;
                    return;
                }

                // Create bootstrap request
                var request = new BootstrapRequest
                {
                    OrganizationName = orgName,
                    OrganizationSubdomain = subdomain,
                    OrganizationDescription = description,
                    AdminEmail = adminEmail,
                    AdminName = adminName,
                    AdminPassword = adminPassword,
                    CreateServicePrincipal = createSp,
                    ServicePrincipalName = spName
                };

                // Call bootstrap API
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("[yellow]Bootstrapping Sorcha platform...[/]", async ctx =>
                    {
                        var client = await clientFactory.CreateTenantServiceClientAsync(profileName);
                        var response = await client.BootstrapAsync(request);

                        ctx.Status("[green]Bootstrap complete![/]");
                        ctx.Spinner(Spinner.Known.Star);
                        await Task.Delay(500);

                        // Display results
                        AnsiConsole.WriteLine();
                        var table = new Table();
                        table.Border(TableBorder.Rounded);
                        table.AddColumn(new TableColumn("[bold]Resource[/]").Centered());
                        table.AddColumn(new TableColumn("[bold]Details[/]"));

                        table.AddRow("[cyan]Organization[/]",
                            $"{response.OrganizationName}\nSubdomain: {response.OrganizationSubdomain}\nID: {response.OrganizationId}");

                        table.AddRow("[cyan]Administrator[/]",
                            $"Email: {response.AdminEmail}\nID: {response.AdminUserId}");

                        if (response.ServicePrincipalId.HasValue)
                        {
                            table.AddRow("[cyan]Service Principal[/]",
                                $"Client ID: {response.ServicePrincipalClientId}\nClient Secret: {response.ServicePrincipalClientSecret}\n[yellow]⚠ Store the secret securely - it won't be shown again![/]");
                        }

                        AnsiConsole.Write(table);
                        AnsiConsole.WriteLine();

                        // Save installation record to config
                        var installation = new Installation
                        {
                            Name = $"{profileName}-{response.OrganizationSubdomain}",
                            ProfileName = profileName,
                            OrganizationId = response.OrganizationId,
                            OrganizationName = response.OrganizationName,
                            OrganizationSubdomain = response.OrganizationSubdomain,
                            AdminUserId = response.AdminUserId,
                            AdminEmail = response.AdminEmail,
                            ServicePrincipalId = response.ServicePrincipalId,
                            ServicePrincipalClientId = response.ServicePrincipalClientId,
                            CreatedAt = DateTime.UtcNow,
                            BootstrapVersion = typeof(BootstrapCommand).Assembly.GetName().Version?.ToString() ?? "unknown"
                        };

                        var config = await configService.GetConfigurationAsync();
                        config.Installations[installation.Name] = installation;

                        // Set as active installation if it's the first one
                        if (config.Installations.Count == 1 || string.IsNullOrEmpty(config.ActiveInstallation))
                        {
                            config.ActiveInstallation = installation.Name;
                        }

                        await configService.SaveConfigurationAsync(config);

                        AnsiConsole.MarkupLine($"[dim]Installation record saved: {installation.Name}[/]");
                        AnsiConsole.WriteLine();

                        // Save credentials reminder
                        var panel = new Panel(
                            new Markup($"[bold green]✓ Bootstrap successful![/]\n\n" +
                                      $"[yellow]Next steps:[/]\n" +
                                      $"1. Login as admin: [cyan]sorcha auth login[/]\n" +
                                      $"2. Email: [cyan]{response.AdminEmail}[/]\n" +
                                      $"3. Use the password you just set\n\n" +
                                      $"[dim]Note: Admin tokens not returned - use login endpoint[/]"))
                        {
                            Header = new PanelHeader("[bold]Bootstrap Complete[/]"),
                            Border = BoxBorder.Double,
                            BorderStyle = new Style(Color.Green)
                        };

                        AnsiConsole.Write(panel);
                    });

                Environment.ExitCode = ExitCodes.Success;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                ConsoleHelper.WriteError("Validation failed:");
                if (!string.IsNullOrEmpty(ex.Content))
                {
                    AnsiConsole.MarkupLine($"[red]{ex.Content.EscapeMarkup()}[/]");
                }
                Environment.ExitCode = ExitCodes.ValidationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                ConsoleHelper.WriteError("Bootstrap conflict - organization or user may already exist.");
                if (!string.IsNullOrEmpty(ex.Content))
                {
                    AnsiConsole.MarkupLine($"[red]{ex.Content.EscapeMarkup()}[/]");
                }
                Environment.ExitCode = ExitCodes.GeneralError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API Error: {ex.Message}");
                if (!string.IsNullOrEmpty(ex.Content))
                {
                    AnsiConsole.MarkupLine($"[red]Details: {ex.Content}[/]");
                }
                Environment.ExitCode = ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Bootstrap failed: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        });
    }
}
