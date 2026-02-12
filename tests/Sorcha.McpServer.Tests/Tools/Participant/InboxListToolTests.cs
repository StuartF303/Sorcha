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

public sealed class InboxListToolTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<IMcpAuthorizationService> _authServiceMock;
    private readonly Mock<IMcpErrorHandler> _errorHandlerMock;
    private readonly Mock<IServiceAvailabilityTracker> _availabilityTrackerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<InboxListTool>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly InboxListTool _tool;

    public InboxListToolTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _authServiceMock = new Mock<IMcpAuthorizationService>();
        _errorHandlerMock = new Mock<IMcpErrorHandler>();
        _availabilityTrackerMock = new Mock<IServiceAvailabilityTracker>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<InboxListTool>>();

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceClients:BlueprintService:Address"] = "http://localhost:5000"
            })
            .Build();

        _tool = new InboxListTool(
            _sessionServiceMock.Object,
            _authServiceMock.Object,
            _errorHandlerMock.Object,
            _availabilityTrackerMock.Object,
            _httpClientFactoryMock.Object,
            _configuration,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ListInboxAsync_WhenUnauthorized_ReturnsUnauthorizedStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_inbox_list")).Returns(false);

        // Act
        var result = await _tool.ListInboxAsync();

        // Assert
        result.Status.Should().Be("Unauthorized");
        result.Message.Should().Contain("sorcha:participant");
    }

    [Fact]
    public async Task ListInboxAsync_WhenServiceUnavailable_ReturnsUnavailableStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_inbox_list")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(false);

        // Act
        var result = await _tool.ListInboxAsync();

        // Assert
        result.Status.Should().Be("Unavailable");
        result.Message.Should().Contain("Blueprint service");
    }

    [Fact]
    public async Task ListInboxAsync_WithSuccessfulResponse_ReturnsActions()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_inbox_list")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        var response = new
        {
            items = new[]
            {
                new
                {
                    actionInstanceId = "action-1",
                    blueprintId = "bp-1",
                    actionTitle = "Review Document",
                    workflowInstanceId = "wf-1",
                    status = "Pending",
                    createdAt = DateTimeOffset.UtcNow.AddHours(-1)
                },
                new
                {
                    actionInstanceId = "action-2",
                    blueprintId = "bp-2",
                    actionTitle = "Approve Request",
                    workflowInstanceId = "wf-2",
                    status = "Pending",
                    createdAt = DateTimeOffset.UtcNow.AddMinutes(-30)
                }
            },
            totalCount = 2
        };

        SetupHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        // Act
        var result = await _tool.ListInboxAsync();

        // Assert
        result.Status.Should().Be("Success");
        result.Items.Should().HaveCount(2);
        result.Items[0].ActionInstanceId.Should().Be("action-1");
        result.Items[0].ActionTitle.Should().Be("Review Document");
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListInboxAsync_WithEmptyInbox_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_inbox_list")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        var response = new { items = Array.Empty<object>(), totalCount = 0 };
        SetupHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        // Act
        var result = await _tool.ListInboxAsync();

        // Assert
        result.Status.Should().Be("Success");
        result.Items.Should().BeEmpty();
        result.Message.Should().Contain("0 inbox item");
    }

    [Fact]
    public async Task ListInboxAsync_WithPagination_PassesCorrectParameters()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_inbox_list")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        var response = new { items = Array.Empty<object>(), totalCount = 0, page = 2, pageSize = 10, totalPages = 1 };
        var handlerMock = SetupHttpClientWithCapture(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        // Act
        var result = await _tool.ListInboxAsync(page: 2, pageSize: 10);

        // Assert
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.ToString().Contains("page=2") &&
                req.RequestUri.ToString().Contains("pageSize=10")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ListInboxAsync_WithHttpError_ReturnsErrorStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_inbox_list")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        SetupHttpClient(HttpStatusCode.InternalServerError, "{\"error\":\"Server error\"}");

        // Act
        var result = await _tool.ListInboxAsync();

        // Assert
        result.Status.Should().Be("Error");
    }

    [Fact]
    public async Task ListInboxAsync_WithTimeout_ReturnsTimeoutStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_inbox_list")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        SetupHttpClientWithException(new TaskCanceledException());

        // Act
        var result = await _tool.ListInboxAsync();

        // Assert
        result.Status.Should().Be("Timeout");
        _availabilityTrackerMock.Verify(x => x.RecordFailure("Blueprint"), Times.Once);
    }

    [Fact]
    public async Task ListInboxAsync_WithHttpException_ReturnsErrorStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_inbox_list")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Blueprint")).Returns(true);

        SetupHttpClientWithException(new HttpRequestException("Connection failed"));

        // Act
        var result = await _tool.ListInboxAsync();

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Connection failed");
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
