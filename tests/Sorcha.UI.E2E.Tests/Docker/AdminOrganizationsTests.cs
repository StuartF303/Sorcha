// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.UI.E2E.Tests.Infrastructure;
using Sorcha.UI.E2E.Tests.PageObjects.AdminPages;

namespace Sorcha.UI.E2E.Tests.Docker;

/// <summary>
/// E2E tests for the Organizations admin page.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
[Category("Docker")]
[Category("Admin")]
public class AdminOrganizationsTests : AuthenticatedDockerTestBase
{
    private OrganizationsPage _organizationsPage = null!;

    [SetUp]
    public void SetUp()
    {
        _organizationsPage = new OrganizationsPage(Page);
    }

    #region Smoke Tests

    [Test]
    [Retry(3)]
    public async Task OrganizationsPage_LoadsSuccessfully()
    {
        await _organizationsPage.NavigateAsync();
        var loaded = await _organizationsPage.WaitForPageAsync();
        Assert.That(loaded, Is.True, "Organizations page should load successfully");
    }

    [Test]
    [Retry(3)]
    public async Task OrganizationsPage_ShowsCreateButton()
    {
        await _organizationsPage.NavigateAsync();
        await _organizationsPage.WaitForPageAsync();
        await Expect(_organizationsPage.CreateButton).ToBeVisibleAsync();
    }

    [Test]
    [Retry(3)]
    public async Task OrganizationsPage_ShowsOrgTable()
    {
        await _organizationsPage.NavigateAsync();
        await _organizationsPage.WaitForPageAsync();
        await Expect(_organizationsPage.OrgTable).ToBeVisibleAsync();
    }

    #endregion

    #region Create Organization Flow

    [Test]
    [Retry(3)]
    public async Task CreateButton_OpensDialog()
    {
        await _organizationsPage.NavigateAsync();
        await _organizationsPage.WaitForPageAsync();
        await _organizationsPage.ClickCreateAsync();
        await Expect(_organizationsPage.OrgFormDialog).ToBeVisibleAsync();
    }

    #endregion

    #region Display Tests

    [Test]
    [Retry(3)]
    public async Task OrganizationsPage_ShowsInactiveCheckbox()
    {
        await _organizationsPage.NavigateAsync();
        await _organizationsPage.WaitForPageAsync();
        await Expect(_organizationsPage.ShowInactiveCheckbox).ToBeVisibleAsync();
    }

    #endregion
}
