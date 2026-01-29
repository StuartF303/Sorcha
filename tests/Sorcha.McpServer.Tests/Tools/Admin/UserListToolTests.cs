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

public class UserListToolTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<IMcpAuthorizationService> _authServiceMock;
    private readonly Mock<IMcpErrorHandler> _errorHandlerMock;
    private readonly Mock<IServiceAvailabilityTracker> _availabilityTrackerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<UserListTool>> _loggerMock;

    public UserListToolTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _authServiceMock = new Mock<IMcpAuthorizationService>();
        _errorHandlerMock = new Mock<IMcpErrorHandler>();
        _availabilityTrackerMock = new Mock<IServiceAvailabilityTracker>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<UserListTool>>();

        _configurationMock.Setup(c => c["ServiceClients:TenantService:Address"])
            .Returns("http://localhost:5110");
    }

    private UserListTool CreateTool()
    {
        return new UserListTool(
            _sessionServiceMock.Object,
            _authServiceMock.Object,
            _errorHandlerMock.Object,
            _availabilityTrackerMock.Object,
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ListUsersAsync_Unauthorized_ReturnsUnauthorizedResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_list")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.ListUsersAsync();

        // Assert
        result.Status.Should().Be("Unauthorized");
        result.Message.Should().Contain("Access denied");
    }

    [Fact]
    public async Task ListUsersAsync_ServiceUnavailable_ReturnsUnavailableResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_list")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.ListUsersAsync();

        // Assert
        result.Status.Should().Be("Unavailable");
        result.Message.Should().Contain("unavailable");
    }

    [Fact]
    public async Task ListUsersAsync_InvalidRole_ReturnsError()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_list")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.ListUsersAsync(role: "InvalidRole");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Invalid role");
    }

    [Fact]
    public async Task ListUsersAsync_InvalidStatus_ReturnsError()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_list")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.ListUsersAsync(status: "InvalidStatus");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Invalid status");
    }

    [Fact]
    public async Task ListUsersAsync_Success_ReturnsUsers()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_list")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(true);

        var response = new
        {
            Items = new[]
            {
                new { UserId = "user-1", Email = "user1@test.com", DisplayName = "User One", OrganizationId = "tenant-1", OrganizationName = "Tenant One", Roles = new[] { "Admin" }, Status = "Active", LastLoginAt = DateTimeOffset.UtcNow.AddHours(-1), CreatedAt = DateTimeOffset.UtcNow.AddDays(-30) },
                new { UserId = "user-2", Email = "user2@test.com", DisplayName = "User Two", OrganizationId = "tenant-1", OrganizationName = "Tenant One", Roles = new[] { "Designer" }, Status = "Active", LastLoginAt = DateTimeOffset.UtcNow.AddDays(-1), CreatedAt = DateTimeOffset.UtcNow.AddDays(-60) }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 20,
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
        var result = await tool.ListUsersAsync();

        // Assert
        result.Status.Should().Be("Success");
        result.Users.Should().HaveCount(2);
        result.Users[0].UserId.Should().Be("user-1");
        result.Users[0].Email.Should().Be("user1@test.com");
        result.Users[0].Roles.Should().Contain("Admin");
        result.TotalCount.Should().Be(2);
        _availabilityTrackerMock.Verify(a => a.RecordSuccess("Tenant"), Times.Once);
    }

    [Fact]
    public async Task ListUsersAsync_WithFilters_IncludesQueryParameters()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_list")).Returns(true);
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
                Content = new StringContent(JsonSerializer.Serialize(new { Items = Array.Empty<object>(), TotalCount = 0, Page = 1, PageSize = 20, TotalPages = 0 }))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        await tool.ListUsersAsync(tenantId: "tenant-1", role: "Admin", status: "Active", search: "john", page: 2, pageSize: 50);

        // Assert
        capturedRequest.Should().NotBeNull();
        var url = capturedRequest!.RequestUri!.ToString();
        url.Should().Contain("organizationId=tenant-1");
        url.Should().Contain("role=Admin");
        url.Should().Contain("status=Active");
        url.Should().Contain("search=john");
        url.Should().Contain("page=2");
        url.Should().Contain("pageSize=50");
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Designer")]
    [InlineData("Participant")]
    public async Task ListUsersAsync_ValidRoles_Accepted(string role)
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_list")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(true);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { Items = Array.Empty<object>(), TotalCount = 0, Page = 1, PageSize = 20, TotalPages = 0 }))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.ListUsersAsync(role: role);

        // Assert
        result.Status.Should().Be("Success");
    }

    [Theory]
    [InlineData("Active")]
    [InlineData("Inactive")]
    [InlineData("Locked")]
    public async Task ListUsersAsync_ValidStatuses_Accepted(string status)
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_list")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(true);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { Items = Array.Empty<object>(), TotalCount = 0, Page = 1, PageSize = 20, TotalPages = 0 }))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.ListUsersAsync(status: status);

        // Assert
        result.Status.Should().Be("Success");
    }

    [Fact]
    public async Task ListUsersAsync_PageSizeExceeds100_ClampedTo100()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_list")).Returns(true);
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
                Content = new StringContent(JsonSerializer.Serialize(new { Items = Array.Empty<object>(), TotalCount = 0, Page = 1, PageSize = 100, TotalPages = 0 }))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        await tool.ListUsersAsync(pageSize: 500);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString().Should().Contain("pageSize=100");
    }

    [Fact]
    public async Task ListUsersAsync_HttpError_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_list")).Returns(true);
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
        var result = await tool.ListUsersAsync();

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("500");
    }

    [Fact]
    public async Task ListUsersAsync_Timeout_ReturnsTimeoutResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_list")).Returns(true);
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
        var result = await tool.ListUsersAsync();

        // Assert
        result.Status.Should().Be("Timeout");
        _availabilityTrackerMock.Verify(a => a.RecordFailure("Tenant"), Times.Once);
    }
}
