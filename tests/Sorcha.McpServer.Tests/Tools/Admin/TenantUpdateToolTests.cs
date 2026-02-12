// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Sorcha.McpServer.Infrastructure;
using Sorcha.McpServer.Services;
using Sorcha.McpServer.Tools.Admin;

namespace Sorcha.McpServer.Tests.Tools.Admin;

public class TenantUpdateToolTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<IMcpAuthorizationService> _authServiceMock;
    private readonly Mock<IMcpErrorHandler> _errorHandlerMock;
    private readonly Mock<IServiceAvailabilityTracker> _availabilityTrackerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<TenantUpdateTool>> _loggerMock;

    public TenantUpdateToolTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _authServiceMock = new Mock<IMcpAuthorizationService>();
        _errorHandlerMock = new Mock<IMcpErrorHandler>();
        _availabilityTrackerMock = new Mock<IServiceAvailabilityTracker>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<TenantUpdateTool>>();

        _configurationMock.Setup(c => c["ServiceClients:TenantService:Address"])
            .Returns("http://localhost:5110");
    }

    private TenantUpdateTool CreateTool()
    {
        return new TenantUpdateTool(
            _sessionServiceMock.Object,
            _authServiceMock.Object,
            _errorHandlerMock.Object,
            _availabilityTrackerMock.Object,
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task UpdateTenantAsync_Unauthorized_ReturnsUnauthorizedResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_tenant_update")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.UpdateTenantAsync("tenant-123", name: "New Name");

        // Assert
        result.Status.Should().Be("Unauthorized");
        result.Message.Should().Contain("Access denied");
    }

    [Fact]
    public async Task UpdateTenantAsync_ServiceUnavailable_ReturnsUnavailableResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_tenant_update")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.UpdateTenantAsync("tenant-123", name: "New Name");

        // Assert
        result.Status.Should().Be("Unavailable");
        result.Message.Should().Contain("unavailable");
    }

    [Fact]
    public async Task UpdateTenantAsync_MissingTenantId_ReturnsError()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_tenant_update")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.UpdateTenantAsync("", name: "New Name");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Tenant ID is required");
    }

    [Fact]
    public async Task UpdateTenantAsync_InvalidStatus_ReturnsError()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_tenant_update")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.UpdateTenantAsync("tenant-123", status: "InvalidStatus");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Invalid status");
    }

    [Fact]
    public async Task UpdateTenantAsync_NoUpdateFields_ReturnsError()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_tenant_update")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.UpdateTenantAsync("tenant-123");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("At least one update field");
    }

    [Fact]
    public async Task UpdateTenantAsync_UpdateName_Success()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_tenant_update")).Returns(true);
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
        var result = await tool.UpdateTenantAsync("tenant-123", name: "New Name");

        // Assert
        result.Status.Should().Be("Success");
        result.TenantId.Should().Be("tenant-123");
        result.UpdatedName.Should().Be("New Name");
        result.UpdatedStatus.Should().BeNull();
        result.Message.Should().Contain("name to 'New Name'");
        _availabilityTrackerMock.Verify(a => a.RecordSuccess("Tenant"), Times.Once);
    }

    [Fact]
    public async Task UpdateTenantAsync_UpdateStatus_Success()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_tenant_update")).Returns(true);
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
        var result = await tool.UpdateTenantAsync("tenant-123", status: "Suspended");

        // Assert
        result.Status.Should().Be("Success");
        result.TenantId.Should().Be("tenant-123");
        result.UpdatedStatus.Should().Be("Suspended");
        result.UpdatedName.Should().BeNull();
        result.Message.Should().Contain("status to 'Suspended'");
    }

    [Fact]
    public async Task UpdateTenantAsync_UpdateBothNameAndStatus_Success()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_tenant_update")).Returns(true);
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
        var result = await tool.UpdateTenantAsync("tenant-123", name: "New Name", status: "Active");

        // Assert
        result.Status.Should().Be("Success");
        result.UpdatedName.Should().Be("New Name");
        result.UpdatedStatus.Should().Be("Active");
        result.Message.Should().Contain("name to 'New Name'");
        result.Message.Should().Contain("status to 'Active'");
    }

    [Theory]
    [InlineData("Active")]
    [InlineData("Suspended")]
    public async Task UpdateTenantAsync_ValidStatuses_Accepted(string status)
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_tenant_update")).Returns(true);
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
        var result = await tool.UpdateTenantAsync("tenant-123", status: status);

        // Assert
        result.Status.Should().Be("Success");
    }

    [Fact]
    public async Task UpdateTenantAsync_HttpError_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_tenant_update")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Tenant")).Returns(true);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { Error = "Tenant not found" }))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.UpdateTenantAsync("nonexistent-tenant", name: "New Name");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Tenant not found");
    }

    [Fact]
    public async Task UpdateTenantAsync_Timeout_ReturnsTimeoutResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_tenant_update")).Returns(true);
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
        var result = await tool.UpdateTenantAsync("tenant-123", name: "New Name");

        // Assert
        result.Status.Should().Be("Timeout");
        _availabilityTrackerMock.Verify(a => a.RecordFailure("Tenant"), Times.Once);
    }

    [Fact]
    public async Task UpdateTenantAsync_UsesPatchMethod()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_tenant_update")).Returns(true);
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
        await tool.UpdateTenantAsync("tenant-123", name: "New Name");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Patch);
        capturedRequest.RequestUri!.ToString().Should().Contain("/api/organizations/tenant-123");
    }
}
