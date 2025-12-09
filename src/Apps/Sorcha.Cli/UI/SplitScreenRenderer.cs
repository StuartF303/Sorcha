// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sorcha.Cli.UI;

/// <summary>
/// Renders a split-screen layout with workflow progress, activity log, and payload detail
/// </summary>
public class SplitScreenRenderer
{
    private readonly WorkflowProgress _progress;
    private readonly ActivityLog _activityLog;
    private readonly PayloadDetail _payloadDetail;
    private readonly object _renderLock = new();
    private bool _isRendering;

    public SplitScreenRenderer(WorkflowProgress progress, ActivityLog activityLog, PayloadDetail payloadDetail)
    {
        _progress = progress;
        _activityLog = activityLog;
        _payloadDetail = payloadDetail;

        // Wire up payload detail to activity log
        activityLog.SetPayloadDetail(payloadDetail);
    }

    /// <summary>
    /// Renders the split-screen layout once
    /// </summary>
    public void Render()
    {
        lock (_renderLock)
        {
            if (_isRendering) return;
            _isRendering = true;
        }

        try
        {
            // Clear and render
            AnsiConsole.Clear();
            RenderHeader();
            RenderSplitLayout();
        }
        finally
        {
            lock (_renderLock)
            {
                _isRendering = false;
            }
        }
    }

    private void RenderHeader()
    {
        var header = new FigletText("Sorcha CLI")
            .Color(Color.Green)
            .Centered();

        var rule = new Rule("[grey]API Workflow Exerciser[/]")
            .RuleStyle("green dim");

        AnsiConsole.Write(header);
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
    }

    private void RenderSplitLayout()
    {
        // Calculate available height for panels
        var availableHeight = Console.WindowHeight - 12; // Account for header and footer
        var logHeight = Math.Max(10, availableHeight);
        var payloadHeight = Math.Max(20, availableHeight);

        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Main")
                    .SplitColumns(
                        new Layout("Progress").Size(35),    // Progress on left
                        new Layout("Log").Size(35),          // Activity log in middle
                        new Layout("Payload")                // Payload detail on right
                    ),
                new Layout("Footer").Size(3)
            );

        // Progress panel (left side)
        layout["Progress"].Update(_progress.RenderPanel());

        // Activity log panel (middle)
        layout["Log"].Update(_activityLog.RenderPanel(logHeight));

        // Payload detail panel (right side)
        layout["Payload"].Update(_payloadDetail.RenderPanel(payloadHeight));

        // Footer with instructions
        var footer = new Panel(
            new Markup("[grey]Press [white]Ctrl+C[/] to cancel | [white]Space[/] to pause | Panels auto-scroll[/]"))
            .Border(BoxBorder.None);
        layout["Footer"].Update(footer);

        AnsiConsole.Write(layout);
    }

    /// <summary>
    /// Runs with live updating display
    /// </summary>
    public async Task RunLiveAsync(Func<Task> workflow, CancellationToken cancellationToken = default)
    {
        // Subscribe to updates
        var needsUpdate = false;
        _progress.OnProgressChanged += () => needsUpdate = true;
        _activityLog.OnLogAdded += () => needsUpdate = true;
        _payloadDetail.OnPayloadAdded += () => needsUpdate = true;

        // Start workflow in background
        var workflowTask = workflow();

        // Render loop
        while (!workflowTask.IsCompleted && !cancellationToken.IsCancellationRequested)
        {
            if (needsUpdate)
            {
                needsUpdate = false;
                Render();
            }
            await Task.Delay(100, cancellationToken);
        }

        // Final render
        await workflowTask;
        Render();
    }
}

/// <summary>
/// Simple console renderer for non-interactive mode
/// </summary>
public class SimpleConsoleRenderer
{
    private readonly ActivityLog _activityLog;

    public SimpleConsoleRenderer(ActivityLog activityLog)
    {
        _activityLog = activityLog;
    }

    public void RenderStep(int stepNumber, string stepName, StepStatus status, string? detail = null)
    {
        var icon = status switch
        {
            StepStatus.Pending => "[grey]○[/]",
            StepStatus.Running => "[yellow]●[/]",
            StepStatus.Completed => "[green]✓[/]",
            StepStatus.Failed => "[red]✗[/]",
            StepStatus.Skipped => "[grey]−[/]",
            _ => " "
        };

        AnsiConsole.MarkupLine($"{icon} Step {stepNumber}: {Markup.Escape(stepName)}");

        if (!string.IsNullOrEmpty(detail))
        {
            AnsiConsole.MarkupLine($"   [grey]{Markup.Escape(detail)}[/]");
        }
    }

    public void RenderLogPanel(int entries = 10)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(_activityLog.RenderPanel(entries + 2));
    }

    public void RenderSummary(WorkflowProgress progress)
    {
        AnsiConsole.WriteLine();

        var summaryTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Metric")
            .AddColumn("Value");

        summaryTable.AddRow("Total Steps", progress.TotalCount.ToString());
        summaryTable.AddRow("Completed", $"[green]{progress.CompletedCount}[/]");
        summaryTable.AddRow("Failed", progress.HasFailures ? "[red]Yes[/]" : "[green]No[/]");

        AnsiConsole.Write(new Panel(summaryTable)
            .Header("[bold]Workflow Summary[/]")
            .BorderColor(progress.HasFailures ? Color.Red : Color.Green));
    }
}
