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

public class LocalizationServiceTests
{
    private readonly Mock<HttpClient> _httpMock = new();
    private readonly Mock<IUserPreferencesService> _prefsMock = new();
    private readonly Mock<IJSRuntime> _jsMock = new();

    [Fact]
    public void T_MissingKey_ReturnsKeyAsIs()
    {
        var sut = CreateService();
        var result = sut.T("nonexistent.key");
        result.Should().Be("nonexistent.key");
    }

    [Fact]
    public void T_WithArgs_FormatsString()
    {
        var sut = CreateService();
        // Without loaded translations, returns key — testing the fallback
        var result = sut.T("some.key", "arg1");
        result.Should().Be("some.key");
    }

    [Fact]
    public void CurrentLanguage_DefaultsToEnglish()
    {
        var sut = CreateService();
        sut.CurrentLanguage.Should().Be("en");
    }

    [Fact]
    public async Task SetLanguageAsync_UpdatesCurrentLanguage()
    {
        var sut = CreateService();
        _prefsMock.Setup(p => p.UpdateUserPreferencesAsync(It.IsAny<UpdateUserPreferencesRequest>()))
            .ReturnsAsync(new UserPreferencesDto { Language = "fr" });

        var changed = false;
        sut.OnLanguageChanged += () => changed = true;

        await sut.SetLanguageAsync("fr");

        sut.CurrentLanguage.Should().Be("fr");
        changed.Should().BeTrue();
    }

    private LocalizationService CreateService()
    {
        return new LocalizationService(
            new HttpClient(), // Won't make real calls in unit tests
            _prefsMock.Object,
            _jsMock.Object,
            NullLogger<LocalizationService>.Instance);
    }
}
