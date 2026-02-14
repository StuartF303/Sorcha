// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Sorcha.UI.Core.Models.Dashboard;
using Sorcha.UI.Core.Services;
using Xunit;

namespace Sorcha.UI.Core.Tests.Services;

/// <summary>
/// Unit tests for DashboardService wallet detection logic.
/// Tests verify the IsLoaded flag behavior for success and failure scenarios.
/// </summary>
public class DashboardServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<DashboardService>> _loggerMock;
    private readonly DashboardService _dashboardService;

    public DashboardServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:80")
        };
        _loggerMock = new Mock<ILogger<DashboardService>>();
        _dashboardService = new DashboardService(_httpClient, _loggerMock.Object);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// T015: Verify IsLoaded=true when API returns success
    /// </summary>
    [Fact]
    public async Task GetDashboardStatsAsync_Success_ReturnsStatsWithIsLoadedTrue()
    {
        // Arrange
        var expectedStats = new DashboardStatsViewModel
        {
            ActiveBlueprints = 5,
            TotalWallets = 2,
            RecentTransactions = 10,
            ConnectedPeers = 3,
            ActiveRegisters = 1,
            TotalOrganizations = 1
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expectedStats)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains("/api/dashboard")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _dashboardService.GetDashboardStatsAsync();

        // Assert
        result.IsLoaded.Should().BeTrue("API call succeeded");
        result.TotalWallets.Should().Be(2, "should return correct wallet count");
        result.ActiveBlueprints.Should().Be(5);
        result.RecentTransactions.Should().Be(10);
        result.ConnectedPeers.Should().Be(3);
        result.ActiveRegisters.Should().Be(1);
        result.TotalOrganizations.Should().Be(1);
    }

    /// <summary>
    /// T016: Verify IsLoaded=false when API returns error status
    /// </summary>
    [Fact]
    public async Task GetDashboardStatsAsync_ApiFailure_ReturnsIsLoadedFalse()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal Server Error")
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains("/api/dashboard")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _dashboardService.GetDashboardStatsAsync();

        // Assert
        result.IsLoaded.Should().BeFalse("API returned error status code");
        result.TotalWallets.Should().Be(0, "should have default value when load fails");

        // Verify warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Failed to fetch dashboard stats")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// T017: Verify IsLoaded=false when exception occurs
    /// </summary>
    [Fact]
    public async Task GetDashboardStatsAsync_Exception_ReturnsIsLoadedFalse()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _dashboardService.GetDashboardStatsAsync();

        // Assert
        result.IsLoaded.Should().BeFalse("exception occurred during API call");
        result.TotalWallets.Should().Be(0, "should have default value when exception occurs");

        // Verify error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error fetching dashboard statistics")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Additional test: Verify behavior with ServiceUnavailable (503) status
    /// </summary>
    [Fact]
    public async Task GetDashboardStatsAsync_ServiceUnavailable_ReturnsIsLoadedFalse()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("Service Unavailable")
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _dashboardService.GetDashboardStatsAsync();

        // Assert
        result.IsLoaded.Should().BeFalse("service unavailable (503)");
        result.TotalWallets.Should().Be(0);
    }
}
