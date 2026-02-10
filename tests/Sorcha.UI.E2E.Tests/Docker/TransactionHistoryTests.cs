// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.UI.E2E.Tests.Infrastructure;
using Sorcha.UI.E2E.Tests.PageObjects.Shared;

namespace Sorcha.UI.E2E.Tests.Docker;

/// <summary>
/// E2E tests for My Transactions page with real Register Service queries.
/// </summary>
[TestFixture]
[Category("Docker")]
[Category("Transactions")]
[Parallelizable(ParallelScope.Self)]
public class TransactionHistoryTests : AuthenticatedDockerTestBase
{
    [SetUp]
    public override async Task BaseSetUp()
    {
        await base.BaseSetUp();
    }

    [Test]
    [Retry(2)]
    public async Task MyTransactions_PageLoads_ShowsTitleAndContent()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.MyTransactions);

        var content = await Page.TextContentAsync("body") ?? "";
        Assert.That(content, Does.Contain("Transaction"),
            "My Transactions page should render transaction-related content");
    }

    [Test]
    [Retry(2)]
    public async Task MyTransactions_ShowsFilterControls()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.MyTransactions);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        var searchInput = Page.Locator("input[placeholder*='Search']");
        var refreshButton = MudBlazorHelpers.Button(Page, "Refresh");

        var hasSearch = await searchInput.CountAsync() > 0;
        var hasRefresh = await refreshButton.CountAsync() > 0;

        Assert.That(hasSearch || hasRefresh, Is.True,
            "My Transactions page should have filter controls");
    }

    [Test]
    [Retry(2)]
    public async Task MyTransactions_ShowsTable_OrEmptyState()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.MyTransactions);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        var table = MudBlazorHelpers.Table(Page);
        var emptyState = Page.Locator(".mud-main-content >> text=No Transactions").First;
        var serviceError = Page.Locator(".mud-alert:has-text('unavailable')");

        var hasContent = await table.CountAsync() > 0 ||
                         await emptyState.CountAsync() > 0 ||
                         await serviceError.CountAsync() > 0;
        Assert.That(hasContent, Is.True,
            "Page should show transaction table, empty state, or service error");
    }

    [Test]
    [Retry(2)]
    public async Task MyTransactions_TransactionIds_UseTruncation()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.MyTransactions);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        var truncatedIds = Page.Locator(".truncated-id");
        var count = await truncatedIds.CountAsync();

        if (count > 0)
        {
            var firstId = await truncatedIds.First.TextContentAsync();
            Assert.That(firstId, Does.Contain("..."),
                "Transaction IDs should use truncation pattern");
        }
    }

    [Test]
    [Retry(2)]
    public async Task MyTransactions_NoConsoleErrors()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.MyTransactions);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        var criticalErrors = GetCriticalConsoleErrors();
        Assert.That(criticalErrors, Is.Empty,
            "My Transactions page should not have critical console errors");
    }
}
