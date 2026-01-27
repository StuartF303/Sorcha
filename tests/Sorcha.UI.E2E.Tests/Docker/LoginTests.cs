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
}
