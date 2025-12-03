using Spectre.Console;

namespace Sorcha.Cli.Demo.Utilities;

/// <summary>
/// Renders UI elements using Spectre.Console
/// </summary>
public class ConsoleRenderer
{
    /// <summary>
    /// Shows the main menu and returns user selection
    /// </summary>
    public string ShowMainMenu()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[cyan]Main Menu[/]").LeftJustified());
        AnsiConsole.WriteLine();

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [green]blueprint demo[/] to run:")
                .PageSize(10)
                .AddChoices(new[]
                {
                    "expense", "purchase", "loan", "settings", "exit"
                })
                .UseConverter(choice => choice switch
                {
                    "expense" => "💰 Expense Approval Workflow",
                    "purchase" => "📦 Purchase Order Processing",
                    "loan" => "🏦 Loan Application Process",
                    "settings" => "⚙️  Settings",
                    "exit" => "🚪 Exit",
                    _ => choice
                }));

        return selection;
    }

    /// <summary>
    /// Shows the settings menu
    /// </summary>
    public void ShowSettingsMenu(DemoContext context)
    {
        var done = false;
        while (!done)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[cyan]Settings[/]").LeftJustified());
            AnsiConsole.WriteLine();

            var currentSettings = new Table()
                .AddColumn("Setting")
                .AddColumn("Value");

            currentSettings.AddRow("Verbose Mode (API calls)", context.Settings.VerboseMode ? "[green]ON[/]" : "[dim]OFF[/]");
            currentSettings.AddRow("Step-by-Step Mode", context.Settings.StepByStepMode ? "[green]ON[/]" : "[dim]OFF[/]");
            currentSettings.AddRow("Show Validation", context.Settings.ShowValidation ? "[green]ON[/]" : "[dim]OFF[/]");
            currentSettings.AddRow("Show Calculations", context.Settings.ShowCalculations ? "[green]ON[/]" : "[dim]OFF[/]");
            currentSettings.AddRow("Show Routing", context.Settings.ShowRouting ? "[green]ON[/]" : "[dim]OFF[/]");
            currentSettings.AddRow("Show Disclosure", context.Settings.ShowDisclosure ? "[green]ON[/]" : "[dim]OFF[/]");

            AnsiConsole.Write(currentSettings);
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Toggle a setting:")
                    .AddChoices(new[]
                    {
                        "verbose", "step", "validation", "calculations", "routing", "disclosure", "clear", "back"
                    })
                    .UseConverter(c => c switch
                    {
                        "verbose" => "🔊 Verbose Mode (API calls)",
                        "step" => "👣 Step-by-Step Mode",
                        "validation" => "✅ Show Validation",
                        "calculations" => "🧮 Show Calculations",
                        "routing" => "🔀 Show Routing",
                        "disclosure" => "🔒 Show Disclosure",
                        "clear" => "🗑️  Clear All Wallets",
                        "back" => "⬅️  Back to Main Menu",
                        _ => c
                    }));

            switch (choice)
            {
                case "verbose":
                    context.Settings.VerboseMode = !context.Settings.VerboseMode;
                    break;
                case "step":
                    context.Settings.StepByStepMode = !context.Settings.StepByStepMode;
                    break;
                case "validation":
                    context.Settings.ShowValidation = !context.Settings.ShowValidation;
                    break;
                case "calculations":
                    context.Settings.ShowCalculations = !context.Settings.ShowCalculations;
                    break;
                case "routing":
                    context.Settings.ShowRouting = !context.Settings.ShowRouting;
                    break;
                case "disclosure":
                    context.Settings.ShowDisclosure = !context.Settings.ShowDisclosure;
                    break;
                case "clear":
                    if (AnsiConsole.Confirm("Are you sure you want to clear all wallets?"))
                    {
                        context.ClearWallets();
                        AnsiConsole.MarkupLine("[yellow]All wallets cleared from memory![/]");
                        AnsiConsole.MarkupLine("[dim]Note: Persistent storage will be cleared on next save.[/]");
                        System.Threading.Thread.Sleep(1500);
                    }
                    break;
                case "back":
                    done = true;
                    break;
            }
        }
    }

    /// <summary>
    /// Shows blueprint overview
    /// </summary>
    public void ShowBlueprintOverview(IBlueprintExample example)
    {
        var overview = new Panel($@"[bold cyan]{example.Name}[/]

[dim]{example.Description}[/]

[yellow]Participants:[/] {string.Join(", ", example.GetParticipants())}
")
        {
            Header = new PanelHeader("[bold]📋 Blueprint Overview[/]"),
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Cyan1)
        };

        AnsiConsole.Write(overview);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Shows wallet assignments for participants
    /// </summary>
    public void ShowWalletAssignments(string[] participants, DemoContext context)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold]Participant[/]").Centered())
            .AddColumn(new TableColumn("[bold]Wallet Address[/]"));

        foreach (var participant in participants)
        {
            if (context.ParticipantWallets.TryGetValue(participant, out var wallet))
            {
                table.AddRow(
                    $"[cyan]{participant}[/]",
                    $"[dim]{wallet[..16]}...{wallet[^16..]}[/]"
                );
            }
        }

        var panel = new Panel(table)
        {
            Header = new PanelHeader("[bold]👛 Participant Wallets[/]"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Shows transaction chain summary
    /// </summary>
    public void ShowTransactionChainSummary(DemoContext context)
    {
        var summary = new Panel($@"[bold]Workflow Execution Summary[/]

[cyan]Total Actions Executed:[/] {context.ExecutionData.Count}
[cyan]Transactions Created:[/] {context.TransactionChain.Count}
[cyan]Participants Involved:[/] {context.ParticipantWallets.Count}

[green]✓ Workflow completed successfully![/]
")
        {
            Header = new PanelHeader("[bold]📊 Summary[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green)
        };

        AnsiConsole.Write(summary);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Shows action header with step number and participant
    /// </summary>
    public void ShowActionHeader(int actionIndex, Sorcha.Blueprint.Models.Action action, string participant)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        var rule = new Rule($"[bold cyan]Step {actionIndex + 1}: {action.Title}[/]")
        {
            Justification = Justify.Left,
            Style = new Style(Color.Cyan1)
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var header = new Panel($@"[yellow]👤 Participant:[/] [bold cyan]{participant}[/]
[yellow]📝 Action:[/] {action.Title}
{(!string.IsNullOrEmpty(action.Description) ? $"[dim]{action.Description}[/]" : "")}
")
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1),
            Padding = new Padding(1, 0, 1, 0)
        };

        AnsiConsole.Write(header);
        AnsiConsole.WriteLine();
    }
}
