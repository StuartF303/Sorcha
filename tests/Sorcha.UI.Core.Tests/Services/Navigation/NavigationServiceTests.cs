// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Sorcha.UI.Core.Services.Navigation;
using Xunit;

namespace Sorcha.UI.Core.Tests.Services.Navigation;

/// <summary>
/// Unit tests for <see cref="NavigationService"/>.
/// Uses bUnit's FakeNavigationManager for testing Blazor navigation.
/// </summary>
public class NavigationServiceTests : IDisposable
{
    private readonly BunitContext _ctx;
    private NavigationManager NavigationManager => _ctx.Services.GetRequiredService<NavigationManager>();

    public NavigationServiceTests()
    {
        _ctx = new BunitContext();
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    #region CurrentUri

    [Fact]
    public void CurrentUri_ReturnsNavigationManagerUri()
    {
        // Arrange
        var service = new NavigationService(NavigationManager);

        // Act
        var result = service.CurrentUri;

        // Assert
        result.Should().Be(NavigationManager.Uri);
    }

    #endregion

    #region RedirectToLoginAsync

    [Fact]
    public async Task RedirectToLoginAsync_WithValidReturnUrl_NavigatesToLoginWithParameter()
    {
        // Arrange
        var service = new NavigationService(NavigationManager);
        var returnUrl = "/app/registers/123";

        // Act
        await service.RedirectToLoginAsync(returnUrl);

        // Assert
        NavigationManager.Uri.Should().Contain("auth/login");
        NavigationManager.Uri.Should().Contain("returnUrl=");
    }

    [Fact]
    public async Task RedirectToLoginAsync_WithNullReturnUrl_NavigatesToLoginWithoutParameter()
    {
        // Arrange
        var service = new NavigationService(NavigationManager);

        // Act
        await service.RedirectToLoginAsync(null);

        // Assert
        NavigationManager.Uri.Should().EndWith("auth/login");
        NavigationManager.Uri.Should().NotContain("returnUrl=");
    }

    [Fact]
    public async Task RedirectToLoginAsync_WithInvalidReturnUrl_NavigatesToLoginWithoutParameter()
    {
        // Arrange
        var service = new NavigationService(NavigationManager);
        var returnUrl = "https://evil.com/steal-credentials";

        // Act
        await service.RedirectToLoginAsync(returnUrl);

        // Assert
        NavigationManager.Uri.Should().EndWith("auth/login");
        NavigationManager.Uri.Should().NotContain("returnUrl=");
    }

    [Fact]
    public async Task RedirectToLoginAsync_WhenAlreadyOnLoginPage_DoesNotNavigate()
    {
        // Arrange
        NavigationManager.NavigateTo("auth/login");
        var initialUri = NavigationManager.Uri;
        var service = new NavigationService(NavigationManager);

        // Act
        await service.RedirectToLoginAsync("/dashboard");

        // Assert - URI should not have changed (no new returnUrl added)
        NavigationManager.Uri.Should().Be(initialUri);
    }

    [Fact]
    public async Task RedirectToLoginAsync_WhenOnLoginPageWithQueryParams_DoesNotNavigate()
    {
        // Arrange
        NavigationManager.NavigateTo("auth/login?returnUrl=%2Fdashboard");
        var initialUri = NavigationManager.Uri;
        var service = new NavigationService(NavigationManager);

        // Act
        await service.RedirectToLoginAsync("/other-page");

        // Assert - URI should not have changed
        NavigationManager.Uri.Should().Be(initialUri);
    }

    #endregion

    #region NavigateToValidatedUrl

    [Fact]
    public void NavigateToValidatedUrl_WithValidUrl_NavigatesToUrl()
    {
        // Arrange
        var service = new NavigationService(NavigationManager);
        var url = "/app/registers/123";

        // Act
        service.NavigateToValidatedUrl(url);

        // Assert
        NavigationManager.Uri.Should().EndWith(url);
    }

    [Fact]
    public void NavigateToValidatedUrl_WithInvalidUrl_NavigatesToDefaultDestination()
    {
        // Arrange
        var service = new NavigationService(NavigationManager);
        var invalidUrl = "https://evil.com/";

        // Act
        service.NavigateToValidatedUrl(invalidUrl);

        // Assert
        NavigationManager.Uri.Should().EndWith("dashboard");
    }

    [Fact]
    public void NavigateToValidatedUrl_WithNullUrl_NavigatesToDefaultDestination()
    {
        // Arrange
        var service = new NavigationService(NavigationManager);

        // Act
        service.NavigateToValidatedUrl(null);

        // Assert
        NavigationManager.Uri.Should().EndWith("dashboard");
    }

    [Fact]
    public void NavigateToValidatedUrl_WithCustomDefaultDestination_UsesCustomDefault()
    {
        // Arrange
        var service = new NavigationService(NavigationManager);
        var customDefault = "home";

        // Act
        service.NavigateToValidatedUrl(null, customDefault);

        // Assert
        NavigationManager.Uri.Should().EndWith(customDefault);
    }

    [Fact]
    public void NavigateToValidatedUrl_WithJavaScriptUrl_NavigatesToDefaultDestination()
    {
        // Arrange
        var service = new NavigationService(NavigationManager);
        var jsUrl = "javascript:alert('xss')";

        // Act
        service.NavigateToValidatedUrl(jsUrl);

        // Assert
        NavigationManager.Uri.Should().EndWith("dashboard");
    }

    #endregion
}
