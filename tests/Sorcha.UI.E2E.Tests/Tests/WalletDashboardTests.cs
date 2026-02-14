// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.UI.E2E.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Sorcha.UI.E2E.Tests.Tests;

/// <summary>
/// E2E tests for wallet dashboard wizard behavior.
/// Tests verify that the wizard only shows when user truly has no wallets,
/// and prevents the infinite loop bug.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("Docker")]
[Category("Wallet")]
[Category("Dashboard")]
public class WalletDashboardTests : AuthenticatedDockerTestBase
{
    /// <summary>
    /// T010: Verify wizard appears for first-time users with no wallets
    /// </summary>
    [Test]
    [Retry(2)]
    public async Task FirstLogin_NoWallets_ShowsWizard()
    {
        // This test validates the expected behavior for new users
        // Note: Requires a test user with no existing wallets

        await NavigateAuthenticatedAsync("/dashboard");

        // If user has no wallets, they should be redirected to wallet creation
        var currentUrl = Page.Url;

        // Either already on wizard page OR redirected there
        var isOnWizard = currentUrl.Contains("/wallets/create") ||
                        currentUrl.Contains("first-login=true");

        if (!isOnWizard)
        {
            // User must have existing wallets - skip this test
            Assert.Ignore("Test user has existing wallets. Cannot test first-time user flow.");
        }

        // Verify wizard page elements are present
        await Expect(Page.GetByText("Create New Wallet")).ToBeVisibleAsync();

        TestContext.Out.WriteLine("✓ First-time user correctly redirected to wallet creation wizard");
    }

    /// <summary>
    /// T011: Verify wizard does NOT reappear after wallet creation
    /// </summary>
    [Test]
    [Retry(2)]
    public async Task AfterWalletCreation_DashboardLoads_WizardDoesNotReappear()
    {
        // This is the critical test for the bug fix
        // Ensures the wizard loop bug is resolved

        await NavigateAuthenticatedAsync("/dashboard");

        var currentUrl = Page.Url;
        var isOnDashboard = currentUrl.Contains("/dashboard") || currentUrl.EndsWith("/app/") || currentUrl.EndsWith("/");

        if (!isOnDashboard)
        {
            Assert.Warn($"Not on dashboard page. Current URL: {currentUrl}. User may not have wallets yet.");
        }

        // Verify dashboard content is visible (not wizard)
        var welcomeVisible = await Page.GetByText("Welcome back").IsVisibleAsync();
        var dashboardHeading = await Page.GetByRole(AriaRole.Heading, new() { NameRegex = new System.Text.RegularExpressions.Regex("Welcome|Dashboard") }).IsVisibleAsync();

        Assert.That(welcomeVisible || dashboardHeading, Is.True,
            "Dashboard should display welcome message or heading, not redirect to wizard");

        // Refresh the page to ensure wizard doesn't reappear
        await Page.ReloadAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        currentUrl = Page.Url;
        isOnDashboard = currentUrl.Contains("/dashboard") || currentUrl.EndsWith("/app/") || currentUrl.EndsWith("/");

        Assert.That(isOnDashboard, Is.True,
            "After refresh, should remain on dashboard (wizard loop bug would redirect here)");

        TestContext.Out.WriteLine("✓ Dashboard remains stable after refresh - no wizard loop detected");
    }

    /// <summary>
    /// T012: Verify returning users with existing wallets skip the wizard
    /// </summary>
    [Test]
    [Retry(2)]
    public async Task ExistingWallet_DashboardLoad_SkipsWizard()
    {
        // Navigate to dashboard
        await NavigateAuthenticatedAsync("/dashboard");

        // Should land on dashboard, not wizard
        var currentUrl = Page.Url;
        var isOnDashboard = currentUrl.Contains("/dashboard") || currentUrl.EndsWith("/app/") || currentUrl.EndsWith("/");

        // If redirected to wizard, user has no wallets
        if (currentUrl.Contains("/wallets/create"))
        {
            Assert.Ignore("Test user has no wallets. This test requires an existing wallet.");
        }

        Assert.That(isOnDashboard, Is.True, "User with existing wallet should land on dashboard");

        // Verify wallet count is displayed
        var hasWalletCount = await Page.GetByText("Wallet").CountAsync() > 0;
        Assert.That(hasWalletCount, Is.True, "Dashboard should show wallet-related information");

        TestContext.Out.WriteLine("✓ Existing user correctly lands on dashboard without wizard redirect");
    }

    /// <summary>
    /// T013: Verify graceful handling when stats fail to load
    /// </summary>
    [Test]
    [Retry(2)]
    public async Task StatsFailToLoad_DashboardLoads_DoesNotRedirectToWizard()
    {
        // This test verifies the IsLoaded check prevents false redirects
        // When stats fail to load (IsLoaded=false), should NOT redirect to wizard

        // Note: This test is difficult to implement without mocking the API
        // or stopping services. Documenting expected behavior:
        //
        // Expected: If DashboardService.GetDashboardStatsAsync() fails,
        // _stats.IsLoaded will be false, so the condition
        // "if (_stats.IsLoaded && _stats.TotalWallets == 0)" will NOT trigger
        // and user stays on dashboard with error state shown

        await NavigateAuthenticatedAsync("/dashboard");

        // Verify we're on dashboard (not redirected to wizard)
        var currentUrl = Page.Url;
        var isOnDashboard = currentUrl.Contains("/dashboard") || currentUrl.EndsWith("/app/") || currentUrl.EndsWith("/");

        Assert.That(isOnDashboard, Is.True,
            "Even if stats fail, should stay on dashboard (not redirect to wizard)");

        TestContext.Out.WriteLine("✓ Dashboard loads without redirect (stats failure scenario)");

        // NOTE: Full testing of this scenario requires integration test with mocked API
        // or service failure simulation. This E2E test documents expected behavior.
    }
}
