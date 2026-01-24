using System.CommandLine;
using System.CommandLine.Parsing;
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
    private readonly Option<string?> _orgNameOption;
    private readonly Option<string?> _subdomainOption;
    private readonly Option<string?> _descriptionOption;
    private readonly Option<string?> _adminEmailOption;
    private readonly Option<string?> _adminNameOption;
    private readonly Option<string?> _adminPasswordOption;
    private readonly Option<bool> _createSpOption;
    private readonly Option<string?> _spNameOption;
    private readonly Option<bool> _nonInteractiveOption;

    public BootstrapCommand(
        HttpClientFactory clientFactory,
        IConfigurationService configService)
        : base("bootstrap", "Bootstrap a fresh Sorcha installation")
    {
        // Options
        _orgNameOption = new Option<string?>("--org-name", "-n")
        {
            Description = "Organization name"
        };

        _subdomainOption = new Option<string?>("--subdomain", "-s")
        {
            Description = "Organization subdomain"
        };

        _descriptionOption = new Option<string?>("--description", "-d")
        {
            Description = "Organization description"
        };

        _adminEmailOption = new Option<string?>("--admin-email", "-e")
        {
            Description = "Administrator email address"
        };

        _adminNameOption = new Option<string?>("--admin-name", "-a")
        {
            Description = "Administrator display name"
        };

        _adminPasswordOption = new Option<string?>("--admin-password", "-p")
        {
            Description = "Administrator password (prompted if not provided)"
        };

        _createSpOption = new Option<bool>("--create-sp")
        {
            Description = "Create service principal for automation",
            DefaultValueFactory = _ => false
        };

        _spNameOption = new Option<string?>("--sp-name")
        {
            Description = "Service principal name"
        };

        _nonInteractiveOption = new Option<bool>("--non-interactive", "-y")
        {
            Description = "Non-interactive mode (all options must be provided)",
            DefaultValueFactory = _ => false
        };

        Options.Add(_orgNameOption);
        Options.Add(_subdomainOption);
        Options.Add(_descriptionOption);
        Options.Add(_adminEmailOption);
        Options.Add(_adminNameOption);
        Options.Add(_adminPasswordOption);
        Options.Add(_createSpOption);
        Options.Add(_spNameOption);
        Options.Add(_nonInteractiveOption);

        this.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            var orgName = parseResult.GetValue(_orgNameOption);
            var subdomain = parseResult.GetValue(_subdomainOption);
            var description = parseResult.GetValue(_descriptionOption);
            var adminEmail = parseResult.GetValue(_adminEmailOption);
            var adminName = parseResult.GetValue(_adminNameOption);
            var adminPassword = parseResult.GetValue(_adminPasswordOption);
            var createSp = parseResult.GetValue(_createSpOption);
            var spName = parseResult.GetValue(_spNameOption);
            var nonInteractive = parseResult.GetValue(_nonInteractiveOption);

            try
            {
                // Get profile name from global --profile option or use active profile
                var profileName = BaseCommand.ProfileOption != null
                    ? parseResult.GetValue(BaseCommand.ProfileOption)
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
                    return ExitCodes.ValidationError;
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

                return ExitCodes.Success;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                ConsoleHelper.WriteError("Validation failed:");
                if (!string.IsNullOrEmpty(ex.Content))
                {
                    AnsiConsole.MarkupLine($"[red]{ex.Content.EscapeMarkup()}[/]");
                }
                return ExitCodes.ValidationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                ConsoleHelper.WriteError("Bootstrap conflict - organization or user may already exist.");
                if (!string.IsNullOrEmpty(ex.Content))
                {
                    AnsiConsole.MarkupLine($"[red]{ex.Content.EscapeMarkup()}[/]");
                }
                return ExitCodes.GeneralError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API Error: {ex.Message}");
                if (!string.IsNullOrEmpty(ex.Content))
                {
                    AnsiConsole.MarkupLine($"[red]Details: {ex.Content}[/]");
                }
                return ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Bootstrap failed: {ex.Message}");
                return ExitCodes.GeneralError;
            }
        });
    }
}
