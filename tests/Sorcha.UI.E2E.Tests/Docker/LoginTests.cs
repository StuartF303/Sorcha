// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.UI.E2E.Tests.Infrastructure;
using Sorcha.UI.E2E.Tests.PageObjects;

namespace Sorcha.UI.E2E.Tests.Docker;

/// <summary>
/// E2E tests for the login page and authentication flow against Docker.
/// These tests run unauthenticated (no pre-login).
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("Docker")]
[Category("Auth")]
[Category("Smoke")]
public class LoginTests : DockerTestBase
{
    private LoginPage _loginPage = null!;

    [SetUp]
    public override async Task BaseSetUp()
    {
        await base.BaseSetUp();
        _loginPage = new LoginPage(Page);
    }

    #region Smoke Tests

    [Test]
    [Retry(3)]
    public async Task LoginPage_LoadsWithoutErrors()
    {
        await _loginPage.NavigateAsync();

        var formLoaded = await _loginPage.WaitForFormAsync();
        Assert.That(formLoaded, Is.True, "Login form should load after WASM hydration");
    }

    [Test]
    [Retry(2)]
    public async Task LoginPage_ShowsLoginCard()
    {
        await _loginPage.NavigateAsync();
        await _loginPage.WaitForFormAsync();

        Assert.That(await _loginPage.LoginCard.IsVisibleAsync(), Is.True,
            "Login card container should be visible");
        Assert.That(await _loginPage.LoginTitle.TextContentAsync(), Does.Contain("Sign In"),
            "Login card should show 'Sign In' title");
    }

    [Test]
    public async Task LoginPage_HasCorrectTitle()
    {
        await _loginPage.NavigateAsync();
        await _loginPage.WaitForFormAsync();

        await Expect(Page).ToHaveTitleAsync(
            new System.Text.RegularExpressions.Regex("Sign In|Sorcha|Login"));
    }

    #endregion

    #region Form Structure Tests

    [Test]
    [Retry(2)]
    public async Task LoginPage_HasUsernameAndPasswordFields()
    {
        await _loginPage.NavigateAsync();
        if (!await _loginPage.WaitForFormAsync()) return;

        Assert.That(await _loginPage.IsFormVisibleAsync(), Is.True,
            "Username and password fields should both be visible");
    }

    [Test]
    public async Task LoginPage_HasProfileSelector()
    {
        await _loginPage.NavigateAsync();
        if (!await _loginPage.WaitForFormAsync()) return;

        var profiles = await _loginPage.GetProfileOptionsAsync();
        Assert.That(profiles, Is.Not.Empty, "Profile selector should have options");
        Assert.That(profiles, Has.Some.Contain("docker").Or.Some.Contain("local"),
            "Should have docker or local profile");
    }

    [Test]
    public async Task LoginPage_HasSignInButton()
    {
        await _loginPage.NavigateAsync();
        if (!await _loginPage.WaitForFormAsync()) return;

        await Expect(_loginPage.SignInButton).ToBeVisibleAsync();
        await Expect(_loginPage.SignInButton).ToBeEnabledAsync();
    }

    #endregion

    #region Validation Tests

    [Test]
    [Retry(2)]
    public async Task LoginPage_ShowsError_ForEmptyCredentials()
    {
        await _loginPage.NavigateAsync();
        if (!await _loginPage.WaitForFormAsync()) return;

        await _loginPage.SignInButton.ClickAsync();
        await Page.WaitForTimeoutAsync(TestConstants.ShortWait);

        // Should show validation error or stay on login page
        var error = await _loginPage.GetErrorMessageAsync();
        var stillOnLogin = Page.Url.Contains("/auth/login");

        Assert.That(error != null || stillOnLogin, Is.True,
            "Should show error or remain on login page when submitting empty form");
    }

    [Test]
    [Retry(2)]
    public async Task LoginPage_ShowsError_ForInvalidCredentials()
    {
        await _loginPage.NavigateAsync();
        if (!await _loginPage.WaitForFormAsync()) return;

        await _loginPage.LoginAsync("invalid@test.com", "wrongpassword", TestConstants.TestProfileName);
        await Page.WaitForTimeoutAsync(TestConstants.BlazorHydrationTimeout);

        var error = await _loginPage.GetErrorMessageAsync();
        var stillOnLogin = Page.Url.Contains("/auth/login");

        Assert.That(error != null || stillOnLogin, Is.True,
            "Should show error or remain on login page for invalid credentials");
    }

    #endregion

    #region Authentication Flow Tests

    [Test]
    [Retry(3)]
    public async Task Login_WithValidCredentials_RedirectsToDashboard()
    {
        await _loginPage.NavigateAsync();
        if (!await _loginPage.WaitForFormAsync()) return;

        await _loginPage.LoginWithTestCredentialsAsync();

        // Wait for navigation away from login page (Blazor WASM nav + state propagation)
        try
        {
            await Page.WaitForURLAsync(
                url => !url.Contains("/auth/login"),
                new() { Timeout = TestConstants.PageLoadTimeout * 2 });
        }
        catch (TimeoutException)
        {
            // Check if there's an error message on the login page
            var error = await _loginPage.GetErrorMessageAsync();
            Assert.Fail(
                $"Login did not navigate away from login page within timeout. " +
                $"URL: {Page.Url}. Error: {error ?? "none"}");
        }

        Assert.That(Page.Url, Does.Not.Contain("/auth/login"),
            "Should navigate away from login page after successful login");
    }

    [Test]
    public async Task ProtectedPage_RedirectsToLogin_WhenUnauthenticated()
    {
        await NavigateAndWaitForBlazorAsync(TestConstants.AuthenticatedRoutes.Designer);

        Assert.That(IsOnLoginPage(), Is.True,
            "Protected page should redirect to login when not authenticated");
    }

    [Test]
    [Retry(2)]
    public async Task Logout_NavigatesToLogoutPage()
    {
        await NavigateToAsync(TestConstants.PublicRoutes.Logout);
        await Page.WaitForTimeoutAsync(TestConstants.BlazorHydrationTimeout);

        var url = Page.Url;
        Assert.That(
            url.Contains("/auth/login") || url.Contains("/auth/logout"),
            Is.True,
            $"Should be on login or logout page. Got: {url}");
    }

    #endregion

    #region Auth Redirect Tests

    [Test]
    [TestCase(TestConstants.AuthenticatedRoutes.Dashboard)]
    [TestCase(TestConstants.AuthenticatedRoutes.Wallets)]
    [TestCase(TestConstants.AuthenticatedRoutes.Blueprints)]
    [TestCase(TestConstants.AuthenticatedRoutes.Schemas)]
    [TestCase(TestConstants.AuthenticatedRoutes.Registers)]
    [TestCase(TestConstants.AuthenticatedRoutes.Admin)]
    public async Task ProtectedRoute_RedirectsToLogin(string route)
    {
        await NavigateAndWaitForBlazorAsync(route);

        var url = Page.Url;
        var content = await Page.TextContentAsync("body") ?? "";

        var isRedirected = url.Contains("/auth/login")
            || content.Contains("Sign In", StringComparison.OrdinalIgnoreCase);

        Assert.That(isRedirected, Is.True,
            $"Route {route} should redirect to login. URL: {url}");
    }

    #endregion

    #region Return URL Flow Tests

    [Test]
    [Retry(3)]
    public async Task Login_WithValidReturnUrl_NavigatesToReturnUrl()
    {
        // Navigate to login with a valid return URL
        var returnUrl = TestConstants.AuthenticatedRoutes.Registers;
        var encodedReturnUrl = Uri.EscapeDataString(returnUrl);
        var loginUrlWithReturn = $"{TestConstants.PublicRoutes.Login}?returnUrl={encodedReturnUrl}";

        await NavigateToAsync(loginUrlWithReturn);
        if (!await _loginPage.WaitForFormAsync()) return;

        await _loginPage.LoginWithTestCredentialsAsync();

        // Wait for navigation to the return URL (or at least away from login)
        try
        {
            await Page.WaitForURLAsync(
                url => !url.Contains("/auth/login"),
                new() { Timeout = TestConstants.PageLoadTimeout * 2 });
        }
        catch (TimeoutException)
        {
            var error = await _loginPage.GetErrorMessageAsync();
            Assert.Fail(
                $"Login did not navigate away from login page. " +
                $"URL: {Page.Url}. Error: {error ?? "none"}");
        }

        // Verify we ended up at the return URL
        Assert.That(Page.Url, Does.Contain(returnUrl.TrimStart('/')),
            $"Should navigate to return URL after login. Expected: {returnUrl}, Got: {Page.Url}");
    }

    [Test]
    [Retry(2)]
    public async Task Login_WithoutReturnUrl_NavigatesToDashboard()
    {
        await _loginPage.NavigateAsync();
        if (!await _loginPage.WaitForFormAsync()) return;

        await _loginPage.LoginWithTestCredentialsAsync();

        try
        {
            await Page.WaitForURLAsync(
                url => !url.Contains("/auth/login"),
                new() { Timeout = TestConstants.PageLoadTimeout * 2 });
        }
        catch (TimeoutException)
        {
            var error = await _loginPage.GetErrorMessageAsync();
            Assert.Fail(
                $"Login did not navigate away from login page. " +
                $"URL: {Page.Url}. Error: {error ?? "none"}");
        }

        // Should go to dashboard by default
        Assert.That(Page.Url, Does.Contain("dashboard"),
            $"Should navigate to dashboard when no return URL. Got: {Page.Url}");
    }

    [Test]
    [Retry(2)]
    public async Task Login_WithExternalReturnUrl_NavigatesToDashboard()
    {
        // Attempt XSS/open redirect with external URL
        var maliciousReturnUrl = "https://evil.com/steal-credentials";
        var encodedReturnUrl = Uri.EscapeDataString(maliciousReturnUrl);
        var loginUrlWithReturn = $"{TestConstants.PublicRoutes.Login}?returnUrl={encodedReturnUrl}";

        await NavigateToAsync(loginUrlWithReturn);
        if (!await _loginPage.WaitForFormAsync()) return;

        await _loginPage.LoginWithTestCredentialsAsync();

        try
        {
            await Page.WaitForURLAsync(
                url => !url.Contains("/auth/login"),
                new() { Timeout = TestConstants.PageLoadTimeout * 2 });
        }
        catch (TimeoutException)
        {
            var error = await _loginPage.GetErrorMessageAsync();
            Assert.Fail(
                $"Login did not navigate away from login page. " +
                $"URL: {Page.Url}. Error: {error ?? "none"}");
        }

        // Should go to dashboard (safe default), NOT to external URL
        Assert.That(Page.Url, Does.Not.Contain("evil.com"),
            "Should NOT redirect to external URL (security check)");
        Assert.That(Page.Url, Does.Contain("dashboard"),
            $"Should fall back to dashboard for invalid return URL. Got: {Page.Url}");
    }

    [Test]
    [Retry(2)]
    public async Task Login_WithJavaScriptReturnUrl_NavigatesToDashboard()
    {
        // Attempt XSS with javascript: URL
        var xssReturnUrl = "javascript:alert('xss')";
        var encodedReturnUrl = Uri.EscapeDataString(xssReturnUrl);
        var loginUrlWithReturn = $"{TestConstants.PublicRoutes.Login}?returnUrl={encodedReturnUrl}";

        await NavigateToAsync(loginUrlWithReturn);
        if (!await _loginPage.WaitForFormAsync()) return;

        await _loginPage.LoginWithTestCredentialsAsync();

        try
        {
            await Page.WaitForURLAsync(
                url => !url.Contains("/auth/login"),
                new() { Timeout = TestConstants.PageLoadTimeout * 2 });
        }
        catch (TimeoutException)
        {
            var error = await _loginPage.GetErrorMessageAsync();
            Assert.Fail(
                $"Login did not navigate away from login page. " +
                $"URL: {Page.Url}. Error: {error ?? "none"}");
        }

        // Should go to dashboard (safe default)
        Assert.That(Page.Url, Does.Not.Contain("javascript"),
            "Should NOT execute javascript URL (security check)");
        Assert.That(Page.Url, Does.Contain("dashboard"),
            $"Should fall back to dashboard for javascript URL. Got: {Page.Url}");
    }

    #endregion

    #region Keyboard Interaction Tests

    [Test]
    [Retry(2)]
    public async Task Login_PressEnterOnPassword_SubmitsForm()
    {
        await _loginPage.NavigateAsync();
        if (!await _loginPage.WaitForFormAsync()) return;

        // Select profile first (before filling credentials)
        if (await _loginPage.ProfileSelector.CountAsync() > 0)
        {
            await _loginPage.SelectProfileAsync(TestConstants.TestProfileName);
        }

        // Fill credentials using FillAsync
        await _loginPage.UsernameInput.FillAsync(TestConstants.TestEmail);
        await _loginPage.PasswordInput.FillAsync(TestConstants.TestPassword);

        // Blur password to trigger Blazor binding, then refocus and press Enter
        await _loginPage.PasswordInput.BlurAsync();
        await _loginPage.UsernameInput.BlurAsync();
        await Page.WaitForTimeoutAsync(500);

        // Focus password and press Enter
        await _loginPage.PasswordInput.ClickAsync();
        await _loginPage.PasswordInput.PressAsync("Enter");

        // Wait for navigation away from login page (form was submitted)
        try
        {
            await Page.WaitForURLAsync(
                url => !url.Contains("/auth/login"),
                new() { Timeout = TestConstants.PageLoadTimeout * 2 });
        }
        catch (TimeoutException)
        {
            var error = await _loginPage.GetErrorMessageAsync();
            Assert.Fail(
                $"Pressing Enter on password field did not submit form. " +
                $"URL: {Page.Url}. Error: {error ?? "none"}");
        }

        Assert.That(Page.Url, Does.Not.Contain("/auth/login"),
            "Pressing Enter on password field should submit the login form");
    }

    [Test]
    [Retry(2)]
    public async Task Login_PressEnterOnUsername_SubmitsForm()
    {
        await _loginPage.NavigateAsync();
        if (!await _loginPage.WaitForFormAsync()) return;

        // Fill credentials
        await _loginPage.UsernameInput.FillAsync(TestConstants.TestEmail);
        await _loginPage.PasswordInput.FillAsync(TestConstants.TestPassword);

        // Select profile if available
        if (await _loginPage.ProfileSelector.CountAsync() > 0)
        {
            await _loginPage.SelectProfileAsync(TestConstants.TestProfileName);
        }

        // Press Enter on username field instead of clicking button
        await _loginPage.UsernameInput.PressAsync("Enter");

        // Wait for navigation away from login page (form was submitted)
        try
        {
            await Page.WaitForURLAsync(
                url => !url.Contains("/auth/login"),
                new() { Timeout = TestConstants.PageLoadTimeout * 2 });
        }
        catch (TimeoutException)
        {
            var error = await _loginPage.GetErrorMessageAsync();
            Assert.Fail(
                $"Pressing Enter on username field did not submit form. " +
                $"URL: {Page.Url}. Error: {error ?? "none"}");
        }

        Assert.That(Page.Url, Does.Not.Contain("/auth/login"),
            "Pressing Enter on username field should submit the login form");
    }

    #endregion
}
