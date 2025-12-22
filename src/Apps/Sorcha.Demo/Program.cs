// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sorcha.Demo.Configuration;
using Sorcha.Demo.Models;
using Sorcha.Demo.Services.Api;
using Sorcha.Demo.Services.Blueprints;
using Sorcha.Demo.Services.Execution;
using Sorcha.Demo.Services.Storage;
using Sorcha.Demo.UI;
using Spectre.Console;
using System.CommandLine;

namespace Sorcha.Demo;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables(prefix: "SORCHA_")
            .Build();

        // Parse command-line arguments
        var rootCommand = new RootCommand("Sorcha Demo - Blueprint Workflow Execution");

        var automatedOption = new Option<bool>(
            new[] { "--automated", "-a" },
            () => false,
            "Run in automated mode without pauses");

        var blueprintOption = new Option<string?>(
            new[] { "--blueprint", "-b" },
            "Blueprint to run (expense-approval, purchase-order, loan-application)");

        rootCommand.AddOption(automatedOption);
        rootCommand.AddOption(blueprintOption);

        rootCommand.SetHandler(async (automated, blueprint) =>
        {
            await RunDemoAsync(configuration, automated, blueprint);
        }, automatedOption, blueprintOption);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task RunDemoAsync(IConfiguration configuration, bool automated, string? blueprint)
    {
        // Build service provider
        var services = new ServiceCollection();
        ConfigureServices(services, configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Get services
        var renderer = serviceProvider.GetRequiredService<DemoRenderer>();
        var context = serviceProvider.GetRequiredService<DemoContext>();

        // Set mode from command-line
        if (automated)
        {
            context.Settings.StepByStepMode = false;
        }

        // Show welcome
        renderer.ShowWelcomeBanner();

        // If blueprint specified on command line, run it directly
        if (!string.IsNullOrEmpty(blueprint))
        {
            // Add .json extension if not already present
            var blueprintFile = blueprint.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? blueprint
                : $"{blueprint}.json";

            await RunBlueprintAsync(blueprintFile, context, serviceProvider);
            return;
        }

        // Otherwise, show menu
        var running = true;
        while (running)
        {
            var choice = renderer.ShowMainMenu();

            switch (choice)
            {
                case "expense":
                    await RunBlueprintAsync("expense-approval.json", context, serviceProvider);
                    break;

                case "purchase":
                    await RunBlueprintAsync("purchase-order.json", context, serviceProvider);
                    break;

                case "loan":
                    await RunBlueprintAsync("loan-application.json", context, serviceProvider);
                    break;

                case "custom":
                    var customPath = AnsiConsole.Ask<string>("Enter blueprint file path:");
                    await RunCustomBlueprintAsync(customPath, context, serviceProvider);
                    break;

                case "settings":
                    renderer.ShowSettingsMenu(context);
                    break;

                case "exit":
                    running = false;
                    break;
            }
        }

        AnsiConsole.MarkupLine("\n[cyan]Thank you for using Sorcha Demo![/]");
    }

    static void ConfigureServices(ServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        var apiConfig = new SorchaApiConfiguration();
        configuration.GetSection("SorchaApi").Bind(apiConfig);
        services.AddSingleton(apiConfig);

        var demoConfig = new DemoAppConfiguration();
        configuration.GetSection("Demo").Bind(demoConfig);
        services.AddSingleton(demoConfig);

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        // HTTP Client
        services.AddHttpClient();

        // API Clients
        services.AddSingleton<WalletApiClient>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var logger = sp.GetRequiredService<ILogger<WalletApiClient>>();
            var config = sp.GetRequiredService<SorchaApiConfiguration>();
            httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
            return new WalletApiClient(httpClient, logger, config.GetWalletServiceUrl());
        });

        services.AddSingleton<BlueprintApiClient>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var logger = sp.GetRequiredService<ILogger<BlueprintApiClient>>();
            var config = sp.GetRequiredService<SorchaApiConfiguration>();
            httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
            return new BlueprintApiClient(httpClient, logger, config.GetBlueprintServiceUrl());
        });

        services.AddSingleton<RegisterApiClient>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var logger = sp.GetRequiredService<ILogger<RegisterApiClient>>();
            var config = sp.GetRequiredService<SorchaApiConfiguration>();
            httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
            return new RegisterApiClient(httpClient, logger, config.GetRegisterServiceUrl());
        });

        // Blueprint Services
        services.AddSingleton<JsonBlueprintLoader>();
        services.AddSingleton<JsonETemplateEngine>();

        // Storage
        services.AddSingleton<WalletStorage>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<WalletStorage>>();
            var config = sp.GetRequiredService<DemoAppConfiguration>();
            return new WalletStorage(logger, config.GetExpandedWalletStoragePath());
        });

        // Execution Services
        services.AddSingleton<ParticipantManager>();
        services.AddSingleton<BlueprintFlowExecutor>();

        // Demo Context
        services.AddSingleton<DemoContext>();

        // UI
        services.AddSingleton<DemoRenderer>();
    }

    static async Task RunBlueprintAsync(
        string blueprintFileName,
        DemoContext context,
        IServiceProvider serviceProvider)
    {
        try
        {
            var renderer = serviceProvider.GetRequiredService<DemoRenderer>();
            var blueprintLoader = serviceProvider.GetRequiredService<JsonBlueprintLoader>();
            var templateEngine = serviceProvider.GetRequiredService<JsonETemplateEngine>();
            var participantManager = serviceProvider.GetRequiredService<ParticipantManager>();
            var executor = serviceProvider.GetRequiredService<BlueprintFlowExecutor>();
            var walletStorage = serviceProvider.GetRequiredService<WalletStorage>();

            // Reset context
            context.Reset();

            // Load blueprint template
            var template = await blueprintLoader.LoadBlueprintTemplateAsync(blueprintFileName);

            // Extract participant IDs from template (simple JSON parsing)
            var participantIds = ExtractParticipantIds(template);

            if (participantIds.Length == 0)
            {
                AnsiConsole.MarkupLine("[red]Error: No participants found in blueprint[/]");
                return;
            }

            // Ask user: create new wallets or reuse existing?
            bool reuseWallets = false;
            if (walletStorage.WalletsExist())
            {
                // In automated mode, always reuse existing wallets
                if (context.Settings.StepByStepMode)
                {
                    reuseWallets = AnsiConsole.Confirm(
                        "Existing wallets found. Reuse them?",
                        defaultValue: true);
                }
                else
                {
                    reuseWallets = true;
                    AnsiConsole.MarkupLine("[dim]Existing wallets found. Reusing automatically.[/]");
                }
            }

            // Ensure participants have wallets
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .Start("Creating/loading wallets...", ctx =>
                {
                    var participants = participantManager.EnsureParticipantWalletsAsync(
                        participantIds,
                        reuseWallets).GetAwaiter().GetResult();

                    foreach (var (id, participant) in participants)
                    {
                        context.Participants[id] = participant;
                    }
                });

            // Process blueprint template with wallet addresses
            var walletContext = templateEngine.CreateWalletContext(
                context.Participants.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.WalletAddress));

            var processedJson = templateEngine.ProcessTemplate(template, walletContext);
            var blueprint = blueprintLoader.ParseBlueprint(processedJson);

            context.CurrentBlueprint = blueprint;

            // Show blueprint overview
            renderer.ShowBlueprintOverview(blueprint);

            // Show wallet assignments
            renderer.ShowWalletAssignments(context.Participants);

            // In automated mode, skip confirmation
            if (context.Settings.StepByStepMode)
            {
                if (!AnsiConsole.Confirm("Ready to execute blueprint?"))
                {
                    return;
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[dim]Starting blueprint execution...[/]");
            }

            // Execute workflow
            bool success;
            if (context.Settings.StepByStepMode)
            {
                // Interactive mode - prompt for each action
                success = await executor.ExecuteWorkflowAsync(
                    context,
                    promptForInputAsync: async (actionIndex, action) =>
                    {
                        renderer.ShowActionHeader(actionIndex, action, action.Sender ?? "Unknown");
                        return renderer.PromptForActionData(action);
                    },
                    onActionCompleteAsync: async (result) =>
                    {
                        renderer.ShowActionResult(result);
                        if (!result.WorkflowComplete && context.Settings.StepByStepMode)
                        {
                            AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
                            Console.ReadKey(true);
                        }
                        await Task.CompletedTask;
                    });
            }
            else
            {
                // Automated mode - use default values
                success = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Executing workflow...", async ctx =>
                    {
                        return await executor.ExecuteWorkflowAsync(
                            context,
                            onActionCompleteAsync: async (result) =>
                            {
                                ctx.Status($"Executed action {result.ActionIndex}: {result.ActionTitle}");
                                await Task.CompletedTask;
                            });
                    });
            }

            // Show summary
            renderer.ShowWorkflowSummary(context);

            if (success)
            {
                AnsiConsole.MarkupLine("[green]Workflow completed successfully![/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Workflow completed with issues. Check the summary above.[/]");
            }

            // In interactive mode, wait for user
            if (context.Settings.StepByStepMode)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
                Console.ReadKey(true);
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);

            // In interactive mode, wait for user
            if (context.Settings.StepByStepMode)
            {
                AnsiConsole.MarkupLine("\n[dim]Press any key to continue...[/]");
                Console.ReadKey(true);
            }
        }
    }

    static async Task RunCustomBlueprintAsync(
        string filePath,
        DemoContext context,
        IServiceProvider serviceProvider)
    {
        // TODO: Implement custom blueprint loading
        AnsiConsole.MarkupLine("[yellow]Custom blueprint loading not yet implemented[/]");
        await Task.Delay(1000);
    }

    /// <summary>
    /// Extracts participant IDs from blueprint JSON template (simple parsing)
    /// </summary>
    static string[] ExtractParticipantIds(string blueprintJson)
    {
        var participants = new List<string>();

        // Simple regex to find participant IDs
        // Looks for: "id": "ParticipantName" in participants array
        var matches = System.Text.RegularExpressions.Regex.Matches(
            blueprintJson,
            @"""participants""[\s\S]*?\[\s*([\s\S]*?)\s*\]");

        if (matches.Count > 0)
        {
            var participantsBlock = matches[0].Groups[1].Value;
            var idMatches = System.Text.RegularExpressions.Regex.Matches(
                participantsBlock,
                @"""id""\s*:\s*""([^""]+)""");

            foreach (System.Text.RegularExpressions.Match match in idMatches)
            {
                participants.Add(match.Groups[1].Value);
            }
        }

        return participants.ToArray();
    }
}
