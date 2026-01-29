// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Sorcha.McpServer.Infrastructure;
using Sorcha.McpServer.Services;
using Sorcha.McpServer.Tools.Participant;

namespace Sorcha.McpServer.Tests.Tools.Participant;

public sealed class DisclosedDataToolTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<IMcpAuthorizationService> _authServiceMock;
    private readonly Mock<IMcpErrorHandler> _errorHandlerMock;
    private readonly Mock<IServiceAvailabilityTracker> _availabilityTrackerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<DisclosedDataTool>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly DisclosedDataTool _tool;

    public DisclosedDataToolTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _authServiceMock = new Mock<IMcpAuthorizationService>();
        _errorHandlerMock = new Mock<IMcpErrorHandler>();
        _availabilityTrackerMock = new Mock<IServiceAvailabilityTracker>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<DisclosedDataTool>>();

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceClients:BlueprintService:Address"] = "http://localhost:5000"
            })
            .Build();

        _tool = new DisclosedDataTool(
            _sessionServiceMock.Object,
            _authServiceMock.Object,
            _errorHandlerMock.Object,
            _availabilityTrackerMock.Object,
            _httpClientFactoryMock.Object,
            _configuration,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetDisclosedDataAsync_WhenUnauthorized_ReturnsUnauthorizedStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_disclosed_data")).Returns(false);

        // Act
        var result = await _tool.GetDisclosedDataAsync("wf-123");

        // Assert
        result.Status.Should().Be("Unauthorized");
        result.Message.Should().Contain("sorcha:participant");
    }

    [Fact]
    public async Task GetDisclosedDataAsync_WithEmptyWorkflowId_ReturnsError()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_disclosed_data")).Returns(true);

        // Act
        var result = await _tool.GetDisclosedDataAsync("");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Workflow instance ID is required");
    }

    [Fact]
    public async Task GetDisclosedDataAsync_WhenServiceUnavailable_ReturnsUnavailableStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_disclosed_data")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(false);

        // Act
        var result = await _tool.GetDisclosedDataAsync("wf-123");

        // Assert
        result.Status.Should().Be("Unavailable");
        result.Message.Should().Contain("Blueprint service");
    }

    [Fact]
    public async Task GetDisclosedDataAsync_WithDisclosures_ReturnsDisclosedData()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_disclosed_data")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        var response = new
        {
            disclosures = new[]
            {
                new
                {
                    actionId = 1,
                    actionTitle = "Initial Submission",
                    disclosedAt = DateTimeOffset.UtcNow.AddDays(-1),
                    data = new Dictionary<string, object>
                    {
                        ["applicantName"] = "John Doe",
                        ["email"] = "john@example.com"
                    }
                },
                new
                {
                    actionId = 2,
                    actionTitle = "Manager Review",
                    disclosedAt = DateTimeOffset.UtcNow.AddHours(-2),
                    data = new Dictionary<string, object>
                    {
                        ["reviewerComments"] = "Approved"
                    }
                }
            }
        };

        SetupHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        // Act
        var result = await _tool.GetDisclosedDataAsync("wf-123");

        // Assert
        result.Status.Should().Be("Success");
        result.Disclosures.Should().HaveCount(2);
        result.Disclosures[0].ActionId.Should().Be(1);
        result.Disclosures[0].ActionTitle.Should().Be("Initial Submission");
        result.TotalFields.Should().Be(3); // 2 + 1
    }

    [Fact]
    public async Task GetDisclosedDataAsync_WithNoDisclosures_ReturnsEmptyList()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_disclosed_data")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        var response = new { disclosures = Array.Empty<object>() };
        SetupHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        // Act
        var result = await _tool.GetDisclosedDataAsync("wf-123");

        // Assert
        result.Status.Should().Be("Success");
        result.Disclosures.Should().BeEmpty();
        result.Message.Should().Contain("No data has been disclosed");
    }

    [Fact]
    public async Task GetDisclosedDataAsync_WithActionFilter_UsesCorrectEndpoint()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_disclosed_data")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        var response = new { disclosures = Array.Empty<object>() };
        var handlerMock = SetupHttpClientWithCapture(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        // Act
        await _tool.GetDisclosedDataAsync("wf-123", "action-456");

        // Assert
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.ToString().Contains("/actions/action-456/disclosures")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetDisclosedDataAsync_WithoutActionFilter_UsesWorkflowEndpoint()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_disclosed_data")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        var response = new { disclosures = Array.Empty<object>() };
        var handlerMock = SetupHttpClientWithCapture(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        // Act
        await _tool.GetDisclosedDataAsync("wf-123");

        // Assert
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.ToString().Contains("/workflows/wf-123/disclosures") &&
                !req.RequestUri.ToString().Contains("/actions/")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetDisclosedDataAsync_WithNotFound_ReturnsErrorStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_disclosed_data")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        SetupHttpClient(HttpStatusCode.NotFound, "{\"error\":\"Workflow not found\"}");

        // Act
        var result = await _tool.GetDisclosedDataAsync("wf-invalid");

        // Assert
        result.Status.Should().Be("Error");
    }

    [Fact]
    public async Task GetDisclosedDataAsync_WithTimeout_ReturnsTimeoutStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_disclosed_data")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        SetupHttpClientWithException(new TaskCanceledException());

        // Act
        var result = await _tool.GetDisclosedDataAsync("wf-123");

        // Assert
        result.Status.Should().Be("Timeout");
        _availabilityTrackerMock.Verify(x => x.RecordFailure("Blueprint"), Times.Once);
    }

    [Fact]
    public async Task GetDisclosedDataAsync_WithHttpException_ReturnsErrorStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_disclosed_data")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        SetupHttpClientWithException(new HttpRequestException("Connection refused"));

        // Act
        var result = await _tool.GetDisclosedDataAsync("wf-123");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Connection refused");
    }

    private void SetupHttpClient(HttpStatusCode statusCode, string content)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });

        var client = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
    }

    private Mock<HttpMessageHandler> SetupHttpClientWithCapture(HttpStatusCode statusCode, string content)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });

        var client = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
        return handlerMock;
    }

    private void SetupHttpClientWithException(Exception exception)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);

        var client = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
    }
}
