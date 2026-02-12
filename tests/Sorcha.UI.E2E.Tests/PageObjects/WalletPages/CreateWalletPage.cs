// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Playwright;
using Sorcha.UI.E2E.Tests.PageObjects.Shared;

namespace Sorcha.UI.E2E.Tests.PageObjects.WalletPages;

/// <summary>
/// Page object for the Create Wallet page (/wallets/create).
/// </summary>
public class CreateWalletPage
{
    private readonly IPage _page;

    public CreateWalletPage(IPage page) => _page = page;

    // Page header
    public ILocator PageTitle => _page.Locator("h4:has-text('Create New Wallet')");

    // First-login welcome banner
    public ILocator WelcomeBanner => _page.Locator(".mud-alert:has-text('Welcome to Sorcha')");
    public ILocator WelcomeBannerTitle => _page.Locator("text=Welcome to Sorcha!");
    public ILocator WelcomeBannerDescription => _page.Locator("text=Let's set up your first wallet");

    // Form fields
    public ILocator WalletNameInput => _page.Locator("input").First;
    public ILocator AlgorithmSelect => _page.Locator(".mud-select").First;
    public ILocator WordCountSelect => _page.Locator(".mud-select").Nth(1);

    // Buttons
    public ILocator CancelButton => MudBlazorHelpers.Button(_page, "Cancel");
    public ILocator CreateButton => MudBlazorHelpers.Button(_page, "Create Wallet");

    // Mnemonic display step
    public ILocator MnemonicSection => _page.Locator("text=Your Recovery Phrase");
    public ILocator MnemonicWords => _page.Locator(".mud-grid .mud-paper");
    public ILocator CopyAllButton => MudBlazorHelpers.Button(_page, "Copy All Words");

    // Confirmation checkboxes
    public ILocator WrittenDownCheckbox => _page.Locator("text=I have written down my recovery phrase").Locator("..").Locator("input");
    public ILocator OneTimeCheckbox => _page.Locator("text=I understand this phrase will NEVER").Locator("..").Locator("input");
    public ILocator ContinueButton => MudBlazorHelpers.Button(_page, "Continue to Wallet");

    // Methods
    public async Task NavigateAsync()
    {
        await _page.GotoAsync($"{Infrastructure.TestConstants.UiWebUrl}{Infrastructure.TestConstants.AuthenticatedRoutes.WalletCreate}");
        await MudBlazorHelpers.WaitForBlazorAsync(_page);
    }

    public async Task NavigateFirstLoginAsync()
    {
        await _page.GotoAsync($"{Infrastructure.TestConstants.UiWebUrl}{Infrastructure.TestConstants.AuthenticatedRoutes.WalletCreateFirstLogin}");
        await MudBlazorHelpers.WaitForBlazorAsync(_page);
    }

    public async Task<bool> IsWelcomeBannerVisibleAsync()
    {
        return await WelcomeBanner.CountAsync() > 0;
    }

    public async Task<bool> IsCancelButtonVisibleAsync()
    {
        return await CancelButton.CountAsync() > 0;
    }

    public async Task<bool> IsPageLoadedAsync()
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
}
