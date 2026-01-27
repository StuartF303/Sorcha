// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Playwright;
using Sorcha.UI.E2E.Tests.Infrastructure;
using Sorcha.UI.E2E.Tests.PageObjects.Shared;

namespace Sorcha.UI.E2E.Tests.PageObjects;

/// <summary>
/// Page object for the Dashboard page (/app/dashboard).
/// </summary>
public class DashboardPage
{
    private readonly IPage _page;

    public DashboardPage(IPage page)
    {
        _page = page;
    }

    // Page structure
    public ILocator DashboardContainer => _page.Locator(".dashboard");
    public ILocator WelcomeHeading => _page.Locator(".dashboard h2");
    public ILocator PageTitle => _page.Locator("head title");

    // Stat cards
    public ILocator StatCards => _page.Locator(".stat-card");
    public ILocator BlueprintsStat => _page.Locator(".stat-card:has-text('Blueprints')");
    public ILocator WalletsStat => _page.Locator(".stat-card:has-text('Wallets')");
    public ILocator TransactionsStat => _page.Locator(".stat-card:has-text('Transactions')");

    // Quick actions
    public ILocator QuickActions => _page.Locator(".dashboard-actions");
    public ILocator CreateBlueprintButton => _page.Locator(".dashboard-actions a:has-text('Create Blueprint')");
    public ILocator ManageWalletsButton => _page.Locator(".dashboard-actions a:has-text('Manage Wallets')");
    public ILocator ViewTransactionsButton => _page.Locator(".dashboard-actions a:has-text('View Transactions')");

    // Recent activity
    public ILocator RecentActivity => _page.Locator(".dashboard-recent");
    public ILocator EmptyState => _page.Locator(".empty-state");

    // data-testid selectors (use as pages get updated)
    public ILocator TestIdStatCards => MudBlazorHelpers.TestIdPrefix(_page, "stat-card-");
    public ILocator TestIdBlueprintCount => MudBlazorHelpers.TestId(_page, "stat-blueprints");
    public ILocator TestIdWalletCount => MudBlazorHelpers.TestId(_page, "stat-wallets");
    public ILocator TestIdTransactionCount => MudBlazorHelpers.TestId(_page, "stat-transactions");

    /// <summary>
    /// Navigates directly to the dashboard.
    /// </summary>
    public async Task NavigateAsync()
    {
        await _page.GotoAsync($"{TestConstants.UiWebUrl}{TestConstants.AuthenticatedRoutes.Dashboard}");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await MudBlazorHelpers.WaitForBlazorAsync(_page);
    }

    /// <summary>
    /// Gets the welcome message text.
    /// </summary>
    public async Task<string?> GetWelcomeMessageAsync()
    {
        if (await WelcomeHeading.CountAsync() > 0)
        {
            return await WelcomeHeading.TextContentAsync();
        }
        return null;
    }

    /// <summary>
    /// Gets the stat card value for a given label.
    /// </summary>
    public async Task<string?> GetStatValueAsync(string label)
    {
        var card = _page.Locator($".stat-card:has-text('{label}') .stat-value");
        if (await card.CountAsync() > 0)
        {
            return await card.TextContentAsync();
        }
        return null;
    }

    /// <summary>
    /// Gets the count of visible stat cards.
    /// </summary>
    public async Task<int> GetStatCardCountAsync()
    {
        return await StatCards.CountAsync();
    }

    /// <summary>
    /// Checks whether the quick action buttons are visible.
    /// </summary>
    public async Task<bool> AreQuickActionsVisibleAsync()
    {
        return await QuickActions.CountAsync() > 0 && await QuickActions.IsVisibleAsync();
    }

    /// <summary>
    /// Checks whether the dashboard shows the empty state for recent activity.
    /// </summary>
    public async Task<bool> IsRecentActivityEmptyAsync()
    {
        return await EmptyState.CountAsync() > 0;
    }

    /// <summary>
    /// Checks whether the dashboard has loaded with authenticated content.
    /// </summary>
    public async Task<bool> IsLoadedAsync()
    {
        return await DashboardContainer.CountAsync() > 0
            && await WelcomeHeading.CountAsync() > 0;
    }
}
