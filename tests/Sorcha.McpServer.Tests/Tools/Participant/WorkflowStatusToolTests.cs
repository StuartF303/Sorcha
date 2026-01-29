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

public sealed class WorkflowStatusToolTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<IMcpAuthorizationService> _authServiceMock;
    private readonly Mock<IMcpErrorHandler> _errorHandlerMock;
    private readonly Mock<IServiceAvailabilityTracker> _availabilityTrackerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<WorkflowStatusTool>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly WorkflowStatusTool _tool;

    public WorkflowStatusToolTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _authServiceMock = new Mock<IMcpAuthorizationService>();
        _errorHandlerMock = new Mock<IMcpErrorHandler>();
        _availabilityTrackerMock = new Mock<IServiceAvailabilityTracker>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<WorkflowStatusTool>>();

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceClients:BlueprintService:Address"] = "http://localhost:5000"
            })
            .Build();

        _tool = new WorkflowStatusTool(
            _sessionServiceMock.Object,
            _authServiceMock.Object,
            _errorHandlerMock.Object,
            _availabilityTrackerMock.Object,
            _httpClientFactoryMock.Object,
            _configuration,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetWorkflowStatusAsync_WhenUnauthorized_ReturnsUnauthorizedStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_workflow_status")).Returns(false);

        // Act
        var result = await _tool.GetWorkflowStatusAsync("wf-123");

        // Assert
        result.Status.Should().Be("Unauthorized");
        result.Message.Should().Contain("sorcha:participant");
    }

    [Fact]
    public async Task GetWorkflowStatusAsync_WithEmptyWorkflowId_ReturnsError()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_workflow_status")).Returns(true);

        // Act
        var result = await _tool.GetWorkflowStatusAsync("");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Workflow instance ID is required");
    }

    [Fact]
    public async Task GetWorkflowStatusAsync_WhenServiceUnavailable_ReturnsUnavailableStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_workflow_status")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(false);

        // Act
        var result = await _tool.GetWorkflowStatusAsync("wf-123");

        // Assert
        result.Status.Should().Be("Unavailable");
        result.Message.Should().Contain("Blueprint service");
    }

    [Fact]
    public async Task GetWorkflowStatusAsync_WithActiveWorkflow_ReturnsWorkflowDetails()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_workflow_status")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        var response = new
        {
            workflowInstanceId = "wf-123",
            blueprintId = "bp-1",
            blueprintTitle = "Document Review Process",
            status = "Active",
            currentActionId = 2,
            currentActionTitle = "Manager Approval",
            completedActions = 1,
            totalActions = 4,
            startedAt = DateTimeOffset.UtcNow.AddDays(-1),
            lastActivityAt = DateTimeOffset.UtcNow.AddHours(-2)
        };

        SetupHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        // Act
        var result = await _tool.GetWorkflowStatusAsync("wf-123");

        // Assert
        result.Status.Should().Be("Success");
        result.Workflow.Should().NotBeNull();
        result.Workflow!.WorkflowInstanceId.Should().Be("wf-123");
        result.Workflow.CurrentStatus.Should().Be("Active");
        result.Workflow.CurrentActionId.Should().Be(2);
        result.Workflow.CompletedActions.Should().Be(1);
        result.Workflow.TotalActions.Should().Be(4);
        result.Workflow.Progress.Should().Be(25); // 1/4 = 25%
    }

    [Fact]
    public async Task GetWorkflowStatusAsync_WithCompletedWorkflow_ReturnsCompletedDetails()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_workflow_status")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        var response = new
        {
            workflowInstanceId = "wf-456",
            blueprintId = "bp-2",
            blueprintTitle = "Simple Workflow",
            status = "Completed",
            completedActions = 3,
            totalActions = 3,
            startedAt = DateTimeOffset.UtcNow.AddDays(-2),
            completedAt = DateTimeOffset.UtcNow.AddDays(-1),
            lastActivityAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        SetupHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        // Act
        var result = await _tool.GetWorkflowStatusAsync("wf-456");

        // Assert
        result.Status.Should().Be("Success");
        result.Workflow!.CurrentStatus.Should().Be("Completed");
        result.Workflow.Progress.Should().Be(100);
        result.Workflow.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWorkflowStatusAsync_WithNotFound_ReturnsErrorStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_workflow_status")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        SetupHttpClient(HttpStatusCode.NotFound, "{\"error\":\"Workflow not found\"}");

        // Act
        var result = await _tool.GetWorkflowStatusAsync("wf-invalid");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task GetWorkflowStatusAsync_WithTimeout_ReturnsTimeoutStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_workflow_status")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        SetupHttpClientWithException(new TaskCanceledException());

        // Act
        var result = await _tool.GetWorkflowStatusAsync("wf-123");

        // Assert
        result.Status.Should().Be("Timeout");
        _availabilityTrackerMock.Verify(x => x.RecordFailure("Blueprint"), Times.Once);
    }

    [Fact]
    public async Task GetWorkflowStatusAsync_WithHttpException_ReturnsErrorStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_workflow_status")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        SetupHttpClientWithException(new HttpRequestException("Connection refused"));

        // Act
        var result = await _tool.GetWorkflowStatusAsync("wf-123");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Connection refused");
    }

    [Fact]
    public async Task GetWorkflowStatusAsync_RecordsSuccessOnSuccessfulResponse()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_workflow_status")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        var response = new
        {
            workflowInstanceId = "wf-123",
            blueprintId = "bp-1",
            status = "Active",
            completedActions = 0,
            totalActions = 1
        };

        SetupHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        // Act
        await _tool.GetWorkflowStatusAsync("wf-123");

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
