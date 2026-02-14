// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.UI.E2E.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Sorcha.UI.E2E.Tests.Tests;

/// <summary>
/// E2E tests for wallet navigation URL correctness.
/// Tests verify that wallet detail navigation includes proper /app/ base href.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("Docker")]
[Category("Wallet")]
[Category("Navigation")]
public class WalletNavigationTests : AuthenticatedDockerTestBase
{
    /// <summary>
    /// T024: Verify wallet navigation URLs include /app/ prefix
    /// </summary>
    [Test]
    [Retry(2)]
    public async Task MyWallet_ClickWallet_NavigatesToCorrectUrl()
    {
        // Navigate to My Wallet page
        await NavigateAuthenticatedAsync("/my-wallet");

        // Wait for wallet cards to load
        await Page.WaitForSelectorAsync("text=My Wallet", new() { Timeout = 5000 });

        // Find first wallet card (if any exist)
        var walletCards = await Page.QuerySelectorAllAsync(".mud-card");

        if (walletCards.Count == 0)
        {
            Assert.Ignore("No wallets found. Test requires at least one wallet.");
            return;
        }

        // Get the first wallet card and click it
        var firstCard = walletCards[0];
        await firstCard.ClickAsync();

        // Wait for navigation to complete
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify URL includes /app/ prefix
        var currentUrl = Page.Url;

        Assert.That(currentUrl, Does.Contain("/app/wallets/"),
            $"URL should include '/app/wallets/' prefix. Actual: {currentUrl}");

        TestContext.Out.WriteLine($"✓ Navigation URL correct: {currentUrl}");
    }

    /// <summary>
    /// T025: Verify wallet detail page loads successfully after navigation
    /// </summary>
    [Test]
    [Retry(2)]
    public async Task MyWallet_ClickWallet_PageLoadsSuccessfully()
    {
        // Navigate to My Wallet page
        await NavigateAuthenticatedAsync("/my-wallet");

        // Wait for page to load
        await Page.WaitForSelectorAsync("text=My Wallet", new() { Timeout = 5000 });

        // Find wallet cards
        var walletCards = await Page.QuerySelectorAllAsync(".mud-card");

        if (walletCards.Count == 0)
        {
            Assert.Ignore("No wallets found. Test requires at least one wallet.");
            return;
        }

        // Click first wallet
        var firstCard = walletCards[0];
        await firstCard.ClickAsync();

        // Wait for wallet detail page to load
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify we're on a wallet detail page (not 404)
        var currentUrl = Page.Url;
        var hasWalletInUrl = currentUrl.Contains("/wallets/");

        Assert.That(hasWalletInUrl, Is.True, "Should be on wallet detail page");

        // Verify page content loaded (not 404 or error page)
        var hasNotFoundText = await Page.GetByText("Page Not Found").IsVisibleAsync();
        var has404Text = await Page.GetByText("404").IsVisibleAsync();

        Assert.That(hasNotFoundText || has404Text, Is.False,
            "Wallet detail page should load successfully (not 404)");

        TestContext.Out.WriteLine($"✓ Wallet detail page loaded successfully at: {currentUrl}");
    }

    /// <summary>
    /// T026: Verify bookmarked wallet URLs work correctly
    /// </summary>
    [Test]
    [Retry(2)]
    public async Task WalletDetailUrl_DirectAccess_PageLoads()
    {
        // First, get a valid wallet URL by navigating through the UI
        await NavigateAuthenticatedAsync("/my-wallet");
        await Page.WaitForSelectorAsync("text=My Wallet", new() { Timeout = 5000 });

        var walletCards = await Page.QuerySelectorAllAsync(".mud-card");

        if (walletCards.Count == 0)
        {
            Assert.Ignore("No wallets found. Test requires at least one wallet.");
            return;
        }

        // Click first wallet to get its URL
        var firstCard = walletCards[0];
        await firstCard.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var walletDetailUrl = Page.Url;

        // Now test direct access (simulating bookmark)
        // Navigate away first
        await NavigateAuthenticatedAsync("/dashboard");

        // Then navigate directly to the bookmarked wallet URL
        await Page.GotoAsync(walletDetailUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify page loads successfully
        var currentUrl = Page.Url;
        Assert.That(currentUrl, Is.EqualTo(walletDetailUrl),
            "Bookmarked URL should work when accessed directly");

        // Verify no 404 error
        var hasNotFoundText = await Page.GetByText("Page Not Found").IsVisibleAsync();
        var has404Text = await Page.GetByText("404").IsVisibleAsync();

        Assert.That(hasNotFoundText || has404Text, Is.False,
            "Bookmarked wallet URL should load successfully");

        TestContext.Out.WriteLine($"✓ Bookmarked URL works: {walletDetailUrl}");
    }
}
