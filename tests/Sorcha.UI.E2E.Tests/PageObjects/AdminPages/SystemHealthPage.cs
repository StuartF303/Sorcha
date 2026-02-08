// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Playwright;
using Sorcha.UI.E2E.Tests.PageObjects.Shared;

namespace Sorcha.UI.E2E.Tests.PageObjects.AdminPages;

/// <summary>
/// Page object for System Health admin page.
/// </summary>
public class SystemHealthPage
{
    private readonly IPage _page;

    public SystemHealthPage(IPage page) => _page = page;

    // Locators
    public ILocator PageTitle => _page.Locator("h4:has-text('System Health')");
    public ILocator KpiCards => MudBlazorHelpers.Cards(_page);
    public ILocator ServiceHealthCards => _page.Locator(".mud-card:has(.mud-chip)");
    public ILocator RefreshButton => MudBlazorHelpers.Button(_page, "Refresh");
    public ILocator LoadingIndicator => MudBlazorHelpers.LinearProgress(_page);
    public ILocator ServiceError => _page.Locator("text=Service Unavailable");

    // Methods
    public async Task NavigateAsync()
    {
        await _page.GotoAsync($"{Infrastructure.TestConstants.UiWebUrl}{Infrastructure.TestConstants.AuthenticatedRoutes.AdminHealth}");
        await MudBlazorHelpers.WaitForBlazorAsync(_page);
    }

    public async Task<bool> WaitForPageAsync()
    {
        try
        {
            await PageTitle.WaitForAsync(new() { Timeout = Infrastructure.TestConstants.PageLoadTimeout });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    public async Task<int> GetKpiCardCountAsync()
    {
        return await KpiCards.CountAsync();
    }

    public async Task ClickRefreshAsync()
    {
        await RefreshButton.ClickAsync();
    }
}
