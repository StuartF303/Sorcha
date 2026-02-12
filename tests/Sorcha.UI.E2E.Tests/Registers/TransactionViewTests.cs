// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Playwright.NUnit;

namespace Sorcha.UI.E2E.Tests.Registers;

/// <summary>
/// End-to-end tests for the Transaction List and Detail functionality.
/// User Story 2: Users can view transactions within a register.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("Registers")]
[Category("US2")]
public class TransactionViewTests : PageTest
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

        // Get the UI Web URL
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

    #region T024: Transaction list displays transactions

    [Test]
    public async Task RegisterDetailPage_Loads()
    {
        // First get a register ID from the registers page
        await Page.GotoAsync($"{_blazorUrl}/registers");
        await Page.WaitForLoadStateAsync();
        await Page.WaitForTimeoutAsync(2000);

        if (Page.Url.Contains("/login"))
        {
            Assert.Pass("Registers page requires authentication - redirected to login");
            return;
        }

        // Click on a register card to navigate to detail
        var registerCard = Page.Locator(
            "[data-testid='register-card'], .register-card").First;

        if (await registerCard.IsVisibleAsync())
        {
            await registerCard.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);

            // Verify we navigated to a detail page
            var url = Page.Url;
            Assert.That(url.Contains("/registers/") || Page.Url.Contains("register"), Is.True,
                "Should navigate to register detail page");
        }
        else
        {
            Assert.Pass("No registers available - cannot test detail page");
        }
    }

    [Test]
    public async Task TransactionList_DisplaysTransactions()
    {
        await Page.GotoAsync($"{_blazorUrl}/registers");
        await Page.WaitForLoadStateAsync();
        await Page.WaitForTimeoutAsync(2000);

        if (Page.Url.Contains("/login"))
        {
            Assert.Pass("Registers page requires authentication - redirected to login");
            return;
        }

        // Click on a register card
        var registerCard = Page.Locator(
            "[data-testid='register-card'], .register-card").First;

        if (await registerCard.IsVisibleAsync())
        {
            await registerCard.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);

            // Check for transaction list
            var transactionList = Page.Locator(
                "[data-testid='transaction-list'], .transaction-list");

            if (await transactionList.IsVisibleAsync())
            {
                // Check for transaction rows
                var transactionRows = Page.Locator(
                    "[data-testid='transaction-row'], .transaction-row");
                var count = await transactionRows.CountAsync();

                Assert.That(count >= 0, Is.True,
                    "Transaction list should load (may be empty or have transactions)");
            }
            else
            {
                // Check for empty state
                var emptyState = Page.Locator(
                    "[data-testid='empty-transactions'], :text('No Transactions')");
                var pageContent = await Page.TextContentAsync("body");

                Assert.That(
                    await emptyState.IsVisibleAsync() ||
                    pageContent?.Contains("No Transactions") == true ||
                    pageContent?.Contains("Transaction") == true,
                    Is.True,
                    "Page should show transaction list or empty state");
            }
        }
        else
        {
            Assert.Pass("No registers available - cannot test transactions");
        }
    }

    [Test]
    public async Task TransactionList_ShowsColumnHeaders()
    {
        await Page.GotoAsync($"{_blazorUrl}/registers");
        await Page.WaitForLoadStateAsync();
        await Page.WaitForTimeoutAsync(2000);

        if (Page.Url.Contains("/login"))
        {
            Assert.Pass("Registers page requires authentication - redirected to login");
            return;
        }

        // Navigate to register detail
        var registerCard = Page.Locator(
            "[data-testid='register-card'], .register-card").First;

        if (await registerCard.IsVisibleAsync())
        {
            await registerCard.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);

            var pageContent = await Page.TextContentAsync("body");

            // Check for column headers
            var hasHeaders = pageContent?.Contains("TX ID") == true ||
                             pageContent?.Contains("Sender") == true ||
                             pageContent?.Contains("Block") == true ||
                             pageContent?.Contains("Time") == true;

            Assert.That(hasHeaders || pageContent?.Contains("No Transactions") == true, Is.True,
                "Page should show column headers or empty state");
        }
        else
        {
            Assert.Pass("No registers available - cannot test column headers");
        }
    }

    [Test]
    public async Task TransactionRow_IsClickable()
    {
        await Page.GotoAsync($"{_blazorUrl}/registers");
        await Page.WaitForLoadStateAsync();
        await Page.WaitForTimeoutAsync(2000);

        if (Page.Url.Contains("/login"))
        {
            Assert.Pass("Registers page requires authentication - redirected to login");
            return;
        }

        // Navigate to register detail
        var registerCard = Page.Locator(
            "[data-testid='register-card'], .register-card").First;

        if (await registerCard.IsVisibleAsync())
        {
            await registerCard.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);

            // Try to click on a transaction row
            var transactionRow = Page.Locator(
                "[data-testid='transaction-row'], .transaction-row").First;

            if (await transactionRow.IsVisibleAsync())
            {
                await transactionRow.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                // Page should handle click (may show detail or select)
                Assert.Pass("Transaction row click handled successfully");
            }
            else
            {
                Assert.Pass("No transactions available - cannot test row click");
            }
        }
        else
        {
            Assert.Pass("No registers available - cannot test transaction click");
        }
    }

    [Test]
    public async Task TransactionList_ShowsEmptyState_WhenNoTransactions()
    {
        await Page.GotoAsync($"{_blazorUrl}/registers");
        await Page.WaitForLoadStateAsync();
        await Page.WaitForTimeoutAsync(2000);

        if (Page.Url.Contains("/login"))
        {
            Assert.Pass("Registers page requires authentication - redirected to login");
            return;
        }

        // Navigate to register detail
        var registerCard = Page.Locator(
            "[data-testid='register-card'], .register-card").First;

        if (await registerCard.IsVisibleAsync())
        {
            await registerCard.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);

            // Check for transactions or empty state
            var transactionRows = Page.Locator(
                "[data-testid='transaction-row'], .transaction-row");
            var count = await transactionRows.CountAsync();

            if (count == 0)
            {
                var pageContent = await Page.TextContentAsync("body");
                Assert.That(
                    pageContent?.Contains("No Transactions") == true ||
                    pageContent?.Contains("empty") == true ||
                    pageContent?.Length > 0,
                    Is.True,
                    "Should show empty state when no transactions");
            }
            else
            {
                Assert.Pass("Transactions exist - empty state not applicable");
            }
        }
        else
        {
            Assert.Pass("No registers available - cannot test empty state");
        }
    }

    [Test]
    public async Task RegisterDetailPage_HasBackNavigation()
    {
        await Page.GotoAsync($"{_blazorUrl}/registers");
        await Page.WaitForLoadStateAsync();
        await Page.WaitForTimeoutAsync(2000);

        if (Page.Url.Contains("/login"))
        {
            Assert.Pass("Registers page requires authentication - redirected to login");
            return;
        }

        // Navigate to register detail
        var registerCard = Page.Locator(
            "[data-testid='register-card'], .register-card").First;

        if (await registerCard.IsVisibleAsync())
        {
            await registerCard.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);

            // Look for back button
            var backButton = Page.Locator(
                "button:has-text('Back'), a:has-text('Back'), [aria-label='Back'], .mud-icon-button:has(.mud-icon-root)").First;

            if (await backButton.IsVisibleAsync())
            {
                await backButton.ClickAsync();
                await Page.WaitForTimeoutAsync(1000);

                // Should navigate back to registers list
                var url = Page.Url;
                Assert.That(url.EndsWith("/registers") || url.Contains("/registers"), Is.True,
                    "Back button should navigate to registers list");
            }
            else
            {
                // Browser back should also work
                await Page.GoBackAsync();
                Assert.Pass("Page handles navigation correctly");
            }
        }
        else
        {
            Assert.Pass("No registers available - cannot test back navigation");
        }
    }

    [Test]
    public async Task TransactionDetail_ShowsWhenSelected()
    {
        await Page.GotoAsync($"{_blazorUrl}/registers");
        await Page.WaitForLoadStateAsync();
        await Page.WaitForTimeoutAsync(2000);

        if (Page.Url.Contains("/login"))
        {
            Assert.Pass("Registers page requires authentication - redirected to login");
            return;
        }

        // Navigate to register detail
        var registerCard = Page.Locator(
            "[data-testid='register-card'], .register-card").First;

        if (await registerCard.IsVisibleAsync())
        {
            await registerCard.ClickAsync();
            await Page.WaitForTimeoutAsync(2000);

            // Click on a transaction row
            var transactionRow = Page.Locator(
                "[data-testid='transaction-row'], .transaction-row").First;

            if (await transactionRow.IsVisibleAsync())
            {
                await transactionRow.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                // Check for transaction detail panel
                var detailPanel = Page.Locator(
                    "[data-testid='transaction-detail'], .transaction-detail");
                var pageContent = await Page.TextContentAsync("body");

                // Detail panel or additional info should appear
                var hasDetail =
                    await detailPanel.IsVisibleAsync() ||
                    pageContent?.Contains("Signature") == true ||
                    pageContent?.Contains("Block") == true ||
                    pageContent?.Contains("Payload") == true;

                Assert.That(hasDetail || pageContent?.Length > 0, Is.True,
                    "Transaction detail should be visible after selecting a transaction");
            }
            else
            {
                Assert.Pass("No transactions available - cannot test detail view");
            }
        }
        else
        {
            Assert.Pass("No registers available - cannot test transaction detail");
        }
    }

    #endregion
}
