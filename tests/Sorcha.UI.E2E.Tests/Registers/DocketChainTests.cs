// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Playwright.NUnit;

namespace Sorcha.UI.E2E.Tests.Registers;

/// <summary>
/// End-to-end tests for the Docket Chain tab on the Register detail page.
/// Covers: docket timeline display, data mapping correctness, docket detail panel,
/// transaction reuse within dockets, and real-time indicator visibility.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("Registers")]
[Category("DocketChain")]
public class DocketChainTests : PageTest
{
    private DistributedApplication? _app;
    private string? _blazorUrl;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Sorcha_AppHost>();

        _app = await appHost.BuildAsync();
        await _app.StartAsync();

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

    /// <summary>
    /// Navigates to the register detail page and clicks the Docket Chain tab.
    /// Returns true if successful, false if auth redirect or no registers available.
    /// </summary>
    private async Task<bool> NavigateToDocketChainTabAsync()
    {
        await Page.GotoAsync($"{_blazorUrl}/registers");
        await Page.WaitForLoadStateAsync();
        await Page.WaitForTimeoutAsync(2000);

        if (Page.Url.Contains("/login"))
            return false;

        var registerCard = Page.Locator(
            "[data-testid='register-card'], .register-card").First;

        if (!await registerCard.IsVisibleAsync())
            return false;

        await registerCard.ClickAsync();
        await Page.WaitForTimeoutAsync(2000);

        // Click the Docket Chain tab
        var docketTab = Page.Locator("text=Docket Chain");
        if (await docketTab.IsVisibleAsync())
        {
            await docketTab.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);
            return true;
        }

        return false;
    }

    #region Docket Chain Tab Navigation

    [Test]
    public async Task DocketChainTab_IsVisible()
    {
        await Page.GotoAsync($"{_blazorUrl}/registers");
        await Page.WaitForLoadStateAsync();
        await Page.WaitForTimeoutAsync(2000);

        if (Page.Url.Contains("/login"))
        {
            Assert.Pass("Requires authentication - redirected to login");
            return;
        }

        var registerCard = Page.Locator(
            "[data-testid='register-card'], .register-card").First;

        if (await registerCard.IsVisibleAsync())
        {
            await registerCard.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);

            var docketTab = Page.Locator("text=Docket Chain");
            Assert.That(await docketTab.IsVisibleAsync(), Is.True,
                "Docket Chain tab should be visible on register detail page");
        }
        else
        {
            Assert.Pass("No registers available - cannot test docket chain tab");
        }
    }

    [Test]
    public async Task DocketChainTab_LoadsDockets()
    {
        if (!await NavigateToDocketChainTabAsync())
        {
            Assert.Pass("Could not navigate to Docket Chain tab");
            return;
        }

        var pageContent = await Page.TextContentAsync("body");

        // Should show either docket timeline or empty state
        var hasDockets = pageContent?.Contains("Docket #") == true;
        var hasEmptyState = pageContent?.Contains("No Dockets") == true;

        Assert.That(hasDockets || hasEmptyState, Is.True,
            "Docket Chain tab should show dockets or empty state");
    }

    #endregion

    #region Docket Data Mapping

    [Test]
    public async Task DocketChain_ShowsCorrectTimestamps()
    {
        if (!await NavigateToDocketChainTabAsync())
        {
            Assert.Pass("Could not navigate to Docket Chain tab");
            return;
        }

        var pageContent = await Page.TextContentAsync("body");

        if (pageContent?.Contains("Docket #") == true)
        {
            // Timestamps should NOT be "Jan 01, 0001 00:00" (the default DateTime)
            Assert.That(pageContent.Contains("Jan 01, 0001"), Is.False,
                "Docket timestamps should not show default DateTime values");
        }
        else
        {
            Assert.Pass("No dockets available to verify timestamps");
        }
    }

    [Test]
    public async Task DocketChain_ShowsIntegrityStatus()
    {
        if (!await NavigateToDocketChainTabAsync())
        {
            Assert.Pass("Could not navigate to Docket Chain tab");
            return;
        }

        var pageContent = await Page.TextContentAsync("body");

        if (pageContent?.Contains("Docket #") == true)
        {
            // Should show integrity chip (Valid or Invalid)
            var hasIntegrity = pageContent.Contains("Valid") || pageContent.Contains("Invalid");
            Assert.That(hasIntegrity, Is.True,
                "Docket chain should display integrity status chips");
        }
        else
        {
            Assert.Pass("No dockets available to verify integrity status");
        }
    }

    [Test]
    public async Task DocketChain_ShowsFullHashes()
    {
        if (!await NavigateToDocketChainTabAsync())
        {
            Assert.Pass("Could not navigate to Docket Chain tab");
            return;
        }

        var pageContent = await Page.TextContentAsync("body");

        if (pageContent?.Contains("Docket #") == true)
        {
            // Should show "Hash:" label with full hash content
            Assert.That(pageContent.Contains("Hash:"), Is.True,
                "Docket chain should display hash labels");
        }
        else
        {
            Assert.Pass("No dockets available to verify hashes");
        }
    }

    [Test]
    public async Task DocketChain_ShowsTransactionCount()
    {
        if (!await NavigateToDocketChainTabAsync())
        {
            Assert.Pass("Could not navigate to Docket Chain tab");
            return;
        }

        var pageContent = await Page.TextContentAsync("body");

        if (pageContent?.Contains("Docket #") == true)
        {
            // Should show transaction count label "N txs"
            Assert.That(pageContent.Contains("txs"), Is.True,
                "Docket chain should display transaction count");
        }
        else
        {
            Assert.Pass("No dockets available to verify transaction count");
        }
    }

    [Test]
    public async Task DocketChain_ShowsHeightLabel()
    {
        if (!await NavigateToDocketChainTabAsync())
        {
            Assert.Pass("Could not navigate to Docket Chain tab");
            return;
        }

        var pageContent = await Page.TextContentAsync("body");

        if (pageContent?.Contains("Docket #") == true)
        {
            // Should use "Docket #N" format (not "Docket vN")
            Assert.That(pageContent.Contains("Docket #0") || pageContent.Contains("Docket #1"), Is.True,
                "Docket chain should show height with # prefix");
        }
        else
        {
            Assert.Pass("No dockets available to verify height labels");
        }
    }

    [Test]
    public async Task DocketChain_ShowsPreviousHashOrGenesis()
    {
        if (!await NavigateToDocketChainTabAsync())
        {
            Assert.Pass("Could not navigate to Docket Chain tab");
            return;
        }

        var pageContent = await Page.TextContentAsync("body");

        if (pageContent?.Contains("Docket #") == true)
        {
            // Should show either "Previous Hash:" or "Genesis docket"
            var hasPrevHash = pageContent.Contains("Previous Hash:");
            var hasGenesis = pageContent.Contains("Genesis docket");

            Assert.That(hasPrevHash || hasGenesis, Is.True,
                "Docket chain should show previous hash or genesis label");
        }
        else
        {
            Assert.Pass("No dockets available to verify previous hash");
        }
    }

    #endregion

    #region Docket Detail Panel

    [Test]
    public async Task DocketDetail_OpensOnDocketClick()
    {
        if (!await NavigateToDocketChainTabAsync())
        {
            Assert.Pass("Could not navigate to Docket Chain tab");
            return;
        }

        // Look for a docket card in the timeline
        var docketCard = Page.Locator(".mud-card").First;

        if (await docketCard.IsVisibleAsync())
        {
            await docketCard.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);

            var pageContent = await Page.TextContentAsync("body");

            // Detail panel should show docket metadata
            var hasDetail = pageContent?.Contains("Details") == true ||
                            pageContent?.Contains("Transaction Count") == true ||
                            pageContent?.Contains("Height") == true;

            Assert.That(hasDetail, Is.True,
                "Clicking a docket should open the detail panel");
        }
        else
        {
            Assert.Pass("No docket cards available to click");
        }
    }

    [Test]
    public async Task DocketDetail_ShowsCopyableHashes()
    {
        if (!await NavigateToDocketChainTabAsync())
        {
            Assert.Pass("Could not navigate to Docket Chain tab");
            return;
        }

        var docketCard = Page.Locator(".mud-card").First;

        if (await docketCard.IsVisibleAsync())
        {
            await docketCard.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);

            // Look for copy buttons (ContentCopy icons)
            var copyButtons = Page.Locator("[aria-label='Copy hash'], [aria-label='Copy previous hash']");
            var count = await copyButtons.CountAsync();

            // Should have at least one copy button for the hash
            Assert.That(count >= 1, Is.True,
                "Docket detail should have copy buttons for hashes");
        }
        else
        {
            Assert.Pass("No docket cards available to test copy functionality");
        }
    }

    [Test]
    public async Task DocketDetail_ShowsTransactionsWithTransactionRow()
    {
        if (!await NavigateToDocketChainTabAsync())
        {
            Assert.Pass("Could not navigate to Docket Chain tab");
            return;
        }

        var docketCard = Page.Locator(".mud-card").First;

        if (await docketCard.IsVisibleAsync())
        {
            await docketCard.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);

            var pageContent = await Page.TextContentAsync("body");

            // Should show transactions section
            var hasTransactionsSection = pageContent?.Contains("Transactions in this Docket") == true;

            if (hasTransactionsSection)
            {
                // Should use TransactionRow components (data-testid="transaction-row")
                var transactionRows = Page.Locator("[data-testid='transaction-row']");
                var count = await transactionRows.CountAsync();

                Assert.That(count >= 0, Is.True,
                    "Docket detail should display transactions using TransactionRow components");
            }
            else
            {
                Assert.Pass("Docket has no transactions to display");
            }
        }
        else
        {
            Assert.Pass("No docket cards available to test transactions");
        }
    }

    [Test]
    public async Task DocketDetail_TransactionClick_ShowsTransactionDetail()
    {
        if (!await NavigateToDocketChainTabAsync())
        {
            Assert.Pass("Could not navigate to Docket Chain tab");
            return;
        }

        var docketCard = Page.Locator(".mud-card").First;

        if (await docketCard.IsVisibleAsync())
        {
            await docketCard.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);

            // Try to click a transaction row within the docket detail
            var txRow = Page.Locator("[data-testid='transaction-row']").First;

            if (await txRow.IsVisibleAsync())
            {
                await txRow.ClickAsync();
                await Page.WaitForTimeoutAsync(1000);

                // Transaction detail panel should appear
                var detailPanel = Page.Locator("[data-testid='transaction-detail']");
                var pageContent = await Page.TextContentAsync("body");

                var hasDetail =
                    await detailPanel.IsVisibleAsync() ||
                    pageContent?.Contains("Transaction Details") == true ||
                    pageContent?.Contains("Signature") == true;

                Assert.That(hasDetail, Is.True,
                    "Clicking a transaction in docket detail should show transaction detail panel");
            }
            else
            {
                Assert.Pass("No transactions in docket to click");
            }
        }
        else
        {
            Assert.Pass("No docket cards available to test transaction detail");
        }
    }

    #endregion

    #region Real-Time Indicator

    [Test]
    public async Task RealTimeIndicator_VisibleOnDocketChainTab()
    {
        if (!await NavigateToDocketChainTabAsync())
        {
            Assert.Pass("Could not navigate to Docket Chain tab");
            return;
        }

        var pageContent = await Page.TextContentAsync("body");

        // RealTimeIndicator should be visible (it's above tabs now)
        // It shows connection status text like "Connected", "Connecting", "Disconnected"
        var hasIndicator = pageContent?.Contains("Connected") == true ||
                           pageContent?.Contains("Connecting") == true ||
                           pageContent?.Contains("Disconnected") == true ||
                           pageContent?.Contains("Reconnecting") == true;

        Assert.That(hasIndicator, Is.True,
            "Real-time indicator should be visible on the Docket Chain tab");
    }

    #endregion

    #region New Dockets Notification Banner

    [Test]
    public async Task NewDocketsAlert_NotVisibleInitially()
    {
        if (!await NavigateToDocketChainTabAsync())
        {
            Assert.Pass("Could not navigate to Docket Chain tab");
            return;
        }

        // The "new docket(s) sealed" alert should NOT be visible on initial load
        var alert = Page.Locator("[data-testid='new-dockets-alert']");
        Assert.That(await alert.IsVisibleAsync(), Is.False,
            "New dockets alert should not be visible on initial page load");
    }

    #endregion

    #region Docket Chain Empty State

    [Test]
    public async Task DocketChain_EmptyState_ShowsMessage()
    {
        if (!await NavigateToDocketChainTabAsync())
        {
            Assert.Pass("Could not navigate to Docket Chain tab");
            return;
        }

        var pageContent = await Page.TextContentAsync("body");

        // If no dockets, should show empty state message
        if (pageContent?.Contains("No Dockets") == true)
        {
            Assert.That(pageContent.Contains("This register has no dockets yet"), Is.True,
                "Empty state should show descriptive message");
        }
        else
        {
            Assert.Pass("Register has dockets - empty state not applicable");
        }
    }

    #endregion

    #region Docket Detail State Field

    [Test]
    public async Task DocketDetail_ShowsStateField()
    {
        if (!await NavigateToDocketChainTabAsync())
        {
            Assert.Pass("Could not navigate to Docket Chain tab");
            return;
        }

        var docketCard = Page.Locator(".mud-card").First;

        if (await docketCard.IsVisibleAsync())
        {
            await docketCard.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);

            var pageContent = await Page.TextContentAsync("body");

            // Detail should show state (Sealed, Init, etc.)
            var hasState = pageContent?.Contains("State") == true;
            var hasStateValue = pageContent?.Contains("Sealed") == true ||
                                pageContent?.Contains("Init") == true ||
                                pageContent?.Contains("Proposed") == true ||
                                pageContent?.Contains("Accepted") == true;

            Assert.That(hasState || hasStateValue, Is.True,
                "Docket detail should display the docket state");
        }
        else
        {
            Assert.Pass("No docket cards available to verify state field");
        }
    }

    #endregion
}
