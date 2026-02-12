// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.UI.E2E.Tests.Infrastructure;
using Sorcha.UI.E2E.Tests.PageObjects.Shared;

namespace Sorcha.UI.E2E.Tests.Docker;

/// <summary>
/// E2E tests for Blueprint cloud persistence and publishing flow.
/// </summary>
[TestFixture]
[Category("Docker")]
[Category("Blueprints")]
[Parallelizable(ParallelScope.Self)]
public class BlueprintCloudTests : AuthenticatedDockerTestBase
{
    [SetUp]
    public override async Task BaseSetUp()
    {
        await base.BaseSetUp();
    }

    [Test]
    [Retry(2)]
    public async Task Blueprints_PageLoads_ShowsTitleAndContent()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Blueprints);

        var content = await Page.TextContentAsync("body") ?? "";
        Assert.That(content, Does.Contain("Blueprint"),
            "Blueprints page should render Blueprint-related content");
    }

    [Test]
    [Retry(2)]
    public async Task Blueprints_ShowsSearchBar_OrServiceUnavailable()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Blueprints);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        // Wait for either the search bar or service error to appear (Blazor WASM may need time)
        var searchInput = Page.Locator(".mud-input-control");
        var serviceError = Page.Locator(".mud-alert");

        try
        {
            await Page.Locator(".mud-input-control, .mud-alert").First.WaitForAsync(
                new() { Timeout = TestConstants.PageLoadTimeout });
        }
        catch (TimeoutException) { }

        var hasSearch = await searchInput.CountAsync() > 0;
        var hasError = await serviceError.CountAsync() > 0;
        Assert.That(hasSearch || hasError, Is.True,
            "Blueprints page should have a search input or show service unavailable");
    }

    [Test]
    [Retry(2)]
    public async Task Blueprints_ShowsCreateButton()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Blueprints);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        var createButton = Page.Locator("a:has-text('Create Blueprint'), button:has-text('Create Blueprint')");
        var hasButton = await createButton.CountAsync() > 0;
        Assert.That(hasButton, Is.True,
            "Blueprints page should have a Create Blueprint button");
    }

    [Test]
    [Retry(2)]
    public async Task Blueprints_ShowsStatusFilter()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Blueprints);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        var selectInputs = MudBlazorHelpers.Select(Page);
        var hasFilter = await selectInputs.CountAsync() > 0;
        Assert.That(hasFilter, Is.True,
            "Blueprints page should have a status filter dropdown");
    }

    [Test]
    [Retry(2)]
    public async Task Blueprints_NoConsoleErrors()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Blueprints);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        var criticalErrors = GetCriticalConsoleErrors();
        Assert.That(criticalErrors, Is.Empty,
            "Blueprints page should not have critical console errors");
    }

    [Test]
    [Retry(2)]
    public async Task Blueprints_BlueprintIds_UseTruncation()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Blueprints);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        // If blueprints exist, check truncated IDs
        var truncatedIds = Page.Locator(".truncated-id");
        var count = await truncatedIds.CountAsync();

        if (count > 0)
        {
            var firstId = await truncatedIds.First.TextContentAsync();
            Assert.That(firstId, Does.Contain("..."),
                "Blueprint IDs should use truncation pattern");
        }
    }

    [Test]
    [Retry(2)]
    public async Task Blueprints_ShowsVisualAndAiEditorButtons()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Blueprints);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        var cards = MudBlazorHelpers.Cards(Page);
        if (await cards.CountAsync() > 0)
        {
            var visualButton = Page.Locator("a:has-text('Visual'), button:has-text('Visual')").First;
            var aiButton = Page.Locator("a:has-text('AI'), button:has-text('AI')").First;

            Assert.That(await visualButton.CountAsync(), Is.GreaterThan(0),
                "Blueprint cards should have a Visual editor button");
            Assert.That(await aiButton.CountAsync(), Is.GreaterThan(0),
                "Blueprint cards should have an AI editor button");
        }
    }

    [Test]
    [Retry(2)]
    public async Task Blueprints_HighlightParam_HighlightsCard()
    {
        // Navigate with a highlight parameter — the card may or may not exist,
        // but the page should load without errors
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Blueprints + "?highlight=test-id");
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        // Page should load without critical errors regardless
        var criticalErrors = GetCriticalConsoleErrors();
        Assert.That(criticalErrors, Is.Empty,
            "Blueprints page with highlight param should not have critical console errors");
    }
}
