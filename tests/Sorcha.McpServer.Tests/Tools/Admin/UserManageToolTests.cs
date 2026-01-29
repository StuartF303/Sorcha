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

public class UserManageToolTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<IMcpAuthorizationService> _authServiceMock;
    private readonly Mock<IMcpErrorHandler> _errorHandlerMock;
    private readonly Mock<IServiceAvailabilityTracker> _availabilityTrackerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<UserManageTool>> _loggerMock;

    public UserManageToolTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _authServiceMock = new Mock<IMcpAuthorizationService>();
        _errorHandlerMock = new Mock<IMcpErrorHandler>();
        _availabilityTrackerMock = new Mock<IServiceAvailabilityTracker>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<UserManageTool>>();

        _configurationMock.Setup(c => c["ServiceClients:TenantService:Address"])
            .Returns("http://localhost:5110");
    }

    private UserManageTool CreateTool()
    {
        return new UserManageTool(
            _sessionServiceMock.Object,
            _authServiceMock.Object,
            _errorHandlerMock.Object,
            _availabilityTrackerMock.Object,
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ManageUserAsync_Unauthorized_ReturnsUnauthorizedResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_manage")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.ManageUserAsync("user-123", "Activate");

        // Assert
        result.Status.Should().Be("Unauthorized");
        result.Message.Should().Contain("Access denied");
    }

    [Fact]
    public async Task ManageUserAsync_ServiceUnavailable_ReturnsUnavailableResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_manage")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.ManageUserAsync("user-123", "Activate");

        // Assert
        result.Status.Should().Be("Unavailable");
        result.Message.Should().Contain("unavailable");
    }

    [Fact]
    public async Task ManageUserAsync_MissingUserId_ReturnsError()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_manage")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.ManageUserAsync("", "Activate");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("User ID is required");
    }

    [Fact]
    public async Task ManageUserAsync_MissingAction_ReturnsError()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_manage")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.ManageUserAsync("user-123", "");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Action is required");
    }

    [Fact]
    public async Task ManageUserAsync_InvalidAction_ReturnsError()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_manage")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.ManageUserAsync("user-123", "InvalidAction");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Invalid action");
    }

    [Fact]
    public async Task ManageUserAsync_AddRoleWithoutRole_ReturnsError()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_manage")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.ManageUserAsync("user-123", "AddRole");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Role is required for AddRole/RemoveRole");
    }

    [Fact]
    public async Task ManageUserAsync_AddRoleWithInvalidRole_ReturnsError()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_manage")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.ManageUserAsync("user-123", "AddRole", role: "InvalidRole");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Invalid role");
    }

    [Theory]
    [InlineData("Activate", "activated")]
    [InlineData("Deactivate", "deactivated")]
    [InlineData("Lock", "locked")]
    [InlineData("Unlock", "unlocked")]
    public async Task ManageUserAsync_StatusActions_Success(string action, string expectedDescription)
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_manage")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(true);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.ManageUserAsync("user-123", action);

        // Assert
        result.Status.Should().Be("Success");
        result.UserId.Should().Be("user-123");
        result.ActionPerformed.Should().Be(action);
        result.Message.Should().Contain(expectedDescription);
        _availabilityTrackerMock.Verify(a => a.RecordSuccess("Tenant"), Times.Once);
    }

    [Theory]
    [InlineData("AddRole", "Admin", "granted Admin role")]
    [InlineData("AddRole", "Designer", "granted Designer role")]
    [InlineData("AddRole", "Participant", "granted Participant role")]
    [InlineData("RemoveRole", "Admin", "removed Admin role")]
    public async Task ManageUserAsync_RoleActions_Success(string action, string role, string expectedDescription)
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_manage")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(true);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.ManageUserAsync("user-123", action, role: role);

        // Assert
        result.Status.Should().Be("Success");
        result.UserId.Should().Be("user-123");
        result.ActionPerformed.Should().Be(action);
        result.RoleAffected.Should().Be(role);
        result.Message.Should().Contain(expectedDescription);
    }

    [Fact]
    public async Task ManageUserAsync_SendsCorrectRequestBody()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_manage")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(true);

        HttpRequestMessage? capturedRequest = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        await tool.ManageUserAsync("user-123", "AddRole", role: "Admin");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);
        capturedRequest.RequestUri!.ToString().Should().Contain("/api/users/user-123/actions");
        var content = await capturedRequest.Content!.ReadAsStringAsync();
        content.Should().Contain("AddRole");
        content.Should().Contain("Admin");
    }

    [Fact]
    public async Task ManageUserAsync_HttpError_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_manage")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(true);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { Error = "User not found" }))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.ManageUserAsync("nonexistent-user", "Activate");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("User not found");
    }

    [Fact]
    public async Task ManageUserAsync_Timeout_ReturnsTimeoutResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_manage")).Returns(true);
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
        var result = await tool.ManageUserAsync("user-123", "Activate");

        // Assert
        result.Status.Should().Be("Timeout");
        _availabilityTrackerMock.Verify(a => a.RecordFailure("Tenant"), Times.Once);
    }

    [Fact]
    public async Task ManageUserAsync_ResponseTimeIsRecorded()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_user_manage")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(true);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.ManageUserAsync("user-123", "Activate");

        // Assert
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.CheckedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
