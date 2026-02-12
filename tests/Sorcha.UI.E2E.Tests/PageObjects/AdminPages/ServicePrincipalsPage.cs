// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Playwright;
using Sorcha.UI.E2E.Tests.PageObjects.Shared;

namespace Sorcha.UI.E2E.Tests.PageObjects.AdminPages;

/// <summary>
/// Page object for Service Principals admin page.
/// </summary>
public class ServicePrincipalsPage
{
    private readonly IPage _page;

    public ServicePrincipalsPage(IPage page) => _page = page;

    // Locators
    public ILocator PageTitle => _page.Locator("h4:has-text('Service Principals')");
    public ILocator CredentialTable => MudBlazorHelpers.Table(_page);
    public ILocator CredentialRows => MudBlazorHelpers.TableRows(_page);
    public ILocator InfoBanner => MudBlazorHelpers.Alert(_page);
    public ILocator LoadingIndicator => MudBlazorHelpers.LinearProgress(_page);
    public ILocator ServiceError => _page.Locator("text=Service Unavailable");

    // Methods
    public async Task NavigateAsync()
    {
        await _page.GotoAsync($"{Infrastructure.TestConstants.UiWebUrl}{Infrastructure.TestConstants.AuthenticatedRoutes.AdminPrincipals}");
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

    public async Task<bool> IsInfoBannerVisibleAsync()
    {
        return await InfoBanner.IsVisibleAsync();
    }
}
