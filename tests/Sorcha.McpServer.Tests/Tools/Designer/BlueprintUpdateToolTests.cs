// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Sorcha.McpServer.Infrastructure;
using Sorcha.McpServer.Services;
using Sorcha.McpServer.Tools.Designer;

namespace Sorcha.McpServer.Tests.Tools.Designer;

public class BlueprintUpdateToolTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<IMcpAuthorizationService> _authServiceMock;
    private readonly Mock<IMcpErrorHandler> _errorHandlerMock;
    private readonly Mock<IServiceAvailabilityTracker> _availabilityTrackerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<BlueprintUpdateTool>> _loggerMock;

    public BlueprintUpdateToolTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _authServiceMock = new Mock<IMcpAuthorizationService>();
        _errorHandlerMock = new Mock<IMcpErrorHandler>();
        _availabilityTrackerMock = new Mock<IServiceAvailabilityTracker>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<BlueprintUpdateTool>>();

        _configurationMock.Setup(c => c["ServiceClients:BlueprintService:Address"])
            .Returns("http://localhost:5000");
    }

    private BlueprintUpdateTool CreateTool()
    {
        return new BlueprintUpdateTool(
            _sessionServiceMock.Object,
            _authServiceMock.Object,
            _errorHandlerMock.Object,
            _availabilityTrackerMock.Object,
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task UpdateBlueprintAsync_Unauthorized_ReturnsUnauthorizedResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_update")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.UpdateBlueprintAsync("bp-123", "{}");

        // Assert
        result.Status.Should().Be("Unauthorized");
        result.Message.Should().Contain("Access denied");
    }

    [Fact]
    public async Task UpdateBlueprintAsync_EmptyBlueprintId_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_update")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.UpdateBlueprintAsync("", "{}");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Blueprint ID");
    }

    [Fact]
    public async Task UpdateBlueprintAsync_EmptyJson_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_update")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.UpdateBlueprintAsync("bp-123", "");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Blueprint JSON");
    }

    [Fact]
    public async Task UpdateBlueprintAsync_InvalidJson_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_update")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.UpdateBlueprintAsync("bp-123", "{ invalid }");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Invalid JSON");
    }

    [Fact]
    public async Task UpdateBlueprintAsync_MissingTitle_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_update")).Returns(true);
        var tool = CreateTool();

        var blueprintJson = JsonSerializer.Serialize(new
        {
            participants = new[] { new { id = "p1" }, new { id = "p2" } },
            actions = new[] { new { id = 0 } }
        });

        // Act
        var result = await tool.UpdateBlueprintAsync("bp-123", blueprintJson);

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("title");
    }

    [Fact]
    public async Task UpdateBlueprintAsync_InsufficientParticipants_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_update")).Returns(true);
        var tool = CreateTool();

        var blueprintJson = JsonSerializer.Serialize(new
        {
            title = "Test",
            participants = new[] { new { id = "p1" } },
            actions = new[] { new { id = 0 } }
        });

        // Act
        var result = await tool.UpdateBlueprintAsync("bp-123", blueprintJson);

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("2 participants");
    }

    [Fact]
    public async Task UpdateBlueprintAsync_NoActions_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_update")).Returns(true);
        var tool = CreateTool();

        var blueprintJson = JsonSerializer.Serialize(new
        {
            title = "Test",
            participants = new[] { new { id = "p1" }, new { id = "p2" } },
            actions = Array.Empty<object>()
        });

        // Act
        var result = await tool.UpdateBlueprintAsync("bp-123", blueprintJson);

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("1 action");
    }

    [Fact]
    public async Task UpdateBlueprintAsync_ServiceUnavailable_ReturnsUnavailableResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_update")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(false);
        var tool = CreateTool();

        var blueprintJson = JsonSerializer.Serialize(new
        {
            title = "Test",
            participants = new[] { new { id = "p1" }, new { id = "p2" } },
            actions = new[] { new { id = 0 } }
        });

        // Act
        var result = await tool.UpdateBlueprintAsync("bp-123", blueprintJson);

        // Assert
        result.Status.Should().Be("Unavailable");
        result.Message.Should().Contain("unavailable");
    }

    [Fact]
    public async Task UpdateBlueprintAsync_SuccessfulUpdate_ReturnsSuccessResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_update")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var updateResponse = new
        {
            id = "bp-123",
            title = "Updated Blueprint",
            version = 2,
            status = "Draft",
            modifiedAt = DateTimeOffset.UtcNow
        };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(updateResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        var blueprintJson = JsonSerializer.Serialize(new
        {
            title = "Updated Blueprint",
            description = "Updated description",
            participants = new[] { new { id = "p1" }, new { id = "p2" } },
            actions = new[] { new { id = 0, title = "Start" } }
        });

        // Act
        var result = await tool.UpdateBlueprintAsync("bp-123", blueprintJson);

        // Assert
        result.Status.Should().Be("Success");
        result.Message.Should().Contain("Updated Blueprint");
        result.Blueprint.Should().NotBeNull();
        result.Blueprint!.Id.Should().Be("bp-123");
        result.Blueprint.Title.Should().Be("Updated Blueprint");
        result.Blueprint.Version.Should().Be(2);

        _availabilityTrackerMock.Verify(a => a.RecordSuccess("Blueprint"), Times.Once);
    }

    [Fact]
    public async Task UpdateBlueprintAsync_BlueprintNotFound_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_update")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var errorResponse = new { error = "Blueprint not found" };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(JsonSerializer.Serialize(errorResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        var blueprintJson = JsonSerializer.Serialize(new
        {
            title = "Test",
            participants = new[] { new { id = "p1" }, new { id = "p2" } },
            actions = new[] { new { id = 0 } }
        });

        // Act
        var result = await tool.UpdateBlueprintAsync("nonexistent", blueprintJson);

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Blueprint not found");
    }

    [Fact]
    public async Task UpdateBlueprintAsync_CorrectEndpointCalled()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_update")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var updateResponse = new { id = "bp-123", title = "Test", version = 1 };

        string? capturedUrl = null;
        HttpMethod? capturedMethod = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                capturedUrl = req.RequestUri?.ToString();
                capturedMethod = req.Method;
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(updateResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        var blueprintJson = JsonSerializer.Serialize(new
        {
            title = "Test",
            participants = new[] { new { id = "p1" }, new { id = "p2" } },
            actions = new[] { new { id = 0 } }
        });

        // Act
        await tool.UpdateBlueprintAsync("bp-123", blueprintJson);

        // Assert
        capturedUrl.Should().Be("http://localhost:5000/api/blueprints/bp-123");
        capturedMethod.Should().Be(HttpMethod.Put);
    }

    [Fact]
    public async Task UpdateBlueprintAsync_ResponseTimeIsRecorded()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_update")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var updateResponse = new { id = "bp-123", title = "Test", version = 1 };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(updateResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        var blueprintJson = JsonSerializer.Serialize(new
        {
            title = "Test",
            participants = new[] { new { id = "p1" }, new { id = "p2" } },
            actions = new[] { new { id = 0 } }
        });

        // Act
        var result = await tool.UpdateBlueprintAsync("bp-123", blueprintJson);

        // Assert
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.CheckedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
