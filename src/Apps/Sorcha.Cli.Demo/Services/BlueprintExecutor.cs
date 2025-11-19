using Sorcha.Cli.Demo.Utilities;
using Sorcha.Blueprint.Engine.Interfaces;
using Sorcha.Blueprint.Engine.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Sorcha.Cli.Demo.Services;

/// <summary>
/// Executes blueprints step-by-step with interactive user prompts
/// </summary>
public class BlueprintExecutor
{
    private readonly IExecutionEngine _executionEngine;
    private readonly ILogger<BlueprintExecutor> _logger;

    public BlueprintExecutor(
        IExecutionEngine executionEngine,
        ILogger<BlueprintExecutor> logger)
    {
        _executionEngine = executionEngine;
        _logger = logger;
    }

    /// <summary>
    /// Executes a blueprint example interactively
    /// </summary>
    public async Task ExecuteAsync(IBlueprintExample example, DemoContext context, ConsoleRenderer renderer)
    {
        context.Reset();

        // Load blueprint with runtime wallet addresses injected via JSON-e
        context.CurrentBlueprint = await example.GetBlueprintAsync(context.ParticipantWallets);

        var blueprint = context.CurrentBlueprint;
        var currentActionIndex = 0;
        string? previousTxHash = null;
        var workflowData = new Dictionary<string, object>();

        AnsiConsole.MarkupLine($"[bold cyan]Starting Workflow:[/] {blueprint.Title}");
        AnsiConsole.WriteLine();

        while (currentActionIndex < blueprint.Actions.Count)
        {
            var action = blueprint.Actions[currentActionIndex];
            var participant = action.Sender ?? "Unknown";

            // Show action header
            renderer.ShowActionHeader(currentActionIndex, action, participant);

            if (!context.ParticipantWallets.TryGetValue(participant, out var walletAddress))
            {
                AnsiConsole.MarkupLine($"[red]Error: No wallet found for participant '{participant}'[/]");
                return;
            }

            // Prompt for input data
            var inputData = PromptForActionData(action, workflowData);

            if (context.Settings.StepByStepMode)
            {
                if (!AnsiConsole.Confirm($"Execute action as [cyan]{participant}[/]?", true))
                {
                    AnsiConsole.MarkupLine("[yellow]Workflow cancelled by user[/]");
                    return;
                }
            }

            // Create execution context
            var execContext = new Sorcha.Blueprint.Engine.Models.ExecutionContext
            {
                Blueprint = blueprint,
                Action = action,
                ActionData = inputData,
                PreviousData = workflowData.Count > 0 ? new Dictionary<string, object>(workflowData) : null,
                PreviousTransactionHash = previousTxHash,
                InstanceId = $"demo-{System.Guid.NewGuid():N}",
                ParticipantId = participant,
                WalletAddress = walletAddress,
                Mode = Sorcha.Blueprint.Engine.Models.ExecutionMode.Full
            };

            // Execute the action
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .Start($"Executing action {currentActionIndex}...", ctx =>
                {
                    var result = _executionEngine.ExecuteActionAsync(execContext).GetAwaiter().GetResult();

                    // Store execution data
                    var executionData = new ActionExecutionData
                    {
                        ActionIndex = currentActionIndex,
                        ActionTitle = action.Title ?? $"Action {currentActionIndex}",
                        Participant = participant,
                        InputData = inputData,
                        OutputData = result.ProcessedData,
                        CalculationsBefore = new Dictionary<string, object>(inputData),
                        CalculationsAfter = result.CalculatedValues,
                        RoutingDecision = result.Routing.NextParticipantId ?? "__Complete__"
                    };

                    context.ExecutionData[currentActionIndex] = executionData;

                    // Show results
                    ctx.Status("Processing results...");
                    ShowExecutionResult(result, context, renderer);

                    // Merge processed data into workflow data
                    foreach (var kvp in result.ProcessedData)
                    {
                        workflowData[kvp.Key] = kvp.Value;
                    }

                    // Determine next action
                    if (result.Success && result.Routing.NextParticipantId != null &&
                        !result.Routing.IsWorkflowComplete)
                    {
                        // Find next action (in this simplified version, we just go to next in sequence)
                        currentActionIndex++;

                        if (currentActionIndex >= blueprint.Actions.Count)
                        {
                            AnsiConsole.MarkupLine("[green]✓ Workflow complete (end of actions)[/]");
                            return;
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[green]✓ Workflow complete[/]");
                        return;
                    }
                });

            if (context.Settings.StepByStepMode && currentActionIndex < blueprint.Actions.Count)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.Markup("[dim]Press any key to continue to next action...[/]");
                System.Console.ReadKey(true);
                AnsiConsole.WriteLine();
            }
        }

        AnsiConsole.MarkupLine("[green]✓ Workflow complete (all actions executed)[/]");
    }

    /// <summary>
    /// Prompts user for action input data based on the action's schema
    /// </summary>
    private Dictionary<string, object> PromptForActionData(
        Sorcha.Blueprint.Models.Action action,
        Dictionary<string, object> workflowData)
    {
        var data = new Dictionary<string, object>();

        // For demo purposes, we'll generate sample data based on action requirements
        // In a real implementation, this would prompt the user or read from a file

        if (action.DataSchemas != null && action.DataSchemas.Any())
        {
            var schema = action.DataSchemas.First();
            var schemaJson = JsonSerializer.Serialize(schema);
            var schemaObj = JsonSerializer.Deserialize<Dictionary<string, object>>(schemaJson);

            if (schemaObj != null && schemaObj.TryGetValue("properties", out var propsObj))
            {
                var propsJson = JsonSerializer.Serialize(propsObj);
                var properties = JsonSerializer.Deserialize<Dictionary<string, object>>(propsJson);

                if (properties != null)
                {
                    foreach (var prop in properties.Keys)
                    {
                        // Generate sample data based on property name
                        data[prop] = GenerateSampleValue(prop, workflowData);
                    }
                }
            }
        }

        // Display the input data
        var inputPanel = new Panel(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }))
        {
            Header = new PanelHeader("[yellow]Action Input Data[/]"),
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(inputPanel);
        AnsiConsole.WriteLine();

        return data;
    }

    /// <summary>
    /// Generates sample values for demo purposes
    /// </summary>
    private object GenerateSampleValue(string propertyName, Dictionary<string, object> workflowData)
    {
        // Try to reuse values from workflow data first
        if (workflowData.ContainsKey(propertyName))
        {
            return workflowData[propertyName];
        }

        // Generate sample data based on property name
        return propertyName.ToLowerInvariant() switch
        {
            var name when name.Contains("name") => "John Doe",
            var name when name.Contains("email") => "john.doe@example.com",
            var name when name.Contains("amount") => 7500.00,
            var name when name.Contains("date") => DateTime.Now.ToString("yyyy-MM-dd"),
            var name when name.Contains("approved") || name.Contains("confirmed") => true,
            var name when name.Contains("score") => 720,
            var name when name.Contains("status") => "delivered",
            var name when name.Contains("comments") || name.Contains("description") || name.Contains("notes") => $"Sample comment for {propertyName}",
            var name when name.Contains("address") => "123 Main St, City, State 12345",
            var name when name.Contains("department") => "Engineering",
            var name when name.Contains("category") || name.Contains("purpose") => "Business expense",
            var name when name.Contains("income") => 75000,
            var name when name.Contains("code") || name.Contains("number") || name.Contains("tracking") => $"#{System.Guid.NewGuid():N}"[..10].ToUpperInvariant(),
            var name when name.Contains("rate") => 4.5,
            var name when name.Contains("method") => "ACH Transfer",
            var name when name.Contains("employment") => "Full-time",
            var name when name.Contains("phone") => "+1-555-0123",
            _ => $"Sample value for {propertyName}"
        };
    }

    /// <summary>
    /// Shows execution results in the console
    /// </summary>
    private void ShowExecutionResult(ActionExecutionResult result, DemoContext context, ConsoleRenderer renderer)
    {
        if (!result.Success)
        {
            AnsiConsole.MarkupLine("[red]✗ Execution failed[/]");

            if (result.Validation != null && !result.Validation.IsValid)
            {
                AnsiConsole.MarkupLine("[red]Validation errors:[/]");
                foreach (var error in result.Validation.Errors)
                {
                    AnsiConsole.MarkupLine($"  • [red]{error.InstanceLocation}:[/] {error.Message}");
                }
            }

            foreach (var error in result.Errors)
            {
                AnsiConsole.MarkupLine($"  • [red]{error}[/]");
            }
            return;
        }

        AnsiConsole.MarkupLine("[green]✓ Action executed successfully[/]");

        // Show validation result
        if (context.Settings.ShowValidation && result.Validation != null)
        {
            AnsiConsole.MarkupLine($"  [dim]Validation:[/] [green]✓ Passed[/]");
        }

        // Show calculations
        if (context.Settings.ShowCalculations && result.CalculatedValues.Count > 0)
        {
            AnsiConsole.MarkupLine($"  [dim]Calculations:[/] [cyan]{result.CalculatedValues.Count}[/] fields computed");
            foreach (var calc in result.CalculatedValues.Take(3))
            {
                AnsiConsole.MarkupLine($"    • [cyan]{calc.Key}[/] = {JsonSerializer.Serialize(calc.Value)}");
            }
        }

        // Show routing
        if (context.Settings.ShowRouting && result.Routing != null)
        {
            var nextParticipant = result.Routing.NextParticipantId ?? "__Complete__";
            var routingColor = nextParticipant == "__Complete__" ? "green" : "yellow";
            AnsiConsole.MarkupLine($"  [dim]Routing:[/] [{routingColor}]→ {nextParticipant}[/]");
        }

        // Show disclosure count
        if (context.Settings.ShowDisclosure && result.Disclosures.Count > 0)
        {
            AnsiConsole.MarkupLine($"  [dim]Disclosure:[/] [cyan]{result.Disclosures.Count}[/] participants receive filtered data");
        }

        AnsiConsole.WriteLine();
    }
}
