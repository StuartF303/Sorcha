// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Sorcha.McpServer.Infrastructure;
using Sorcha.McpServer.Services;
using Sorcha.McpServer.Tools.Designer;

namespace Sorcha.McpServer.Tests.Tools.Designer;

public class BlueprintGetToolTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<IMcpAuthorizationService> _authServiceMock;
    private readonly Mock<IMcpErrorHandler> _errorHandlerMock;
    private readonly Mock<IServiceAvailabilityTracker> _availabilityTrackerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<BlueprintGetTool>> _loggerMock;

    public BlueprintGetToolTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _authServiceMock = new Mock<IMcpAuthorizationService>();
        _errorHandlerMock = new Mock<IMcpErrorHandler>();
        _availabilityTrackerMock = new Mock<IServiceAvailabilityTracker>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<BlueprintGetTool>>();

        _configurationMock.Setup(c => c["ServiceClients:BlueprintService:Address"])
            .Returns("http://localhost:5000");
    }

    private BlueprintGetTool CreateTool()
    {
        return new BlueprintGetTool(
            _sessionServiceMock.Object,
            _authServiceMock.Object,
            _errorHandlerMock.Object,
            _availabilityTrackerMock.Object,
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetBlueprintAsync_Unauthorized_ReturnsUnauthorizedResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_get")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.GetBlueprintAsync("bp-123");

        // Assert
        result.Status.Should().Be("Unauthorized");
        result.Message.Should().Contain("Access denied");
        result.Blueprint.Should().BeNull();
    }

    [Fact]
    public async Task GetBlueprintAsync_ServiceUnavailable_ReturnsUnavailableResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_get")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.GetBlueprintAsync("bp-123");

        // Assert
        result.Status.Should().Be("Unavailable");
        result.Message.Should().Contain("unavailable");
    }

    [Fact]
    public async Task GetBlueprintAsync_EmptyBlueprintId_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_get")).Returns(true);
        var tool = CreateTool();

        // Act
        var result = await tool.GetBlueprintAsync("");

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("required");
    }

    [Fact]
    public async Task GetBlueprintAsync_BlueprintExists_ReturnsSuccessResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_get")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var blueprintResponse = new
        {
            Id = "bp-123",
            Title = "Test Blueprint",
            Description = "A test blueprint for unit testing",
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-10),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-5),
            Metadata = new Dictionary<string, string> { ["author"] = "test-user" },
            Participants = new[]
            {
                new
                {
                    Id = "participant-1",
                    Name = "Applicant",
                    Organisation = "Test Org",
                    WalletAddress = "0xabc123",
                    DidUri = "did:example:123",
                    UseStealthAddress = false
                },
                new
                {
                    Id = "participant-2",
                    Name = "Approver",
                    Organisation = "Test Org",
                    WalletAddress = "0xdef456",
                    DidUri = (string?)null,
                    UseStealthAddress = true
                }
            },
            Actions = new[]
            {
                new
                {
                    Id = 0,
                    Title = "Submit Application",
                    Description = "Submit the initial application",
                    Sender = "participant-1",
                    Target = "participant-2",
                    IsStartingAction = true,
                    RequiredActionData = new[] { "form-data" },
                    AdditionalRecipients = Array.Empty<string>(),
                    Disclosures = new object[] { new { }, new { } }
                },
                new
                {
                    Id = 1,
                    Title = "Review Application",
                    Description = "Review and approve or reject",
                    Sender = "participant-2",
                    Target = (string?)null,
                    IsStartingAction = false,
                    RequiredActionData = Array.Empty<string>(),
                    AdditionalRecipients = new[] { "participant-1" },
                    Disclosures = new object[] { }
                }
            }
        };

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(blueprintResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.GetBlueprintAsync("bp-123");

        // Assert
        result.Status.Should().Be("Success");
        result.Message.Should().Contain("Test Blueprint");
        result.Blueprint.Should().NotBeNull();
        result.Blueprint!.Id.Should().Be("bp-123");
        result.Blueprint.Title.Should().Be("Test Blueprint");
        result.Blueprint.Description.Should().Be("A test blueprint for unit testing");
        result.Blueprint.Version.Should().Be(1);
        result.Blueprint.Participants.Should().HaveCount(2);
        result.Blueprint.Participants[0].Name.Should().Be("Applicant");
        result.Blueprint.Participants[1].UseStealthAddress.Should().BeTrue();
        result.Blueprint.Actions.Should().HaveCount(2);
        result.Blueprint.Actions[0].Title.Should().Be("Submit Application");
        result.Blueprint.Actions[0].IsStartingAction.Should().BeTrue();
        result.Blueprint.Actions[0].DisclosureCount.Should().Be(2);
        result.Blueprint.Actions[1].AdditionalRecipients.Should().Contain("participant-1");
        result.Blueprint.Metadata.Should().ContainKey("author");

        _availabilityTrackerMock.Verify(a => a.RecordSuccess("Blueprint"), Times.Once);
    }

    [Fact]
    public async Task GetBlueprintAsync_BlueprintNotFound_ReturnsNotFoundResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_get")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.GetBlueprintAsync("nonexistent-bp");

        // Assert
        result.Status.Should().Be("NotFound");
        result.Message.Should().Contain("nonexistent-bp");
        result.Message.Should().Contain("not found");
        result.Blueprint.Should().BeNull();

        // Service is still working, so record success
        _availabilityTrackerMock.Verify(a => a.RecordSuccess("Blueprint"), Times.Once);
    }

    [Fact]
    public async Task GetBlueprintAsync_ServiceReturnsError_ReturnsErrorResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_get")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

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
        var result = await tool.GetBlueprintAsync("bp-123");

        // Assert
        result.Status.Should().Be("NotFound"); // Non-success treated as not found in fetch method
        result.Blueprint.Should().BeNull();
    }

    [Fact]
    public async Task GetBlueprintAsync_InvalidJson_ReturnsNotFoundResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_get")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("invalid json")
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.GetBlueprintAsync("bp-123");

        // Assert
        result.Status.Should().Be("NotFound");
        result.Blueprint.Should().BeNull();
    }

    [Fact]
    public async Task GetBlueprintAsync_CorrectUrlCalled()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_get")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var blueprintResponse = new
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
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(blueprintResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        await tool.GetBlueprintAsync("bp-123");

        // Assert
        capturedUrl.Should().Be("http://localhost:5000/api/blueprints/bp-123");
    }

    [Fact]
    public async Task GetBlueprintAsync_ResponseTimeIsRecorded()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_blueprint_get")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Blueprint")).Returns(true);

        var blueprintResponse = new
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
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(blueprintResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.GetBlueprintAsync("bp-123");

        // Assert
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.CheckedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
