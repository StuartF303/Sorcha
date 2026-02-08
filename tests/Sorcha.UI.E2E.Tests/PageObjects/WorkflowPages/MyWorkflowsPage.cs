// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Playwright;
using Sorcha.UI.E2E.Tests.PageObjects.Shared;

namespace Sorcha.UI.E2E.Tests.PageObjects.WorkflowPages;

/// <summary>
/// Page object for My Workflows page.
/// </summary>
public class MyWorkflowsPage
{
    private readonly IPage _page;

    public MyWorkflowsPage(IPage page) => _page = page;

    // Locators
    public ILocator PageTitle => _page.Locator("h4:has-text('My Workflows')");
    public ILocator WorkflowTable => MudBlazorHelpers.Table(_page);
    public ILocator WorkflowCards => MudBlazorHelpers.Cards(_page);
    public ILocator EmptyState => _page.Locator("[data-testid='empty-state'], .mud-alert");
    public ILocator LoadingIndicator => MudBlazorHelpers.Skeleton(_page);
    public ILocator ServiceError => _page.Locator("text=Service Unavailable");

    public ILocator WorkflowRow(string instanceId) =>
        _page.Locator($"tr:has-text('{instanceId}')");

    public ILocator StatusChip => MudBlazorHelpers.Chip(_page);

    // Methods
    public async Task NavigateAsync()
    {
        await _page.GotoAsync($"{Infrastructure.TestConstants.UiWebUrl}{Infrastructure.TestConstants.AuthenticatedRoutes.MyWorkflows}");
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

    public async Task<bool> IsEmptyStateVisibleAsync()
    {
        return await EmptyState.IsVisibleAsync();
    }

    public async Task<int> GetWorkflowCountAsync()
    {
        var rows = MudBlazorHelpers.TableRows(_page);
        return await rows.CountAsync();
    }
}
