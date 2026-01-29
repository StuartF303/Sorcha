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

public class WorkflowInstancesToolTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<IMcpAuthorizationService> _authServiceMock;
    private readonly Mock<IMcpErrorHandler> _errorHandlerMock;
    private readonly Mock<IServiceAvailabilityTracker> _availabilityTrackerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<WorkflowInstancesTool>> _loggerMock;

    public WorkflowInstancesToolTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _authServiceMock = new Mock<IMcpAuthorizationService>();
        _errorHandlerMock = new Mock<IMcpErrorHandler>();
        _availabilityTrackerMock = new Mock<IServiceAvailabilityTracker>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<WorkflowInstancesTool>>();

        _configurationMock.Setup(c => c["ServiceClients:BlueprintService:Address"])
            .Returns("http://localhost:5000");
    }

    private WorkflowInstancesTool CreateTool()
    {
        return new WorkflowInstancesTool(
            _sessionServiceMock.Object,
            _authServiceMock.Object,
            _errorHandlerMock.Object,
            _availabilityTrackerMock.Object,
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ListWorkflowInstancesAsync_Unauthorized_ReturnsUnauthorizedResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_workflow_instances")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.ListWorkflowInstancesAsync();

        // Assert
        result.Status.Should().Be("Unauthorized");
        result.Message.Should().Contain("Access denied");
    }

    [Fact]
    public async Task ListWorkflowInstancesAsync_InvalidStatus_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_workflow_instances")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.ListWorkflowInstancesAsync(status: "Invalid");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Invalid status");
    }

    [Fact]
    public async Task ListWorkflowInstancesAsync_ServiceUnavailable_ReturnsUnavailableResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_workflow_instances")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.ListWorkflowInstancesAsync();

        // Assert
        result.Status.Should().Be("Unavailable");
        result.Message.Should().Contain("unavailable");
    }

    [Fact]
    public async Task ListWorkflowInstancesAsync_WithInstances_ReturnsSuccessResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_workflow_instances")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var listResponse = new
        {
            items = new[]
            {
                new
                {
                    instanceId = "wf-001",
                    blueprintId = "bp-123",
                    blueprintTitle = "Approval Workflow",
                    status = "Active",
                    currentActionId = 2,
                    currentActionTitle = "Review",
                    startedAt = DateTimeOffset.UtcNow.AddHours(-1),
                    completedAt = (DateTimeOffset?)null,
                    lastActivityAt = DateTimeOffset.UtcNow.AddMinutes(-5)
                },
                new
                {
                    instanceId = "wf-002",
                    blueprintId = "bp-123",
                    blueprintTitle = "Approval Workflow",
                    status = "Completed",
                    currentActionId = 3,
                    currentActionTitle = "Complete",
                    startedAt = DateTimeOffset.UtcNow.AddDays(-1),
                    completedAt = (DateTimeOffset?)DateTimeOffset.UtcNow.AddHours(-2),
                    lastActivityAt = DateTimeOffset.UtcNow.AddHours(-2)
                }
            },
            totalCount = 2,
            page = 1,
            pageSize = 20,
            totalPages = 1
        };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(listResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.ListWorkflowInstancesAsync();

        // Assert
        result.Status.Should().Be("Success");
        result.Message.Should().Contain("2 workflow instance(s)");
        result.Instances.Should().HaveCount(2);
        result.Instances[0].InstanceId.Should().Be("wf-001");
        result.Instances[0].BlueprintId.Should().Be("bp-123");
        result.Instances[0].Status.Should().Be("Active");
        result.Instances[0].CurrentActionId.Should().Be(2);
        result.Instances[1].Status.Should().Be("Completed");
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);

        _availabilityTrackerMock.Verify(a => a.RecordSuccess("Blueprint"), Times.Once);
    }

    [Fact]
    public async Task ListWorkflowInstancesAsync_NoInstances_ReturnsEmptyList()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_workflow_instances")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var listResponse = new
        {
            items = Array.Empty<object>(),
            totalCount = 0,
            page = 1,
            pageSize = 20,
            totalPages = 0
        };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(listResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.ListWorkflowInstancesAsync();

        // Assert
        result.Status.Should().Be("Success");
        result.Message.Should().Contain("0 workflow instance(s)");
        result.Instances.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task ListWorkflowInstancesAsync_WithBlueprintIdFilter_BuildsCorrectUrl()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_workflow_instances")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var listResponse = new { items = Array.Empty<object>(), totalCount = 0, page = 1, pageSize = 20, totalPages = 0 };

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
                Content = new StringContent(JsonSerializer.Serialize(listResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        await tool.ListWorkflowInstancesAsync(blueprintId: "bp-123");

        // Assert
        capturedUrl.Should().Contain("blueprintId=bp-123");
    }

    [Fact]
    public async Task ListWorkflowInstancesAsync_WithStatusFilter_BuildsCorrectUrl()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_workflow_instances")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var listResponse = new { items = Array.Empty<object>(), totalCount = 0, page = 1, pageSize = 20, totalPages = 0 };

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
                Content = new StringContent(JsonSerializer.Serialize(listResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        await tool.ListWorkflowInstancesAsync(status: "Active");

        // Assert
        capturedUrl.Should().Contain("status=Active");
    }

    [Fact]
    public async Task ListWorkflowInstancesAsync_WithPagination_BuildsCorrectUrl()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_workflow_instances")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var listResponse = new { items = Array.Empty<object>(), totalCount = 0, page = 2, pageSize = 50, totalPages = 0 };

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
                Content = new StringContent(JsonSerializer.Serialize(listResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        await tool.ListWorkflowInstancesAsync(page: 2, pageSize: 50);

        // Assert
        capturedUrl.Should().Contain("page=2");
        capturedUrl.Should().Contain("pageSize=50");
    }

    [Fact]
    public async Task ListWorkflowInstancesAsync_PageSizeExceedsMax_CapsAt100()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_workflow_instances")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var listResponse = new { items = Array.Empty<object>(), totalCount = 0, page = 1, pageSize = 100, totalPages = 0 };

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
                Content = new StringContent(JsonSerializer.Serialize(listResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        await tool.ListWorkflowInstancesAsync(pageSize: 500);

        // Assert - pageSize should be capped at 100
        capturedUrl.Should().Contain("pageSize=100");
    }

    [Fact]
    public async Task ListWorkflowInstancesAsync_ResponseTimeIsRecorded()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_workflow_instances")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var listResponse = new { items = Array.Empty<object>(), totalCount = 0, page = 1, pageSize = 20, totalPages = 0 };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(listResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.ListWorkflowInstancesAsync();

        // Assert
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.CheckedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
