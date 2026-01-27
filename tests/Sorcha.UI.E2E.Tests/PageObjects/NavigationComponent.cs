// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Playwright;
using Sorcha.UI.E2E.Tests.Infrastructure;
using Sorcha.UI.E2E.Tests.PageObjects.Shared;

namespace Sorcha.UI.E2E.Tests.PageObjects;

/// <summary>
/// Page object for the MudBlazor navigation drawer and app bar.
/// Works on any page that uses MainLayout.
/// </summary>
public class NavigationComponent
{
    private readonly IPage _page;

    public NavigationComponent(IPage page)
    {
        _page = page;
    }

    // App Bar
    public ILocator AppBar => MudBlazorHelpers.AppBar(_page);
    public ILocator AppTitle => _page.Locator(".mud-appbar .mud-typography-h5");
    public ILocator MenuToggle => _page.Locator(".mud-appbar .mud-icon-button").First;
    public ILocator UserMenu => _page.Locator(".mud-appbar .mud-menu");
    public ILocator SignInButton => _page.Locator(".mud-appbar a:has-text('Sign In'), .mud-appbar button:has-text('Sign In')");

    // Drawer
    public ILocator Drawer => MudBlazorHelpers.Drawer(_page);
    public ILocator DrawerHeader => _page.Locator(".mud-drawer-header");
    public ILocator NavMenu => MudBlazorHelpers.NavMenu(_page);

    // Navigation links (authenticated)
    public ILocator DashboardLink => _page.Locator(".mud-nav-link:has-text('Dashboard')");
    public ILocator PendingActionsLink => _page.Locator(".mud-nav-link:has-text('Pending Actions')");
    public ILocator MyWorkflowsLink => _page.Locator(".mud-nav-link:has-text('My Workflows')");
    public ILocator MyTransactionsLink => _page.Locator(".mud-nav-link:has-text('My Transactions')");
    public ILocator MyWalletLink => _page.Locator(".mud-nav-link:has-text('My Wallet')");
    public ILocator AllBlueprintsLink => _page.Locator(".mud-nav-link:has-text('All Blueprints')");
    public ILocator CreateBlueprintLink => _page.Locator(".mud-nav-link:has-text('Create Blueprint')");
    public ILocator TemplatesLink => _page.Locator(".mud-nav-link:has-text('Templates')");
    public ILocator SchemaLibraryLink => _page.Locator(".mud-nav-link:has-text('Schema Library')");
    public ILocator AllWalletsLink => _page.Locator(".mud-nav-link:has-text('All Wallets')");
    public ILocator CreateWalletLink => _page.Locator(".mud-nav-link:has-text('Create Wallet')");
    public ILocator RecoverWalletLink => _page.Locator(".mud-nav-link:has-text('Recover Wallet')");
    public ILocator RegistersLink => _page.Locator(".mud-nav-link:has-text('Registers')");
    public ILocator AdministrationLink => _page.Locator(".mud-nav-link:has-text('Administration')");
    public ILocator SettingsLink => _page.Locator(".mud-nav-link:has-text('Settings')");
    public ILocator HelpLink => _page.Locator(".mud-nav-link:has-text('Help')");

    // Nav groups (expandable)
    public ILocator BlueprintsGroup => _page.Locator(".mud-nav-group:has-text('Blueprints')");
    public ILocator WalletsGroup => _page.Locator(".mud-nav-group:has-text('Wallets')");

    // Section headers
    public ILocator MyActivitySection => _page.Locator(".mud-text-secondary:has-text('MY ACTIVITY')");
    public ILocator DesignerSection => _page.Locator(".mud-text-secondary:has-text('DESIGNER')");
    public ILocator ManagementSection => _page.Locator(".mud-text-secondary:has-text('MANAGEMENT')");

    /// <summary>
    /// Toggles the drawer open/closed.
    /// </summary>
    public async Task ToggleDrawerAsync()
    {
        await MenuToggle.ClickAsync();
        await _page.WaitForTimeoutAsync(300); // Wait for animation
    }

    /// <summary>
    /// Checks whether the drawer is currently open.
    /// </summary>
    public async Task<bool> IsDrawerOpenAsync()
    {
        // MudBlazor adds mud-drawer--open class when open
        var drawerClass = await Drawer.GetAttributeAsync("class") ?? "";
        return drawerClass.Contains("open", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Expands a nav group if it's collapsed.
    /// MudBlazor nav groups use a toggle element (.mud-nav-link) as the clickable header.
    /// </summary>
    public async Task ExpandNavGroupAsync(ILocator navGroup)
    {
        // MudNavGroup renders the toggle header as a .mud-nav-link inside the group
        var toggle = navGroup.Locator("button, .mud-nav-link").First;
        if (await toggle.CountAsync() > 0)
        {
            await toggle.ClickAsync();
            await _page.WaitForTimeoutAsync(500);
        }
    }

    /// <summary>
    /// Navigates to a page by clicking a nav link.
    /// </summary>
    public async Task NavigateToAsync(ILocator navLink)
    {
        await navLink.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.WaitForTimeoutAsync(TestConstants.ShortWait);
    }

    /// <summary>
    /// Opens the user menu in the app bar.
    /// </summary>
    public async Task OpenUserMenuAsync()
    {
        await UserMenu.Locator(".mud-icon-button").ClickAsync();
        await _page.WaitForTimeoutAsync(300);
    }

    /// <summary>
    /// Gets the displayed username from the user menu.
    /// </summary>
    public async Task<string?> GetDisplayedUsernameAsync()
    {
        await OpenUserMenuAsync();
        var menuContent = _page.Locator(".mud-popover-open .mud-typography-body2");
        if (await menuContent.CountAsync() > 0)
        {
            return await menuContent.TextContentAsync();
        }
        return null;
    }

    /// <summary>
    /// Clicks logout in the user menu.
    /// </summary>
    public async Task LogoutAsync()
    {
        await OpenUserMenuAsync();
        await _page.Locator(".mud-popover-open .mud-menu-item:has-text('Logout')").ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Gets all visible nav link texts.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetVisibleNavLinksAsync()
    {
        return await _page.Locator(".mud-nav-link").AllTextContentsAsync();
    }

    /// <summary>
    /// Checks whether the authenticated nav menu is visible (vs. the unauthenticated one).
    /// </summary>
    public async Task<bool> IsAuthenticatedNavVisibleAsync()
    {
        return await DashboardLink.CountAsync() > 0;
    }
}
