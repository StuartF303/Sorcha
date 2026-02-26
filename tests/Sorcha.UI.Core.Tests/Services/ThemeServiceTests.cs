// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moq;
using Sorcha.UI.Core.Models;
using Sorcha.UI.Core.Services;
using Xunit;

namespace Sorcha.UI.Core.Tests.Services;

public class ThemeServiceTests
{
    private readonly Mock<IUserPreferencesService> _prefsMock = new();
    private readonly Mock<IJSRuntime> _jsMock = new();
    private readonly ThemeService _sut;

    public ThemeServiceTests()
    {
        _sut = new ThemeService(
            _prefsMock.Object,
            _jsMock.Object,
            NullLogger<ThemeService>.Instance);
    }

    [Fact]
    public async Task InitializeAsync_LoadsPreferenceFromServer()
    {
        _prefsMock.Setup(p => p.GetUserPreferencesAsync())
            .ReturnsAsync(new UserPreferencesDto { Theme = "Dark" });

        await _sut.InitializeAsync();

        _sut.CurrentTheme.Should().Be(ThemePreference.Dark);
        _sut.IsDarkMode.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_SystemTheme_DetectsOsDarkMode()
    {
        _prefsMock.Setup(p => p.GetUserPreferencesAsync())
            .ReturnsAsync(new UserPreferencesDto { Theme = "System" });
        _jsMock.Setup(j => j.InvokeAsync<bool>("eval", It.IsAny<object[]>()))
            .ReturnsAsync(true);

        await _sut.InitializeAsync();

        _sut.CurrentTheme.Should().Be(ThemePreference.System);
        _sut.IsDarkMode.Should().BeTrue();
    }

    [Fact]
    public async Task SetThemeAsync_UpdatesPreferenceAndNotifies()
    {
        _prefsMock.Setup(p => p.UpdateUserPreferencesAsync(It.IsAny<UpdateUserPreferencesRequest>()))
            .ReturnsAsync(new UserPreferencesDto { Theme = "Light" });

        var changed = false;
        _sut.OnThemeChanged += () => changed = true;

        await _sut.SetThemeAsync(ThemePreference.Light);

        _sut.CurrentTheme.Should().Be(ThemePreference.Light);
        _sut.IsDarkMode.Should().BeFalse();
        changed.Should().BeTrue();
    }

    [Fact]
    public void IsDarkMode_LightTheme_ReturnsFalse()
    {
        // Default is Light (ThemePreference.Light = 0)
        _sut.IsDarkMode.Should().BeFalse();
    }
}
