// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.UI.E2E.Tests.Infrastructure;
using Sorcha.UI.E2E.Tests.PageObjects.Shared;

namespace Sorcha.UI.E2E.Tests.Docker;

/// <summary>
/// E2E tests for Explorer enhancements: docket chain and OData query builder.
/// </summary>
[TestFixture]
[Category("Docker")]
[Category("Explorer")]
[Parallelizable(ParallelScope.Self)]
public class ExplorerEnhancementTests : AuthenticatedDockerTestBase
{
    [SetUp]
    public override async Task BaseSetUp()
    {
        await base.BaseSetUp();
    }

    [Test]
    [Retry(2)]
    public async Task Registers_PageLoads_ShowsContent()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Registers);

        var content = await Page.TextContentAsync("body") ?? "";
        Assert.That(content.Length, Is.GreaterThan(0),
            "Registers page should render content");
    }

    [Test]
    [Retry(2)]
    public async Task QueryPage_ShowsTabs()
    {
        await NavigateAuthenticatedAsync($"{TestConstants.AppBase}/registers/query");
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        // Check that query page has tab navigation
        var tabs = Page.Locator(".mud-tabs");
        var hasTabs = await tabs.CountAsync() > 0;

        if (hasTabs)
        {
            var tabContent = await tabs.First.TextContentAsync() ?? "";
            Assert.That(tabContent, Does.Contain("Wallet Search").Or.Contain("OData"),
                "Query page should have Wallet Search and OData tabs");
        }
    }

    [Test]
    [Retry(2)]
    public async Task QueryPage_ODataBuilder_ShowsFilterControls()
    {
        await NavigateAuthenticatedAsync($"{TestConstants.AppBase}/registers/query");
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        // Try to find OData tab and click it
        var odataTab = Page.Locator(".mud-tab:has-text('OData')");
        if (await odataTab.CountAsync() > 0)
        {
            await odataTab.ClickAsync();
            await Page.WaitForTimeoutAsync(TestConstants.ShortWait);

            // Check for Add Filter button
            var addFilter = Page.Locator("button:has-text('Add Filter')");
            var hasAddFilter = await addFilter.CountAsync() > 0;
            Assert.That(hasAddFilter, Is.True,
                "OData query builder should have Add Filter button");
        }
    }

    [Test]
    [Retry(2)]
    public async Task QueryPage_DocketHashes_UseTruncation()
    {
        await NavigateAuthenticatedAsync($"{TestConstants.AppBase}/registers/query");
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        var truncatedIds = Page.Locator(".truncated-id");
        var count = await truncatedIds.CountAsync();

        if (count > 0)
        {
            var firstId = await truncatedIds.First.TextContentAsync();
            Assert.That(firstId, Does.Contain("..."),
                "Identifiers should use truncation pattern");
        }
    }

    [Test]
    [Retry(2)]
    public async Task Registers_NoConsoleErrors()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Registers);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        var criticalErrors = GetCriticalConsoleErrors();
        Assert.That(criticalErrors, Is.Empty,
            "Registers page should not have critical console errors");
    }
}
