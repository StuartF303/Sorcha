// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace Sorcha.UI.E2E.Tests;

/// <summary>
/// End-to-end tests for dashboard layout and responsive design
/// Tests login flow and dashboard layout rendering
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class DashboardLayoutTests : PageTest
{
    // Docker environment URLs - use API Gateway, not direct UI port
    private const string UiWebUrl = "http://localhost";

    // Test credentials
    private const string TestEmail = "admin@sorcha.local";
    private const string TestPassword = "Dev_Pass_2025!";

    private string? _screenshotDir;

    [SetUp]
    public async Task SetUp()
    {
        // Create screenshots directory
        _screenshotDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "screenshots");
        Directory.CreateDirectory(_screenshotDir);

        // Clear cookies and storage before each test
        await Context.ClearCookiesAsync();
    }

    #region Login Helper

    /// <summary>
    /// Helper method to login to the application
    /// </summary>
    private async Task<bool> LoginAsync()
    {
        try
        {
            TestContext.Out.WriteLine($"Navigating to: {UiWebUrl}/app/auth/login");
            await Page.GotoAsync($"{UiWebUrl}/app/auth/login", new PageGotoOptions { Timeout = 60000 });
            TestContext.Out.WriteLine($"Page loaded, URL: {Page.Url}");

            // Wait for Blazor WASM to initialize - look for blazor scripts
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            TestContext.Out.WriteLine("DOM Content Loaded");

            // Wait for Blazor to be ready by checking for the blazor script
            try
            {
                await Page.WaitForFunctionAsync("() => window.Blazor !== undefined", new PageWaitForFunctionOptions { Timeout = 30000 });
                TestContext.Out.WriteLine("Blazor loaded");
            }
            catch
            {
                TestContext.Out.WriteLine("Warning: Blazor object not found, continuing anyway");
            }

            // Additional wait for content to render
            await Page.WaitForTimeoutAsync(10000);
            TestContext.Out.WriteLine("Waited 10s for rendering");

            // Take screenshot of login page
            if (_screenshotDir != null)
            {
                var loginScreenshot = Path.Combine(_screenshotDir, "login-page.png");
                await Page.ScreenshotAsync(new() { Path = loginScreenshot, FullPage = true });
                TestContext.Out.WriteLine($"Login page screenshot: {loginScreenshot}");
            }

            // Wait for form to be ready - try multiple selectors
            ILocator? usernameInput = null;
            ILocator? passwordInput = null;

            // Try to find username input
            try
            {
                usernameInput = Page.Locator("input[type='text']").First;
                await usernameInput.WaitForAsync(new LocatorWaitForOptions { Timeout = 20000 });
                TestContext.Out.WriteLine("Found username input");
            }
            catch
            {
                TestContext.Out.WriteLine("Username input not found with type='text', trying placeholder");
                usernameInput = Page.Locator("input[placeholder*='username' i]").First;
                await usernameInput.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
            }

            // Try to find password input
            try
            {
                passwordInput = Page.Locator("input[type='password']").First;
                await passwordInput.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
                TestContext.Out.WriteLine("Found password input");
            }
            catch
            {
                TestContext.Out.WriteLine("Password input not found");
                throw;
            }

            // Fill in credentials
            await usernameInput.FillAsync(TestEmail);
            await passwordInput.FillAsync(TestPassword);
            TestContext.Out.WriteLine($"Filled credentials: {TestEmail}");

            // Select Docker profile if available
            var profileSelector = Page.Locator("select");
            var profileCount = await profileSelector.CountAsync();
            TestContext.Out.WriteLine($"Found {profileCount} select elements");

            if (profileCount > 0)
            {
                try
                {
                    var options = await profileSelector.First.Locator("option").AllTextContentsAsync();
                    TestContext.Out.WriteLine($"Available profiles: {string.Join(", ", options)}");

                    // Try to select docker profile (lowercase)
                    var dockerOption = options.FirstOrDefault(o => o.Equals("docker", StringComparison.OrdinalIgnoreCase));
                    if (dockerOption != null)
                    {
                        await profileSelector.First.SelectOptionAsync(dockerOption);
                        TestContext.Out.WriteLine($"Selected profile: {dockerOption}");
                    }
                    else
                    {
                        TestContext.Out.WriteLine("docker profile not found, using default");
                    }
                }
                catch (Exception ex)
                {
                    TestContext.Out.WriteLine($"Profile selection warning: {ex.Message}");
                }
            }

            // Click login
            var loginButton = Page.Locator("button:has-text('Sign In'), button:has-text('Login')").First;
            await loginButton.ClickAsync();
            TestContext.Out.WriteLine("Clicked Sign In button");

            // Wait for navigation or error message
            await Page.WaitForTimeoutAsync(15000);
            TestContext.Out.WriteLine($"After login, URL: {Page.Url}");

            // Check for error messages
            var errorMessages = Page.Locator(".alert-danger, .text-danger, [role='alert']");
            var errorCount = await errorMessages.CountAsync();
            if (errorCount > 0)
            {
                for (int i = 0; i < errorCount; i++)
                {
                    var errorText = await errorMessages.Nth(i).TextContentAsync();
                    TestContext.Out.WriteLine($"Error message {i + 1}: {errorText}");
                }
            }

            // Check for loading spinner (might still be processing)
            var spinner = Page.Locator(".spinner-border");
            var spinnerVisible = await spinner.CountAsync() > 0 && await spinner.First.IsVisibleAsync();
            if (spinnerVisible)
            {
                TestContext.Out.WriteLine("Login spinner still visible, waiting longer...");
                await Page.WaitForTimeoutAsync(10000);
                TestContext.Out.WriteLine($"After extended wait, URL: {Page.Url}");
            }

            // Check if login succeeded
            var currentUrl = Page.Url;
            var loginSucceeded = !currentUrl.Contains("/auth/login");
            TestContext.Out.WriteLine($"Login succeeded: {loginSucceeded}");

            // If login failed, take a screenshot
            if (!loginSucceeded && _screenshotDir != null)
            {
                var failedScreenshot = Path.Combine(_screenshotDir, "login-failed.png");
                await Page.ScreenshotAsync(new() { Path = failedScreenshot, FullPage = true });
                TestContext.Out.WriteLine($"Login failed screenshot: {failedScreenshot}");
            }

            return loginSucceeded;
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"Login failed with exception: {ex.Message}");
            TestContext.Out.WriteLine($"Stack trace: {ex.StackTrace}");

            // Take error screenshot
            if (_screenshotDir != null)
            {
                var errorScreenshot = Path.Combine(_screenshotDir, "login-error.png");
                await Page.ScreenshotAsync(new() { Path = errorScreenshot, FullPage = true });
                TestContext.Out.WriteLine($"Error screenshot: {errorScreenshot}");
            }

            return false;
        }
    }

    #endregion

    #region Dashboard Layout Tests

    [Test]
    [Retry(2)]
    public async Task Dashboard_LoadsAfterLogin()
    {
        // Login first
        var loginSuccess = await LoginAsync();
        if (!loginSuccess)
        {
            Assert.Fail("Login failed - cannot test dashboard");
            return;
        }

        // Navigate to dashboard (if not already there)
        if (!Page.Url.Contains("/app/home") && !Page.Url.Contains("/app/dashboard"))
        {
            await Page.GotoAsync($"{UiWebUrl}/app/home");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.WaitForTimeoutAsync(3000);
        }

        // Take screenshot
        var screenshotPath = Path.Combine(_screenshotDir!, "dashboard-after-login.png");
        await Page.ScreenshotAsync(new() { Path = screenshotPath, FullPage = true });
        TestContext.Out.WriteLine($"Screenshot saved: {screenshotPath}");

        // Verify dashboard loaded
        var pageContent = await Page.TextContentAsync("body");
        Assert.That(pageContent, Is.Not.Empty, "Dashboard should have content");

        TestContext.Out.WriteLine($"Current URL: {Page.Url}");
        TestContext.Out.WriteLine($"Page title: {await Page.TitleAsync()}");
    }

    [Test]
    [Retry(2)]
    public async Task Dashboard_LayoutInspection()
    {
        // Login first
        var loginSuccess = await LoginAsync();
        if (!loginSuccess)
        {
            Assert.Fail("Login failed - cannot test dashboard layout");
            return;
        }

        // Navigate to dashboard
        await Page.GotoAsync($"{UiWebUrl}/app/home");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(3000);

        // Take full page screenshot
        var fullScreenshotPath = Path.Combine(_screenshotDir!, "dashboard-layout-full.png");
        await Page.ScreenshotAsync(new() { Path = fullScreenshotPath, FullPage = true });
        TestContext.Out.WriteLine($"Full page screenshot: {fullScreenshotPath}");

        // Get page dimensions and layout info
        var viewport = Page.ViewportSize;
        TestContext.Out.WriteLine($"Viewport: {viewport?.Width}x{viewport?.Height}");

        // Check for common layout elements
        var elements = new Dictionary<string, string>
        {
            {"Header", "header, [role='banner'], .mud-appbar"},
            {"Navigation", "nav, [role='navigation'], .mud-drawer"},
            {"Main Content", "main, [role='main'], .mud-main-content"},
            {"Footer", "footer, [role='contentinfo']"}
        };

        foreach (var element in elements)
        {
            var locator = Page.Locator(element.Value);
            var count = await locator.CountAsync();
            var isVisible = count > 0 && await locator.First.IsVisibleAsync();

            TestContext.Out.WriteLine($"{element.Key}: Found={count}, Visible={isVisible}");

            if (isVisible)
            {
                var box = await locator.First.BoundingBoxAsync();
                if (box != null)
                {
                    TestContext.Out.WriteLine($"  Position: ({box.X}, {box.Y}), Size: {box.Width}x{box.Height}");
                }
            }
        }

        // Check for MudBlazor components
        var mudComponents = Page.Locator("[class*='mud-']");
        var mudCount = await mudComponents.CountAsync();
        TestContext.Out.WriteLine($"MudBlazor components found: {mudCount}");

        // Check for layout/spacing issues
        var overlappingElements = await Page.EvaluateAsync<bool>(@"() => {
            const elements = Array.from(document.querySelectorAll('*'));
            for (let i = 0; i < elements.length - 1; i++) {
                const rect1 = elements[i].getBoundingClientRect();
                for (let j = i + 1; j < elements.length; j++) {
                    const rect2 = elements[j].getBoundingClientRect();
                    // Check if elements overlap
                    if (!(rect1.right < rect2.left ||
                          rect1.left > rect2.right ||
                          rect1.bottom < rect2.top ||
                          rect1.top > rect2.bottom)) {
                        // Elements overlap - check if one contains the other
                        if (!elements[i].contains(elements[j]) && !elements[j].contains(elements[i])) {
                            console.log('Overlapping elements found');
                            return true;
                        }
                    }
                }
            }
            return false;
        }");

        if (overlappingElements)
        {
            TestContext.Out.WriteLine("WARNING: Possible overlapping elements detected!");
        }

        Assert.That(mudCount, Is.GreaterThan(0), "Dashboard should contain MudBlazor components");
    }

    [Test]
    [Retry(2)]
    public async Task Dashboard_ResponsiveLayout_Desktop()
    {
        // Login first
        var loginSuccess = await LoginAsync();
        if (!loginSuccess)
        {
            Assert.Fail("Login failed");
            return;
        }

        // Test desktop viewport (1920x1080)
        await Page.SetViewportSizeAsync(1920, 1080);
        await Page.GotoAsync($"{UiWebUrl}/app/home");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(2000);

        var desktopScreenshot = Path.Combine(_screenshotDir!, "dashboard-desktop-1920x1080.png");
        await Page.ScreenshotAsync(new() { Path = desktopScreenshot, FullPage = true });
        TestContext.Out.WriteLine($"Desktop screenshot: {desktopScreenshot}");

        // Check layout
        var pageContent = await Page.TextContentAsync("body");
        Assert.That(pageContent, Is.Not.Empty, "Desktop layout should display content");
    }

    [Test]
    [Retry(2)]
    public async Task Dashboard_ResponsiveLayout_Tablet()
    {
        // Login first
        var loginSuccess = await LoginAsync();
        if (!loginSuccess)
        {
            Assert.Fail("Login failed");
            return;
        }

        // Test tablet viewport (768x1024)
        await Page.SetViewportSizeAsync(768, 1024);
        await Page.GotoAsync($"{UiWebUrl}/app/home");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(2000);

        var tabletScreenshot = Path.Combine(_screenshotDir!, "dashboard-tablet-768x1024.png");
        await Page.ScreenshotAsync(new() { Path = tabletScreenshot, FullPage = true });
        TestContext.Out.WriteLine($"Tablet screenshot: {tabletScreenshot}");

        // Check layout
        var pageContent = await Page.TextContentAsync("body");
        Assert.That(pageContent, Is.Not.Empty, "Tablet layout should display content");
    }

    [Test]
    [Retry(2)]
    public async Task Dashboard_ResponsiveLayout_Mobile()
    {
        // Login first
        var loginSuccess = await LoginAsync();
        if (!loginSuccess)
        {
            Assert.Fail("Login failed");
            return;
        }

        // Test mobile viewport (375x667 - iPhone SE)
        await Page.SetViewportSizeAsync(375, 667);
        await Page.GotoAsync($"{UiWebUrl}/app/home");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(2000);

        var mobileScreenshot = Path.Combine(_screenshotDir!, "dashboard-mobile-375x667.png");
        await Page.ScreenshotAsync(new() { Path = mobileScreenshot, FullPage = true });
        TestContext.Out.WriteLine($"Mobile screenshot: {mobileScreenshot}");

        // Check layout
        var pageContent = await Page.TextContentAsync("body");
        Assert.That(pageContent, Is.Not.Empty, "Mobile layout should display content");
    }

    [Test]
    [Retry(2)]
    public async Task Dashboard_CSSAnalysis()
    {
        // Login first
        var loginSuccess = await LoginAsync();
        if (!loginSuccess)
        {
            Assert.Fail("Login failed");
            return;
        }

        await Page.GotoAsync($"{UiWebUrl}/app/home");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(3000);

        // Analyze CSS and layout issues
        var cssIssues = await Page.EvaluateAsync<object>(@"() => {
            const issues = {
                overflowingElements: [],
                missingDimensions: [],
                largeZIndex: [],
                fixedPositionElements: [],
                absolutePositionElements: []
            };

            const elements = Array.from(document.querySelectorAll('*'));

            elements.forEach(el => {
                const styles = window.getComputedStyle(el);
                const rect = el.getBoundingClientRect();

                // Check for overflow
                if (styles.overflow === 'visible' && (rect.width > window.innerWidth || rect.height > window.innerHeight)) {
                    issues.overflowingElements.push({
                        tag: el.tagName,
                        class: el.className,
                        width: rect.width,
                        height: rect.height
                    });
                }

                // Check for missing dimensions
                if (rect.width === 0 || rect.height === 0) {
                    issues.missingDimensions.push({
                        tag: el.tagName,
                        class: el.className
                    });
                }

                // Check for high z-index
                const zIndex = parseInt(styles.zIndex);
                if (zIndex > 1000) {
                    issues.largeZIndex.push({
                        tag: el.tagName,
                        class: el.className,
                        zIndex: zIndex
                    });
                }

                // Check positioning
                if (styles.position === 'fixed') {
                    issues.fixedPositionElements.push({
                        tag: el.tagName,
                        class: el.className
                    });
                }

                if (styles.position === 'absolute') {
                    issues.absolutePositionElements.push({
                        tag: el.tagName,
                        class: el.className
                    });
                }
            });

            return issues;
        }");

        TestContext.Out.WriteLine("CSS Analysis Results:");
        TestContext.Out.WriteLine(System.Text.Json.JsonSerializer.Serialize(cssIssues, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        // This test is informational - it passes but logs potential issues
        Assert.Pass("CSS analysis complete - check test output for details");
    }

    #endregion
}
