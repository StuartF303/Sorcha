// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Spectre.Console;
using Sorcha.Blueprint.Models;
using Sorcha.Demo.Models;

namespace Sorcha.Demo.UI;

/// <summary>
/// Renders UI elements for the demo application using Spectre.Console
/// </summary>
public class DemoRenderer
{
    /// <summary>
    /// Shows the application welcome banner
    /// </summary>
    public void ShowWelcomeBanner()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(
            new FigletText("Sorcha Demo")
                .Color(Color.Cyan1));

        AnsiConsole.MarkupLine("[dim]Blueprint Workflow Execution with Real Services[/]");
        AnsiConsole.MarkupLine("[dim]Powered by Blueprint Service Orchestration API[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Shows the main menu and returns user selection
    /// </summary>
    public string ShowMainMenu()
    {
        AnsiConsole.Write(new Rule("[cyan]Main Menu[/]").LeftJustified());
        AnsiConsole.WriteLine();

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [green]blueprint demo[/] to run:")
                .PageSize(10)
                .AddChoices(["expense", "purchase", "loan", "custom", "settings", "exit"])
                .UseConverter(choice => choice switch
                {
                    "expense" => "💰 Expense Approval Workflow",
                    "purchase" => "📦 Purchase Order Processing",
                    "loan" => "🏦 Loan Application Process",
                    "custom" => "📂 Load Custom Blueprint",
                    "settings" => "⚙️  Settings",
                    "exit" => "🚪 Exit",
                    _ => choice
                }));

        return selection;
    }

    /// <summary>
    /// Shows blueprint overview
    /// </summary>
    public void ShowBlueprintOverview(Sorcha.Blueprint.Models.Blueprint blueprint)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule($"[cyan]{blueprint.Title}[/]").LeftJustified());
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine($"[bold]Description:[/] {blueprint.Description}");
        AnsiConsole.MarkupLine($"[bold]Blueprint ID:[/] {blueprint.Id}");
        AnsiConsole.MarkupLine($"[bold]Version:[/] {blueprint.Version}");
        AnsiConsole.MarkupLine($"[bold]Actions:[/] {blueprint.Actions?.Count ?? 0}");
        AnsiConsole.WriteLine();

        // Show participants
        if (blueprint.Participants != null && blueprint.Participants.Count > 0)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("[bold]Participant[/]")
                .AddColumn("[bold]Organization[/]");

            foreach (var participant in blueprint.Participants)
            {
                table.AddRow(
                    $"[cyan]{participant.Name}[/]",
                    participant.Organisation ?? "-");
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }
    }

    /// <summary>
    /// Shows wallet assignments for participants
    /// </summary>
    public void ShowWalletAssignments(Dictionary<string, ParticipantContext> participants)
    {
        AnsiConsole.Write(new Rule("[yellow]Wallet Assignments[/]").LeftJustified());
        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Participant[/]")
            .AddColumn("[bold]Wallet Address[/]")
            .AddColumn("[bold]Algorithm[/]");

        foreach (var (id, participant) in participants)
        {
            table.AddRow(
                $"[cyan]{id}[/]",
                $"[dim]{(participant.WalletAddress.Length > 16 ? participant.WalletAddress[..16] + "..." : participant.WalletAddress)}[/]",
                participant.Algorithm);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Shows action header before execution
    /// </summary>
    public void ShowActionHeader(int actionIndex, Sorcha.Blueprint.Models.Action action, string participant)
    {
        AnsiConsole.Write(new Rule($"[green]Action {actionIndex}: {action.Title}[/]").LeftJustified());
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Participant:[/] [cyan]{participant}[/]");
        AnsiConsole.MarkupLine($"[bold]Description:[/] {action.Description}");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Shows action execution result
    /// </summary>
    public void ShowActionResult(ActionExecutionResult result)
    {
        if (result.Success)
        {
            AnsiConsole.MarkupLine($"[green]✓ Action completed successfully[/]");
            if (result.TransactionHash != null)
            {
                var txHash = result.TransactionHash.Length > 16
                    ? result.TransactionHash[..16] + "..."
                    : result.TransactionHash;
                AnsiConsole.MarkupLine($"  [dim]Transaction:[/] {txHash}");
            }
            if (result.NextParticipant != null)
            {
                AnsiConsole.MarkupLine($"  [dim]Next:[/] [cyan]{result.NextParticipant}[/]");
            }
            AnsiConsole.MarkupLine($"  [dim]Time:[/] {result.ExecutionTimeMs}ms");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗ Action failed[/]");
            if (result.ErrorMessage != null)
            {
                AnsiConsole.MarkupLine($"  [red]{result.ErrorMessage}[/]");
            }
        }
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Shows workflow completion summary
    /// </summary>
    public void ShowWorkflowSummary(DemoContext context)
    {
        AnsiConsole.Write(new Rule("[green]Workflow Complete[/]").LeftJustified());
        AnsiConsole.WriteLine();

        var successCount = context.ExecutionHistory.Count(r => r.Success);
        var failCount = context.ExecutionHistory.Count(r => !r.Success);

        AnsiConsole.MarkupLine($"[bold]Total Actions:[/] {context.ExecutionHistory.Count}");
        AnsiConsole.MarkupLine($"[green]Successful:[/] {successCount}");
        if (failCount > 0)
        {
            AnsiConsole.MarkupLine($"[red]Failed:[/] {failCount}");
        }
        AnsiConsole.WriteLine();

        // Show execution history table
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]#[/]")
            .AddColumn("[bold]Action[/]")
            .AddColumn("[bold]Participant[/]")
            .AddColumn("[bold]Status[/]")
            .AddColumn("[bold]Transaction[/]");

        foreach (var result in context.ExecutionHistory)
        {
            table.AddRow(
                result.ActionIndex.ToString(),
                result.ActionTitle,
                $"[cyan]{result.ParticipantId}[/]",
                result.Success ? "[green]✓[/]" : "[red]✗[/]",
                result.TransactionHash != null && result.TransactionHash.Length > 12
                    ? result.TransactionHash[..12] + "..."
                    : result.TransactionHash ?? "-");
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Shows settings menu
    /// </summary>
    public void ShowSettingsMenu(DemoContext context)
    {
        var done = false;
        while (!done)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[cyan]Settings[/]").LeftJustified());
            AnsiConsole.WriteLine();

            var table = new Table()
                .AddColumn("Setting")
                .AddColumn("Value");

            table.AddRow("Step-by-Step Mode", context.Settings.StepByStepMode ? "[green]ON[/]" : "[dim]OFF[/]");
            table.AddRow("Verbose Mode", context.Settings.VerboseMode ? "[green]ON[/]" : "[dim]OFF[/]");
            table.AddRow("Show Validation", context.Settings.ShowValidation ? "[green]ON[/]" : "[dim]OFF[/]");
            table.AddRow("Show Calculations", context.Settings.ShowCalculations ? "[green]ON[/]" : "[dim]OFF[/]");
            table.AddRow("Show Routing", context.Settings.ShowRouting ? "[green]ON[/]" : "[dim]OFF[/]");
            table.AddRow("Show Disclosure", context.Settings.ShowDisclosure ? "[green]ON[/]" : "[dim]OFF[/]");

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Toggle a setting:")
                    .AddChoices(["step", "verbose", "validation", "calculations", "routing", "disclosure", "clear", "back"])
                    .UseConverter(c => c switch
                    {
                        "step" => "👣 Step-by-Step Mode",
                        "verbose" => "🔊 Verbose Mode",
                        "validation" => "✅ Show Validation",
                        "calculations" => "🧮 Show Calculations",
                        "routing" => "🔀 Show Routing",
                        "disclosure" => "🔒 Show Disclosure",
                        "clear" => "🗑️  Clear Wallets",
                        "back" => "⬅️  Back",
                        _ => c
                    }));

            switch (choice)
            {
                case "step":
                    context.Settings.StepByStepMode = !context.Settings.StepByStepMode;
                    break;
                case "verbose":
                    context.Settings.VerboseMode = !context.Settings.VerboseMode;
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
                        AnsiConsole.MarkupLine("[yellow]Wallets cleared[/]");
                        Thread.Sleep(1000);
                    }
                    break;
                case "back":
                    done = true;
                    break;
            }
        }
    }

    /// <summary>
    /// Prompts user for input data based on schema
    /// </summary>
    public Dictionary<string, object> PromptForActionData(Sorcha.Blueprint.Models.Action action)
    {
        var inputData = new Dictionary<string, object>();

        if (action.DataSchemas == null || !action.DataSchemas.Any())
        {
            return inputData;
        }

        var schema = action.DataSchemas.First();
        var schemaJson = System.Text.Json.JsonSerializer.Serialize(schema);
        var schemaObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(schemaJson);

        if (schemaObj != null && schemaObj.TryGetValue("properties", out var propsObj))
        {
            var propsJson = System.Text.Json.JsonSerializer.Serialize(propsObj);
            var properties = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(propsJson);

            if (properties != null)
            {
                AnsiConsole.MarkupLine("[bold]Input Required:[/]");
                AnsiConsole.WriteLine();

                foreach (var prop in properties.Keys)
                {
                    var propJson = System.Text.Json.JsonSerializer.Serialize(properties[prop]);
                    var propDef = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(propJson);

                    if (propDef != null)
                    {
                        var value = PromptForProperty(prop, propDef);
                        inputData[prop] = value;
                    }
                }
            }
        }

        return inputData;
    }

    /// <summary>
    /// Prompts for a single property value based on its schema
    /// </summary>
    private object PromptForProperty(string key, Dictionary<string, object> propertyDef)
    {
        var propType = propertyDef.ContainsKey("type") ? propertyDef["type"].ToString() : "string";
        var propTitle = propertyDef.ContainsKey("title") ? propertyDef["title"].ToString() : key;
        var title = propTitle ?? key;

        // Handle enum values
        if (propertyDef.ContainsKey("enum"))
        {
            var enumJson = System.Text.Json.JsonSerializer.Serialize(propertyDef["enum"]);
            var enumValues = System.Text.Json.JsonSerializer.Deserialize<List<string>>(enumJson);
            if (enumValues != null && enumValues.Any())
            {
                return AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title($"{title}:")
                        .AddChoices(enumValues));
            }
        }

        // Get minimum value if present
        double? minimum = null;
        if (propertyDef.ContainsKey("minimum"))
        {
            var minJson = System.Text.Json.JsonSerializer.Serialize(propertyDef["minimum"]);
            minimum = System.Text.Json.JsonSerializer.Deserialize<double>(minJson);
        }

        // Get format if present
        var format = propertyDef.ContainsKey("format") ? propertyDef["format"].ToString() : null;

        return propType?.ToLower() switch
        {
            "string" when format == "date" => AnsiConsole.Ask($"{title} (YYYY-MM-DD):", DateTime.UtcNow.ToString("yyyy-MM-dd")),
            "string" => AnsiConsole.Ask<string>($"{title}:"),
            "number" => AnsiConsole.Ask<double>($"{title}:", minimum ?? 0.0),
            "integer" => AnsiConsole.Ask<int>($"{title}:", (int)(minimum ?? 0)),
            "boolean" => AnsiConsole.Confirm($"{title}?", true),
            _ => AnsiConsole.Ask<string>($"{title}:")
        };
    }
}
