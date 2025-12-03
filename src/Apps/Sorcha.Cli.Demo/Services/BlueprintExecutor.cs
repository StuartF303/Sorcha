using Sorcha.Cli.Demo.Utilities;
using Sorcha.Blueprint.Engine.Interfaces;
using Sorcha.Blueprint.Engine.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Text;
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

            // Show what will happen in this action
            ShowActionExplanation(action, workflowData);

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

            // Show disclosure views after execution
            if (context.Settings.ShowDisclosure && action.Disclosures != null && action.Disclosures.Any())
            {
                ShowDisclosureViews(action, inputData, context);
            }

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

        if (action.DataSchemas != null && action.DataSchemas.Any())
        {
            var schema = action.DataSchemas.First();
            var schemaJson = JsonSerializer.Serialize(schema);
            var schemaObj = JsonSerializer.Deserialize<Dictionary<string, object>>(schemaJson);

            if (schemaObj != null && schemaObj.TryGetValue("properties", out var propsObj))
            {
                var propsJson = JsonSerializer.Serialize(propsObj);
                var properties = JsonSerializer.Deserialize<Dictionary<string, object>>(propsJson);

                // Get required fields
                var requiredFields = new List<string>();
                if (schemaObj.TryGetValue("required", out var reqObj))
                {
                    var reqJson = JsonSerializer.Serialize(reqObj);
                    var reqList = JsonSerializer.Deserialize<List<string>>(reqJson);
                    if (reqList != null)
                        requiredFields = reqList;
                }

                if (properties != null)
                {
                    AnsiConsole.MarkupLine("[yellow]📝 Enter data for this action:[/]");
                    AnsiConsole.WriteLine();

                    foreach (var prop in properties.Keys)
                    {
                        var isRequired = requiredFields.Contains(prop);
                        var propJson = JsonSerializer.Serialize(properties[prop]);
                        var propDef = JsonSerializer.Deserialize<Dictionary<string, object>>(propJson);

                        // Get property metadata
                        var propType = propDef?.ContainsKey("type") == true ? propDef["type"].ToString() : "string";
                        var propTitle = propDef?.ContainsKey("title") == true ? propDef["title"].ToString() : prop;
                        var propEnum = propDef?.ContainsKey("enum") == true ? propDef["enum"] : null;

                        // Prompt for value based on type
                        data[prop] = PromptForValue(prop, propTitle!, propType!, propEnum, isRequired, workflowData);
                    }

                    AnsiConsole.WriteLine();
                }
            }
        }

        // Display the collected input data
        var inputPanel = new Panel(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }))
        {
            Header = new PanelHeader("[yellow]✅ Action Input Data (Collected)[/]"),
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(inputPanel);
        AnsiConsole.WriteLine();

        return data;
    }

    /// <summary>
    /// Prompts for a single value based on its type and constraints
    /// </summary>
    private object PromptForValue(
        string propertyName,
        string propertyTitle,
        string propertyType,
        object? enumValues,
        bool isRequired,
        Dictionary<string, object> workflowData)
    {
        var requiredMarker = isRequired ? "[red]*[/]" : "";
        var defaultValue = GenerateSampleValue(propertyName, workflowData);

        // If value exists in workflow data, offer to reuse it
        if (workflowData.ContainsKey(propertyName))
        {
            var existingValue = workflowData[propertyName];
            AnsiConsole.MarkupLine($"[dim]📌 Using value from previous action: {propertyName} = {JsonSerializer.Serialize(existingValue)}[/]");
            return existingValue;
        }

        // Handle enum/choice fields
        if (enumValues != null)
        {
            var enumJson = JsonSerializer.Serialize(enumValues);
            var choices = JsonSerializer.Deserialize<List<string>>(enumJson);
            if (choices != null && choices.Any())
            {
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title($"{requiredMarker} [cyan]{propertyTitle}[/]:")
                        .AddChoices(choices)
                        .AddChoices("<Use default>"));

                return choice == "<Use default>" ? choices.First() : choice;
            }
        }

        // Handle boolean fields
        if (propertyType == "boolean")
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"{requiredMarker} [cyan]{propertyTitle}[/]:")
                    .AddChoices(new[] { "Yes", "No", "<Use default: Yes>" }));

            if (choice == "<Use default: Yes>")
                return true;
            return choice == "Yes";
        }

        // Handle number fields
        if (propertyType == "number")
        {
            var input = AnsiConsole.Prompt(
                new TextPrompt<string>($"{requiredMarker} [cyan]{propertyTitle}[/]:")
                    .DefaultValue(defaultValue.ToString()!)
                    .AllowEmpty());

            if (string.IsNullOrWhiteSpace(input))
                return defaultValue;

            if (double.TryParse(input, out var numValue))
                return numValue;

            return defaultValue;
        }

        // Handle string fields (default)
        var textInput = AnsiConsole.Prompt(
            new TextPrompt<string>($"{requiredMarker} [cyan]{propertyTitle}[/]:")
                .DefaultValue(defaultValue.ToString()!)
                .AllowEmpty());

        return string.IsNullOrWhiteSpace(textInput) ? defaultValue : textInput;
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

        // Show routing with detailed condition evaluation
        if (context.Settings.ShowRouting && result.Routing != null)
        {
            ShowRoutingDecision(result, context);
        }

        // Show disclosure summary
        if (context.Settings.ShowDisclosure && result.Disclosures.Count > 0)
        {
            AnsiConsole.MarkupLine($"  [dim]Disclosure:[/] [cyan]{result.Disclosures.Count}[/] participant(s) will receive data (details below)");
        }

        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Shows an explanation of what will happen during this action
    /// </summary>
    private void ShowActionExplanation(Sorcha.Blueprint.Models.Action action, Dictionary<string, object> workflowData)
    {
        var explanationText = new StringBuilder();
        explanationText.AppendLine("[bold yellow]📖 What will happen in this action:[/]");
        explanationText.AppendLine();

        // Explain data collection
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
                    explanationText.AppendLine($"   [cyan]1. Data Collection:[/] You'll be prompted to enter [yellow]{properties.Count}[/] field(s)");
                }
            }
        }

        // Explain validation
        explanationText.AppendLine($"   [cyan]2. Validation:[/] Input will be validated against JSON Schema");

        // Explain calculations
        if (action.Calculations != null && action.Calculations.Any())
        {
            explanationText.AppendLine($"   [cyan]3. Calculations:[/] [yellow]{action.Calculations.Count}[/] field(s) will be computed");
        }

        // Explain routing
        if (action.Condition != null)
        {
            explanationText.AppendLine($"   [cyan]4. Routing:[/] The workflow will determine the next participant based on your input");

            // Try to extract routing information from condition
            var conditionJson = JsonSerializer.Serialize(action.Condition);
            if (conditionJson.Contains("\"if\""))
            {
                explanationText.AppendLine($"      [dim]• Conditional routing will be evaluated[/]");
            }
        }
        else
        {
            explanationText.AppendLine($"   [cyan]4. Routing:[/] Workflow will proceed to next action");
        }

        // Explain disclosure
        if (action.Disclosures != null && action.Disclosures.Any())
        {
            explanationText.AppendLine($"   [cyan]5. Disclosure:[/] Data will be selectively shared with [yellow]{action.Disclosures.Count()}[/] participant(s)");
            foreach (var disclosure in action.Disclosures.Take(5))
            {
                var recipientId = disclosure.ParticipantAddress ?? "Unknown";
                var pointerCount = disclosure.DataPointers?.Count() ?? 0;
                var dataScope = pointerCount == 1 && disclosure.DataPointers?.First() == "/*"
                    ? "all fields"
                    : $"only {pointerCount} field(s)";
                var icon = dataScope.Contains("all") ? "👁️" : "🔒";
                explanationText.AppendLine($"      [dim]{icon} {recipientId} → will see {dataScope}[/]");
            }
            explanationText.AppendLine($"      [dim]💡 After execution, you'll see what each participant receives[/]");
        }

        var panel = new Panel(explanationText.ToString())
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Yellow)
        };
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Shows detailed routing decision with condition evaluation
    /// </summary>
    private void ShowRoutingDecision(ActionExecutionResult result, DemoContext context)
    {
        var nextParticipant = result.Routing.NextParticipantId ?? "__Complete__";
        var routingColor = nextParticipant == "__Complete__" ? "green" : "yellow";

        var routingPanel = new Panel(BuildRoutingExplanation(result, context))
        {
            Header = new PanelHeader($"[bold cyan]🔀 Routing Decision[/]"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(routingPanel);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Builds a detailed explanation of the routing decision
    /// </summary>
    private string BuildRoutingExplanation(ActionExecutionResult result, DemoContext context)
    {
        var explanation = new StringBuilder();
        var nextParticipant = result.Routing.NextParticipantId ?? "__Complete__";
        var routingColor = nextParticipant == "__Complete__" ? "green" : "yellow";

        // Show the decision
        explanation.AppendLine($"[bold]Decision:[/] Route to [{routingColor}]{nextParticipant}[/]");
        explanation.AppendLine();

        // If there was a condition evaluated, show the details
        if (result.Routing.MatchedCondition != null)
        {
            explanation.AppendLine($"[yellow]Matched Condition:[/]");
            explanation.AppendLine($"[dim]{result.Routing.MatchedCondition}[/]");
            explanation.AppendLine();
        }

        // Try to show the specific checks that were performed
        var currentAction = context.CurrentBlueprint?.Actions.FirstOrDefault(a =>
            result.ProcessedData.ContainsKey("actionId") &&
            a.Id.ToString() == result.ProcessedData["actionId"].ToString());

        if (currentAction?.Condition != null)
        {
            explanation.AppendLine($"[yellow]Condition Logic:[/]");

            var conditionJson = JsonSerializer.Serialize(currentAction.Condition, new JsonSerializerOptions { WriteIndented = true });
            var condition = JsonSerializer.Deserialize<Dictionary<string, object>>(conditionJson);

            if (condition != null && condition.ContainsKey("if"))
            {
                var ifArray = JsonSerializer.Deserialize<object[]>(JsonSerializer.Serialize(condition["if"]));

                if (ifArray != null && ifArray.Length >= 3)
                {
                    // Extract the condition, true path, and false path
                    var checkCondition = ifArray[0];
                    var truePath = ifArray[1];
                    var falsePath = ifArray[2];

                    explanation.AppendLine($"   IF: [cyan]{JsonSerializer.Serialize(checkCondition)}[/]");
                    explanation.AppendLine($"   THEN: Route to [yellow]{truePath}[/]");
                    explanation.AppendLine($"   ELSE: Route to [yellow]{falsePath}[/]");
                    explanation.AppendLine();

                    // Try to extract the actual values being checked
                    var checkJson = JsonSerializer.Serialize(checkCondition);
                    if (checkJson.Contains("\"var\""))
                    {
                        var checkObj = JsonSerializer.Deserialize<Dictionary<string, object>>(checkJson);
                        if (checkObj != null)
                        {
                            // Extract operator and variable
                            var op = checkObj.Keys.FirstOrDefault(k => k != "var");
                            if (op != null)
                            {
                                explanation.AppendLine($"[yellow]Values Checked:[/]");

                                // Show relevant data from processed data
                                foreach (var kvp in result.ProcessedData.Take(5))
                                {
                                    if (checkJson.Contains($"\"{kvp.Key}\""))
                                    {
                                        explanation.AppendLine($"   • [cyan]{kvp.Key}[/] = [yellow]{JsonSerializer.Serialize(kvp.Value)}[/]");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return explanation.ToString();
    }

    /// <summary>
    /// Shows what each participant sees based on disclosure settings
    /// </summary>
    private void ShowDisclosureViews(
        Sorcha.Blueprint.Models.Action action,
        Dictionary<string, object> fullData,
        DemoContext context)
    {
        AnsiConsole.WriteLine();
        var rule = new Rule("[bold yellow]📤 Data Distribution (Selective Disclosure)[/]")
        {
            Justification = Justify.Left,
            Style = new Style(Color.Yellow)
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        // Show what the current participant submitted (full data)
        var senderPanel = new Panel(JsonSerializer.Serialize(fullData, new JsonSerializerOptions { WriteIndented = true }))
        {
            Header = new PanelHeader($"[bold cyan]📝 {action.Sender} (Sender) - Full Data Submitted[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1)
        };
        AnsiConsole.Write(senderPanel);
        AnsiConsole.WriteLine();

        // Show what each disclosed recipient sees
        if (action.Disclosures != null)
        {
            foreach (var disclosure in action.Disclosures)
            {
                var recipientId = disclosure.ParticipantAddress ?? "Unknown";

                // Skip if recipient is the sender
                if (recipientId == action.Sender)
                    continue;

                // Filter data based on disclosure pointers
                var filteredData = FilterDataByPointers(fullData, disclosure.DataPointers);
                var isFullDisclosure = disclosure.DataPointers?.Count() == 1 &&
                                      disclosure.DataPointers.First() == "/*";

                // Create panel showing what this participant receives
                var disclosureType = isFullDisclosure ? "Full Data" : "Filtered Data";
                var borderColor = isFullDisclosure ? Color.Green : Color.Yellow;
                var icon = isFullDisclosure ? "👁️" : "🔒";

                var recipientPanel = new Panel(
                    JsonSerializer.Serialize(filteredData, new JsonSerializerOptions { WriteIndented = true }))
                {
                    Header = new PanelHeader($"[bold]{icon} {recipientId} (Recipient) - {disclosureType}[/]"),
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(borderColor)
                };

                // Add explanation of what was filtered
                if (!isFullDisclosure)
                {
                    var pointerList = string.Join(", ", disclosure.DataPointers ?? Enumerable.Empty<string>());
                    AnsiConsole.MarkupLine($"[dim]  Disclosed fields: {pointerList}[/]");
                }

                AnsiConsole.Write(recipientPanel);
                AnsiConsole.WriteLine();
            }
        }

        AnsiConsole.MarkupLine("[dim]💡 Notice: Different participants see different data based on disclosure settings[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Filters data based on JSON Pointer paths
    /// </summary>
    private Dictionary<string, object> FilterDataByPointers(
        Dictionary<string, object> fullData,
        IEnumerable<string>? pointers)
    {
        if (pointers == null || !pointers.Any())
            return new Dictionary<string, object>();

        // If wildcard pointer, return all data
        if (pointers.Count() == 1 && pointers.First() == "/*")
            return new Dictionary<string, object>(fullData);

        var filtered = new Dictionary<string, object>();

        foreach (var pointer in pointers)
        {
            // JSON Pointer format: "/fieldName"
            var fieldName = pointer.TrimStart('/');

            if (fullData.ContainsKey(fieldName))
            {
                filtered[fieldName] = fullData[fieldName];
            }
        }

        return filtered;
    }
}
