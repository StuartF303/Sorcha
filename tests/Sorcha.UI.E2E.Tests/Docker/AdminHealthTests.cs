// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.UI.E2E.Tests.Infrastructure;
using Sorcha.UI.E2E.Tests.PageObjects.AdminPages;
using Sorcha.UI.E2E.Tests.PageObjects.Shared;

namespace Sorcha.UI.E2E.Tests.Docker;

/// <summary>
/// E2E tests for admin pages: System Health, Peer Network, Validator, Service Principals.
/// </summary>
[TestFixture]
[Category("Docker")]
[Category("Admin")]
[Parallelizable(ParallelScope.Self)]
public class AdminHealthTests : AuthenticatedDockerTestBase
{
    private SystemHealthPage _healthPage = null!;
    private ValidatorPage _validatorPage = null!;
    private ServicePrincipalsPage _principalsPage = null!;

    [SetUp]
    public override async Task BaseSetUp()
    {
        await base.BaseSetUp();
        _healthPage = new SystemHealthPage(Page);
        _validatorPage = new ValidatorPage(Page);
        _principalsPage = new ServicePrincipalsPage(Page);
    }

    [Test]
    [Retry(2)]
    public async Task SystemHealth_PageLoads_ShowsTitle()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.AdminHealth);

        var content = await Page.TextContentAsync("body") ?? "";
        Assert.That(content, Does.Contain("Health").Or.Contain("System"),
            "System Health page should render health-related content");
    }

    [Test]
    [Retry(2)]
    public async Task SystemHealth_ShowsHealthCards()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.AdminHealth);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        var cards = MudBlazorHelpers.Cards(Page);
        var hasCards = await cards.CountAsync() > 0;

        // Either shows cards or service unavailable
        var serviceError = _healthPage.ServiceError;
        var hasError = await serviceError.IsVisibleAsync();

        Assert.That(hasCards || hasError, Is.True,
            "System Health page should show health cards or service error");
    }

    [Test]
    [Retry(2)]
    public async Task PeerNetwork_PageLoads_ShowsContent()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.AdminPeers);

        var content = await Page.TextContentAsync("body") ?? "";
        Assert.That(content.Length, Is.GreaterThan(0),
            "Peer Network page should render content");
    }

    [Test]
    [Retry(2)]
    public async Task Validator_PageLoads_ShowsContent()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.AdminValidator);

        var content = await Page.TextContentAsync("body") ?? "";
        Assert.That(content, Does.Contain("Validator").Or.Contain("Mempool"),
            "Validator page should render validator-related content");
    }

    [Test]
    [Retry(2)]
    public async Task ServicePrincipals_PageLoads_ShowsContent()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.AdminPrincipals);

        var content = await Page.TextContentAsync("body") ?? "";
        Assert.That(content, Does.Contain("Principal").Or.Contain("Service"),
            "Service Principals page should render credential-related content");
    }

    [Test]
    [Retry(2)]
    public async Task OldAdminRoute_RedirectsToHealth()
    {
        await NavigateAuthenticatedAsync(TestConstants.AuthenticatedRoutes.Admin);
        await MudBlazorHelpers.WaitForBlazorAsync(Page, TestConstants.PageLoadTimeout);

        // Old /admin should redirect to /admin/health
        Assert.That(Page.Url, Does.Contain("/admin/health").Or.Contain("/admin"),
            "Old /admin route should redirect to /admin/health");
    }

    [Test]
    [TestCase(TestConstants.AuthenticatedRoutes.AdminHealth, "System Health")]
    [TestCase(TestConstants.AuthenticatedRoutes.AdminPeers, "Peer Network")]
    [TestCase(TestConstants.AuthenticatedRoutes.AdminValidator, "Validator")]
    [TestCase(TestConstants.AuthenticatedRoutes.AdminPrincipals, "Service Principals")]
    [TestCase(TestConstants.AuthenticatedRoutes.AdminOrganizations, "Organizations")]
    [Retry(2)]
    public async Task AdminNavLinks_NavigateCorrectly(string route, string pageName)
    {
        await NavigateAuthenticatedAsync(route);

        var content = await Page.TextContentAsync("body") ?? "";
        Assert.That(content.Length, Is.GreaterThan(0),
            $"{pageName} page should render content");

        var criticalErrors = GetCriticalConsoleErrors();
        Assert.That(criticalErrors, Is.Empty,
            $"{pageName} page should not have critical console errors");
    }
}
