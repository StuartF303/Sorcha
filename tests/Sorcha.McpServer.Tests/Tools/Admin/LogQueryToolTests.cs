// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Sorcha.McpServer.Infrastructure;
using Sorcha.McpServer.Services;
using Sorcha.McpServer.Tools.Admin;

namespace Sorcha.McpServer.Tests.Tools.Admin;

public class LogQueryToolTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<IMcpAuthorizationService> _authServiceMock;
    private readonly Mock<IMcpErrorHandler> _errorHandlerMock;
    private readonly Mock<IServiceAvailabilityTracker> _availabilityTrackerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<LogQueryTool>> _loggerMock;

    public LogQueryToolTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _authServiceMock = new Mock<IMcpAuthorizationService>();
        _errorHandlerMock = new Mock<IMcpErrorHandler>();
        _availabilityTrackerMock = new Mock<IServiceAvailabilityTracker>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<LogQueryTool>>();

        _configurationMock.Setup(c => c["ServiceClients:ApiGateway:Address"])
            .Returns("http://localhost:80");
    }

    private LogQueryTool CreateTool()
    {
        return new LogQueryTool(
            _sessionServiceMock.Object,
            _authServiceMock.Object,
            _errorHandlerMock.Object,
            _availabilityTrackerMock.Object,
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task QueryLogsAsync_Unauthorized_ReturnsUnauthorizedResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_log_query")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.QueryLogsAsync();

        // Assert
        result.Status.Should().Be("Unauthorized");
        result.Message.Should().Contain("Access denied");
    }

    [Fact]
    public async Task QueryLogsAsync_ServiceUnavailable_ReturnsUnavailableResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_log_query")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("ApiGateway")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.QueryLogsAsync();

        // Assert
        result.Status.Should().Be("Unavailable");
        result.Message.Should().Contain("unavailable");
    }

    [Fact]
    public async Task QueryLogsAsync_InvalidLogLevel_ReturnsError()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_log_query")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.QueryLogsAsync(level: "InvalidLevel");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Invalid log level");
    }

    [Fact]
    public async Task QueryLogsAsync_Success_ReturnsLogEntries()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_log_query")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("ApiGateway")).Returns(true);

        var response = new
        {
            Entries = new[]
            {
                new { Timestamp = DateTimeOffset.UtcNow, Service = "Blueprint", Level = "Info", Message = "Test log 1", Exception = (string?)null, CorrelationId = "corr-1" },
                new { Timestamp = DateTimeOffset.UtcNow, Service = "Register", Level = "Error", Message = "Test log 2", Exception = "Stack trace", CorrelationId = "corr-2" }
            },
            TotalCount = 2
        };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(response))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.QueryLogsAsync();

        // Assert
        result.Status.Should().Be("Success");
        result.Entries.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        _availabilityTrackerMock.Verify(a => a.RecordSuccess("ApiGateway"), Times.Once);
    }

    [Fact]
    public async Task QueryLogsAsync_WithFilters_IncludesQueryParameters()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_log_query")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("ApiGateway")).Returns(true);

        HttpRequestMessage? capturedRequest = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { Entries = Array.Empty<object>(), TotalCount = 0 }))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        await tool.QueryLogsAsync(service: "Blueprint", level: "Error", search: "test", limit: 50);

        // Assert
        capturedRequest.Should().NotBeNull();
        var url = capturedRequest!.RequestUri!.ToString();
        url.Should().Contain("service=Blueprint");
        url.Should().Contain("level=Error");
        url.Should().Contain("search=test");
        url.Should().Contain("limit=50");
    }

    [Fact]
    public async Task QueryLogsAsync_LimitExceeds1000_ClampedTo1000()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_log_query")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("ApiGateway")).Returns(true);

        HttpRequestMessage? capturedRequest = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { Entries = Array.Empty<object>(), TotalCount = 0 }))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        await tool.QueryLogsAsync(limit: 5000);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString().Should().Contain("limit=1000");
    }

    [Fact]
    public async Task QueryLogsAsync_HttpError_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_log_query")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("ApiGateway")).Returns(true);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.QueryLogsAsync();

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("500");
    }

    [Fact]
    public async Task QueryLogsAsync_Timeout_ReturnsTimeoutResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_log_query")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("ApiGateway")).Returns(true);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.QueryLogsAsync();

        // Assert
        result.Status.Should().Be("Timeout");
        _availabilityTrackerMock.Verify(a => a.RecordFailure("ApiGateway"), Times.Once);
    }

    [Fact]
    public async Task QueryLogsAsync_ResponseTimeIsRecorded()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_log_query")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("ApiGateway")).Returns(true);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { Entries = Array.Empty<object>(), TotalCount = 0 }))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.QueryLogsAsync();

        // Assert
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.CheckedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
