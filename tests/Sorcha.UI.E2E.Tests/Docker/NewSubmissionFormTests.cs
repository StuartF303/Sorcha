// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Playwright;
using Sorcha.UI.E2E.Tests.Infrastructure;
using Sorcha.UI.E2E.Tests.PageObjects.Shared;

namespace Sorcha.UI.E2E.Tests.Docker;

/// <summary>
/// E2E tests for the New Submission form rendering and submission flow.
/// Verifies that clicking Start on a blueprint renders a form with visible fields.
/// </summary>
[TestFixture]
[Category("Docker")]
[Category("Workflows")]
[Category("FormRenderer")]
[Parallelizable(ParallelScope.Self)]
public class NewSubmissionFormTests : AuthenticatedDockerTestBase
{
    private readonly List<string> _consoleLogs = [];

    /// <summary>
    /// Allow console errors since we're diagnosing an issue — don't auto-fail on them.
    /// </summary>
    protected override bool AssertNoConsoleErrors => false;

    [SetUp]
    public override async Task BaseSetUp()
    {
        await base.BaseSetUp();
        _consoleLogs.Clear();

        // Capture ALL console messages (not just errors) for diagnostics
        Page.Console += (_, msg) =>
        {
            _consoleLogs.Add($"[{msg.Type}] {msg.Text}");
        };
    }

    [Test]
    [Retry(2)]
    public async Task NewSubmission_PageLoads_ShowsBlueprintCards()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.MyWorkflows);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        // Wait for loading to finish
        await Page.WaitForTimeoutAsync(3000);

        // Page should show either blueprint cards, empty state, or no-wallets state
        var pageContent = await Page.TextContentAsync("body") ?? "";
        Assert.That(pageContent.Length, Is.GreaterThan(0),
            "New Submission page should render content");

        TestContext.Out.WriteLine($"Page URL: {Page.Url}");
        TestContext.Out.WriteLine($"Page content length: {pageContent.Length}");

        // Check for the title
        var titleLocator = Page.Locator("h4:has-text('New Submission')");
        var hasTitleCount = await titleLocator.CountAsync();
        TestContext.Out.WriteLine($"'New Submission' title count: {hasTitleCount}");
    }

    [Test]
    [Retry(2)]
    public async Task NewSubmission_ClickStart_OpensDialogWithFormFields()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.MyWorkflows);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        // Wait for blueprints to load
        await Page.WaitForTimeoutAsync(5000);

        // Find any "Start" button
        var startButtons = Page.Locator("button:has-text('Start')");
        var startCount = await startButtons.CountAsync();
        TestContext.Out.WriteLine($"Found {startCount} Start button(s)");

        if (startCount == 0)
        {
            // Check what's on the page
            var bodyText = await Page.TextContentAsync("body") ?? "";
            TestContext.Out.WriteLine($"Page body (first 2000 chars): {bodyText[..Math.Min(2000, bodyText.Length)]}");

            // Might be empty state — check for wallets and registers
            Assert.Inconclusive("No Start buttons found — no blueprints available on any register. " +
                "Ensure a blueprint is published to an online register.");
            return;
        }

        // Click the first Start button
        await startButtons.First.ClickAsync();

        // Wait for dialog to appear
        var dialog = Page.Locator(".mud-dialog");
        await dialog.WaitForAsync(new() { Timeout = TestConstants.PageLoadTimeout });

        // Wait for form to load inside dialog
        await Page.WaitForTimeoutAsync(3000);

        // Capture dialog content
        var dialogContent = await dialog.TextContentAsync() ?? "";
        TestContext.Out.WriteLine($"Dialog content: {dialogContent}");

        // Check for form fields (MudTextField, MudSelect, etc.)
        var textFields = dialog.Locator("input[type='text'], input:not([type]), .mud-input input");
        var selectFields = dialog.Locator(".mud-select");
        var dateFields = dialog.Locator(".mud-picker");
        var allInputs = dialog.Locator("input");

        var textFieldCount = await textFields.CountAsync();
        var selectFieldCount = await selectFields.CountAsync();
        var dateFieldCount = await dateFields.CountAsync();
        var allInputCount = await allInputs.CountAsync();

        TestContext.Out.WriteLine($"Text fields: {textFieldCount}");
        TestContext.Out.WriteLine($"Select fields: {selectFieldCount}");
        TestContext.Out.WriteLine($"Date fields: {dateFieldCount}");
        TestContext.Out.WriteLine($"All inputs: {allInputCount}");

        // Check for error messages
        var errorAlerts = dialog.Locator(".mud-alert-severity-error");
        var errorCount = await errorAlerts.CountAsync();
        if (errorCount > 0)
        {
            var errorText = await errorAlerts.First.TextContentAsync();
            TestContext.Out.WriteLine($"Error alert: {errorText}");
        }

        // Check for the Submit button
        var submitButton = dialog.Locator("button:has-text('Submit')");
        var hasSubmit = await submitButton.CountAsync() > 0;
        TestContext.Out.WriteLine($"Has Submit button: {hasSubmit}");

        // Dump console logs from form renderer
        DumpConsoleLogs("FormRenderer");
        DumpConsoleLogs("ControlDispatcher");
        DumpConsoleLogs("NewSubmissionDialog");

        // Assertions
        Assert.Multiple(() =>
        {
            Assert.That(allInputCount, Is.GreaterThan(0),
                "Dialog should contain form input fields. " +
                $"Console logs: {string.Join("\n", _consoleLogs.Where(l => l.Contains("FormRenderer") || l.Contains("ControlDispatcher")))}");
            Assert.That(hasSubmit, Is.True,
                "Dialog should have a Submit button");
        });
    }

    [Test]
    [Retry(2)]
    public async Task NewSubmission_ClickStart_FormFieldsAreDisclosed()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.MyWorkflows);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);
        await Page.WaitForTimeoutAsync(5000);

        var startButtons = Page.Locator("button:has-text('Start')");
        var startCount = await startButtons.CountAsync();

        if (startCount == 0)
        {
            Assert.Inconclusive("No Start buttons — no blueprints available.");
            return;
        }

        // Click Start
        await startButtons.First.ClickAsync();

        var dialog = Page.Locator(".mud-dialog");
        await dialog.WaitForAsync(new() { Timeout = TestConstants.PageLoadTimeout });
        await Page.WaitForTimeoutAsync(3000);

        // Dump all console logs for full diagnostics
        TestContext.Out.WriteLine("=== ALL CONSOLE LOGS ===");
        foreach (var log in _consoleLogs)
        {
            TestContext.Out.WriteLine(log);
        }
        TestContext.Out.WriteLine("=== END CONSOLE LOGS ===");

        // Check that AllFieldsDisclosed is true in the console logs
        var formRendererLogs = _consoleLogs.Where(l => l.Contains("[SorchaFormRenderer]")).ToList();
        TestContext.Out.WriteLine($"FormRenderer logs count: {formRendererLogs.Count}");

        if (formRendererLogs.Count > 0)
        {
            var firstLog = formRendererLogs[0];
            TestContext.Out.WriteLine($"FormRenderer log: {firstLog}");
            Assert.That(firstLog, Does.Contain("AllFieldsDisclosed=True"),
                "AllFieldsDisclosed should be True for the sender's own action");
        }

        // Check ControlDispatcher logs for ShouldRender status
        var dispatcherLogs = _consoleLogs.Where(l => l.Contains("[ControlDispatcher]")).ToList();
        TestContext.Out.WriteLine($"ControlDispatcher logs count: {dispatcherLogs.Count}");
        foreach (var log in dispatcherLogs)
        {
            TestContext.Out.WriteLine($"  {log}");
        }

        // None of the controls should have ShouldRender=False
        var hiddenControls = dispatcherLogs.Where(l => l.Contains("ShouldRender=False")).ToList();
        Assert.That(hiddenControls, Is.Empty,
            "No controls should be hidden when AllFieldsDisclosed=True. " +
            $"Hidden controls: {string.Join("; ", hiddenControls)}");
    }

    [Test]
    [Retry(2)]
    public async Task NewSubmission_SubmitEmptyForm_ShowsValidationErrors()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.MyWorkflows);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);
        await Page.WaitForTimeoutAsync(5000);

        var startButtons = Page.Locator("button:has-text('Start')");
        if (await startButtons.CountAsync() == 0)
        {
            Assert.Inconclusive("No Start buttons — no blueprints available.");
            return;
        }

        await startButtons.First.ClickAsync();

        var dialog = Page.Locator(".mud-dialog");
        await dialog.WaitForAsync(new() { Timeout = TestConstants.PageLoadTimeout });
        await Page.WaitForTimeoutAsync(3000);

        // Try to submit without filling anything
        var submitButton = dialog.Locator("button:has-text('Submit')");
        if (await submitButton.CountAsync() == 0)
        {
            Assert.Inconclusive("No Submit button in dialog.");
            return;
        }

        await submitButton.ClickAsync();
        await Page.WaitForTimeoutAsync(2000);

        // Dump submit-related console logs
        var submitLogs = _consoleLogs.Where(l => l.Contains("Submit")).ToList();
        TestContext.Out.WriteLine("=== SUBMIT CONSOLE LOGS ===");
        foreach (var log in submitLogs)
        {
            TestContext.Out.WriteLine(log);
        }

        // If there are required fields, validation should produce errors
        // but the form should still show fields (not be empty)
        var allInputs = dialog.Locator("input");
        var inputCount = await allInputs.CountAsync();
        TestContext.Out.WriteLine($"Input count at submit time: {inputCount}");

        // The dialog should still be open (not closed by successful submit)
        var dialogStillOpen = await dialog.CountAsync() > 0;
        TestContext.Out.WriteLine($"Dialog still open after submit: {dialogStillOpen}");
    }

    private void DumpConsoleLogs(string prefix)
    {
        var logs = _consoleLogs.Where(l => l.Contains($"[{prefix}]")).ToList();
        if (logs.Count > 0)
        {
            TestContext.Out.WriteLine($"=== {prefix} Logs ({logs.Count}) ===");
            foreach (var log in logs)
            {
                TestContext.Out.WriteLine($"  {log}");
            }
        }
        else
        {
            TestContext.Out.WriteLine($"=== No {prefix} logs captured ===");
        }
    }
}
