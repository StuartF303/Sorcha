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

public class BlueprintCreateToolTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<IMcpAuthorizationService> _authServiceMock;
    private readonly Mock<IMcpErrorHandler> _errorHandlerMock;
    private readonly Mock<IServiceAvailabilityTracker> _availabilityTrackerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<BlueprintCreateTool>> _loggerMock;

    public BlueprintCreateToolTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _authServiceMock = new Mock<IMcpAuthorizationService>();
        _errorHandlerMock = new Mock<IMcpErrorHandler>();
        _availabilityTrackerMock = new Mock<IServiceAvailabilityTracker>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<BlueprintCreateTool>>();

        _configurationMock.Setup(c => c["ServiceClients:BlueprintService:Address"])
            .Returns("http://localhost:5000");
    }

    private BlueprintCreateTool CreateTool()
    {
        return new BlueprintCreateTool(
            _sessionServiceMock.Object,
            _authServiceMock.Object,
            _errorHandlerMock.Object,
            _availabilityTrackerMock.Object,
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    private static string GetValidBlueprintJson() => JsonSerializer.Serialize(new
    {
        title = "Test Blueprint",
        description = "A test blueprint for testing purposes",
        participants = new[]
        {
            new { id = "p1", name = "Participant 1", walletAddress = "0x123" },
            new { id = "p2", name = "Participant 2", walletAddress = "0x456" }
        },
        actions = new[]
        {
            new { id = 0, title = "Action 1", sender = "p1" }
        }
    });

    [Fact]
    public async Task CreateBlueprintAsync_Unauthorized_ReturnsUnauthorizedResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_create")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.CreateBlueprintAsync(GetValidBlueprintJson());

        // Assert
        result.Status.Should().Be("Unauthorized");
        result.Message.Should().Contain("Access denied");
        result.CreatedBlueprint.Should().BeNull();
    }

    [Fact]
    public async Task CreateBlueprintAsync_ServiceUnavailable_ReturnsUnavailableResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_create")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.CreateBlueprintAsync(GetValidBlueprintJson());

        // Assert
        result.Status.Should().Be("Unavailable");
        result.Message.Should().Contain("unavailable");
    }

    [Fact]
    public async Task CreateBlueprintAsync_EmptyJson_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_create")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.CreateBlueprintAsync("");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("required");
    }

    [Fact]
    public async Task CreateBlueprintAsync_InvalidJson_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_create")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.CreateBlueprintAsync("{ invalid json }");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Invalid JSON");
    }

    [Fact]
    public async Task CreateBlueprintAsync_MissingTitle_ReturnsValidationError()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_create")).Returns(true);
        var tool = CreateTool();

        var json = JsonSerializer.Serialize(new
        {
            description = "Test description here",
            participants = new[]
            {
                new { id = "p1", name = "Participant 1" },
                new { id = "p2", name = "Participant 2" }
            },
            actions = new[]
            {
                new { id = 0, title = "Action 1" }
            }
        });

        // Act
        var result = await tool.CreateBlueprintAsync(json);

        // Assert
        result.Status.Should().Be("ValidationError");
        result.ValidationErrors.Should().Contain(e => e.Contains("title"));
    }

    [Fact]
    public async Task CreateBlueprintAsync_TitleTooShort_ReturnsValidationError()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_create")).Returns(true);
        var tool = CreateTool();

        var json = JsonSerializer.Serialize(new
        {
            title = "AB", // Too short (< 3 chars)
            description = "Test description here",
            participants = new[]
            {
                new { id = "p1", name = "Participant 1" },
                new { id = "p2", name = "Participant 2" }
            },
            actions = new[]
            {
                new { id = 0, title = "Action 1" }
            }
        });

        // Act
        var result = await tool.CreateBlueprintAsync(json);

        // Assert
        result.Status.Should().Be("ValidationError");
        result.ValidationErrors.Should().Contain(e => e.Contains("title") && e.Contains("3 characters"));
    }

    [Fact]
    public async Task CreateBlueprintAsync_InsufficientParticipants_ReturnsValidationError()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_create")).Returns(true);
        var tool = CreateTool();

        var json = JsonSerializer.Serialize(new
        {
            title = "Test Blueprint",
            description = "Test description here",
            participants = new[]
            {
                new { id = "p1", name = "Participant 1" } // Only 1 participant
            },
            actions = new[]
            {
                new { id = 0, title = "Action 1" }
            }
        });

        // Act
        var result = await tool.CreateBlueprintAsync(json);

        // Assert
        result.Status.Should().Be("ValidationError");
        result.ValidationErrors.Should().Contain(e => e.Contains("participants") && e.Contains("at least 2"));
    }

    [Fact]
    public async Task CreateBlueprintAsync_NoActions_ReturnsValidationError()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_create")).Returns(true);
        var tool = CreateTool();

        var json = JsonSerializer.Serialize(new
        {
            title = "Test Blueprint",
            description = "Test description here",
            participants = new[]
            {
                new { id = "p1", name = "Participant 1" },
                new { id = "p2", name = "Participant 2" }
            },
            actions = Array.Empty<object>()
        });

        // Act
        var result = await tool.CreateBlueprintAsync(json);

        // Assert
        result.Status.Should().Be("ValidationError");
        result.ValidationErrors.Should().Contain(e => e.Contains("actions") && e.Contains("at least 1"));
    }

    [Fact]
    public async Task CreateBlueprintAsync_ValidBlueprint_ReturnsSuccessResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_create")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var createdResponse = new
        {
            Id = "bp-new-123",
            Title = "Test Blueprint",
            Description = "A test blueprint for testing purposes",
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            Participants = new[]
            {
                new { id = "p1", name = "Participant 1" },
                new { id = "p2", name = "Participant 2" }
            },
            Actions = new[]
            {
                new { id = 0, title = "Action 1" }
            }
        };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(JsonSerializer.Serialize(createdResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.CreateBlueprintAsync(GetValidBlueprintJson());

        // Assert
        result.Status.Should().Be("Success");
        result.Message.Should().Contain("Test Blueprint");
        result.Message.Should().Contain("bp-new-123");
        result.CreatedBlueprint.Should().NotBeNull();
        result.CreatedBlueprint!.Id.Should().Be("bp-new-123");
        result.CreatedBlueprint.Title.Should().Be("Test Blueprint");
        result.CreatedBlueprint.Version.Should().Be(1);
        result.CreatedBlueprint.ParticipantCount.Should().Be(2);
        result.CreatedBlueprint.ActionCount.Should().Be(1);

        _availabilityTrackerMock.Verify(a => a.RecordSuccess("Blueprint"), Times.Once);
    }

    [Fact]
    public async Task CreateBlueprintAsync_ServiceReturnsError_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_create")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Validation failed")
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.CreateBlueprintAsync(GetValidBlueprintJson());

        // Assert
        result.Status.Should().Be("Error");
        result.CreatedBlueprint.Should().BeNull();
    }

    [Fact]
    public async Task CreateBlueprintAsync_CorrectUrlCalled()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_create")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var createdResponse = new
        {
            Id = "bp-123",
            Title = "Test",
            Description = "Test",
            Version = 1,
            Participants = Array.Empty<object>(),
            Actions = Array.Empty<object>()
        };

        string? capturedUrl = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedUrl = req.RequestUri?.ToString())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(JsonSerializer.Serialize(createdResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        await tool.CreateBlueprintAsync(GetValidBlueprintJson());

        // Assert
        capturedUrl.Should().Be("http://localhost:5000/api/blueprints/");
    }

    [Fact]
    public async Task CreateBlueprintAsync_ResponseTimeIsRecorded()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_create")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var createdResponse = new
        {
            Id = "bp-123",
            Title = "Test",
            Description = "Test",
            Version = 1,
            Participants = Array.Empty<object>(),
            Actions = Array.Empty<object>()
        };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(JsonSerializer.Serialize(createdResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.CreateBlueprintAsync(GetValidBlueprintJson());

        // Assert
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.CheckedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
