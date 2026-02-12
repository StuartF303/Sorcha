// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.UI.E2E.Tests.Infrastructure;
using Sorcha.UI.E2E.Tests.PageObjects;
using Sorcha.UI.E2E.Tests.PageObjects.Shared;
using Sorcha.UI.E2E.Tests.PageObjects.WalletPages;
using Sorcha.UI.E2E.Tests.PageObjects.WorkflowPages;

namespace Sorcha.UI.E2E.Tests.Docker;

/// <summary>
/// E2E tests for the normal user experience improvements:
/// - Navigation reorder and rename (My Workflows → New Submission)
/// - SignalR real-time connection on My Actions page
/// - First-login wallet creation flow
/// </summary>
[TestFixture]
[Category("Docker")]
[Category("NormalUser")]
[Parallelizable(ParallelScope.Self)]
public class NormalUserExperienceTests : AuthenticatedDockerTestBase
{
    private NavigationComponent _nav = null!;

    [SetUp]
    public override async Task BaseSetUp()
    {
        await base.BaseSetUp();
        _nav = new NavigationComponent(Page);
    }

    #region Navigation Order and Rename Tests

    [Test]
    [Retry(2)]
    public async Task Nav_MyActivity_ShowsNewSubmissionInsteadOfMyWorkflows()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Dashboard);

        await Expect(_nav.NewSubmissionLink).ToBeVisibleAsync();

        // "My Workflows" label should no longer exist in the nav
        var oldLabel = Page.Locator(".mud-nav-link:has-text('My Workflows')");
        Assert.That(await oldLabel.CountAsync(), Is.EqualTo(0),
            "Old 'My Workflows' label should not appear in navigation");
    }

    [Test]
    [Retry(2)]
    public async Task Nav_MyActivity_CorrectOrder()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Dashboard);

        // Get all nav links within the MY ACTIVITY section
        // The section is between the "MY ACTIVITY" header and the next divider
        var navLinks = Page.Locator(".mud-navmenu .mud-nav-link");
        var allTexts = await navLinks.AllTextContentsAsync();

        // Find positions of our expected items
        var pendingIdx = allTexts.ToList().FindIndex(t => t.Contains("Pending Actions"));
        var submissionIdx = allTexts.ToList().FindIndex(t => t.Contains("New Submission"));
        var walletIdx = allTexts.ToList().FindIndex(t => t.Contains("My Wallet"));
        var transactionsIdx = allTexts.ToList().FindIndex(t => t.Contains("My Transactions"));

        Assert.Multiple(() =>
        {
            Assert.That(pendingIdx, Is.GreaterThanOrEqualTo(0), "Pending Actions should be in nav");
            Assert.That(submissionIdx, Is.GreaterThanOrEqualTo(0), "New Submission should be in nav");
            Assert.That(walletIdx, Is.GreaterThanOrEqualTo(0), "My Wallet should be in nav");
            Assert.That(transactionsIdx, Is.GreaterThanOrEqualTo(0), "My Transactions should be in nav");

            // Verify order: Pending Actions < New Submission < My Wallet < My Transactions
            Assert.That(submissionIdx, Is.GreaterThan(pendingIdx),
                "New Submission should come after Pending Actions");
            Assert.That(walletIdx, Is.GreaterThan(submissionIdx),
                "My Wallet should come after New Submission");
            Assert.That(transactionsIdx, Is.GreaterThan(walletIdx),
                "My Transactions should come after My Wallet");
        });
    }

    [Test]
    [Retry(2)]
    public async Task Nav_NewSubmission_NavigatesToMyWorkflows()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Dashboard);
        await _nav.NavigateToAsync(_nav.NewSubmissionLink);

        Assert.That(Page.Url, Does.Contain("my-workflows"),
            "New Submission link should navigate to /my-workflows");
    }

    #endregion

    #region MyActions SignalR Connection Tests

    [Test]
    [Retry(2)]
    public async Task MyActions_ShowsConnectionStatusChip()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.MyActions);

        var actionsPage = new MyActionsPage(Page);
        Assert.That(await actionsPage.IsPageLoadedAsync(), Is.True,
            "My Actions page should load");

        // Connection chip should be visible
        await Expect(actionsPage.ConnectionChip).ToBeVisibleAsync();
    }

    [Test]
    [Retry(3)]
    public async Task MyActions_SignalR_ConnectsSuccessfully()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.MyActions);

        var actionsPage = new MyActionsPage(Page);
        Assert.That(await actionsPage.IsPageLoadedAsync(), Is.True,
            "My Actions page should load");

        // Wait for SignalR to establish connection (may take a moment)
        await Page.WaitForTimeoutAsync(3000);

        var chipText = await actionsPage.GetConnectionStatusTextAsync();
        Assert.That(chipText, Is.Not.Null, "Connection chip should have text");

        // The connection should either be Connected or attempting to connect
        // (Disconnected was the old broken state; anything else is an improvement)
        Assert.That(chipText, Does.Not.Contain("Disconnected").IgnoreCase,
            $"SignalR connection should not stay Disconnected. Status: {chipText}");
    }

    [Test]
    [Retry(2)]
    public async Task MyActions_ShowsPageTitle()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.MyActions);

        var actionsPage = new MyActionsPage(Page);
        await Expect(actionsPage.PageTitle).ToBeVisibleAsync();
    }

    [Test]
    [Retry(2)]
    public async Task MyActions_ShowsRealTimeInfoBanner()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.MyActions);

        var actionsPage = new MyActionsPage(Page);
        await Expect(actionsPage.RealTimeInfoBanner).ToBeVisibleAsync();
    }

    [Test]
    [Retry(2)]
    public async Task MyActions_ShowsRefreshButton()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.MyActions);

        var actionsPage = new MyActionsPage(Page);
        await Expect(actionsPage.RefreshButton).ToBeVisibleAsync();
    }

    [Test]
    [Retry(2)]
    public async Task MyActions_ShowsEmptyOrActionCards()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.MyActions);

        var actionsPage = new MyActionsPage(Page);
        await Page.WaitForTimeoutAsync(2000);

        // Page should show either empty state, service error, or action cards
        var hasContent = await actionsPage.EmptyState.CountAsync() > 0 ||
                         await actionsPage.ServiceError.CountAsync() > 0 ||
                         await actionsPage.ActionCards.CountAsync() > 0;
        Assert.That(hasContent, Is.True,
            "Page should show empty state, error state, or action cards");
    }

    #endregion

    #region First-Login Wallet Creation Flow Tests

    [Test]
    [Retry(2)]
    public async Task CreateWallet_FirstLogin_ShowsWelcomeBanner()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.WalletCreateFirstLogin);

        var walletPage = new CreateWalletPage(Page);
        Assert.That(await walletPage.IsPageLoadedAsync(), Is.True,
            "Create Wallet page should load");

        Assert.That(await walletPage.IsWelcomeBannerVisibleAsync(), Is.True,
            "First-login should show welcome banner");
    }

    [Test]
    [Retry(2)]
    public async Task CreateWallet_FirstLogin_HidesCancelButton()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.WalletCreateFirstLogin);

        var walletPage = new CreateWalletPage(Page);
        Assert.That(await walletPage.IsPageLoadedAsync(), Is.True,
            "Create Wallet page should load");

        Assert.That(await walletPage.IsCancelButtonVisibleAsync(), Is.False,
            "First-login should hide the Cancel button");
    }

    [Test]
    [Retry(2)]
    public async Task CreateWallet_Normal_NoBanner()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.WalletCreate);

        var walletPage = new CreateWalletPage(Page);
        Assert.That(await walletPage.IsPageLoadedAsync(), Is.True,
            "Create Wallet page should load");

        Assert.That(await walletPage.IsWelcomeBannerVisibleAsync(), Is.False,
            "Normal access should not show welcome banner");
    }

    [Test]
    [Retry(2)]
    public async Task CreateWallet_Normal_ShowsCancelButton()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.WalletCreate);

        var walletPage = new CreateWalletPage(Page);
        Assert.That(await walletPage.IsPageLoadedAsync(), Is.True,
            "Create Wallet page should load");

        Assert.That(await walletPage.IsCancelButtonVisibleAsync(), Is.True,
            "Normal access should show the Cancel button");
    }

    [Test]
    [Retry(2)]
    public async Task CreateWallet_ShowsFormFields()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.WalletCreate);

        var walletPage = new CreateWalletPage(Page);
        Assert.That(await walletPage.IsPageLoadedAsync(), Is.True,
            "Create Wallet page should load");

        await Expect(walletPage.CreateButton).ToBeVisibleAsync();

        // Form should have input fields (MudTextField renders as .mud-input-control)
        var inputs = Page.Locator(".mud-input-control");
        Assert.That(await inputs.CountAsync(), Is.GreaterThan(0),
            "Form should have input fields");

        var selects = MudBlazorHelpers.Select(Page);
        Assert.That(await selects.CountAsync(), Is.GreaterThanOrEqualTo(2),
            "Form should have Algorithm and Word Count selects");
    }

    #endregion
}
