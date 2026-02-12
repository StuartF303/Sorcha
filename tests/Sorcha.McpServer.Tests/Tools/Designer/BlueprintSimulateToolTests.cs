// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Sorcha.McpServer.Infrastructure;
using Sorcha.McpServer.Services;
using Sorcha.McpServer.Tools.Designer;

namespace Sorcha.McpServer.Tests.Tools.Designer;

public class BlueprintSimulateToolTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<IMcpAuthorizationService> _authServiceMock;
    private readonly Mock<IMcpErrorHandler> _errorHandlerMock;
    private readonly Mock<IServiceAvailabilityTracker> _availabilityTrackerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<BlueprintSimulateTool>> _loggerMock;

    public BlueprintSimulateToolTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _authServiceMock = new Mock<IMcpAuthorizationService>();
        _errorHandlerMock = new Mock<IMcpErrorHandler>();
        _availabilityTrackerMock = new Mock<IServiceAvailabilityTracker>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<BlueprintSimulateTool>>();

        _configurationMock.Setup(c => c["ServiceClients:BlueprintService:Address"])
            .Returns("http://localhost:5000");
    }

    private BlueprintSimulateTool CreateTool()
    {
        return new BlueprintSimulateTool(
            _sessionServiceMock.Object,
            _authServiceMock.Object,
            _errorHandlerMock.Object,
            _availabilityTrackerMock.Object,
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SimulateActionAsync_Unauthorized_ReturnsUnauthorizedResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_simulate")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.SimulateActionAsync("bp-123", "0", "{}");

        // Assert
        result.Status.Should().Be("Unauthorized");
        result.Message.Should().Contain("Access denied");
    }

    [Fact]
    public async Task SimulateActionAsync_ServiceUnavailable_ReturnsUnavailableResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_simulate")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.SimulateActionAsync("bp-123", "0", "{}");

        // Assert
        result.Status.Should().Be("Unavailable");
        result.Message.Should().Contain("unavailable");
    }

    [Fact]
    public async Task SimulateActionAsync_EmptyBlueprintId_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_simulate")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.SimulateActionAsync("", "0", "{}");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Blueprint ID");
    }

    [Fact]
    public async Task SimulateActionAsync_EmptyActionId_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_simulate")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.SimulateActionAsync("bp-123", "", "{}");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Action ID");
    }

    [Fact]
    public async Task SimulateActionAsync_InvalidJson_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_simulate")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.SimulateActionAsync("bp-123", "0", "{ invalid }");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Invalid data JSON");
    }

    [Fact]
    public async Task SimulateActionAsync_SuccessfulSimulation_ReturnsSuccessResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_simulate")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var routeResponse = new
        {
            nextActions = new[]
            {
                new { actionId = 1, title = "Review", isTerminal = false },
                new { actionId = 2, title = "Approve", isTerminal = true }
            },
            matchedRoute = "default",
            routeDescription = "Routes to review or approval"
        };

        var calculateResponse = new
        {
            processedData = new Dictionary<string, object>
            {
                ["amount"] = 1000,
                ["tax"] = 100,
                ["total"] = 1100
            },
            calculatedFields = new[] { "tax", "total" }
        };

        var handler = new Mock<HttpMessageHandler>();

        // Route endpoint
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("/route")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(routeResponse))
            });

        // Calculate endpoint
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("/calculate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(calculateResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.SimulateActionAsync("bp-123", "0", "{\"amount\": 1000}");

        // Assert
        result.Status.Should().Be("Success");
        result.Message.Should().Contain("2 next action");

        result.Routing.Should().NotBeNull();
        result.Routing!.NextActions.Should().HaveCount(2);
        result.Routing.NextActions[0].ActionId.Should().Be(1);
        result.Routing.NextActions[0].Title.Should().Be("Review");
        result.Routing.NextActions[1].IsTerminal.Should().BeTrue();
        result.Routing.MatchedRoute.Should().Be("default");

        result.Calculations.Should().NotBeNull();
        result.Calculations!.CalculatedFields.Should().Contain("tax");
        result.Calculations.CalculatedFields.Should().Contain("total");

        _availabilityTrackerMock.Verify(a => a.RecordSuccess("Blueprint"), Times.Once);
    }

    [Fact]
    public async Task SimulateActionAsync_NoRouting_ReturnsSuccessWithNoRoutes()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_simulate")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var routeResponse = new
        {
            nextActions = Array.Empty<object>(),
            matchedRoute = (string?)null,
            routeDescription = (string?)null
        };

        var calculateResponse = new
        {
            processedData = new Dictionary<string, object> { ["value"] = 100 },
            calculatedFields = Array.Empty<string>()
        };

        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("/route")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(routeResponse))
            });

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("/calculate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(calculateResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.SimulateActionAsync("bp-123", "0", "{}");

        // Assert
        result.Status.Should().Be("Success");
        result.Message.Should().Contain("No routing");
        result.Routing!.NextActions.Should().BeEmpty();
    }

    [Fact]
    public async Task SimulateActionAsync_BlueprintNotFound_ReturnsErrorFromService()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_simulate")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var errorResponse = new { error = "Blueprint not found" };

        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("/route")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(JsonSerializer.Serialize(errorResponse))
            });

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("/calculate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(JsonSerializer.Serialize(errorResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.SimulateActionAsync("nonexistent", "0", "{}");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Blueprint not found");
    }

    [Fact]
    public async Task SimulateActionAsync_CorrectEndpointsCalled()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_simulate")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var routeResponse = new { nextActions = Array.Empty<object>() };
        var calculateResponse = new { processedData = new Dictionary<string, object>(), calculatedFields = Array.Empty<string>() };

        var calledUrls = new List<string>();
        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => calledUrls.Add(req.RequestUri?.ToString() ?? ""))
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                var content = req.RequestUri!.ToString().Contains("/route")
                    ? JsonSerializer.Serialize(routeResponse)
                    : JsonSerializer.Serialize(calculateResponse);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content)
                };
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        await tool.SimulateActionAsync("bp-123", "0", "{}");

        // Assert
        calledUrls.Should().Contain(url => url.Contains("/api/execution/route"));
        calledUrls.Should().Contain(url => url.Contains("/api/execution/calculate"));
    }

    [Fact]
    public async Task SimulateActionAsync_ResponseTimeIsRecorded()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_simulate")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var routeResponse = new { nextActions = Array.Empty<object>() };
        var calculateResponse = new { processedData = new Dictionary<string, object>(), calculatedFields = Array.Empty<string>() };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                var content = req.RequestUri!.ToString().Contains("/route")
                    ? JsonSerializer.Serialize(routeResponse)
                    : JsonSerializer.Serialize(calculateResponse);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content)
                };
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.SimulateActionAsync("bp-123", "0", "{}");

        // Assert
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.CheckedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
