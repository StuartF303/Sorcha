// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.DependencyInjection;
using Sorcha.Cli.Services;
using Sorcha.Cli.UI;
using Sorcha.Cli.Workflows;
using Spectre.Console;
using System.CommandLine;

namespace Sorcha.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Sorcha CLI - API Workflow Exerciser")
        {
            Description = "Exercise Sorcha API endpoints with test credentials"
        };

        // Workflow selection option
        var workflowOption = new Option<string>(
            ["--workflow", "-w"],
            () => "all",
            "Workflow to run: health, admin, user, or all");

        // Interactive mode option
        var interactiveOption = new Option<bool>(
            ["--interactive", "-i"],
            () => true,
            "Run in interactive mode with split-screen UI");

        // Verbose option
        var verboseOption = new Option<bool>(
            ["--verbose", "-v"],
            () => false,
            "Show detailed output");

        rootCommand.AddOption(workflowOption);
        rootCommand.AddOption(interactiveOption);
        rootCommand.AddOption(verboseOption);

        rootCommand.SetHandler(async (workflow, interactive, verbose) =>
        {
            await RunAsync(workflow, interactive, verbose);
        }, workflowOption, interactiveOption, verboseOption);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task RunAsync(string workflowName, bool interactive, bool verbose)
    {
        // Setup activity log
        var activityLog = new ActivityLog();
        var progress = new WorkflowProgress();

        // Setup DI
        var services = new ServiceCollection();
        ConfigureServices(services, activityLog);
        var serviceProvider = services.BuildServiceProvider();

        // Display header
        if (!interactive)
        {
            AnsiConsole.Write(new FigletText("Sorcha CLI").Color(Color.Green).Centered());
            AnsiConsole.Write(new Rule("[grey]API Workflow Exerciser[/]").RuleStyle("green dim"));
            AnsiConsole.WriteLine();
        }

        // Get workflows to run
        var workflows = GetWorkflows(serviceProvider, workflowName);

        if (!workflows.Any())
        {
            AnsiConsole.MarkupLine("[red]No workflows selected. Use --workflow option.[/]");
            AnsiConsole.MarkupLine("[grey]Available: health, admin, user, all[/]");
            return;
        }

        // Cancel token
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            AnsiConsole.MarkupLine("\n[yellow]Cancellation requested...[/]");
        };

        if (interactive)
        {
            await RunInteractiveAsync(workflows, progress, activityLog, cts.Token);
        }
        else
        {
            await RunSimpleAsync(workflows, progress, activityLog, verbose, cts.Token);
        }
    }

    static void ConfigureServices(ServiceCollection services, ActivityLog activityLog)
    {
        // Register activity log
        services.AddSingleton(activityLog);

        // Register HTTP client with logging handler
        services.AddTransient<LoggingHttpHandler>();
        services.AddHttpClient("SorchaApi")
            .ConfigurePrimaryHttpMessageHandler(sp => new LoggingHttpHandler(activityLog));

        // Register API clients
        services.AddTransient(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new TenantApiClient(factory.CreateClient("SorchaApi"), activityLog);
        });

        services.AddTransient(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new WalletApiClient(factory.CreateClient("SorchaApi"), activityLog);
        });

        services.AddTransient(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new BlueprintApiClient(factory.CreateClient("SorchaApi"), activityLog);
        });

        services.AddTransient(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new RegisterApiClient(factory.CreateClient("SorchaApi"), activityLog);
        });

        // Register workflows
        services.AddTransient<HealthCheckWorkflow>();
        services.AddTransient<AdminWorkflow>();
        services.AddTransient<UserWorkflow>();
    }

    static IEnumerable<IWorkflow> GetWorkflows(ServiceProvider sp, string workflowName)
    {
        return workflowName.ToLower() switch
        {
            "health" => [sp.GetRequiredService<HealthCheckWorkflow>()],
            "admin" => [sp.GetRequiredService<AdminWorkflow>()],
            "user" => [sp.GetRequiredService<UserWorkflow>()],
            "all" =>
            [
                sp.GetRequiredService<HealthCheckWorkflow>(),
                sp.GetRequiredService<AdminWorkflow>(),
                sp.GetRequiredService<UserWorkflow>()
            ],
            _ => []
        };
    }

    static async Task RunInteractiveAsync(
        IEnumerable<IWorkflow> workflows,
        WorkflowProgress progress,
        ActivityLog activityLog,
        CancellationToken ct)
    {
        var renderer = new SplitScreenRenderer(progress, activityLog);

        foreach (var workflow in workflows)
        {
            if (ct.IsCancellationRequested) break;

            progress.StartWorkflow(workflow.Name, workflow.StepNames);
            activityLog.LogInfo($"Starting workflow: {workflow.Name}");
            activityLog.LogInfo(workflow.Description);

            await renderer.RunLiveAsync(
                async () => await workflow.ExecuteAsync(progress, activityLog, ct),
                ct);

            if (!ct.IsCancellationRequested)
            {
                activityLog.LogSuccess($"Workflow '{workflow.Name}' completed");
                await Task.Delay(1000, ct); // Pause between workflows
            }
        }

        // Final render
        renderer.Render();

        // Summary
        AnsiConsole.WriteLine();
        if (progress.HasFailures)
        {
            AnsiConsole.MarkupLine("[red]Some steps failed. Check the activity log for details.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[green]All workflows completed successfully![/]");
        }

        AnsiConsole.MarkupLine("\n[grey]Press any key to exit...[/]");
        Console.ReadKey(true);
    }

    static async Task RunSimpleAsync(
        IEnumerable<IWorkflow> workflows,
        WorkflowProgress progress,
        ActivityLog activityLog,
        bool verbose,
        CancellationToken ct)
    {
        var consoleRenderer = new SimpleConsoleRenderer(activityLog);

        foreach (var workflow in workflows)
        {
            if (ct.IsCancellationRequested) break;

            AnsiConsole.Write(new Rule($"[green]{workflow.Name}[/]").LeftJustified());
            AnsiConsole.MarkupLine($"[grey]{workflow.Description}[/]");
            AnsiConsole.WriteLine();

            progress.StartWorkflow(workflow.Name, workflow.StepNames);

            try
            {
                await workflow.ExecuteAsync(progress, activityLog, ct);
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("[yellow]Workflow cancelled.[/]");
                break;
            }

            // Show step results
            foreach (var (stepName, index) in workflow.StepNames.Select((s, i) => (s, i)))
            {
                // Find status from activity log or assume completed
                consoleRenderer.RenderStep(index + 1, stepName, StepStatus.Completed);
            }

            if (verbose)
            {
                consoleRenderer.RenderLogPanel(15);
            }

            AnsiConsole.WriteLine();
        }

        // Final summary
        consoleRenderer.RenderSummary(progress);
    }
}
