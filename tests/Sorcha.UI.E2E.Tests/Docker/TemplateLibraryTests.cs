// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.UI.E2E.Tests.Infrastructure;
using Sorcha.UI.E2E.Tests.PageObjects.Shared;

namespace Sorcha.UI.E2E.Tests.Docker;

/// <summary>
/// E2E tests for Template Library page with backend API integration.
/// </summary>
[TestFixture]
[Category("Docker")]
[Category("Templates")]
[Parallelizable(ParallelScope.Self)]
public class TemplateLibraryTests : AuthenticatedDockerTestBase
{
    [SetUp]
    public override async Task BaseSetUp()
    {
        await base.BaseSetUp();
    }

    [Test]
    [Retry(2)]
    public async Task Templates_PageLoads_ShowsTitleAndContent()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Templates);

        var content = await Page.TextContentAsync("body") ?? "";
        Assert.That(content, Does.Contain("Template"),
            "Templates page should render template-related content");
    }

    [Test]
    [Retry(2)]
    public async Task Templates_ShowsSearchBar()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Templates);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        var searchInput = Page.Locator("input[placeholder*='Search']");
        var hasSearch = await searchInput.CountAsync() > 0;
        Assert.That(hasSearch, Is.True,
            "Templates page should have a search input");
    }

    [Test]
    [Retry(2)]
    public async Task Templates_ShowsTemplateCards_OrEmptyState()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Templates);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        var cards = MudBlazorHelpers.Cards(Page);
        var emptyState = Page.Locator("text=No Templates");
        var serviceError = Page.Locator("text=Service Unavailable");

        var hasContent = await cards.CountAsync() > 0 ||
                         await emptyState.IsVisibleAsync() ||
                         await serviceError.IsVisibleAsync();
        Assert.That(hasContent, Is.True,
            "Page should show template cards, empty state, or service error");
    }

    [Test]
    [Retry(2)]
    public async Task Templates_NoConsoleErrors()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Templates);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        var criticalErrors = GetCriticalConsoleErrors();
        Assert.That(criticalErrors, Is.Empty,
            "Templates page should not have critical console errors");
    }
}
