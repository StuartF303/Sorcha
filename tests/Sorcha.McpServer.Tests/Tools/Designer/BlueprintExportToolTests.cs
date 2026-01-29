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

public class BlueprintExportToolTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<IMcpAuthorizationService> _authServiceMock;
    private readonly Mock<IMcpErrorHandler> _errorHandlerMock;
    private readonly Mock<IServiceAvailabilityTracker> _availabilityTrackerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<BlueprintExportTool>> _loggerMock;

    public BlueprintExportToolTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _authServiceMock = new Mock<IMcpAuthorizationService>();
        _errorHandlerMock = new Mock<IMcpErrorHandler>();
        _availabilityTrackerMock = new Mock<IServiceAvailabilityTracker>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<BlueprintExportTool>>();

        _configurationMock.Setup(c => c["ServiceClients:BlueprintService:Address"])
            .Returns("http://localhost:5000");
    }

    private BlueprintExportTool CreateTool()
    {
        return new BlueprintExportTool(
            _sessionServiceMock.Object,
            _authServiceMock.Object,
            _errorHandlerMock.Object,
            _availabilityTrackerMock.Object,
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExportBlueprintAsync_Unauthorized_ReturnsUnauthorizedResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_export")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.ExportBlueprintAsync("bp-123");

        // Assert
        result.Status.Should().Be("Unauthorized");
        result.Message.Should().Contain("Access denied");
    }

    [Fact]
    public async Task ExportBlueprintAsync_EmptyBlueprintId_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_export")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.ExportBlueprintAsync("");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Blueprint ID");
    }

    [Fact]
    public async Task ExportBlueprintAsync_InvalidFormat_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_export")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.ExportBlueprintAsync("bp-123", "xml");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("json' or 'yaml");
    }

    [Fact]
    public async Task ExportBlueprintAsync_ServiceUnavailable_ReturnsUnavailableResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_export")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.ExportBlueprintAsync("bp-123");

        // Assert
        result.Status.Should().Be("Unavailable");
        result.Message.Should().Contain("unavailable");
    }

    [Fact]
    public async Task ExportBlueprintAsync_JsonFormat_ReturnsJsonContent()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_export")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var blueprintData = new
        {
            id = "bp-123",
            title = "Test Blueprint",
            participants = new[] { new { id = "p1" }, new { id = "p2" } },
            actions = new[] { new { id = 0, title = "Start" } }
        };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(blueprintData))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.ExportBlueprintAsync("bp-123", "json");

        // Assert
        result.Status.Should().Be("Success");
        result.Format.Should().Be("json");
        result.Content.Should().NotBeNullOrEmpty();
        result.Content.Should().Contain("Test Blueprint");
        result.ContentLength.Should().BeGreaterThan(0);

        _availabilityTrackerMock.Verify(a => a.RecordSuccess("Blueprint"), Times.Once);
    }

    [Fact]
    public async Task ExportBlueprintAsync_YamlFormat_ReturnsYamlContent()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_export")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var blueprintData = new
        {
            id = "bp-123",
            title = "Test Blueprint",
            participants = new[] { new { id = "p1" }, new { id = "p2" } },
            actions = new[] { new { id = 0, title = "Start" } }
        };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(blueprintData))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.ExportBlueprintAsync("bp-123", "yaml");

        // Assert
        result.Status.Should().Be("Success");
        result.Format.Should().Be("yaml");
        result.Content.Should().NotBeNullOrEmpty();
        // YAML doesn't have curly braces like JSON
        result.Content.Should().NotContain("{");
        result.ContentLength.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportBlueprintAsync_DefaultsToJson()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_export")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var blueprintData = new { id = "bp-123", title = "Test" };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(blueprintData))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act - no format specified
        var result = await tool.ExportBlueprintAsync("bp-123");

        // Assert
        result.Format.Should().Be("json");
    }

    [Fact]
    public async Task ExportBlueprintAsync_BlueprintNotFound_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_export")).Returns(true);
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
        var result = await tool.ExportBlueprintAsync("nonexistent");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Blueprint not found");
    }

    [Fact]
    public async Task ExportBlueprintAsync_CorrectEndpointCalled()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_export")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var blueprintData = new { id = "bp-123", title = "Test" };

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
                Content = new StringContent(JsonSerializer.Serialize(blueprintData))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        await tool.ExportBlueprintAsync("bp-123");

        // Assert
        capturedUrl.Should().Be("http://localhost:5000/api/blueprints/bp-123");
    }

    [Fact]
    public async Task ExportBlueprintAsync_ResponseTimeIsRecorded()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_export")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var blueprintData = new { id = "bp-123", title = "Test" };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(blueprintData))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.ExportBlueprintAsync("bp-123");

        // Assert
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.CheckedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
