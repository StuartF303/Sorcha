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

public sealed class ActionSubmitToolTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<IMcpAuthorizationService> _authServiceMock;
    private readonly Mock<IMcpErrorHandler> _errorHandlerMock;
    private readonly Mock<IServiceAvailabilityTracker> _availabilityTrackerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<ActionSubmitTool>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly ActionSubmitTool _tool;

    public ActionSubmitToolTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _authServiceMock = new Mock<IMcpAuthorizationService>();
        _errorHandlerMock = new Mock<IMcpErrorHandler>();
        _availabilityTrackerMock = new Mock<IServiceAvailabilityTracker>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<ActionSubmitTool>>();

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceClients:BlueprintService:Address"] = "http://localhost:5000"
            })
            .Build();

        _tool = new ActionSubmitTool(
            _sessionServiceMock.Object,
            _authServiceMock.Object,
            _errorHandlerMock.Object,
            _availabilityTrackerMock.Object,
            _httpClientFactoryMock.Object,
            _configuration,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SubmitActionAsync_WhenUnauthorized_ReturnsUnauthorizedStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_action_submit")).Returns(false);

        // Act
        var result = await _tool.SubmitActionAsync("action-123", "{}");

        // Assert
        result.Status.Should().Be("Unauthorized");
        result.Message.Should().Contain("sorcha:participant");
    }

    [Fact]
    public async Task SubmitActionAsync_WithEmptyActionId_ReturnsError()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_action_submit")).Returns(true);

        // Act
        var result = await _tool.SubmitActionAsync("", "{}");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Action instance ID is required");
    }

    [Fact]
    public async Task SubmitActionAsync_WithEmptyData_ReturnsError()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_action_submit")).Returns(true);

        // Act
        var result = await _tool.SubmitActionAsync("action-123", "");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Data JSON is required");
    }

    [Fact]
    public async Task SubmitActionAsync_WithInvalidJson_ReturnsError()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_action_submit")).Returns(true);

        // Act
        var result = await _tool.SubmitActionAsync("action-123", "not valid json {");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Invalid data JSON format");
    }

    [Fact]
    public async Task SubmitActionAsync_WhenServiceUnavailable_ReturnsUnavailableStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_action_submit")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(false);

        // Act
        var result = await _tool.SubmitActionAsync("action-123", "{\"name\":\"test\"}");

        // Assert
        result.Status.Should().Be("Unavailable");
        result.Message.Should().Contain("Blueprint service");
    }

    [Fact]
    public async Task SubmitActionAsync_WithSuccessfulSubmission_ReturnsSuccess()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_action_submit")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        var response = new
        {
            message = "Action submitted successfully.",
            transactionId = "tx-789",
            nextActions = new[]
            {
                new { actionId = 2, title = "Next Action", assignedTo = "participant-2" }
            }
        };

        SetupHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        // Act
        var result = await _tool.SubmitActionAsync("action-123", "{\"name\":\"John Doe\"}");

        // Assert
        result.Status.Should().Be("Success");
        result.TransactionId.Should().Be("tx-789");
        result.NextActions.Should().HaveCount(1);
    }

    [Fact]
    public async Task SubmitActionAsync_WithValidationError_ReturnsValidationErrors()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_action_submit")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        var response = new
        {
            error = "Validation failed",
            validationErrors = new[]
            {
                "Invalid email format",
                "Phone number required"
            }
        };

        SetupHttpClient(HttpStatusCode.BadRequest, JsonSerializer.Serialize(response));

        // Act
        var result = await _tool.SubmitActionAsync("action-123", "{\"name\":\"test\"}");

        // Assert
        result.Status.Should().Be("Error");
        result.ValidationErrors.Should().HaveCount(2);
    }

    [Fact]
    public async Task SubmitActionAsync_WithNotFound_ReturnsErrorStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_action_submit")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        SetupHttpClient(HttpStatusCode.NotFound, "{\"error\":\"Action not found\"}");

        // Act
        var result = await _tool.SubmitActionAsync("action-invalid", "{\"data\":\"test\"}");

        // Assert
        result.Status.Should().Be("Error");
    }

    [Fact]
    public async Task SubmitActionAsync_WithTimeout_ReturnsTimeoutStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_action_submit")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        SetupHttpClientWithException(new TaskCanceledException());

        // Act
        var result = await _tool.SubmitActionAsync("action-123", "{\"data\":\"test\"}");

        // Assert
        result.Status.Should().Be("Timeout");
        _availabilityTrackerMock.Verify(x => x.RecordFailure("Blueprint"), Times.Once);
    }

    [Fact]
    public async Task SubmitActionAsync_WithHttpException_ReturnsErrorStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_action_submit")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        SetupHttpClientWithException(new HttpRequestException("Network error"));

        // Act
        var result = await _tool.SubmitActionAsync("action-123", "{\"data\":\"test\"}");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Network error");
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
