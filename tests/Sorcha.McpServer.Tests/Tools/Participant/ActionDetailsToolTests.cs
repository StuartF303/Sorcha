// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

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

public sealed class ActionDetailsToolTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<IMcpAuthorizationService> _authServiceMock;
    private readonly Mock<IMcpErrorHandler> _errorHandlerMock;
    private readonly Mock<IServiceAvailabilityTracker> _availabilityTrackerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<ActionDetailsTool>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly ActionDetailsTool _tool;

    public ActionDetailsToolTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _authServiceMock = new Mock<IMcpAuthorizationService>();
        _errorHandlerMock = new Mock<IMcpErrorHandler>();
        _availabilityTrackerMock = new Mock<IServiceAvailabilityTracker>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<ActionDetailsTool>>();

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceClients:BlueprintService:Address"] = "http://localhost:5000"
            })
            .Build();

        _tool = new ActionDetailsTool(
            _sessionServiceMock.Object,
            _authServiceMock.Object,
            _errorHandlerMock.Object,
            _availabilityTrackerMock.Object,
            _httpClientFactoryMock.Object,
            _configuration,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetActionDetailsAsync_WhenUnauthorized_ReturnsUnauthorizedStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_action_details")).Returns(false);

        // Act
        var result = await _tool.GetActionDetailsAsync("action-123");

        // Assert
        result.Status.Should().Be("Unauthorized");
        result.Message.Should().Contain("sorcha:participant");
    }

    [Fact]
    public async Task GetActionDetailsAsync_WithEmptyActionId_ReturnsError()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_action_details")).Returns(true);

        // Act
        var result = await _tool.GetActionDetailsAsync("");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Action instance ID is required");
    }

    [Fact]
    public async Task GetActionDetailsAsync_WhenServiceUnavailable_ReturnsUnavailableStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_action_details")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(false);

        // Act
        var result = await _tool.GetActionDetailsAsync("action-123");

        // Assert
        result.Status.Should().Be("Unavailable");
        result.Message.Should().Contain("Blueprint service");
    }

    [Fact]
    public async Task GetActionDetailsAsync_WithSuccessfulResponse_ReturnsActionDetails()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_action_details")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        var response = new
        {
            actionInstanceId = "action-123",
            blueprintId = "bp-1",
            title = "Submit Application",
            description = "Submit your application details",
            workflowInstanceId = "wf-1",
            actionId = 1,
            status = "Pending",
            inputSchema = "{\"type\":\"object\"}",
            requiredFields = new[] { "name", "email" },
            assignedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        SetupHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        // Act
        var result = await _tool.GetActionDetailsAsync("action-123");

        // Assert
        result.Status.Should().Be("Success");
        result.Action.Should().NotBeNull();
        result.Action!.ActionInstanceId.Should().Be("action-123");
        result.Action.Title.Should().Be("Submit Application");
        result.Action.Status.Should().Be("Pending");
        result.Action.RequiredFields.Should().Contain("name");
    }

    [Fact]
    public async Task GetActionDetailsAsync_WithNotFound_ReturnsErrorStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_action_details")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        SetupHttpClient(HttpStatusCode.NotFound, "{\"error\":\"Action not found\"}");

        // Act
        var result = await _tool.GetActionDetailsAsync("action-invalid");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task GetActionDetailsAsync_WithTimeout_ReturnsTimeoutStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_action_details")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        SetupHttpClientWithException(new TaskCanceledException());

        // Act
        var result = await _tool.GetActionDetailsAsync("action-123");

        // Assert
        result.Status.Should().Be("Timeout");
        _availabilityTrackerMock.Verify(x => x.RecordFailure("Blueprint"), Times.Once);
    }

    [Fact]
    public async Task GetActionDetailsAsync_WithHttpException_ReturnsErrorStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_action_details")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        SetupHttpClientWithException(new HttpRequestException("Connection refused"));

        // Act
        var result = await _tool.GetActionDetailsAsync("action-123");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Connection refused");
    }

    [Fact]
    public async Task GetActionDetailsAsync_RecordsSuccessOnSuccessfulResponse()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_action_details")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        var response = new
        {
            actionInstanceId = "action-123",
            blueprintId = "bp-1",
            actionTitle = "Test",
            actionType = "DataCapture",
            workflowInstanceId = "wf-1",
            status = "Pending"
        };

        SetupHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        // Act
        await _tool.GetActionDetailsAsync("action-123");

        // Assert
        _availabilityTrackerMock.Verify(x => x.RecordSuccess("Blueprint"), Times.Once);
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
