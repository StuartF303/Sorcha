// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.UI.E2E.Tests.Infrastructure;
using Sorcha.UI.E2E.Tests.PageObjects;
using Sorcha.UI.E2E.Tests.PageObjects.Shared;

namespace Sorcha.UI.E2E.Tests.Docker;

/// <summary>
/// E2E tests for the navigation drawer, app bar, and page routing against Docker.
/// Verifies layout integrity, navigation links, and MudBlazor component rendering.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("Docker")]
[Category("Navigation")]
[Category("Authenticated")]
public class NavigationTests : AuthenticatedDockerTestBase
{
    private NavigationComponent _nav = null!;

    [SetUp]
    public override async Task BaseSetUp()
    {
        await base.BaseSetUp();
        _nav = new NavigationComponent(Page);
    }

    #region Layout Smoke Tests

    [Test]
    [Retry(2)]
    public async Task Layout_MudBlazorRendered()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Dashboard);

        var issues = await MudBlazorHelpers.ValidateLayoutHealthAsync(Page);
        Assert.That(issues, Is.Empty,
            $"MudBlazor layout issues: {string.Join(", ", issues)}");
    }

    [Test]
    public async Task Layout_AppBarVisible()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Dashboard);

        await Expect(_nav.AppBar).ToBeVisibleAsync();
    }

    [Test]
    public async Task Layout_AppBarShowsTitle()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Dashboard);

        var title = await _nav.AppTitle.TextContentAsync();
        Assert.That(title, Does.Contain("Sorcha"),
            "App bar should display 'Sorcha' title");
    }

    [Test]
    public async Task Layout_DrawerVisible()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Dashboard);

        await Expect(_nav.Drawer).ToBeVisibleAsync();
    }

    [Test]
    public async Task Layout_MudBlazorStylesLoaded()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Dashboard);

        // MudBlazor components should have rendered CSS classes
        var mudComponents = await Page.Locator("[class*='mud-']").CountAsync();
        Assert.That(mudComponents, Is.GreaterThan(5),
            "Multiple MudBlazor components should be rendered with CSS classes");
    }

    #endregion

    #region Navigation Menu Structure Tests

    [Test]
    public async Task Nav_ShowsAuthenticatedMenu()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Dashboard);

        Assert.That(await _nav.IsAuthenticatedNavVisibleAsync(), Is.True,
            "Authenticated user should see the full navigation menu");
    }

    [Test]
    public async Task Nav_HasMyActivitySection()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Dashboard);

        await Expect(_nav.DashboardLink).ToBeVisibleAsync();
        await Expect(_nav.PendingActionsLink).ToBeVisibleAsync();
        await Expect(_nav.MyWorkflowsLink).ToBeVisibleAsync();
        await Expect(_nav.MyTransactionsLink).ToBeVisibleAsync();
        await Expect(_nav.MyWalletLink).ToBeVisibleAsync();
    }

    [Test]
    public async Task Nav_HasDesignerSection()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Dashboard);

        await Expect(_nav.MyBlueprintsLink).ToBeVisibleAsync();
        await Expect(_nav.VisualDesignerLink).ToBeVisibleAsync();
        await Expect(_nav.CatalogueLink).ToBeVisibleAsync();
        await Expect(_nav.DataSchemasLink).ToBeVisibleAsync();
    }

    [Test]
    public async Task Nav_HasManagementSection()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Dashboard);

        await Expect(_nav.WalletsGroup).ToBeVisibleAsync();
        await Expect(_nav.RegistersLink).ToBeVisibleAsync();
        await Expect(_nav.AdministrationGroup).ToBeVisibleAsync();
    }

    [Test]
    public async Task Nav_HasUtilityLinks()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Dashboard);

        await Expect(_nav.SettingsLink).ToBeVisibleAsync();
        await Expect(_nav.HelpLink).ToBeVisibleAsync();
    }

    #endregion

    #region Drawer Toggle Tests

    [Test]
    public async Task Nav_DrawerCanBeToggled()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Dashboard);

        // Drawer starts open
        var initialState = await _nav.IsDrawerOpenAsync();

        // Toggle drawer
        await _nav.ToggleDrawerAsync();
        var afterToggle = await _nav.IsDrawerOpenAsync();

        Assert.That(afterToggle, Is.Not.EqualTo(initialState),
            "Drawer state should change after toggle");
    }

    #endregion

    #region Navigation Link Tests - Click and Verify URL

    [Test]
    [Retry(2)]
    public async Task Nav_DashboardLink_NavigatesCorrectly()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Settings);
        await _nav.NavigateToAsync(_nav.DashboardLink);

        Assert.That(Page.Url, Does.Contain("dashboard"),
            "Should navigate to dashboard");
    }

    [Test]
    public async Task Nav_PendingActionsLink_NavigatesCorrectly()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Dashboard);
        await _nav.NavigateToAsync(_nav.PendingActionsLink);

        Assert.That(Page.Url, Does.Contain("my-actions"),
            "Should navigate to pending actions");
    }

    [Test]
    public async Task Nav_DataSchemasLink_NavigatesCorrectly()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Dashboard);
        await _nav.NavigateToAsync(_nav.DataSchemasLink);

        Assert.That(Page.Url, Does.Contain("schemas"),
            "Should navigate to data schemas");
    }

    [Test]
    public async Task Nav_RegistersLink_NavigatesCorrectly()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Dashboard);
        await _nav.NavigateToAsync(_nav.RegistersLink);

        Assert.That(Page.Url, Does.Contain("registers"),
            "Should navigate to registers");
    }

    [Test]
    public async Task Nav_AdministrationLink_NavigatesCorrectly()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Dashboard);
        await _nav.ExpandNavGroupAsync(_nav.AdministrationGroup);
        await _nav.NavigateToAsync(_nav.SystemHealthLink);

        Assert.That(Page.Url, Does.Contain("admin"),
            "Should navigate to administration");
    }

    #endregion

    #region Expandable Nav Group Tests

    [Test]
    public async Task Nav_WalletsGroup_ExpandsAndShowsLinks()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Dashboard);

        await _nav.ExpandNavGroupAsync(_nav.WalletsGroup);

        await Expect(_nav.AllWalletsLink).ToBeVisibleAsync();
        await Expect(_nav.CreateWalletLink).ToBeVisibleAsync();
        await Expect(_nav.RecoverWalletLink).ToBeVisibleAsync();
    }

    #endregion

    #region User Menu Tests

    [Test]
    [Retry(2)]
    public async Task UserMenu_IsVisibleInAppBar()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Dashboard);

        await Expect(_nav.UserMenu).ToBeVisibleAsync();
    }

    [Test]
    [Retry(2)]
    public async Task UserMenu_ShowsUsername()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Dashboard);

        var username = await _nav.GetDisplayedUsernameAsync();
        Assert.That(username, Is.Not.Null.And.Not.Empty,
            "User menu should display the logged-in username");
    }

    #endregion

    #region Page Smoke Tests - Each authenticated route loads without errors

    [Test]
    [TestCase(TestConstants.AuthenticatedRoutes.Dashboard, "Dashboard")]
    [TestCase(TestConstants.AuthenticatedRoutes.MyActions, "Pending Actions")]
    [TestCase(TestConstants.AuthenticatedRoutes.MyWorkflows, "My Workflows")]
    [TestCase(TestConstants.AuthenticatedRoutes.MyTransactions, "My Transactions")]
    [TestCase(TestConstants.AuthenticatedRoutes.MyWallet, "My Wallet")]
    [TestCase(TestConstants.AuthenticatedRoutes.Blueprints, "Blueprints")]
    [TestCase(TestConstants.AuthenticatedRoutes.Designer, "Designer")]
    [TestCase(TestConstants.AuthenticatedRoutes.Templates, "Templates")]
    [TestCase(TestConstants.AuthenticatedRoutes.Schemas, "Data Schemas")]
    [TestCase(TestConstants.AuthenticatedRoutes.Wallets, "Wallets")]
    [TestCase(TestConstants.AuthenticatedRoutes.Registers, "Registers")]
    [TestCase(TestConstants.AuthenticatedRoutes.Admin, "Administration")]
    [TestCase(TestConstants.AuthenticatedRoutes.Settings, "Settings")]
    [TestCase(TestConstants.AuthenticatedRoutes.Help, "Help")]
    [Retry(2)]
    public async Task Page_LoadsWithoutCriticalErrors(string route, string pageName)
    {
        await NavigateAuthenticatedAsync(route);

        // Verify page rendered content
        var content = await Page.TextContentAsync("body") ?? "";
        Assert.That(content.Length, Is.GreaterThan(0),
            $"{pageName} page should render content");

        // Verify no critical console errors
        var criticalErrors = GetCriticalConsoleErrors();
        Assert.That(criticalErrors, Is.Empty,
            $"{pageName} page has console errors: {string.Join(", ", criticalErrors.Select(e => e.Text))}");
    }

    #endregion
}
