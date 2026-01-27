// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Playwright;

namespace Sorcha.UI.E2E.Tests.PageObjects.Shared;

/// <summary>
/// Helper methods for interacting with MudBlazor components in Playwright tests.
/// MudBlazor renders specific CSS classes we can use as stable selectors.
/// </summary>
public static class MudBlazorHelpers
{
    // Layout selectors
    public static ILocator Layout(IPage page) => page.Locator(".mud-layout");
    public static ILocator AppBar(IPage page) => page.Locator(".mud-appbar");
    public static ILocator Drawer(IPage page) => page.Locator(".mud-drawer");
    public static ILocator MainContent(IPage page) => page.Locator(".mud-main-content");

    // Navigation
    public static ILocator NavMenu(IPage page) => page.Locator(".mud-navmenu");
    public static ILocator NavLink(IPage page, string href) => page.Locator($".mud-nav-link[href='{href}']");
    public static ILocator NavLinks(IPage page) => page.Locator(".mud-nav-link");
    public static ILocator NavGroup(IPage page, string title) => page.Locator($".mud-nav-group:has-text('{title}')");

    // Cards and Paper
    public static ILocator Cards(IPage page) => page.Locator(".mud-card");
    public static ILocator Paper(IPage page) => page.Locator(".mud-paper");

    // Tables
    public static ILocator Table(IPage page) => page.Locator(".mud-table");
    public static ILocator TableRows(IPage page) => page.Locator(".mud-table-body .mud-table-row");
    public static ILocator TableHeaders(IPage page) => page.Locator(".mud-table-head th");

    // Dialogs
    public static ILocator Dialog(IPage page) => page.Locator(".mud-dialog");
    public static ILocator DialogTitle(IPage page) => page.Locator(".mud-dialog-title");
    public static ILocator DialogActions(IPage page) => page.Locator(".mud-dialog-actions");

    // Alerts and Feedback
    public static ILocator Alert(IPage page) => page.Locator(".mud-alert");
    public static ILocator Snackbar(IPage page) => page.Locator(".mud-snackbar");
    public static ILocator Chip(IPage page) => page.Locator(".mud-chip");

    // Progress indicators
    public static ILocator CircularProgress(IPage page) => page.Locator(".mud-progress-circular");
    public static ILocator LinearProgress(IPage page) => page.Locator(".mud-progress-linear");
    public static ILocator Skeleton(IPage page) => page.Locator(".mud-skeleton");

    // Buttons
    public static ILocator IconButton(IPage page) => page.Locator(".mud-icon-button");
    public static ILocator Button(IPage page, string text) => page.Locator($".mud-button-root:has-text('{text}')");

    // Form elements
    public static ILocator TextField(IPage page) => page.Locator(".mud-input-text");
    public static ILocator Select(IPage page) => page.Locator(".mud-select");
    public static ILocator InputError(IPage page) => page.Locator(".mud-input-error");

    /// <summary>
    /// Checks whether the MudBlazor layout has rendered correctly.
    /// Returns a list of issues found, empty if everything looks good.
    /// </summary>
    public static async Task<List<string>> ValidateLayoutHealthAsync(IPage page)
    {
        var issues = new List<string>();

        var layoutCount = await Layout(page).CountAsync();
        if (layoutCount == 0)
            issues.Add("MudBlazor .mud-layout not found - layout may not have rendered");

        var appBarCount = await AppBar(page).CountAsync();
        if (appBarCount == 0)
            issues.Add("MudBlazor .mud-appbar not found - app bar may not have rendered");

        var mainContentCount = await MainContent(page).CountAsync();
        if (mainContentCount == 0)
            issues.Add("MudBlazor .mud-main-content not found - main content area missing");

        // Check for elements with 0x0 dimensions (invisible/broken components)
        var brokenElements = await page.EvaluateAsync<int>("""
            (() => {
                const mudElements = document.querySelectorAll('[class*="mud-"]');
                let broken = 0;
                for (const el of mudElements) {
                    const rect = el.getBoundingClientRect();
                    // Only flag elements that should be visible but have no dimensions
                    const style = window.getComputedStyle(el);
                    if (style.display !== 'none' && style.visibility !== 'hidden' &&
                        rect.width === 0 && rect.height === 0 &&
                        el.children.length > 0) {
                        broken++;
                    }
                }
                return broken;
            })()
            """);

        if (brokenElements > 0)
            issues.Add($"{brokenElements} MudBlazor element(s) with 0x0 dimensions detected (possible CSS failure)");

        return issues;
    }

    /// <summary>
    /// Waits for Blazor WASM to finish hydrating by checking for the presence
    /// of rendered MudBlazor components.
    /// </summary>
    public static async Task WaitForBlazorAsync(IPage page, int timeoutMs = 15000)
    {
        // Wait for network to settle
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for MudBlazor layout to appear (indicates WASM has hydrated)
        try
        {
            await Layout(page).WaitForAsync(new() { Timeout = timeoutMs });
        }
        catch (TimeoutException)
        {
            // Layout might not be present on auth pages - check for any rendered content
            await page.WaitForSelectorAsync("body *", new() { Timeout = 5000 });
        }
    }

    /// <summary>
    /// Locates elements by data-testid attribute. Use this as the primary selector strategy.
    /// </summary>
    public static ILocator TestId(IPage page, string testId) =>
        page.Locator($"[data-testid='{testId}']");

    /// <summary>
    /// Locates elements whose data-testid starts with a given prefix.
    /// Useful for collections like wallet-card-1, wallet-card-2, etc.
    /// </summary>
    public static ILocator TestIdPrefix(IPage page, string prefix) =>
        page.Locator($"[data-testid^='{prefix}']");
}
