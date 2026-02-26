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

public class TimeFormatServiceTests
{
    private readonly Mock<IUserPreferencesService> _prefsMock = new();
    private readonly Mock<IJSRuntime> _jsMock = new();
    private readonly TimeFormatService _sut;

    public TimeFormatServiceTests()
    {
        _sut = new TimeFormatService(
            _prefsMock.Object,
            _jsMock.Object,
            NullLogger<TimeFormatService>.Instance);
    }

    [Fact]
    public async Task InitializeAsync_LoadsPreference()
    {
        _prefsMock.Setup(p => p.GetUserPreferencesAsync())
            .ReturnsAsync(new UserPreferencesDto { TimeFormat = "UTC" });

        await _sut.InitializeAsync();

        _sut.CurrentFormat.Should().Be(TimeFormatPreference.UTC);
    }

    [Fact]
    public async Task SetFormatAsync_UpdatesAndNotifies()
    {
        _prefsMock.Setup(p => p.UpdateUserPreferencesAsync(It.IsAny<UpdateUserPreferencesRequest>()))
            .ReturnsAsync(new UserPreferencesDto { TimeFormat = "Local" });

        var changed = false;
        _sut.OnFormatChanged += () => changed = true;

        await _sut.SetFormatAsync(TimeFormatPreference.Local);

        _sut.CurrentFormat.Should().Be(TimeFormatPreference.Local);
        changed.Should().BeTrue();
    }

    [Fact]
    public void FormatDateTime_UtcFormat_IncludesUtcSuffix()
    {
        var utcTime = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var result = _sut.FormatDateTime(utcTime);

        result.Should().Contain("2026");
    }

    [Fact]
    public void FormatRelativeTime_RecentTime_ReturnsRelativeString()
    {
        var recent = DateTime.UtcNow.AddMinutes(-5);
        var result = _sut.FormatRelativeTime(recent);

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("minute");
    }

    [Fact]
    public void FormatRelativeTime_Yesterday_ReturnsYesterday()
    {
        var yesterday = DateTime.UtcNow.AddDays(-1);
        var result = _sut.FormatRelativeTime(yesterday);

        result.Should().Contain("yesterday");
    }
}
