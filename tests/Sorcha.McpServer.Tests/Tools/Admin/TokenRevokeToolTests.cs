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

public class TokenRevokeToolTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<IMcpAuthorizationService> _authServiceMock;
    private readonly Mock<IMcpErrorHandler> _errorHandlerMock;
    private readonly Mock<IServiceAvailabilityTracker> _availabilityTrackerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<TokenRevokeTool>> _loggerMock;

    public TokenRevokeToolTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _authServiceMock = new Mock<IMcpAuthorizationService>();
        _errorHandlerMock = new Mock<IMcpErrorHandler>();
        _availabilityTrackerMock = new Mock<IServiceAvailabilityTracker>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<TokenRevokeTool>>();

        _configurationMock.Setup(c => c["ServiceClients:TenantService:Address"])
            .Returns("http://localhost:5110");
    }

    private TokenRevokeTool CreateTool()
    {
        return new TokenRevokeTool(
            _sessionServiceMock.Object,
            _authServiceMock.Object,
            _errorHandlerMock.Object,
            _availabilityTrackerMock.Object,
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task RevokeTokensAsync_Unauthorized_ReturnsUnauthorizedResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_token_revoke")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.RevokeTokensAsync(userId: "user-123", reason: "Security incident");

        // Assert
        result.Status.Should().Be("Unauthorized");
        result.Message.Should().Contain("Access denied");
    }

    [Fact]
    public async Task RevokeTokensAsync_ServiceUnavailable_ReturnsUnavailableResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_token_revoke")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.RevokeTokensAsync(userId: "user-123", reason: "Security incident");

        // Assert
        result.Status.Should().Be("Unavailable");
        result.Message.Should().Contain("unavailable");
    }

    [Fact]
    public async Task RevokeTokensAsync_NoTargetProvided_ReturnsError()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_token_revoke")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.RevokeTokensAsync(reason: "Security incident");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Either userId or tenantId is required");
    }

    [Fact]
    public async Task RevokeTokensAsync_NoReasonProvided_ReturnsError()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_token_revoke")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.RevokeTokensAsync(userId: "user-123");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Reason for revocation is required");
    }

    [Fact]
    public async Task RevokeTokensAsync_ForUser_Success()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_token_revoke")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(true);

        var response = new
        {
            TokensRevoked = 3,
            UsersAffected = 1
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
        var result = await tool.RevokeTokensAsync(userId: "user-123", reason: "Security incident");

        // Assert
        result.Status.Should().Be("Success");
        result.TokensRevoked.Should().Be(3);
        result.UsersAffected.Should().Be(1);
        result.UserId.Should().Be("user-123");
        result.Reason.Should().Be("Security incident");
        result.Message.Should().Contain("3 token(s)");
        result.Message.Should().Contain("user user-123");
        _availabilityTrackerMock.Verify(a => a.RecordSuccess("Tenant"), Times.Once);
    }

    [Fact]
    public async Task RevokeTokensAsync_ForTenant_Success()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_token_revoke")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(true);

        var response = new
        {
            TokensRevoked = 50,
            UsersAffected = 15
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
        var result = await tool.RevokeTokensAsync(tenantId: "tenant-123", reason: "Organization security reset");

        // Assert
        result.Status.Should().Be("Success");
        result.TokensRevoked.Should().Be(50);
        result.UsersAffected.Should().Be(15);
        result.TenantId.Should().Be("tenant-123");
        result.Reason.Should().Be("Organization security reset");
        result.Message.Should().Contain("50 token(s)");
        result.Message.Should().Contain("tenant tenant-123");
    }

    [Fact]
    public async Task RevokeTokensAsync_SendsCorrectRequestBody()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_token_revoke")).Returns(true);
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
                Content = new StringContent(JsonSerializer.Serialize(new { TokensRevoked = 1, UsersAffected = 1 }))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        await tool.RevokeTokensAsync(userId: "user-123", tenantId: "tenant-456", reason: "Test reason");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);
        capturedRequest.RequestUri!.ToString().Should().Contain("/api/tokens/revoke");
        var content = await capturedRequest.Content!.ReadAsStringAsync();
        content.Should().Contain("user-123");
        content.Should().Contain("tenant-456");
        content.Should().Contain("Test reason");
    }

    [Fact]
    public async Task RevokeTokensAsync_HttpError_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_token_revoke")).Returns(true);
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
        var result = await tool.RevokeTokensAsync(userId: "nonexistent-user", reason: "Security incident");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("User not found");
    }

    [Fact]
    public async Task RevokeTokensAsync_Timeout_ReturnsTimeoutResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_token_revoke")).Returns(true);
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
        var result = await tool.RevokeTokensAsync(userId: "user-123", reason: "Security incident");

        // Assert
        result.Status.Should().Be("Timeout");
        _availabilityTrackerMock.Verify(a => a.RecordFailure("Tenant"), Times.Once);
    }

    [Fact]
    public async Task RevokeTokensAsync_ConnectionError_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_token_revoke")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(true);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.RevokeTokensAsync(userId: "user-123", reason: "Security incident");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Failed to connect");
        _availabilityTrackerMock.Verify(a => a.RecordFailure("Tenant", It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public async Task RevokeTokensAsync_ResponseTimeIsRecorded()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_token_revoke")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(true);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { TokensRevoked = 1, UsersAffected = 1 }))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.RevokeTokensAsync(userId: "user-123", reason: "Security incident");

        // Assert
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.CheckedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
