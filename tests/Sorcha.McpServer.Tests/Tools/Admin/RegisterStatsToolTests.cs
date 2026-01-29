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

public class RegisterStatsToolTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<IMcpAuthorizationService> _authServiceMock;
    private readonly Mock<IMcpErrorHandler> _errorHandlerMock;
    private readonly Mock<IServiceAvailabilityTracker> _availabilityTrackerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<RegisterStatsTool>> _loggerMock;

    public RegisterStatsToolTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _authServiceMock = new Mock<IMcpAuthorizationService>();
        _errorHandlerMock = new Mock<IMcpErrorHandler>();
        _availabilityTrackerMock = new Mock<IServiceAvailabilityTracker>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<RegisterStatsTool>>();

        _configurationMock.Setup(c => c["ServiceClients:RegisterService:Address"])
            .Returns("http://localhost:5290");
    }

    private RegisterStatsTool CreateTool()
    {
        return new RegisterStatsTool(
            _sessionServiceMock.Object,
            _authServiceMock.Object,
            _errorHandlerMock.Object,
            _availabilityTrackerMock.Object,
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetRegisterStatsAsync_Unauthorized_ReturnsUnauthorizedResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_register_stats")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.GetRegisterStatsAsync();

        // Assert
        result.Status.Should().Be("Unauthorized");
        result.Message.Should().Contain("Access denied");
        result.OverallStats.Should().BeNull();
    }

    [Fact]
    public async Task GetRegisterStatsAsync_ServiceUnavailable_ReturnsUnavailableResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_register_stats")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Register")).Returns(false);
        var tool = CreateTool();

        // Act
        var result = await tool.GetRegisterStatsAsync();

        // Assert
        result.Status.Should().Be("Unavailable");
        result.Message.Should().Contain("unavailable");
    }

    [Fact]
    public async Task GetRegisterStatsAsync_OverallStats_ReturnsHealthyResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_register_stats")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Register")).Returns(true);

        var countResponse = new { Count = 5 };
        var listResponse = new[]
        {
            new { Id = "reg-1", Name = "Test Register 1", Status = "Active", TenantId = "tenant-1", Height = 100L, CreatedAt = DateTimeOffset.UtcNow.AddDays(-10) },
            new { Id = "reg-2", Name = "Test Register 2", Status = "Active", TenantId = "tenant-1", Height = 50L, CreatedAt = DateTimeOffset.UtcNow.AddDays(-5) }
        };

        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("/stats/count")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(countResponse))
            });

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().EndsWith("/api/registers/")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(listResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.GetRegisterStatsAsync();

        // Assert
        result.Status.Should().Be("Healthy");
        result.Message.Should().Contain("5 registers");
        result.OverallStats.Should().NotBeNull();
        result.OverallStats!.RegisterCount.Should().Be(5);
        result.OverallStats.RecentRegisters.Should().HaveCount(2);
        result.RegisterStats.Should().BeNull(); // No registerId provided

        _availabilityTrackerMock.Verify(a => a.RecordSuccess("Register"), Times.Once);
    }

    [Fact]
    public async Task GetRegisterStatsAsync_WithRegisterId_ReturnsTransactionStats()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_register_stats")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Register")).Returns(true);

        var countResponse = new { Count = 1 };
        var transactionStats = new
        {
            TotalTransactions = 150,
            UniqueWallets = 25,
            UniqueSenders = 10,
            UniqueRecipients = 20,
            TotalPayloads = 300,
            EarliestTransaction = DateTime.UtcNow.AddDays(-30),
            LatestTransaction = DateTime.UtcNow.AddHours(-1)
        };

        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("/stats/count")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(countResponse))
            });

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().EndsWith("/api/registers/")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("/api/query/stats")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(transactionStats))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.GetRegisterStatsAsync("reg-123");

        // Assert
        result.Status.Should().Be("Healthy");
        result.Message.Should().Contain("reg-123");
        result.RegisterStats.Should().NotBeNull();
        result.RegisterStats!.RegisterId.Should().Be("reg-123");
        result.RegisterStats.TotalTransactions.Should().Be(150);
        result.RegisterStats.UniqueWallets.Should().Be(25);
        result.RegisterStats.TotalPayloads.Should().Be(300);
    }

    [Fact]
    public async Task GetRegisterStatsAsync_RegisterNotFound_ReturnsPartialResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_register_stats")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Register")).Returns(true);

        var countResponse = new { Count = 5 };

        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("/stats/count")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(countResponse))
            });

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().EndsWith("/api/registers/")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("/api/query/stats")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.GetRegisterStatsAsync("nonexistent-register");

        // Assert
        result.Status.Should().Be("Partial");
        result.Message.Should().Contain("could not retrieve stats");
        result.OverallStats.Should().NotBeNull();
        result.RegisterStats.Should().BeNull();
    }

    [Fact]
    public async Task GetRegisterStatsAsync_AllEndpointsFail_ReturnsUnknownResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_register_stats")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Register")).Returns(true);

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
        var result = await tool.GetRegisterStatsAsync();

        // Assert
        result.Status.Should().Be("Unknown");
        result.Message.Should().Contain("Unable to retrieve");
    }

    [Fact]
    public async Task GetRegisterStatsAsync_ResponseTimeIsRecorded()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_register_stats")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Register")).Returns(true);

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"count\": 0}")
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.GetRegisterStatsAsync();

        // Assert
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.CheckedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetRegisterStatsAsync_RecentRegistersLimitedTo10()
    {
        // Arrange
        _authServiceMock.Setup(a => a.CanInvokeTool("sorcha_register_stats")).Returns(true);
        _availabilityTrackerMock.Setup(a => a.IsServiceAvailable("Register")).Returns(true);

        var countResponse = new { Count = 15 };
        // Create 15 registers
        var listResponse = Enumerable.Range(1, 15)
            .Select(i => new
            {
                Id = $"reg-{i}",
                Name = $"Test Register {i}",
                Status = "Active",
                TenantId = "tenant-1",
                Height = 100L - i,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-i)
            })
            .ToList();

        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("/stats/count")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(countResponse))
            });

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().EndsWith("/api/registers/")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(listResponse))
            });

        var httpClient = new HttpClient(handler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var tool = CreateTool();

        // Act
        var result = await tool.GetRegisterStatsAsync();

        // Assert
        result.Status.Should().Be("Healthy");
        result.OverallStats!.RegisterCount.Should().Be(15);
        result.OverallStats.RecentRegisters.Should().HaveCount(10); // Limited to 10
    }
}
