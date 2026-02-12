// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Playwright;
using Sorcha.UI.E2E.Tests.PageObjects.Shared;

namespace Sorcha.UI.E2E.Tests.PageObjects.AdminPages;

/// <summary>
/// Page object for Validator admin page.
/// </summary>
public class ValidatorPage
{
    private readonly IPage _page;

    public ValidatorPage(IPage page) => _page = page;

    // Locators
    public ILocator PageTitle => _page.Locator("h4:has-text('Validator')");
    public ILocator MempoolStats => MudBlazorHelpers.Cards(_page);
    public ILocator RegisterTable => MudBlazorHelpers.Table(_page);
    public ILocator RegisterRows => MudBlazorHelpers.TableRows(_page);
    public ILocator LoadingIndicator => MudBlazorHelpers.LinearProgress(_page);
    public ILocator ServiceError => _page.Locator("text=Service Unavailable");

    // Methods
    public async Task NavigateAsync()
    {
        await _page.GotoAsync($"{Infrastructure.TestConstants.UiWebUrl}{Infrastructure.TestConstants.AuthenticatedRoutes.AdminValidator}");
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

    public async Task<int> GetMempoolStatCardCountAsync()
    {
        return await MempoolStats.CountAsync();
    }
}
