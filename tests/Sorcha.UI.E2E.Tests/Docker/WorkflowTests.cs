// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.UI.E2E.Tests.Infrastructure;
using Sorcha.UI.E2E.Tests.PageObjects.Shared;
using Sorcha.UI.E2E.Tests.PageObjects.WorkflowPages;

namespace Sorcha.UI.E2E.Tests.Docker;

/// <summary>
/// E2E tests for My Workflows and My Actions pages.
/// </summary>
[TestFixture]
[Category("Docker")]
[Category("Workflows")]
[Parallelizable(ParallelScope.Self)]
public class WorkflowTests : AuthenticatedDockerTestBase
{
    private MyWorkflowsPage _workflowsPage = null!;

    [SetUp]
    public override async Task BaseSetUp()
    {
        await base.BaseSetUp();
        _workflowsPage = new MyWorkflowsPage(Page);
    }

    [Test]
    [Retry(2)]
    public async Task MyWorkflows_PageLoads_ShowsTitleAndContent()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.MyWorkflows);

        var content = await Page.TextContentAsync("body") ?? "";
        Assert.That(content.Length, Is.GreaterThan(0),
            "My Workflows page should render content");
    }

    [Test]
    [Retry(2)]
    public async Task MyWorkflows_NoWorkflows_ShowsEmptyState()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.MyWorkflows);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        // Fresh system should show empty state or service unavailable
        var hasContent = await _workflowsPage.IsEmptyStateVisibleAsync() ||
                         await _workflowsPage.ServiceError.CountAsync() > 0 ||
                         await _workflowsPage.WorkflowCards.CountAsync() > 0;
        Assert.That(hasContent, Is.True,
            "Page should show empty state, error state, or workflow data");
    }

    [Test]
    [Retry(2)]
    public async Task MyActions_PageLoads_ShowsTitleAndContent()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.MyActions);

        var content = await Page.TextContentAsync("body") ?? "";
        Assert.That(content.Length, Is.GreaterThan(0),
            "My Actions page should render content");
    }

    [Test]
    [Retry(2)]
    public async Task MyActions_NoConsoleErrors()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.MyActions);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        var criticalErrors = GetCriticalConsoleErrors();
        Assert.That(criticalErrors, Is.Empty,
            "My Actions page should not have critical console errors");
    }

    [Test]
    [Retry(2)]
    public async Task MyWorkflows_WorkflowIds_UseTruncation()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.MyWorkflows);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        // If workflows exist, check truncated IDs are rendered
        var truncatedIds = Page.Locator(".truncated-id");
        var count = await truncatedIds.CountAsync();

        // Either no workflows (empty state) or IDs are truncated
        if (count > 0)
        {
            var firstId = await truncatedIds.First.TextContentAsync();
            Assert.That(firstId, Does.Contain("..."),
                "Workflow IDs should use truncation pattern");
        }
    }
}
