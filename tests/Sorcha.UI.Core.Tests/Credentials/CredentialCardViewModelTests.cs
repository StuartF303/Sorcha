// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.UI.Core.Models.Credentials;
using Xunit;

namespace Sorcha.UI.Core.Tests.Credentials;

public class CredentialCardViewModelTests
{
    [Fact]
    public void IsExpiringSoon_WithinThirtyDays_ReturnsTrue()
    {
        var vm = new CredentialCardViewModel
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(15)
        };

        vm.IsExpiringSoon.Should().BeTrue();
    }

    [Fact]
    public void IsExpiringSoon_MoreThanThirtyDays_ReturnsFalse()
    {
        var vm = new CredentialCardViewModel
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(60)
        };

        vm.IsExpiringSoon.Should().BeFalse();
    }

    [Fact]
    public void IsExpiringSoon_AlreadyExpired_ReturnsFalse()
    {
        var vm = new CredentialCardViewModel
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        vm.IsExpiringSoon.Should().BeFalse();
    }

    [Fact]
    public void IsExpiringSoon_NullExpiry_ReturnsFalse()
    {
        var vm = new CredentialCardViewModel
        {
            ExpiresAt = null
        };

        vm.IsExpiringSoon.Should().BeFalse();
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var vm = new CredentialCardViewModel();

        vm.CredentialId.Should().BeEmpty();
        vm.Type.Should().BeEmpty();
        vm.Status.Should().Be("Active");
        vm.UsagePolicy.Should().Be("Reusable");
        vm.PresentationCount.Should().Be(0);
        vm.HighlightClaims.Should().BeEmpty();
        vm.AvailableActions.Should().BeEmpty();
        vm.DisplayConfig.Should().NotBeNull();
    }

    [Fact]
    public void DisplayConfig_DefaultValues_AreCorrect()
    {
        var config = new CredentialDisplayViewModel();

        config.BackgroundColor.Should().Be("#1976D2");
        config.TextColor.Should().Be("#FFFFFF");
        config.Icon.Should().Be("Certificate");
        config.CardLayout.Should().Be("Standard");
    }
}
