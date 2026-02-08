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
    public async Task Blueprints_ShowsSearchBar()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Blueprints);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        var searchInput = MudBlazorHelpers.TextField(Page);
        var hasSearch = await searchInput.CountAsync() > 0;
        Assert.That(hasSearch, Is.True,
            "Blueprints page should have a search input");
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
}
