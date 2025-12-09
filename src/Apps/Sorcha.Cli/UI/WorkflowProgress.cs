// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sorcha.Cli.UI;

/// <summary>
/// Tracks and displays workflow step progress
/// </summary>
public class WorkflowProgress
{
    private readonly List<WorkflowStep> _steps = [];
    private int _currentStepIndex = -1;
    private string _workflowName = "Workflow";
    private bool _pauseAfterEachStep = true;

    public event Action? OnProgressChanged;

    public bool PauseAfterEachStep
    {
        get => _pauseAfterEachStep;
        set => _pauseAfterEachStep = value;
    }

    public void StartWorkflow(string name, IEnumerable<string> stepNames)
    {
        _workflowName = name;
        _steps.Clear();
        _currentStepIndex = -1;

        foreach (var stepName in stepNames)
        {
            _steps.Add(new WorkflowStep { Name = stepName, Status = StepStatus.Pending });
        }

        OnProgressChanged?.Invoke();
    }

    public void StartStep(string? description = null)
    {
        _currentStepIndex++;
        if (_currentStepIndex < _steps.Count)
        {
            _steps[_currentStepIndex].Status = StepStatus.Running;
            _steps[_currentStepIndex].StartTime = DateTime.Now;
            if (description != null)
            {
                _steps[_currentStepIndex].Description = description;
            }
        }
        OnProgressChanged?.Invoke();
    }

    public void UpdateStepDescription(string description)
    {
        if (_currentStepIndex >= 0 && _currentStepIndex < _steps.Count)
        {
            _steps[_currentStepIndex].Description = description;
        }
        OnProgressChanged?.Invoke();
    }

    public void CompleteStep(string? result = null)
    {
        if (_currentStepIndex >= 0 && _currentStepIndex < _steps.Count)
        {
            _steps[_currentStepIndex].Status = StepStatus.Completed;
            _steps[_currentStepIndex].EndTime = DateTime.Now;
            _steps[_currentStepIndex].Result = result;
        }
        OnProgressChanged?.Invoke();
    }

    /// <summary>
    /// Waits for user input before continuing (if PauseAfterEachStep is enabled)
    /// </summary>
    public void WaitForContinue()
    {
        if (!_pauseAfterEachStep) return;

        _steps[_currentStepIndex].IsPaused = true;
        OnProgressChanged?.Invoke();
    }

    /// <summary>
    /// Continues execution after pause
    /// </summary>
    public void Continue()
    {
        if (_currentStepIndex >= 0 && _currentStepIndex < _steps.Count)
        {
            _steps[_currentStepIndex].IsPaused = false;
            OnProgressChanged?.Invoke();
        }
    }

    public void FailStep(string error)
    {
        if (_currentStepIndex >= 0 && _currentStepIndex < _steps.Count)
        {
            _steps[_currentStepIndex].Status = StepStatus.Failed;
            _steps[_currentStepIndex].EndTime = DateTime.Now;
            _steps[_currentStepIndex].Error = error;
        }
        OnProgressChanged?.Invoke();
    }

    public void SkipStep(string reason)
    {
        _currentStepIndex++;
        if (_currentStepIndex < _steps.Count)
        {
            _steps[_currentStepIndex].Status = StepStatus.Skipped;
            _steps[_currentStepIndex].Result = reason;
        }
        OnProgressChanged?.Invoke();
    }

    public int CompletedCount => _steps.Count(s => s.Status == StepStatus.Completed);
    public int TotalCount => _steps.Count;
    public bool IsComplete => _steps.All(s => s.Status is StepStatus.Completed or StepStatus.Skipped or StepStatus.Failed);
    public bool HasFailures => _steps.Any(s => s.Status == StepStatus.Failed);

    /// <summary>
    /// Renders the progress panel as a Spectre.Console renderable
    /// </summary>
    public Panel RenderPanel()
    {
        var content = new Rows(RenderContent().ToArray());

        return new Panel(content)
            .Header($"[bold green]{Markup.Escape(_workflowName)}[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green);
    }

    private IEnumerable<IRenderable> RenderContent()
    {
        // Progress bar
        var progress = _steps.Count > 0
            ? (double)CompletedCount / TotalCount * 100
            : 0;

        var progressBar = new BreakdownChart()
            .Width(60)
            .AddItem("Complete", CompletedCount, Color.Green)
            .AddItem("Remaining", TotalCount - CompletedCount, Color.Grey);

        yield return new Text($"Progress: {CompletedCount}/{TotalCount} steps ({progress:F0}%)");
        yield return new Text("");

        // Step list
        foreach (var (step, index) in _steps.Select((s, i) => (s, i)))
        {
            var icon = step.Status switch
            {
                StepStatus.Pending => "[grey]○[/]",
                StepStatus.Running => "[yellow]●[/]",
                StepStatus.Completed => "[green]✓[/]",
                StepStatus.Failed => "[red]✗[/]",
                StepStatus.Skipped => "[grey]−[/]",
                _ => " "
            };

            var nameColor = step.Status switch
            {
                StepStatus.Running => "yellow",
                StepStatus.Completed => "green",
                StepStatus.Failed => "red",
                StepStatus.Skipped => "grey",
                _ => "white"
            };

            var duration = step.EndTime.HasValue && step.StartTime.HasValue
                ? $" [grey]({(step.EndTime.Value - step.StartTime.Value).TotalMilliseconds:F0}ms)[/]"
                : step.StartTime.HasValue
                    ? " [grey](running...)[/]"
                    : "";

            var pauseIndicator = step.IsPaused ? " [yellow](⏸ PAUSED - Press any key to continue)[/]" : "";

            yield return new Markup($"  {icon} [{nameColor}]{index + 1}. {Markup.Escape(step.Name)}[/]{duration}{pauseIndicator}");

            if (!string.IsNullOrEmpty(step.Description))
            {
                yield return new Markup($"      [grey]{Markup.Escape(step.Description)}[/]");
            }

            if (!string.IsNullOrEmpty(step.Result))
            {
                yield return new Markup($"      [cyan]→ {Markup.Escape(step.Result)}[/]");
            }

            if (!string.IsNullOrEmpty(step.Error))
            {
                yield return new Markup($"      [red]Error: {Markup.Escape(step.Error)}[/]");
            }
        }
    }
}

public class WorkflowStep
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public StepStatus Status { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Result { get; set; }
    public string? Error { get; set; }
    public bool IsPaused { get; set; }
}

public enum StepStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped
}
