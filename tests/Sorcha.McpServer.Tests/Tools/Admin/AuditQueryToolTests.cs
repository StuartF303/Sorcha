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

public class AuditQueryToolTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<IMcpAuthorizationService> _authServiceMock;
    private readonly Mock<IMcpErrorHandler> _errorHandlerMock;
    private readonly Mock<IServiceAvailabilityTracker> _availabilityTrackerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<AuditQueryTool>> _loggerMock;

    public AuditQueryToolTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _authServiceMock = new Mock<IMcpAuthorizationService>();
        _errorHandlerMock = new Mock<IMcpErrorHandler>();
        _availabilityTrackerMock = new Mock<IServiceAvailabilityTracker>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<AuditQueryTool>>();

        _configurationMock.Setup(c => c["ServiceClients:TenantService:Address"])
            .Returns("http://localhost:5110");
    }

    private AuditQueryTool CreateTool()
    {
        return new AuditQueryTool(
            _sessionServiceMock.Object,
            _authServiceMock.Object,
            _errorHandlerMock.Object,
            _availabilityTrackerMock.Object,
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task QueryAuditLogsAsync_Unauthorized_ReturnsUnauthorizedResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_audit_query")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.QueryAuditLogsAsync();

        // Assert
        result.Status.Should().Be("Unauthorized");
        result.Message.Should().Contain("Access denied");
    }

    [Fact]
    public async Task QueryAuditLogsAsync_ServiceUnavailable_ReturnsUnavailableResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_audit_query")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.QueryAuditLogsAsync();

        // Assert
        result.Status.Should().Be("Unavailable");
        result.Message.Should().Contain("unavailable");
    }

    [Fact]
    public async Task QueryAuditLogsAsync_InvalidEventType_ReturnsError()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_audit_query")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.QueryAuditLogsAsync(eventType: "InvalidType");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Invalid event type");
    }

    [Fact]
    public async Task QueryAuditLogsAsync_InvalidResourceType_ReturnsError()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_audit_query")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.QueryAuditLogsAsync(resourceType: "InvalidResource");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Invalid resource type");
    }

    [Fact]
    public async Task QueryAuditLogsAsync_Success_ReturnsAuditEntries()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_audit_query")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(true);

        var response = new
        {
            Items = new[]
            {
                new { AuditId = "audit-1", Timestamp = DateTimeOffset.UtcNow.AddHours(-1), OrganizationId = "tenant-1", UserId = "user-1", UserEmail = "user1@test.com", EventType = "Login", ResourceType = (string?)null, ResourceId = (string?)null, Action = "User logged in", IpAddress = "192.168.1.1", UserAgent = "Mozilla/5.0", Details = "{}" },
                new { AuditId = "audit-2", Timestamp = DateTimeOffset.UtcNow, OrganizationId = "tenant-1", UserId = "user-1", UserEmail = "user1@test.com", EventType = "Create", ResourceType = (string?)"Blueprint", ResourceId = (string?)"bp-123", Action = "Created blueprint", IpAddress = "192.168.1.1", UserAgent = "Mozilla/5.0", Details = "{\"blueprintName\":\"Test\"}" }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 50,
            TotalPages = 1
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
        var result = await tool.QueryAuditLogsAsync();

        // Assert
        result.Status.Should().Be("Success");
        result.Entries.Should().HaveCount(2);
        result.Entries[0].AuditId.Should().Be("audit-1");
        result.Entries[0].EventType.Should().Be("Login");
        result.Entries[1].ResourceType.Should().Be("Blueprint");
        result.TotalCount.Should().Be(2);
        _availabilityTrackerMock.Verify(a => a.RecordSuccess("Tenant"), Times.Once);
    }

    [Fact]
    public async Task QueryAuditLogsAsync_WithFilters_IncludesQueryParameters()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_audit_query")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(true);

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
                Content = new StringContent(JsonSerializer.Serialize(new { Items = Array.Empty<object>(), TotalCount = 0, Page = 1, PageSize = 50, TotalPages = 0 }))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        await tool.QueryAuditLogsAsync(tenantId: "tenant-1", userId: "user-1", eventType: "Login", resourceType: "User", startTime: "2024-01-01", endTime: "2024-12-31", page: 2, pageSize: 100);

        // Assert
        capturedRequest.Should().NotBeNull();
        var url = capturedRequest!.RequestUri!.ToString();
        url.Should().Contain("organizationId=tenant-1");
        url.Should().Contain("userId=user-1");
        url.Should().Contain("eventType=Login");
        url.Should().Contain("resourceType=User");
        url.Should().Contain("startTime=");
        url.Should().Contain("endTime=");
        url.Should().Contain("page=2");
        url.Should().Contain("pageSize=100");
    }

    [Theory]
    [InlineData("Login")]
    [InlineData("Logout")]
    [InlineData("Create")]
    [InlineData("Update")]
    [InlineData("Delete")]
    [InlineData("Access")]
    public async Task QueryAuditLogsAsync_ValidEventTypes_Accepted(string eventType)
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_audit_query")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(true);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { Items = Array.Empty<object>(), TotalCount = 0, Page = 1, PageSize = 50, TotalPages = 0 }))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.QueryAuditLogsAsync(eventType: eventType);

        // Assert
        result.Status.Should().Be("Success");
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Tenant")]
    [InlineData("Blueprint")]
    [InlineData("Workflow")]
    public async Task QueryAuditLogsAsync_ValidResourceTypes_Accepted(string resourceType)
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_audit_query")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(true);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { Items = Array.Empty<object>(), TotalCount = 0, Page = 1, PageSize = 50, TotalPages = 0 }))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.QueryAuditLogsAsync(resourceType: resourceType);

        // Assert
        result.Status.Should().Be("Success");
    }

    [Fact]
    public async Task QueryAuditLogsAsync_PageSizeExceeds200_ClampedTo200()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_audit_query")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(true);

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
                Content = new StringContent(JsonSerializer.Serialize(new { Items = Array.Empty<object>(), TotalCount = 0, Page = 1, PageSize = 200, TotalPages = 0 }))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        await tool.QueryAuditLogsAsync(pageSize: 500);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString().Should().Contain("pageSize=200");
    }

    [Fact]
    public async Task QueryAuditLogsAsync_HttpError_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_audit_query")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(true);

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
        var result = await tool.QueryAuditLogsAsync();

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("500");
    }

    [Fact]
    public async Task QueryAuditLogsAsync_Timeout_ReturnsTimeoutResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_audit_query")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(true);

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
        var result = await tool.QueryAuditLogsAsync();

        // Assert
        result.Status.Should().Be("Timeout");
        _availabilityTrackerMock.Verify(a => a.RecordFailure("Tenant"), Times.Once);
    }

    [Fact]
    public async Task QueryAuditLogsAsync_ResponseTimeIsRecorded()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_audit_query")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(true);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { Items = Array.Empty<object>(), TotalCount = 0, Page = 1, PageSize = 50, TotalPages = 0 }))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.QueryAuditLogsAsync();

        // Assert
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.CheckedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
