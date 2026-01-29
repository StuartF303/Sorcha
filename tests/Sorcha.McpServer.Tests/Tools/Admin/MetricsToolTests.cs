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

public class MetricsToolTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<IMcpAuthorizationService> _authServiceMock;
    private readonly Mock<IMcpErrorHandler> _errorHandlerMock;
    private readonly Mock<IServiceAvailabilityTracker> _availabilityTrackerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<MetricsTool>> _loggerMock;

    public MetricsToolTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _authServiceMock = new Mock<IMcpAuthorizationService>();
        _errorHandlerMock = new Mock<IMcpErrorHandler>();
        _availabilityTrackerMock = new Mock<IServiceAvailabilityTracker>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<MetricsTool>>();

        _configurationMock.Setup(c => c["ServiceClients:ApiGateway:Address"])
            .Returns("http://localhost:80");
    }

    private MetricsTool CreateTool()
    {
        return new MetricsTool(
            _sessionServiceMock.Object,
            _authServiceMock.Object,
            _errorHandlerMock.Object,
            _availabilityTrackerMock.Object,
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetMetricsAsync_Unauthorized_ReturnsUnauthorizedResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_metrics")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.GetMetricsAsync();

        // Assert
        result.Status.Should().Be("Unauthorized");
        result.Message.Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetMetricsAsync_ServiceUnavailable_ReturnsUnavailableResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_metrics")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("ApiGateway")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.GetMetricsAsync();

        // Assert
        result.Status.Should().Be("Unavailable");
        result.Message.Should().Contain("unavailable");
    }

    [Fact]
    public async Task GetMetricsAsync_InvalidMetricType_ReturnsError()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_metrics")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.GetMetricsAsync(metricType: "InvalidType");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Invalid metric type");
    }

    [Fact]
    public async Task GetMetricsAsync_Success_ReturnsServiceMetrics()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_metrics")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("ApiGateway")).Returns(true);

        var response = new
        {
            Services = new[]
            {
                new { ServiceName = "Blueprint", RequestsPerSecond = 100.0, AverageLatencyMs = 25.0, P95LatencyMs = 50.0, P99LatencyMs = 100.0, ErrorRate = 0.01, ActiveConnections = 10, MemoryUsageMb = 256.0, CpuUsagePercent = 15.0 },
                new { ServiceName = "Register", RequestsPerSecond = 200.0, AverageLatencyMs = 15.0, P95LatencyMs = 30.0, P99LatencyMs = 60.0, ErrorRate = 0.005, ActiveConnections = 20, MemoryUsageMb = 512.0, CpuUsagePercent = 25.0 }
            },
            System = new { TotalRequestsPerSecond = 300.0, TotalActiveConnections = 30, OverallErrorRate = 0.0075, UptimeHours = 72.5 }
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
        var result = await tool.GetMetricsAsync();

        // Assert
        result.Status.Should().Be("Success");
        result.Services.Should().HaveCount(2);
        result.Services[0].ServiceName.Should().Be("Blueprint");
        result.Services[0].RequestsPerSecond.Should().Be(100.0);
        result.SystemMetrics.Should().NotBeNull();
        result.SystemMetrics!.TotalRequestsPerSecond.Should().Be(300.0);
        result.SystemMetrics.UptimeHours.Should().Be(72.5);
        _availabilityTrackerMock.Verify(a => a.RecordSuccess("ApiGateway"), Times.Once);
    }

    [Fact]
    public async Task GetMetricsAsync_WithServiceFilter_IncludesQueryParameter()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_metrics")).Returns(true);
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
                Content = new StringContent(JsonSerializer.Serialize(new { Services = Array.Empty<object>() }))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        await tool.GetMetricsAsync(service: "Blueprint", metricType: "Performance");

        // Assert
        capturedRequest.Should().NotBeNull();
        var url = capturedRequest!.RequestUri!.ToString();
        url.Should().Contain("service=Blueprint");
        url.Should().Contain("type=Performance");
    }

    [Theory]
    [InlineData("All")]
    [InlineData("Performance")]
    [InlineData("Throughput")]
    [InlineData("Errors")]
    public async Task GetMetricsAsync_ValidMetricTypes_Accepted(string metricType)
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_metrics")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("ApiGateway")).Returns(true);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { Services = Array.Empty<object>() }))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.GetMetricsAsync(metricType: metricType);

        // Assert
        result.Status.Should().Be("Success");
    }

    [Fact]
    public async Task GetMetricsAsync_HttpError_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_metrics")).Returns(true);
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
        var result = await tool.GetMetricsAsync();

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("500");
    }

    [Fact]
    public async Task GetMetricsAsync_Timeout_ReturnsTimeoutResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_metrics")).Returns(true);
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
        var result = await tool.GetMetricsAsync();

        // Assert
        result.Status.Should().Be("Timeout");
        _availabilityTrackerMock.Verify(a => a.RecordFailure("ApiGateway"), Times.Once);
    }

    [Fact]
    public async Task GetMetricsAsync_ResponseTimeIsRecorded()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_metrics")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("ApiGateway")).Returns(true);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { Services = Array.Empty<object>() }))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.GetMetricsAsync();

        // Assert
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.CheckedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
