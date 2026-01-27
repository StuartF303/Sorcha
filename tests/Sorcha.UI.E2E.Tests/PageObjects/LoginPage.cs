// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Playwright;
using Sorcha.UI.E2E.Tests.Infrastructure;

namespace Sorcha.UI.E2E.Tests.PageObjects;

/// <summary>
/// Page object for the Login page (/app/auth/login).
/// Encapsulates selectors and actions for the authentication form.
/// </summary>
public class LoginPage
{
    private readonly IPage _page;

    public LoginPage(IPage page)
    {
        _page = page;
    }

    // Locators
    public ILocator UsernameInput => _page.Locator("input[type='text']").First;
    public ILocator PasswordInput => _page.Locator("input[type='password']").First;
    public ILocator ProfileSelector => _page.Locator("select").First;
    public ILocator SignInButton => _page.Locator("button:has-text('Sign In'), button:has-text('Login')").First;
    public ILocator ErrorMessage => _page.Locator(".alert-danger");
    public ILocator LoadingSpinner => _page.Locator(".spinner-border");
    public ILocator LoginCard => _page.Locator(".login-card");
    public ILocator LoginTitle => _page.Locator(".login-title");
    public ILocator LoginSubtitle => _page.Locator(".login-subtitle");
    public ILocator ProfileDescription => _page.Locator(".form-text.text-muted");

    /// <summary>
    /// Navigates to the login page and waits for the form to load.
    /// </summary>
    public async Task NavigateAsync()
    {
        await _page.GotoAsync($"{TestConstants.UiWebUrl}{TestConstants.PublicRoutes.Login}");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.WaitForTimeoutAsync(TestConstants.BlazorHydrationTimeout);
    }

    /// <summary>
    /// Waits for the login form to be interactive (WASM hydration complete).
    /// Returns true if the form loaded, false if it timed out.
    /// </summary>
    public async Task<bool> WaitForFormAsync()
    {
        try
        {
            await UsernameInput.WaitForAsync(new() { Timeout = TestConstants.PageLoadTimeout });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    /// <summary>
    /// Performs a complete login with the given credentials and profile.
    /// </summary>
    public async Task LoginAsync(string email, string password, string? profileName = null)
    {
        await UsernameInput.FillAsync(email);
        await PasswordInput.FillAsync(password);

        if (profileName != null)
        {
            await SelectProfileAsync(profileName);
        }

        await SignInButton.ClickAsync();
    }

    /// <summary>
    /// Performs login with the default test credentials.
    /// </summary>
    public async Task LoginWithTestCredentialsAsync()
    {
        await LoginAsync(
            TestConstants.TestEmail,
            TestConstants.TestPassword,
            TestConstants.TestProfileName);
    }

    /// <summary>
    /// Selects an environment profile from the dropdown.
    /// </summary>
    public async Task SelectProfileAsync(string profileName)
    {
        if (await ProfileSelector.CountAsync() > 0)
        {
            try
            {
                await ProfileSelector.SelectOptionAsync(new SelectOptionValue { Value = profileName });
            }
            catch
            {
                // Profile option may not exist
            }
        }
    }

    /// <summary>
    /// Gets all available profile options from the dropdown.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetProfileOptionsAsync()
    {
        if (await ProfileSelector.CountAsync() == 0)
            return [];

        return await ProfileSelector.Locator("option").AllTextContentsAsync();
    }

    /// <summary>
    /// Returns the current error message text, or null if no error is shown.
    /// </summary>
    public async Task<string?> GetErrorMessageAsync()
    {
        if (await ErrorMessage.CountAsync() > 0 && await ErrorMessage.IsVisibleAsync())
        {
            return await ErrorMessage.TextContentAsync();
        }
        return null;
    }

    /// <summary>
    /// Checks whether the login form is currently visible and interactive.
    /// </summary>
    public async Task<bool> IsFormVisibleAsync()
    {
        return await UsernameInput.CountAsync() > 0
            && await UsernameInput.IsVisibleAsync()
            && await PasswordInput.CountAsync() > 0
            && await PasswordInput.IsVisibleAsync();
    }

    /// <summary>
    /// Checks whether the page is still loading profiles (initial spinner).
    /// </summary>
    public async Task<bool> IsLoadingAsync()
    {
        return await LoadingSpinner.CountAsync() > 0 && await LoadingSpinner.IsVisibleAsync();
    }
}
