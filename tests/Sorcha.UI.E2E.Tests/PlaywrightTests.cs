// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Playwright.NUnit;

namespace Sorcha.UI.E2E.Tests;

/// <summary>
/// End-to-end tests for Blazor UI using Playwright
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class BlazorUITests : PageTest
{
    private DistributedApplication? _app;
    private string? _blazorUrl;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // Start the Aspire application
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Sorcha_AppHost>();

        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        // Get the Blazor client URL
        _blazorUrl = _app.GetEndpoint("ui-web").ToString();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_app != null)
        {
            await _app.DisposeAsync();
        }
    }

    [Test]
    public async Task HomePage_LoadsSuccessfully()
    {
        await Page.GotoAsync(_blazorUrl!);
        await Page.WaitForLoadStateAsync();

        // Should contain the app title
        await Expect(Page).ToHaveTitleAsync(new System.Text.RegularExpressions.Regex("Sorcha|Blueprint"));
    }

    [Test]
    public async Task Navigation_WorksBetweenPages()
    {
        await Page.GotoAsync(_blazorUrl!);
        await Page.WaitForLoadStateAsync();

        // Click on Designer link
        var designerLink = Page.Locator("a:has-text('Designer')");
        if (await designerLink.CountAsync() > 0)
        {
            await designerLink.First.ClickAsync();
            await Page.WaitForLoadStateAsync();

            // Should navigate to designer page
            await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/designer"));
        }
    }

    [Test]
    public async Task BlueprintLibrary_Loads()
    {
        await Page.GotoAsync($"{_blazorUrl}/library");
        await Page.WaitForLoadStateAsync();

        // Should show library page
        var pageContent = await Page.TextContentAsync("body");
        Assert.That(pageContent, Does.Contain("Library").Or.Contain("Blueprint"));
    }

    [Test]
    public async Task SchemaExplorer_Opens()
    {
        await Page.GotoAsync($"{_blazorUrl}/schemas");
        await Page.WaitForLoadStateAsync();

        // Should show schema explorer
        var pageContent = await Page.TextContentAsync("body");
        Assert.That(pageContent, Does.Contain("Schema").Or.Contain("Explorer"));
    }

    [Test]
    public async Task AdminPage_ShowsServices()
    {
        await Page.GotoAsync($"{_blazorUrl}/admin");
        await Page.WaitForLoadStateAsync();

        // Wait for service status to load
        await Page.WaitForTimeoutAsync(2000);

        // Should show service information
        var pageContent = await Page.TextContentAsync("body");
        Assert.That(pageContent, Does.Contain("Blueprint").Or.Contain("Peer").Or.Contain("Service"));
    }

    [Test]
    public async Task Designer_CanOpenNewBlueprint()
    {
        await Page.GotoAsync($"{_blazorUrl}/designer");
        await Page.WaitForLoadStateAsync();

        // Look for "New" button or similar
        var newButton = Page.Locator("button:has-text('New')");
        if (await newButton.CountAsync() > 0)
        {
            await newButton.First.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);

            // Should show designer canvas or dialog
            var hasDialog = await Page.Locator("[role='dialog']").CountAsync() > 0;
            var hasCanvas = await Page.Locator("canvas, svg").CountAsync() > 0;

            Assert.That(hasDialog || hasCanvas, Is.True, "Should show dialog or canvas");
        }
    }

    [Test]
    public async Task EventLog_IsAccessible()
    {
        await Page.GotoAsync($"{_blazorUrl}/events");
        await Page.WaitForLoadStateAsync();

        // Should show event log
        var pageContent = await Page.TextContentAsync("body");
        Assert.That(pageContent, Does.Contain("Event").Or.Contain("Log"));
    }

    [Test]
    public async Task MudBlazorComponents_Load()
    {
        await Page.GotoAsync(_blazorUrl!);
        await Page.WaitForLoadStateAsync();

        // Should have MudBlazor CSS loaded
        var mudStyles = await Page.Locator("link[href*='MudBlazor']").CountAsync();
        Assert.That(mudStyles, Is.GreaterThan(0), "MudBlazor styles should be loaded");
    }

    [Test]
    public async Task NoJavaScriptErrors()
    {
        var errors = new List<string>();
        Page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                errors.Add(msg.Text);
            }
        };

        await Page.GotoAsync(_blazorUrl!);
        await Page.WaitForLoadStateAsync();
        await Page.WaitForTimeoutAsync(2000);

        // Allow Blazor-specific errors but no critical errors
        var criticalErrors = errors.Where(e =>
            !e.Contains("WASM") &&
            !e.Contains("Blazor") &&
            !e.Contains("__webpack")).ToList();

        Assert.That(criticalErrors, Is.Empty, $"Should have no critical JavaScript errors. Found: {string.Join(", ", criticalErrors)}");
    }

    [Test]
    public async Task ResponsiveDesign_Works()
    {
        // Test mobile viewport
        await Page.SetViewportSizeAsync(375, 667);
        await Page.GotoAsync(_blazorUrl!);
        await Page.WaitForLoadStateAsync();

        // Should still be usable
        var pageContent = await Page.TextContentAsync("body");
        Assert.That(pageContent, Is.Not.Empty);

        // Test desktop viewport
        await Page.SetViewportSizeAsync(1920, 1080);
        await Page.ReloadAsync();
        await Page.WaitForLoadStateAsync();

        // Should still be usable
        pageContent = await Page.TextContentAsync("body");
        Assert.That(pageContent, Is.Not.Empty);
    }
}
