// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Sorcha.McpServer.Infrastructure;
using Sorcha.McpServer.Services;
using Sorcha.McpServer.Tools.Participant;

namespace Sorcha.McpServer.Tests.Tools.Participant;

public sealed class WalletInfoToolTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<IMcpAuthorizationService> _authServiceMock;
    private readonly Mock<IMcpErrorHandler> _errorHandlerMock;
    private readonly Mock<IServiceAvailabilityTracker> _availabilityTrackerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<WalletInfoTool>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly WalletInfoTool _tool;

    public WalletInfoToolTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _authServiceMock = new Mock<IMcpAuthorizationService>();
        _errorHandlerMock = new Mock<IMcpErrorHandler>();
        _availabilityTrackerMock = new Mock<IServiceAvailabilityTracker>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<WalletInfoTool>>();

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceClients:WalletService:Address"] = "http://localhost:5001"
            })
            .Build();

        _tool = new WalletInfoTool(
            _sessionServiceMock.Object,
            _authServiceMock.Object,
            _errorHandlerMock.Object,
            _availabilityTrackerMock.Object,
            _httpClientFactoryMock.Object,
            _configuration,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetWalletInfoAsync_WhenUnauthorized_ReturnsUnauthorizedStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_wallet_info")).Returns(false);

        // Act
        var result = await _tool.GetWalletInfoAsync();

        // Assert
        result.Status.Should().Be("Unauthorized");
        result.Message.Should().Contain("sorcha:participant");
    }

    [Fact]
    public async Task GetWalletInfoAsync_WhenServiceUnavailable_ReturnsUnavailableStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_wallet_info")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Wallet")).Returns(false);

        // Act
        var result = await _tool.GetWalletInfoAsync();

        // Assert
        result.Status.Should().Be("Unavailable");
        result.Message.Should().Contain("Wallet service");
    }

    [Fact]
    public async Task GetWalletInfoAsync_WithSuccessfulResponse_ReturnsWalletInfo()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_wallet_info")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Wallet")).Returns(true);

        var response = new
        {
            walletId = "wallet-123",
            primaryAddress = "addr1qx4j7...",
            algorithm = "ED25519",
            publicKey = "ABCDpublickey...",
            createdAt = DateTimeOffset.UtcNow.AddMonths(-1),
            addresses = new[]
            {
                new
                {
                    address = "addr1qx4j7...",
                    derivationPath = "m/44'/1815'/0'/0/0",
                    algorithm = "ED25519",
                    isDefault = true
                },
                new
                {
                    address = "addr1qy5k8...",
                    derivationPath = "m/44'/1815'/0'/0/1",
                    algorithm = "ED25519",
                    isDefault = false
                }
            }
        };

        SetupHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        // Act
        var result = await _tool.GetWalletInfoAsync();

        // Assert
        result.Status.Should().Be("Success");
        result.Wallet.Should().NotBeNull();
        result.Wallet!.WalletId.Should().Be("wallet-123");
        result.Wallet.PrimaryAddress.Should().Be("addr1qx4j7...");
        result.Wallet.Algorithm.Should().Be("ED25519");
        result.Wallet.Addresses.Should().HaveCount(2);
        result.Wallet.Addresses[0].IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task GetWalletInfoAsync_WithMinimalResponse_ReturnsBasicInfo()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_wallet_info")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Wallet")).Returns(true);

        var response = new
        {
            walletId = "wallet-456",
            primaryAddress = "addr1...",
            algorithm = "P256"
        };

        SetupHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        // Act
        var result = await _tool.GetWalletInfoAsync();

        // Assert
        result.Status.Should().Be("Success");
        result.Wallet.Should().NotBeNull();
        result.Wallet!.WalletId.Should().Be("wallet-456");
        result.Wallet.Algorithm.Should().Be("P256");
        result.Wallet.Addresses.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWalletInfoAsync_WithHttpError_ReturnsErrorStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_wallet_info")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Wallet")).Returns(true);

        SetupHttpClient(HttpStatusCode.InternalServerError, "{\"error\":\"Wallet not found\"}");

        // Act
        var result = await _tool.GetWalletInfoAsync();

        // Assert
        result.Status.Should().Be("Error");
    }

    [Fact]
    public async Task GetWalletInfoAsync_WithTimeout_ReturnsTimeoutStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_wallet_info")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Wallet")).Returns(true);

        SetupHttpClientWithException(new TaskCanceledException());

        // Act
        var result = await _tool.GetWalletInfoAsync();

        // Assert
        result.Status.Should().Be("Timeout");
        _availabilityTrackerMock.Verify(x => x.RecordFailure("Wallet"), Times.Once);
    }

    [Fact]
    public async Task GetWalletInfoAsync_WithHttpException_ReturnsErrorStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_wallet_info")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Wallet")).Returns(true);

        SetupHttpClientWithException(new HttpRequestException("Connection refused"));

        // Act
        var result = await _tool.GetWalletInfoAsync();

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Connection refused");
    }

    [Fact]
    public async Task GetWalletInfoAsync_RecordsSuccessOnSuccessfulResponse()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_wallet_info")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Wallet")).Returns(true);

        var response = new
        {
            walletId = "wallet-123",
            primaryAddress = "addr1...",
            algorithm = "ED25519"
        };

        SetupHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        // Act
        await _tool.GetWalletInfoAsync();

        // Assert
        _availabilityTrackerMock.Verify(x => x.RecordSuccess("Wallet"), Times.Once);
    }

    private void SetupHttpClient(HttpStatusCode statusCode, string content)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });

        var client = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
    }

    private void SetupHttpClientWithException(Exception exception)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);

        var client = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
    }
}
