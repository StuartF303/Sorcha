// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Playwright;
using Sorcha.UI.E2E.Tests.Infrastructure;
using Sorcha.UI.E2E.Tests.PageObjects.Shared;

namespace Sorcha.UI.E2E.Tests.PageObjects;

/// <summary>
/// Page object for the Dashboard page (/app/dashboard).
/// Matches actual MudBlazor markup in Home.razor.
/// </summary>
public class DashboardPage
{
    private readonly IPage _page;

    public DashboardPage(IPage page)
    {
        _page = page;
    }

    // Page structure — MudBlazor layout renders content inside mud-main-content
    public ILocator WelcomeHeading => _page.Locator(".mud-typography-h4:has-text('Welcome')");
    public ILocator PageTitle => _page.Locator("head title");

    // Stat cards — each is a MudCard inside a MudGrid
    public ILocator StatCards => _page.Locator(".mud-grid .mud-card");
    public ILocator BlueprintsStat => _page.Locator(".mud-card:has-text('Blueprints')");
    public ILocator WalletsStat => _page.Locator(".mud-card:has-text('Wallets')");
    public ILocator TransactionsStat => _page.Locator(".mud-card:has-text('Transactions')");
    public ILocator PeersStat => _page.Locator(".mud-card:has-text('Peers')");
    public ILocator RegistersStat => _page.Locator(".mud-card:has-text('Registers')");
    public ILocator OrganizationsStat => _page.Locator(".mud-card:has-text('Organizations')");

    // Quick actions — MudStack with MudButtons
    public ILocator QuickActions => _page.Locator(".mud-stack:has(.mud-button)");
    public ILocator CreateBlueprintButton => _page.Locator(".mud-button:has-text('Create Blueprint')");
    public ILocator ManageWalletsButton => _page.Locator(".mud-button:has-text('Manage Wallets')");
    public ILocator ViewRegistersButton => _page.Locator(".mud-button:has-text('View Registers')");

    // Recent activity — MudPaper section
    public ILocator RecentActivity => _page.Locator(".mud-paper:has-text('recent activity')");
    public ILocator EmptyState => _page.Locator(".mud-paper:has-text('No recent activity')");

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
    /// The value is rendered as a MudText Typo.h5 inside the card.
    /// </summary>
    public async Task<string?> GetStatValueAsync(string label)
    {
        var card = _page.Locator($".mud-card:has-text('{label}') .mud-typography-h5");
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
        return await CreateBlueprintButton.CountAsync() > 0;
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
        return await WelcomeHeading.CountAsync() > 0;
    }
}
