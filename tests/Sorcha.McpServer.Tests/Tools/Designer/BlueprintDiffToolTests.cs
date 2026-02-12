// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Sorcha.McpServer.Infrastructure;
using Sorcha.McpServer.Services;
using Sorcha.McpServer.Tools.Designer;

namespace Sorcha.McpServer.Tests.Tools.Designer;

public class BlueprintDiffToolTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<IMcpAuthorizationService> _authServiceMock;
    private readonly Mock<IMcpErrorHandler> _errorHandlerMock;
    private readonly Mock<IServiceAvailabilityTracker> _availabilityTrackerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<BlueprintDiffTool>> _loggerMock;

    public BlueprintDiffToolTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _authServiceMock = new Mock<IMcpAuthorizationService>();
        _errorHandlerMock = new Mock<IMcpErrorHandler>();
        _availabilityTrackerMock = new Mock<IServiceAvailabilityTracker>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<BlueprintDiffTool>>();

        _configurationMock.Setup(c => c["ServiceClients:BlueprintService:Address"])
            .Returns("http://localhost:5000");
    }

    private BlueprintDiffTool CreateTool()
    {
        return new BlueprintDiffTool(
            _sessionServiceMock.Object,
            _authServiceMock.Object,
            _errorHandlerMock.Object,
            _availabilityTrackerMock.Object,
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CompareBlueprintVersionsAsync_Unauthorized_ReturnsUnauthorizedResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_diff")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.CompareBlueprintVersionsAsync("bp-123", 1, 2);

        // Assert
        result.Status.Should().Be("Unauthorized");
        result.Message.Should().Contain("Access denied");
    }

    [Fact]
    public async Task CompareBlueprintVersionsAsync_EmptyBlueprintId_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_diff")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.CompareBlueprintVersionsAsync("", 1, 2);

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Blueprint ID");
    }

    [Fact]
    public async Task CompareBlueprintVersionsAsync_InvalidFromVersion_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_diff")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.CompareBlueprintVersionsAsync("bp-123", 0, 2);

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("From version must be at least 1");
    }

    [Fact]
    public async Task CompareBlueprintVersionsAsync_ServiceUnavailable_ReturnsUnavailableResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_diff")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.CompareBlueprintVersionsAsync("bp-123", 1, 2);

        // Assert
        result.Status.Should().Be("Unavailable");
        result.Message.Should().Contain("unavailable");
    }

    [Fact]
    public async Task CompareBlueprintVersionsAsync_WithChanges_ReturnsSuccessResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_diff")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var diffResponse = new
        {
            fromVersion = 1,
            toVersion = 2,
            changes = new[]
            {
                new { path = "/title", changeType = "Modified", oldValue = (string?)"Old Title", newValue = "New Title" },
                new { path = "/actions/1", changeType = "Added", oldValue = (string?)null, newValue = "New Action" }
            }
        };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(diffResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.CompareBlueprintVersionsAsync("bp-123", 1, 2);

        // Assert
        result.Status.Should().Be("Success");
        result.Message.Should().Contain("2 change(s)");
        result.FromVersion.Should().Be(1);
        result.ToVersion.Should().Be(2);
        result.Changes.Should().HaveCount(2);
        result.Changes[0].Path.Should().Be("/title");
        result.Changes[0].ChangeType.Should().Be("Modified");
        result.Changes[0].OldValue.Should().Be("Old Title");
        result.Changes[0].NewValue.Should().Be("New Title");
        result.TotalChanges.Should().Be(2);

        _availabilityTrackerMock.Verify(a => a.RecordSuccess("Blueprint"), Times.Once);
    }

    [Fact]
    public async Task CompareBlueprintVersionsAsync_NoChanges_ReturnsSuccessResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_diff")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var diffResponse = new
        {
            fromVersion = 1,
            toVersion = 2,
            changes = Array.Empty<object>()
        };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(diffResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.CompareBlueprintVersionsAsync("bp-123", 1, 2);

        // Assert
        result.Status.Should().Be("Success");
        result.Message.Should().Contain("No changes");
        result.Changes.Should().BeEmpty();
        result.TotalChanges.Should().Be(0);
    }

    [Fact]
    public async Task CompareBlueprintVersionsAsync_ToLatest_BuildsCorrectUrl()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_diff")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var diffResponse = new { fromVersion = 1, toVersion = 3, changes = Array.Empty<object>() };

        string? capturedUrl = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedUrl = req.RequestUri?.ToString())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(diffResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act - toVersion = 0 means latest
        await tool.CompareBlueprintVersionsAsync("bp-123", 1, 0);

        // Assert - should not include "to" parameter
        capturedUrl.Should().Be("http://localhost:5000/api/blueprints/bp-123/diff?from=1");
    }

    [Fact]
    public async Task CompareBlueprintVersionsAsync_ToSpecificVersion_BuildsCorrectUrl()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_diff")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var diffResponse = new { fromVersion = 1, toVersion = 2, changes = Array.Empty<object>() };

        string? capturedUrl = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedUrl = req.RequestUri?.ToString())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(diffResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        await tool.CompareBlueprintVersionsAsync("bp-123", 1, 2);

        // Assert
        capturedUrl.Should().Be("http://localhost:5000/api/blueprints/bp-123/diff?from=1&to=2");
    }

    [Fact]
    public async Task CompareBlueprintVersionsAsync_BlueprintNotFound_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_diff")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var errorResponse = new { error = "Blueprint not found" };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(JsonSerializer.Serialize(errorResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.CompareBlueprintVersionsAsync("nonexistent", 1, 2);

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Blueprint not found");
    }

    [Fact]
    public async Task CompareBlueprintVersionsAsync_ResponseTimeIsRecorded()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_diff")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var diffResponse = new { fromVersion = 1, toVersion = 2, changes = Array.Empty<object>() };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(diffResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.CompareBlueprintVersionsAsync("bp-123", 1, 2);

        // Assert
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.CheckedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
